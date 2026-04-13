using System;
using System.Diagnostics;

namespace TradingSystem
{
    class Program
    {
        static void RunSimulation(TradingEngine engine, MarketSimulator sim,
            int totalTicks, int reportInterval)
        {
            Tick lastTick = default;
            for (int i = 0; i < totalTicks; i++)
            {
                var tick = sim.NextTick();
                engine.OnTick(tick);
                lastTick = tick;
                if ((i + 1) % reportInterval == 0)
                {
                    Console.WriteLine($"--- Tick {i + 1} ---");
                    engine.PrintStatus(lastTick);
                    Console.WriteLine();
                }
            }
        }

        static double Benchmark(TradingEngine engine, MarketSimulator sim, int numTicks)
        {
            for (int i = 0; i < 1000; i++)
                engine.OnTick(sim.NextTick());

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < numTicks; i++)
                engine.OnTick(sim.NextTick());
            sw.Stop();

            return (double)sw.ElapsedTicks / numTicks * (1_000_000_000.0 / Stopwatch.Frequency);
        }

        static void Main()
        {
            Console.WriteLine("============================================================");
            Console.WriteLine("   C# Trading System — Interface Dispatch");
            Console.WriteLine("============================================================\n");

            const int simTicks = 5000;
            const int reportEvery = 1000;
            const int benchTicks = 1_000_000;

            // ---- Momentum ----
            {
                Console.WriteLine("========== MOMENTUM STRATEGY ==========\n");
                var engine = new TradingEngine(
                    new MomentumStrategy(20, 0.002),
                    new SimulatedExchange(1000, 0.5),
                    new BasicRiskManager(100, 20, 5000.0), 10);
                var sim = new MarketSimulator(100.0, 0.0015, 0.00001, 2.0, 1_000_000, 42);
                RunSimulation(engine, sim, simTicks, reportEvery);
                Console.WriteLine("===== FINAL =====");
                var ft = sim.NextTick(); engine.OnTick(ft); engine.PrintStatus(ft);
                Console.WriteLine("\n");
            }

            // ---- Mean Reversion ----
            {
                Console.WriteLine("========== MEAN REVERSION STRATEGY ==========\n");
                var engine = new TradingEngine(
                    new MeanReversionStrategy(50, 1.5),
                    new SimulatedExchange(500, 0.3),
                    new BasicRiskManager(80, 15, 3000.0), 8);
                var sim = new MarketSimulator(100.0, 0.002, 0.0, 3.0, 1_000_000, 123);
                RunSimulation(engine, sim, simTicks, reportEvery);
                Console.WriteLine("===== FINAL =====");
                var ft = sim.NextTick(); engine.OnTick(ft); engine.PrintStatus(ft);
                Console.WriteLine("\n");
            }

            // ---- Market Making ----
            {
                Console.WriteLine("========== MARKET MAKING STRATEGY ==========\n");
                var engine = new TradingEngine(
                    new MarketMakingStrategy(30, 2.0, 50.0),
                    new SimulatedExchange(200, 0.2),
                    new BasicRiskManager(50, 10, 2000.0), 5);
                var sim = new MarketSimulator(100.0, 0.001, 0.0, 1.5, 1_000_000, 777);
                RunSimulation(engine, sim, simTicks, reportEvery);
                Console.WriteLine("===== FINAL =====");
                var ft = sim.NextTick(); engine.OnTick(ft); engine.PrintStatus(ft);
                Console.WriteLine("\n");
            }

            // ---- Benchmark ----
            Console.WriteLine("============================================================");
            Console.WriteLine($"   BENCHMARK: {benchTicks} ticks per engine");
            Console.WriteLine("============================================================\n");

            {
                var engine = new TradingEngine(new MomentumStrategy(20, 0.002),
                    new SimulatedExchange(1000, 0.5), new BasicRiskManager(100, 20, 5000.0), 10);
                var sim = new MarketSimulator(100.0, 0.0015, 0.0, 2.0, 1_000_000, 42);
                double ns = Benchmark(engine, sim, benchTicks);
                Console.WriteLine($"  Momentum:      {ns:F1} ns/tick");
            }
            {
                var engine = new TradingEngine(new MeanReversionStrategy(50, 1.5),
                    new SimulatedExchange(500, 0.3), new BasicRiskManager(80, 15, 3000.0), 8);
                var sim = new MarketSimulator(100.0, 0.002, 0.0, 3.0, 1_000_000, 123);
                double ns = Benchmark(engine, sim, benchTicks);
                Console.WriteLine($"  MeanReversion:  {ns:F1} ns/tick");
            }
            {
                var engine = new TradingEngine(new MarketMakingStrategy(30, 2.0, 50.0),
                    new SimulatedExchange(200, 0.2), new BasicRiskManager(50, 10, 2000.0), 5);
                var sim = new MarketSimulator(100.0, 0.001, 0.0, 1.5, 1_000_000, 777);
                double ns = Benchmark(engine, sim, benchTicks);
                Console.WriteLine($"  MarketMaking:   {ns:F1} ns/tick");
            }

            Console.WriteLine("\n  (C# interface dispatch — compare with C++ virtual and CRTP versions)\n");
        }
    }
}
