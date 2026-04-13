#pragma once
#include "market_data.h"
#include <vector>
#include <iostream>
#include <iomanip>

// ============================================================================
// Virtual base for execution
// ============================================================================

class IExecution {
public:
    virtual ~IExecution() = default;
    virtual OrderId submit_order(Side side, Price price, Quantity qty) = 0;
    virtual bool cancel_order(OrderId id) = 0;
    virtual void process_fills(const Tick& tick) = 0;
    virtual std::vector<Fill>& recent_fills() = 0;
    virtual const char* name() const = 0;
    virtual size_t pending_count() const = 0;
};

class SimulatedExchange : public IExecution {
    struct PendingOrder {
        Order    order;
        uint64_t submit_time_ns;
    };

    std::vector<PendingOrder> pending_orders_;
    std::vector<Fill>         recent_fills_;
    OrderId                   next_id_ = 1;
    uint64_t                  latency_ns_;
    double                    fill_probability_;
    double                    slippage_bps_;

public:
    SimulatedExchange(uint64_t latency_ns = 1000, double slippage_bps = 0.5)
        : latency_ns_(latency_ns), fill_probability_(1.0), slippage_bps_(slippage_bps) {}

    OrderId submit_order(Side side, Price price, Quantity qty) override {
        OrderId id = next_id_++;
        Order o { id, side, price, qty, 0, OrderStatus::New };
        pending_orders_.push_back({ o, 0 });
        return id;
    }

    bool cancel_order(OrderId id) override {
        for (auto it = pending_orders_.begin(); it != pending_orders_.end(); ++it) {
            if (it->order.id == id && !it->order.is_done()) {
                it->order.status = OrderStatus::Cancelled;
                pending_orders_.erase(it);
                return true;
            }
        }
        return false;
    }

    void process_fills(const Tick& tick) override {
        recent_fills_.clear();
        auto it = pending_orders_.begin();
        while (it != pending_orders_.end()) {
            auto& po = *it;
            bool filled = false;
            if (po.order.side == Side::Buy) {
                if (tick.ask <= po.order.price) {
                    double slip = tick.ask * slippage_bps_ * 0.0001;
                    Price fill_price = tick.ask + slip;
                    Quantity fill_qty = std::min(po.order.remaining(), tick.ask_size);
                    if (fill_qty > 0) {
                        po.order.filled_qty += fill_qty;
                        po.order.status = (po.order.remaining() == 0)
                            ? OrderStatus::Filled : OrderStatus::PartialFill;
                        recent_fills_.push_back({ po.order.id, fill_price, fill_qty, Side::Buy });
                        filled = (po.order.remaining() == 0);
                    }
                }
            } else {
                if (tick.bid >= po.order.price) {
                    double slip = tick.bid * slippage_bps_ * 0.0001;
                    Price fill_price = tick.bid - slip;
                    Quantity fill_qty = std::min(po.order.remaining(), tick.bid_size);
                    if (fill_qty > 0) {
                        po.order.filled_qty += fill_qty;
                        po.order.status = (po.order.remaining() == 0)
                            ? OrderStatus::Filled : OrderStatus::PartialFill;
                        recent_fills_.push_back({ po.order.id, fill_price, fill_qty, Side::Sell });
                        filled = (po.order.remaining() == 0);
                    }
                }
            }
            if (filled) { it = pending_orders_.erase(it); } else { ++it; }
        }
    }

    std::vector<Fill>& recent_fills() override { return recent_fills_; }
    const char* name() const override { return "SimulatedExchange"; }
    size_t pending_count() const override { return pending_orders_.size(); }
};

// ============================================================================
// Virtual base for risk management
// ============================================================================

class IRiskManager {
public:
    virtual ~IRiskManager() = default;
    virtual bool check_order(Side side, Price price, Quantity qty, const Position& pos) = 0;
    virtual Quantity adjust_quantity(Quantity desired, const Position& pos) = 0;
    virtual const char* name() const = 0;
};

class BasicRiskManager : public IRiskManager {
    Quantity max_position_;
    Quantity max_order_size_;
    double   max_loss_;

public:
    BasicRiskManager(Quantity max_pos = 100, Quantity max_order = 20, double max_loss = 5000.0)
        : max_position_(max_pos), max_order_size_(max_order), max_loss_(max_loss) {}

    bool check_order(Side side, Price /*price*/, Quantity qty, const Position& pos) override {
        Quantity projected = pos.net_qty + (side == Side::Buy ? qty : -qty);
        if (std::abs(projected) > max_position_) return false;
        if (qty > max_order_size_) return false;
        if (pos.total_pnl() < -max_loss_) return false;
        return true;
    }

    Quantity adjust_quantity(Quantity desired, const Position& pos) override {
        Quantity room = max_position_ - std::abs(pos.net_qty);
        return std::min(desired, std::max(Quantity{0}, room));
    }

    const char* name() const override { return "BasicRisk"; }
};
