using System;

namespace TradingSystem
{
    public class MarketSimulator
    {
        private readonly Random _rng;
        private double _price;
        private double _volatility;
        private readonly double _baseVol;
        private readonly double _volMeanRevert;
        private readonly double _volOfVol;
        private readonly double _drift;
        private readonly double _spreadBps;
        private ulong _timestamp;
        private readonly ulong _tickIntervalNs;

        public MarketSimulator(double initialPrice = 100.0, double volatility = 0.0015,
            double drift = 0.0, double spreadBps = 2.0,
            ulong tickIntervalNs = 1_000_000, int seed = 42)
        {
            _rng = new Random(seed);
            _price = initialPrice;
            _volatility = volatility;
            _baseVol = volatility;
            _volMeanRevert = 0.05;
            _volOfVol = 0.3;
            _drift = drift;
            _spreadBps = spreadBps;
            _tickIntervalNs = tickIntervalNs;
        }

        private double NextGaussian()
        {
            double u1 = 1.0 - _rng.NextDouble();
            double u2 = 1.0 - _rng.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        }

        public Tick NextTick()
        {
            double volShock = NextGaussian();
            _volatility += _volMeanRevert * (_baseVol - _volatility) + _volOfVol * _volatility * volShock;
            _volatility = Math.Max(0.0001, _volatility);

            double priceShock = NextGaussian();
            double ret = _drift + _volatility * priceShock;
            _price *= (1.0 + ret);
            _price = Math.Max(0.01, _price);

            double halfSpread = _price * _spreadBps * 0.0001 * 0.5;
            _timestamp += _tickIntervalNs;

            return new Tick
            {
                TimestampNs = _timestamp,
                Bid = _price - halfSpread,
                Ask = _price + halfSpread,
                Last = _price + NextGaussian() * halfSpread * 0.5,
                BidSize = _rng.Next(10, 501),
                AskSize = _rng.Next(10, 501),
                LastSize = _rng.Next(1, 51)
            };
        }
    }
}
