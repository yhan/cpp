#pragma once
#include "market_data.h"
#include <vector>
#include <functional>
#include <iostream>
#include <iomanip>

// ============================================================================
// CRTP base for order execution / routing
// Derived classes implement how orders are sent and matched
// ============================================================================

template <typename Derived>
class ExecutionBase {
public:
    OrderId submit_order(Side side, Price price, Quantity qty) {
        return derived().submit_order_impl(side, price, qty);
    }

    bool cancel_order(OrderId id) {
        return derived().cancel_order_impl(id);
    }

    void process_fills(const Tick& tick) {
        derived().process_fills_impl(tick);
    }

    std::vector<Fill>& recent_fills() {
        return derived().recent_fills_ref();
    }

    const char* name() const {
        return derived().name_impl();
    }

private:
    Derived&       derived()       { return static_cast<Derived&>(*this); }
    const Derived& derived() const { return static_cast<const Derived&>(*this); }
};

// ============================================================================
// Simulated exchange — matches orders against the book with latency model
// ============================================================================

class SimulatedExchange : public ExecutionBase<SimulatedExchange> {
    friend class ExecutionBase<SimulatedExchange>;

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

    OrderId submit_order_impl(Side side, Price price, Quantity qty) {
        OrderId id = next_id_++;
        Order o { id, side, price, qty, 0, OrderStatus::New };
        pending_orders_.push_back({ o, 0 });  // time set on next tick
        return id;
    }

    bool cancel_order_impl(OrderId id) {
        for (auto it = pending_orders_.begin(); it != pending_orders_.end(); ++it) {
            if (it->order.id == id && !it->order.is_done()) {
                it->order.status = OrderStatus::Cancelled;
                pending_orders_.erase(it);
                return true;
            }
        }
        return false;
    }

    void process_fills_impl(const Tick& tick) {
        recent_fills_.clear();

        auto it = pending_orders_.begin();
        while (it != pending_orders_.end()) {
            auto& po = *it;
            bool filled = false;

            if (po.order.side == Side::Buy) {
                // Buy fills if ask <= order price
                if (tick.ask <= po.order.price) {
                    double slip = tick.ask * slippage_bps_ * 0.0001;
                    Price fill_price = tick.ask + slip;
                    Quantity fill_qty = std::min(po.order.remaining(), tick.ask_size);
                    if (fill_qty > 0) {
                        po.order.filled_qty += fill_qty;
                        po.order.status = (po.order.remaining() == 0)
                            ? OrderStatus::Filled : OrderStatus::PartialFill;
                        recent_fills_.push_back({
                            po.order.id, fill_price, fill_qty, Side::Buy
                        });
                        filled = (po.order.remaining() == 0);
                    }
                }
            } else {
                // Sell fills if bid >= order price
                if (tick.bid >= po.order.price) {
                    double slip = tick.bid * slippage_bps_ * 0.0001;
                    Price fill_price = tick.bid - slip;
                    Quantity fill_qty = std::min(po.order.remaining(), tick.bid_size);
                    if (fill_qty > 0) {
                        po.order.filled_qty += fill_qty;
                        po.order.status = (po.order.remaining() == 0)
                            ? OrderStatus::Filled : OrderStatus::PartialFill;
                        recent_fills_.push_back({
                            po.order.id, fill_price, fill_qty, Side::Sell
                        });
                        filled = (po.order.remaining() == 0);
                    }
                }
            }

            if (filled) {
                it = pending_orders_.erase(it);
            } else {
                ++it;
            }
        }
    }

    std::vector<Fill>& recent_fills_ref() { return recent_fills_; }

    const char* name_impl() const { return "SimulatedExchange"; }

public:
    SimulatedExchange(uint64_t latency_ns = 1000, double slippage_bps = 0.5)
        : latency_ns_(latency_ns), fill_probability_(1.0), slippage_bps_(slippage_bps) {}

    size_t pending_count() const { return pending_orders_.size(); }
};

// ============================================================================
// CRTP base for risk management — pre-trade checks
// ============================================================================

template <typename Derived>
class RiskManagerBase {
public:
    bool check_order(Side side, Price price, Quantity qty, const Position& pos) {
        return derived().check_order_impl(side, price, qty, pos);
    }

    Quantity adjust_quantity(Quantity desired, const Position& pos) {
        return derived().adjust_quantity_impl(desired, pos);
    }

    const char* name() const { return derived().name_impl(); }

private:
    Derived&       derived()       { return static_cast<Derived&>(*this); }
    const Derived& derived() const { return static_cast<const Derived&>(*this); }
};

class BasicRiskManager : public RiskManagerBase<BasicRiskManager> {
    friend class RiskManagerBase<BasicRiskManager>;

    Quantity max_position_;
    Quantity max_order_size_;
    double   max_loss_;

    bool check_order_impl(Side side, Price /*price*/, Quantity qty, const Position& pos) {
        // Check position limits
        Quantity projected = pos.net_qty + (side == Side::Buy ? qty : -qty);
        if (std::abs(projected) > max_position_) {
            return false;
        }
        // Check order size
        if (qty > max_order_size_) {
            return false;
        }
        // Check max loss
        if (pos.total_pnl() < -max_loss_) {
            return false;
        }
        return true;
    }

    Quantity adjust_quantity_impl(Quantity desired, const Position& pos) {
        Quantity room = max_position_ - std::abs(pos.net_qty);
        return std::min(desired, std::max(Quantity{0}, room));
    }

    const char* name_impl() const { return "BasicRisk"; }

public:
    BasicRiskManager(Quantity max_pos = 100, Quantity max_order = 20, double max_loss = 5000.0)
        : max_position_(max_pos), max_order_size_(max_order), max_loss_(max_loss) {}
};
