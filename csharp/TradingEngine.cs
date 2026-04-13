using System;

namespace TradingSystem
{
    public class TradingEngine
    {
        private readonly IStrategy _strategy;
        private readonly IExecution _execution;
        private readonly IRiskManager _risk;
        private readonly Position _position = new();
        private readonly long _baseOrderSize;
        private ulong _tickCount;
        private ulong _orderCount;
        private ulong _fillCount;

        public TradingEngine(IStrategy strategy, IExecution execution, IRiskManager risk, long baseSize = 10)
        {
            _strategy = strategy;
            _execution = execution;
            _risk = risk;
            _baseOrderSize = baseSize;
        }

        public void OnTick(in Tick tick)
        {
            _tickCount++;
            _strategy.OnTick(tick);
            _execution.ProcessFills(tick);
            foreach (var fill in _execution.RecentFills)
            {
                _position.ApplyFill(fill);
                _fillCount++;
            }

            var sig = _strategy.GenerateSignal(tick);
            if (sig.Active)
            {
                var side = (sig.Direction > 0) ? Side.Buy : Side.Sell;
                long qty = (long)(_baseOrderSize * sig.Strength);
                if (qty <= 0) qty = 1;
                qty = _risk.AdjustQuantity(qty, _position);
                if (qty > 0 && _risk.CheckOrder(side, tick.Mid, qty, _position))
                {
                    double price = (side == Side.Buy) ? tick.Ask : tick.Bid;
                    _execution.SubmitOrder(side, price, qty);
                    _orderCount++;
                }
            }
            _position.MarkToMarket(tick.Mid);
        }

        public void PrintStatus(in Tick tick)
        {
            Console.WriteLine($"[{_strategy.Name} | {_execution.Name} | {_risk.Name}]");
            Console.WriteLine($"  Ticks: {_tickCount} | Orders: {_orderCount} | Fills: {_fillCount} | Pending: {_execution.PendingCount}");
            Console.WriteLine($"  Last: bid={tick.Bid:F2} ask={tick.Ask:F2} mid={tick.Mid:F2} spread={tick.Spread:F4}");
            _position.Print();
        }

        public Position Position => _position;
    }
}
