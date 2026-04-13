# C++ Techniques for Avoiding Virtual Dispatch

## Why Avoid Virtual?

Virtual calls go through the **vtable** — an indirect pointer lookup + branch. This costs:

- **Indirect branch**: CPU can't predict the target → pipeline stall (~5-25ns)
- **Inlining blocked**: compiler can't see through the pointer → no optimization
- **Cache miss**: vtable pointer may not be in L1 → memory latency
- **Cache line pollution**: vtable + object pointer chasing wastes cache space

In hot loops processing millions of events per second, these costs compound.

---

## 1. CRTP (Curiously Recurring Template Pattern)

The most common replacement. The base class is templated on the derived class.

```cpp
template <typename Derived>
class StrategyBase {
public:
    void on_tick(const Tick& tick) {
        static_cast<Derived*>(this)->on_tick_impl(tick);  // resolved at compile time
    }
};

class Momentum : public StrategyBase<Momentum> {
    void on_tick_impl(const Tick& tick) { /* fully inlined */ }
};
```

**How it works:** `static_cast<Derived*>(this)` tells the compiler the exact type at compile time. No vtable needed. The call is inlined directly.

**Trade-off:** You can't store different strategies in the same container (`vector<StrategyBase*>` is impossible because `StrategyBase` is a template, not a single type).

**Used in:** This project's `cpp_advanced/` folder. Trading firms, game engines.

---

## 2. `std::variant` + `std::visit`

For a **closed set** of types known at compile time.

```cpp
using Shape = std::variant<Circle, Square, Triangle>;

void draw(Shape& s) {
    std::visit([](auto& shape) { shape.draw(); }, s);
}
```

**How it works:** `variant` stores a type index (0, 1, 2...) and the object inline (no heap). `visit` compiles into a switch/jump table — branch-predictor friendly.

**Trade-off:** All types must be known at compile time. Adding a new type requires changing the variant definition.

**Good for:** State machines, AST nodes, message types.

---

## 3. `if constexpr` + Templates

Eliminates branches entirely at compile time.

```cpp
template <typename Policy>
void process(Policy& p) {
    if constexpr (std::is_same_v<Policy, FastPath>) {
        p.fast();       // only this branch exists in the binary
    } else {
        p.slow();       // this branch is completely removed
    }
}
```

**How it works:** The compiler evaluates the condition at compile time and discards the dead branch. Zero runtime cost.

**Trade-off:** Only works when the type is a template parameter (known at compile time).

---

## 4. Policy-Based Design

Replace runtime polymorphism with template parameters. Each "policy" is a type that provides specific behavior.

```cpp
template <typename Allocator, typename Logger, typename Hasher>
class Server {
    Allocator alloc_;
    Logger    log_;
    // alloc_.allocate() and log_.write() are direct calls, fully inlined
};

// Concrete server with specific policies — all resolved at compile time
using ProdServer = Server<PoolAllocator, SyslogLogger, XXHash>;
```

**How it works:** Same as CRTP — the compiler knows every type, so every call is direct.

**Trade-off:** Combinatorial explosion if you have many policies. Each combination is a separate type.

**Used in:** STL itself (`std::map<K, V, Compare, Allocator>`), Bloomberg's BDE library.

---

## 5. Function Pointers (Manual Dispatch)

Replace the entire vtable with a single function pointer.

```cpp
struct Entity {
    void (*update)(Entity*);   // one pointer instead of a vtable pointer
    float x, y;
};

void update_player(Entity* e) { e->x += 1; }
void update_enemy(Entity* e)  { e->x -= 1; }
```

**How it works:** Direct function pointer call. You control the layout — no compiler-generated vtable.

**Trade-off:** Manual bookkeeping. No type safety. Easy to get wrong.

**Good for:** ECS systems, plugin architectures, C interop.

---

## 6. Data-Oriented Design (DOD)

