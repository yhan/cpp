using System;
using System.Collections.Generic;

namespace TradingSystem
{
    public interface IExecution
    {
        ulong SubmitOrder(Side side, double price, long qty);
        bool CancelOrder(ulong id);
        void ProcessFills(in Tick tick);
        List<Fill> RecentFills { get; }
        string Name { get; }
        int PendingCount { get; }
    }

    public class SimulatedExchange : IExecution
    {
        private struct PendingOrder
        {
            public Order Order;
            public ulong SubmitTimeNs;
        }

        private readonly List<PendingOrder> _pendingOrders = new();
        private readonly List<Fill> _recentFills = new();
        private ulong _nextId = 1;
        private readonly ulong _latencyNs;
        private readonly double _slippageBps;

        public List<Fill> RecentFills => _recentFills;
        public string Name => "SimulatedExchange";
        public int PendingCount => _pendingOrders.Count;

        public SimulatedExchange(ulong latencyNs = 1000, double slippageBps = 0.5)
        {
            _latencyNs = latencyNs;
            _slippageBps = slippageBps;
        }

        public ulong SubmitOrder(Side side, double price, long qty)
        {
            ulong id = _nextId++;
            var o = new Order { Id = id, Side = side, Price = price, Qty = qty };
            _pendingOrders.Add(new PendingOrder { Order = o, SubmitTimeNs = 0 });
            return id;
        }

        public bool CancelOrder(ulong id)
        {
            for (int i = 0; i < _pendingOrders.Count; i++)
            {
                if (_pendingOrders[i].Order.Id == id && !_pendingOrders[i].Order.IsDone)
                {
                    _pendingOrders[i].Order.Status = OrderStatus.Cancelled;
                    _pendingOrders.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        public void ProcessFills(in Tick tick)
        {
            _recentFills.Clear();
            int i = 0;
            while (i < _pendingOrders.Count)
            {
                var po = _pendingOrders[i];
                bool filled = false;

                if (po.Order.Side == Side.Buy)
                {
                    if (tick.Ask <= po.Order.Price)
                    {
                        double slip = tick.Ask * _slippageBps * 0.0001;
                        double fillPrice = tick.Ask + slip;
                        long fillQty = Math.Min(po.Order.Remaining, tick.AskSize);
                        if (fillQty > 0)
                        {
                            po.Order.FilledQty += fillQty;
                            po.Order.Status = (po.Order.Remaining == 0) ? OrderStatus.Filled : OrderStatus.PartialFill;
                            _recentFills.Add(new Fill { OrderId = po.Order.Id, FillPrice = fillPrice, FillQty = fillQty, Side = Side.Buy });
                            filled = (po.Order.Remaining == 0);
                            _pendingOrders[i] = po;
                        }
                    }
                }
                else
                {
                    if (tick.Bid >= po.Order.Price)
                    {
                        double slip = tick.Bid * _slippageBps * 0.0001;
                        double fillPrice = tick.Bid - slip;
                        long fillQty = Math.Min(po.Order.Remaining, tick.BidSize);
                        if (fillQty > 0)
                        {
                            po.Order.FilledQty += fillQty;
                            po.Order.Status = (po.Order.Remaining == 0) ? OrderStatus.Filled : OrderStatus.PartialFill;
                            _recentFills.Add(new Fill { OrderId = po.Order.Id, FillPrice = fillPrice, FillQty = fillQty, Side = Side.Sell });
                            filled = (po.Order.Remaining == 0);
                            _pendingOrders[i] = po;
                        }
                    }
                }

                if (filled) _pendingOrders.RemoveAt(i);
                else i++;
            }
        }
    }

    public interface IRiskManager
    {
        bool CheckOrder(Side side, double price, long qty, Position pos);
        long AdjustQuantity(long desired, Position pos);
        string Name { get; }
    }

    public class BasicRiskManager : IRiskManager
    {
        private readonly long _maxPosition;
        private readonly long _maxOrderSize;
        private readonly double _maxLoss;

        public string Name => "BasicRisk";

        public BasicRiskManager(long maxPos = 100, long maxOrder = 20, double maxLoss = 5000.0)
        {
            _maxPosition = maxPos;
            _maxOrderSize = maxOrder;
            _maxLoss = maxLoss;
        }

        public bool CheckOrder(Side side, double price, long qty, Position pos)
        {
            long projected = pos.NetQty + (side == Side.Buy ? qty : -qty);
            if (Math.Abs(projected) > _maxPosition) return false;
            if (qty > _maxOrderSize) return false;
            if (pos.TotalPnl < -_maxLoss) return false;
            return true;
        }

        public long AdjustQuantity(long desired, Position pos)
        {
            long room = _maxPosition - Math.Abs(pos.NetQty);
            return Math.Min(desired, Math.Max(0, room));
        }
    }
}
