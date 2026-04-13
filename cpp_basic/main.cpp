// ============================================================================
// Virtual-dispatch Trading System — traditional OOP with vtable
//
// Same functionality as cpp_advanced, but uses virtual base classes.
// Every strategy/execution/risk call goes through an indirect branch.
//
// Build:  g++ -std=c++17 -O2 -o trading.exe main.cpp
// ============================================================================

#include "trading_engine.h"
#include "market_simulator.h"
#include <iostream>
#include <iomanip>
#include <chrono>

void run_simulation(TradingEngine& engine, MarketSimulator& sim,
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

double benchmark(TradingEngine& engine, MarketSimulator& sim, size_t num_ticks) {
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
    std::cout << "   Virtual Dispatch Trading System — Traditional OOP\n";
    std::cout << "============================================================\n\n";

    constexpr size_t SIM_TICKS    = 5000;
    constexpr size_t REPORT_EVERY = 1000;
    constexpr size_t BENCH_TICKS  = 1'000'000;

    // ---- Engine 1: Momentum ----
    {
        std::cout << "========== MOMENTUM STRATEGY ==========\n\n";
        MomentumStrategy  strat(20, 0.002);
        SimulatedExchange exec(1000, 0.5);
        BasicRiskManager  risk(100, 20, 5000.0);
        TradingEngine engine(&strat, &exec, &risk, 10);
        MarketSimulator sim(100.0, 0.0015, 0.00001, 2.0, 1'000'000, 42);
        run_simulation(engine, sim, SIM_TICKS, REPORT_EVERY);
        std::cout << "===== FINAL =====\n";
        Tick final_tick = sim.next_tick();
        engine.on_tick(final_tick);
        engine.print_status(final_tick);
        std::cout << "\n\n";
    }

    // ---- Engine 2: Mean Reversion ----
    {
        std::cout << "========== MEAN REVERSION STRATEGY ==========\n\n";
        MeanReversionStrategy strat(50, 1.5);
        SimulatedExchange     exec(500, 0.3);
        BasicRiskManager      risk(80, 15, 3000.0);
        TradingEngine engine(&strat, &exec, &risk, 8);
        MarketSimulator sim(100.0, 0.002, 0.0, 3.0, 1'000'000, 123);
        run_simulation(engine, sim, SIM_TICKS, REPORT_EVERY);
        std::cout << "===== FINAL =====\n";
        Tick final_tick = sim.next_tick();
        engine.on_tick(final_tick);
        engine.print_status(final_tick);
        std::cout << "\n\n";
    }

    // ---- Engine 3: Market Making ----
    {
        std::cout << "========== MARKET MAKING STRATEGY ==========\n\n";
        MarketMakingStrategy strat(30, 2.0, 50.0);
        SimulatedExchange    exec(200, 0.2);
        BasicRiskManager     risk(50, 10, 2000.0);
        TradingEngine engine(&strat, &exec, &risk, 5);
        MarketSimulator sim(100.0, 0.001, 0.0, 1.5, 1'000'000, 777);
        run_simulation(engine, sim, SIM_TICKS, REPORT_EVERY);
        std::cout << "===== FINAL =====\n";
        Tick final_tick = sim.next_tick();
        engine.on_tick(final_tick);
        engine.print_status(final_tick);
        std::cout << "\n\n";
    }

    // ---- Benchmark ----
    std::cout << "============================================================\n";
    std::cout << "   BENCHMARK: " << BENCH_TICKS << " ticks per engine\n";
    std::cout << "============================================================\n\n";

    {
        MomentumStrategy  strat(20, 0.002);
        SimulatedExchange exec(1000, 0.5);
        BasicRiskManager  risk(100, 20, 5000.0);
        TradingEngine engine(&strat, &exec, &risk, 10);
        MarketSimulator sim(100.0, 0.0015, 0.0, 2.0, 1'000'000, 42);
        double ns = benchmark(engine, sim, BENCH_TICKS);
        std::cout << "  Momentum:      " << std::fixed << std::setprecision(1) << ns << " ns/tick\n";
    }
    {
        MeanReversionStrategy strat(50, 1.5);
        SimulatedExchange     exec(500, 0.3);
        BasicRiskManager      risk(80, 15, 3000.0);
        TradingEngine engine(&strat, &exec, &risk, 8);
        MarketSimulator sim(100.0, 0.002, 0.0, 3.0, 1'000'000, 123);
        double ns = benchmark(engine, sim, BENCH_TICKS);
        std::cout << "  MeanReversion:  " << std::fixed << std::setprecision(1) << ns << " ns/tick\n";
    }
    {
        MarketMakingStrategy strat(30, 2.0, 50.0);
        SimulatedExchange    exec(200, 0.2);
        BasicRiskManager     risk(50, 10, 2000.0);
        TradingEngine engine(&strat, &exec, &risk, 5);
        MarketSimulator sim(100.0, 0.001, 0.0, 1.5, 1'000'000, 777);
        double ns = benchmark(engine, sim, BENCH_TICKS);
        std::cout << "  MarketMaking:   " << std::fixed << std::setprecision(1) << ns << " ns/tick\n";
    }

    std::cout << "\n  (Lower is better. Virtual dispatch adds overhead per tick.)\n";
    std::cout << "  (Compare with cpp_advanced CRTP version for the difference.)\n\n";

    return 0;
}
