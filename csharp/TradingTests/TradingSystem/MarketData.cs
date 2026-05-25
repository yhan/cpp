using System;

namespace TradingSystem
{
    public enum Side : byte { Buy, Sell }
    public enum OrderStatus : byte { New, PartialFill, Filled, Cancelled, Rejected }

    public struct Tick
    {
        public ulong TimestampNs;
        public double Bid;
        public double Ask;
        public double Last;
        public long BidSize;
        public long AskSize;
        public long LastSize;

        public double Mid => (Bid + Ask) * 0.5;
        public double Spread => Ask - Bid;
    }

    public class Order
    {
        public ulong Id;
        public Side Side;
        public double Price;
        public long Qty;
        public long FilledQty;
        public OrderStatus Status = OrderStatus.New;

        public long Remaining => Qty - FilledQty;
        public bool IsDone => Status == OrderStatus.Filled ||
                              Status == OrderStatus.Cancelled ||
                              Status == OrderStatus.Rejected;
    }

    public struct Fill
    {
        public ulong OrderId;
        public double FillPrice;
        public long FillQty;
        public Side Side;
    }

    public struct Signal
    {
        public double Direction;
        public double Strength;
        public bool Active;

        public static Signal None() => new Signal { Direction = 0, Strength = 0, Active = false };
    }

    public class Position
    {
        public long NetQty;
        public double AvgPrice;
        public double RealizedPnl;
        public double UnrealizedPnl;

        public double TotalPnl => RealizedPnl + UnrealizedPnl;

        public void ApplyFill(in Fill f)
        {
            long signedQty = (f.Side == Side.Buy) ? f.FillQty : -f.FillQty;
            if ((NetQty >= 0 && signedQty > 0) || (NetQty <= 0 && signedQty < 0))
            {
                double totalCost = AvgPrice * Math.Abs(NetQty) + f.FillPrice * f.FillQty;
                NetQty += signedQty;
                AvgPrice = (NetQty != 0) ? totalCost / Math.Abs(NetQty) : 0.0;
            }
            else
            {
                long closeQty = Math.Min(Math.Abs(NetQty), f.FillQty);
                double pnlPerUnit = (f.Side == Side.Sell)
                    ? (f.FillPrice - AvgPrice)
                    : (AvgPrice - f.FillPrice);
                RealizedPnl += pnlPerUnit * closeQty;
                NetQty += signedQty;
                if (NetQty == 0) AvgPrice = 0.0;
                if ((signedQty > 0 && NetQty > 0 && f.FillQty > closeQty) ||
                    (signedQty < 0 && NetQty < 0 && f.FillQty > closeQty))
                {
                    AvgPrice = f.FillPrice;
                }
            }
        }

        public void MarkToMarket(double currentMid)
        {
            UnrealizedPnl = (NetQty != 0) ? (currentMid - AvgPrice) * NetQty : 0.0;
        }

        public void Print()
        {
            Console.WriteLine($"  Position: {NetQty} @ {AvgPrice:F2} | Realized: {RealizedPnl:F2} | Unrealized: {UnrealizedPnl:F2} | Total PnL: {TotalPnl:F2}");
        }
    }
}
