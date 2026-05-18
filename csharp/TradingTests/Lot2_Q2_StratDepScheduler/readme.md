# Question 2 — Strategy Dependency Scheduler

You run `N` strategies on a single worker. Execution is **strictly sequential, no preemption**. You must determine the
execution order and count how many strategies miss their deadline.

## Input format

First line: integer `N` — number of strategies.

Next `N` lines, one per strategy `i` (1-indexed, in ascending `i` order):

```
i c_i d_i p_i k dep_1 dep_2 ... dep_k
```

- `c_i` — CPU cost in milliseconds (positive integer).
- `d_i` — absolute deadline in milliseconds (positive integer). Strategy `i` must **finish** at or before `d_i`.
- `p_i` — priority (integer, **lower = more important**).
- `k` — number of dependencies (? 0).
- `dep_1 ... dep_k` — strategy IDs that must finish before `i` can start. Omitted entirely if `k = 0`.

All tokens on a line are whitespace-separated.

## Scheduling rule

Time starts at `t = 0`. At each step:

1. Compute the **ready set**: strategies whose dependencies have all finished and which haven't yet been scheduled.
2. From the ready set, pick the strategy with:
    - smallest `p_i`; tie-break by
    - smallest `d_i`; tie-break by
    - smallest `i`.
3. Run it to completion. `t` advances by `c_i`.
4. Mark `i` finished. Any strategy that had `i` as its last unmet dep joins the ready set.
5. Repeat until all strategies are scheduled, or the ready set is empty with strategies remaining (? cycle).

`c_i` does **not** influence the pick — only `(p_i, d_i, i)` does. Costs only drive the clock and therefore which
deadlines get missed.

A strategy **misses** its deadline iff its finish time > `d_i`.

## Cycle handling

If the dependency graph contains a cycle, the schedule is undefined. Output the lex-smallest sorted list of strategy IDs
that lie on **any** cycle (i.e., all nodes belonging to a non-trivial SCC, or self-loops). Do not output `ORDER` or
`MISSED` in that case.

## Output

If no cycle:

```
ORDER i1 i2 ... iN
MISSED count
```

where `i1 i2 ... iN` is the execution order (space-separated) and `count` is the number of strategies whose finish time
exceeded their deadline.

If a cycle exists:

```
CYCLE j1 j2 ... jk
```

where `j1 < j2 < ... < jk` are all strategy IDs on any cycle, ascending.

## Constraints

- `1 ? N ? 2 × 10?`
- Total dependency edges `? 5 × 10?`
- `1 ? c_i, d_i ? 10?`
- `p_i` fits in 32-bit signed integer
- Dependencies reference valid strategy IDs in `[1, N]`. No duplicate edges. Self-loops count as a cycle.

---

## Example 1 — clean run, one missed deadline

**Input:**

```
5
1 10 100 2 0
2 20 50 1 0
3 5 40 3 1 1
4 15 80 1 1 2
5 8 90 2 2 2 3
```

**Trace:**

| t   | ready (sorted by p,d,i) | pick | finishes at | deadline | status |
|-----|--------------------------|------|-------------|----------|--------|
| 0   | {2(p1,d50), 1(p2,d100)}  | 2    | 20          | 50       | ok     |
| 20  | {4(p1,d80), 1(p2,d100)}  | 4    | 35          | 80       | ok     |
| 35  | {1(p2,d100)}             | 1    | 45          | 100      | ok     |
| 45  | {3(p3,d40)}              | 3    | 50          | 40       | **miss** |
| 50  | {5(p2,d90)}              | 5    | 58          | 90       | ok     |

**Output:**

```
ORDER 2 4 1 3 5
MISSED 1
```

## Example 2 — cycle

**Input:**

```
4
1 10 100 1 1 3
2 5 50 2 0
3 8 80 1 1 1
4 6 60 3 1 2
```

Strategies 1 and 3 depend on each other. Strategies 2 and 4 are clean but the cycle aborts the schedule.

**Output:**

```
CYCLE 1 3
```

## Example 3 — all ready at t=0

**Input:**

```
3
1 5 20 3 0
2 5 20 1 0
3 5 20 1 0
```

`2` and `3` tie on `(p, d)`; tie-break on `i`.

**Output:**

```
ORDER 2 3 1
MISSED 0
```

Finish times: 5, 10, 15. All ? 20.