Avoid polymorphism entirely. Separate data by type and process in bulk.

```cpp
// Instead of: vector<Shape*> with virtual draw()
// Do:
std::vector<Circle> circles;    // process all circles
std::vector<Square> squares;    // then all squares

for (auto& c : circles) draw_circle(c);   // tight loop, no branching
for (auto& s : squares) draw_square(s);   // SIMD-friendly
```

**How it works:** No dispatch at all. Each type is in its own contiguous array. The CPU prefetcher loves this — sequential memory access, no pointer chasing.

**Trade-off:** You lose the ability to mix types in one collection. Code structure looks very different from OOP.

**Used in:** Game engines (Unity DOTS, Unreal Mass), database engines, physics engines.

---

## 7. `final` Keyword (Devirtualization Hint)

If you must use `virtual`, mark classes/methods `final` so the compiler can devirtualize.

```cpp
class Derived final : public Base {
    void update() override { /* compiler knows this is the only implementation */ }
};
```

**How it works:** `final` tells the compiler "no further overrides exist." If the compiler can prove the concrete type, it replaces the virtual call with a direct call and inlines it.

**Trade-off:** Only works when the compiler can deduce the concrete type. Through a `Base*` pointer with multiple derived types, `final` doesn't help.

**Used in:** Everywhere as a low-effort optimization. Always mark classes `final` if you don't intend them to be subclassed.

---

## 8. Type Erasure with SBO (Small Buffer Optimization)

Hand-rolled virtual dispatch with inline storage to avoid heap allocation.

```cpp
class Function {
    alignas(64) char buffer_[64];          // small buffer, no heap
    void (*invoke_)(void*);                // function pointer, not vtable

    template <typename F>
    Function(F f) {
        static_assert(sizeof(F) <= 64);
        new (buffer_) F(std::move(f));     // placement new into buffer
        invoke_ = [](void* p) { (*static_cast<F*>(p))(); };
    }

    void operator()() { invoke_(buffer_); }
};
```

**How it works:** Like `std::function` but you control the buffer size. Small callables live inline (no `new`). Dispatch is a single function pointer call.

**Trade-off:** Still has one indirection (function pointer). But avoids heap allocation and vtable overhead.

**Used in:** Custom `std::function` replacements in latency-sensitive code.

---

## Comparison Table

| Technique | Dispatch Cost | Inlining | Heap Alloc | Heterogeneous Container |
|---|---|---|---|---|
| `virtual` | ~5-25ns | Blocked | Often (via `new`) | Yes (`Base*`) |
| CRTP | 0 | Full | No | No |
| `variant` + `visit` | ~1-3ns (switch) | Partial | No (inline) | Yes (same variant) |
| `if constexpr` | 0 | Full | No | No |
| Policy-based | 0 | Full | No | No |
| Function pointer | ~2-5ns | Blocked | No | Yes |
| DOD | 0 | Full | No | No (separate arrays) |
| `final` | 0 if devirtualized | Full if devirtualized | Depends | Yes |
| Type erasure + SBO | ~2-5ns | Blocked | No (if fits buffer) | Yes |

---

## When to Use What

```
Need runtime polymorphism with unknown types?
  └─► virtual (with final) — it's fine for non-hot paths

Know all types at compile time?
  ├─► Few types, need one container    → std::variant
  ├─► One type per template instance   → CRTP or Policy-based
  └─► Processing bulk data             → DOD (separate arrays)

Hot loop, millions of calls per second?
  └─► CRTP, DOD, or Policy-based — zero overhead only
```

---

## Project Structure

This repo demonstrates the difference:

| Folder | Technique | Dispatch |
|---|---|---|
| `cpp_advanced/` | CRTP + templates | Compile-time (0 cost) |
| `cpp_basic/` | `virtual` + interfaces | Runtime (vtable) |
| `csharp/` | C# interfaces | Runtime (JIT may devirtualize) |
