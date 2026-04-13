// ============================================================================
// CRTP Trading System — Zero virtual dispatch in the hot path
//
// Demonstrates high-performance C++ patterns:
//   - CRTP for static polymorphism (Strategy, Execution, Risk)
//   - All method calls resolved and inlined at compile time
//   - No heap allocation per tick (only during initialization)
//   - Cache-friendly plain structs for market data
//
// Build:  g++ -std=c++17 -O2 -o trading main.cpp
//         cl /std:c++17 /O2 /EHsc main.cpp /Fe:trading.exe
//
//         verbose:
//            g++ -std=c++17 -O2 -Wall -Wextra -v -o trading.exe main.cpp
// ============================================================================

#include "trading_engine.h"
#include "market_simulator.h"
#include <iostream>
#include <iomanip>
#include <chrono>
#include <vector>

// ----------------------------------------------------------------------------
// Helper: run a single engine through N ticks and print results
// ----------------------------------------------------------------------------
template <typename Engine>
void run_simulation(Engine& engine, MarketSimulator& sim,
                    size_t total_ticks, size_t report_interval)
{
    Tick last_tick{};
    for (size_t i = 0; i < total_ticks; ++i) {
        Tick tick = sim.next_tick();
        engine.on_tick(tick);
        last_tick = tick;

        if ((i + 1) % report_interval == 0) {
            std::cout << "--- Tick " << (i + 1) << " ---\n";
            engine.print_status(last_tick);
            std::cout << "\n";
        }
    }
}

// ----------------------------------------------------------------------------
// Benchmark: measure raw tick processing throughput
// ----------------------------------------------------------------------------
template <typename Engine>
double benchmark(Engine& engine, MarketSimulator& sim, size_t num_ticks) {
    // Warm up
    for (size_t i = 0; i < 1000; ++i) {
        engine.on_tick(sim.next_tick());
    }

    auto start = std::chrono::high_resolution_clock::now();
    for (size_t i = 0; i < num_ticks; ++i) {
        engine.on_tick(sim.next_tick());
    }
    auto end = std::chrono::high_resolution_clock::now();

    double elapsed_ns = std::chrono::duration_cast<std::chrono::nanoseconds>(end - start).count();
    return elapsed_ns / num_ticks;
}

int main() {
    std::cout << "============================================================\n";
    std::cout << "   CRTP Trading System — Zero Virtual Dispatch Demo\n";
    std::cout << "============================================================\n\n";

    constexpr size_t SIM_TICKS    = 5000;
    constexpr size_t REPORT_EVERY = 1000;
    constexpr size_t BENCH_TICKS  = 1'000'000;

    // ---- Engine 1: Momentum Strategy ----
    {
        std::cout << "========== MOMENTUM STRATEGY ==========\n\n";

        // All types known at compile time — no vtable, everything inlined
        auto engine = TradingEngine(
            MomentumStrategy(20, 0.002),
            SimulatedExchange(1000, 0.5),
            BasicRiskManager(100, 20, 5000.0),
            10  // base order size
        );

        MarketSimulator sim(100.0, 0.0015, 0.00001, 2.0, 1'000'000, 42);
        run_simulation(engine, sim, SIM_TICKS, REPORT_EVERY);

        std::cout << "===== FINAL =====\n";
        Tick final_tick = sim.next_tick();
        engine.on_tick(final_tick);
        engine.print_status(final_tick);
        std::cout << "\n\n";
    }

    // ---- Engine 2: Mean Reversion Strategy ----
    {
        std::cout << "========== MEAN REVERSION STRATEGY ==========\n\n";

        auto engine = TradingEngine(
            MeanReversionStrategy(50, 1.5),
            SimulatedExchange(500, 0.3),
            BasicRiskManager(80, 15, 3000.0),
            8
        );

        MarketSimulator sim(100.0, 0.002, 0.0, 3.0, 1'000'000, 123);
        run_simulation(engine, sim, SIM_TICKS, REPORT_EVERY);

        std::cout << "===== FINAL =====\n";
        Tick final_tick = sim.next_tick();
        engine.on_tick(final_tick);
        engine.print_status(final_tick);
        std::cout << "\n\n";
    }

    // ---- Engine 3: Market Making Strategy ----
    {
        std::cout << "========== MARKET MAKING STRATEGY ==========\n\n";

        auto engine = TradingEngine(
            MarketMakingStrategy(30, 2.0, 50.0),
            SimulatedExchange(200, 0.2),
            BasicRiskManager(50, 10, 2000.0),
            5
        );

        MarketSimulator sim(100.0, 0.001, 0.0, 1.5, 1'000'000, 777);
        run_simulation(engine, sim, SIM_TICKS, REPORT_EVERY);

        std::cout << "===== FINAL =====\n";
        Tick final_tick = sim.next_tick();
        engine.on_tick(final_tick);
        engine.print_status(final_tick);
        std::cout << "\n\n";
    }

    // ---- Benchmark all three ----
    std::cout << "============================================================\n";
    std::cout << "   BENCHMARK: " << BENCH_TICKS << " ticks per engine\n";
    std::cout << "============================================================\n\n";

    {
        auto engine = TradingEngine(
            MomentumStrategy(20, 0.002),
            SimulatedExchange(1000, 0.5),
            BasicRiskManager(100, 20, 5000.0), 10
        );
        MarketSimulator sim(100.0, 0.0015, 0.0, 2.0, 1'000'000, 42);
        double ns = benchmark(engine, sim, BENCH_TICKS);
        std::cout << "  Momentum:      " << std::fixed << std::setprecision(1)
                  << ns << " ns/tick\n";
    }
    {
        auto engine = TradingEngine(
            MeanReversionStrategy(50, 1.5),
            SimulatedExchange(500, 0.3),
            BasicRiskManager(80, 15, 3000.0), 8
        );
        MarketSimulator sim(100.0, 0.002, 0.0, 3.0, 1'000'000, 123);
        double ns = benchmark(engine, sim, BENCH_TICKS);
        std::cout << "  MeanReversion:  " << std::fixed << std::setprecision(1)
                  << ns << " ns/tick\n";
    }
    {
        auto engine = TradingEngine(
            MarketMakingStrategy(30, 2.0, 50.0),
            SimulatedExchange(200, 0.2),
            BasicRiskManager(50, 10, 2000.0), 5
        );
        MarketSimulator sim(100.0, 0.001, 0.0, 1.5, 1'000'000, 777);
        double ns = benchmark(engine, sim, BENCH_TICKS);
        std::cout << "  MarketMaking:   " << std::fixed << std::setprecision(1)
                  << ns << " ns/tick\n";
    }

    std::cout << "\n  (Lower is better. No virtual dispatch overhead in the hot path.)\n";
    std::cout << "  (Compare: a single virtual call ~5-25ns due to icache miss + branch mispredict)\n\n";

    return 0;
}
