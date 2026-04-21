using System;
using System.Collections.Generic;

namespace TradingSystem
{
    /**-------------------------------------------------------------------
     * CHOOSE BETWEEN TREND FOLLOWING (MOMENTUM) AND MEAN REVERSION
     * -------------------------------------------------------------------
     * Short timeframe  →  Mean Reversion dominates
                    (prices bounce off bid/ask, noise reverts)

Long timeframe   →  Momentum dominates
                    (real economic trends persist)

┌──────────────────────────────────────────────────┐
│  microsec   sec   min   hour   day   week  month │
│  ◄── Mean Reversion ──►◄──── Momentum ────►      │
│                         ↑                         │
│                    crossover zone                 │
│                    (both weak here)               │
└──────────────────────────────────────────────────┘

 // TREND FOLLOW :price goes up => BUY;   price going down => SELL
    MEAN REVERT  : price goes up, SELL; price going down, BUY
     */
    // Virtual base — same as cpp_basic
    public interface IStrategy
    {
        void OnTick(in Tick tick);
        Signal GenerateSignal(in Tick tick);
        string Name { get; }
        void Reset();
    }

    public class MomentumStrategy : IStrategy
    {
        private readonly LinkedList<double> _priceHistory = new();
        private readonly int _lookback;
        private readonly double _threshold;
        private double _lastSignal;

        public string Name => "Momentum";

        public MomentumStrategy(int lookback = 20, double threshold = 0.002)
        {
            _lookback = lookback;
            _threshold = threshold;
        }

        public void OnTick(in Tick tick)
        {
            _priceHistory.AddLast(tick.Mid);
            if (_priceHistory.Count > _lookback)
                _priceHistory.RemoveFirst();
        }

        public Signal GenerateSignal(in Tick tick)
        {
            if (_priceHistory.Count < _lookback) return Signal.None();
            double oldest = _priceHistory.First!.Value;
            double current = tick.Mid;
            double ret = (current - oldest) / oldest; // long term       :price goes up => BUY;   price going down => SELL
                                                      // Compare to mean reversion: price goes up, SELL; price going down, BUY
            if (Math.Abs(ret) < _threshold) return Signal.None();
            double strength = Math.Min(Math.Abs(ret) / (_threshold * 3.0), 1.0);
            _lastSignal = ret;
            return new Signal { Direction = ret > 0 ? 1.0 : -1.0, Strength = strength, Active = true };
        }

        public void Reset() { _priceHistory.Clear(); _lastSignal = 0; }
    }

    public class MeanReversionStrategy : IStrategy
    {
        private readonly LinkedList<double> _priceHistory = new();
        private readonly int _lookback;
        private readonly double _entryZscore;
        private double _runningSum;
        private double _runningSumSq;

        public string Name => "MeanReversion";

        public MeanReversionStrategy(int lookback = 50, double entryZscore = 1.5)
        {
            _lookback = lookback;
            _entryZscore = entryZscore;
        }

        public void OnTick(in Tick tick)
        {
            double p = tick.Mid;
            _runningSum += p;
            _runningSumSq += p * p;
            _priceHistory.AddLast(p);
            if (_priceHistory.Count > _lookback)
            {
                double old = _priceHistory.First!.Value;
                _runningSum -= old;
                _runningSumSq -= old * old;
                _priceHistory.RemoveFirst();
            }
        }

        public Signal GenerateSignal(in Tick tick)
        {
            if (_priceHistory.Count < _lookback) return Signal.None();
            double n = _priceHistory.Count;
            double mean = _runningSum / n;
            double var = (_runningSumSq / n) - (mean * mean);
            if (var <= 0) return Signal.None();
            double stddev = Math.Sqrt(var);
            double zscore = (tick.Mid - mean) / stddev;
            if (Math.Abs(zscore) < _entryZscore) return Signal.None();
            double direction = (zscore > 0) ? -1.0 : 1.0; // price goes up, SELL; price going down, BUY
            double strength = Math.Min(Math.Abs(zscore) / (_entryZscore * 2.0), 1.0);
            return new Signal { Direction = direction, Strength = strength, Active = true };
        }

        public void Reset() { _priceHistory.Clear(); _runningSum = 0; _runningSumSq = 0; }
    }

    public class MarketMakingStrategy : IStrategy
    {
        private readonly List<double> _priceHistory = new();
        private readonly int _volWindow;
        private readonly double _spreadMult;
        private readonly double _skewFactor;
        private double _volatility;

        public string Name => "MarketMaking";

        public MarketMakingStrategy(int volWindow = 30, double spreadMult = 2.0, double skew = 50.0)
        {
            _volWindow = volWindow;
            _spreadMult = spreadMult;
            _skewFactor = skew;
        }

        public void OnTick(in Tick tick)
        {
            _priceHistory.Add(tick.Mid);
            if (_priceHistory.Count > _volWindow)
                _priceHistory.RemoveAt(0);
            if (_priceHistory.Count >= 2)
            {
                double sumSq = 0;
                for (int i = 1; i < _priceHistory.Count; i++)
                {
                    double ret = (_priceHistory[i] - _priceHistory[i - 1]) / _priceHistory[i - 1];
                    sumSq += ret * ret;
                }
                _volatility = Math.Sqrt(sumSq / (_priceHistory.Count - 1)); // σ = std dev of returns ==>  realized volatility (RV)
            }
        }
        
        /**********************************************
         * Low volatility:   Calm market. Grandma is trading. 
         *         → Quote tight, go big, collect the spread. Easy money.

         *  High volatility:  Goldman's algo is hunting. Prices jumping.
         *         → Quote wide, go small, or you'll get eaten alive.
         ************************************************/
        public Signal GenerateSignal(in Tick tick)
        {
            if (_priceHistory.Count < _volWindow / 2) return Signal.None();
            double strength = Math.Max(0.1, 1.0 - _volatility * 100.0);  // inverse volatility
            double recentRet = 0;
            if (_priceHistory.Count >= 5)
            {
                int n = _priceHistory.Count;
                recentRet = (_priceHistory[n - 1] - _priceHistory[n - 5]) / _priceHistory[n - 5];
            }
            double direction = -recentRet * _skewFactor;// direction est utilisé pour placer des ordres, si positive, BUY; sinon SELL
                                                        // si le prix baisse selon history, then BUY ...
            return new Signal { Direction = direction, Strength = strength, Active = true };
        }

        public void Reset() { _priceHistory.Clear(); _volatility = 0; }
    }
}
