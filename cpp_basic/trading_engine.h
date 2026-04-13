#pragma once
#include "market_data.h"
#include "strategy.h"
#include "execution.h"
#include <iostream>
#include <iomanip>

// ============================================================================
// Virtual-dispatch Trading Engine — uses interface pointers
// Every call to strategy/execution/risk goes through the vtable
// ============================================================================

class TradingEngine {
    IStrategy*    strategy_;
    IExecution*   execution_;
    IRiskManager* risk_;
    Position      position_;
    Quantity      base_order_size_;
    uint64_t      tick_count_ = 0;
    uint64_t      order_count_ = 0;
    uint64_t      fill_count_ = 0;

public:
    TradingEngine(IStrategy* strategy, IExecution* execution, IRiskManager* risk,
                  Quantity base_size = 10)
        : strategy_(strategy), execution_(execution), risk_(risk),
          base_order_size_(base_size) {}

    void on_tick(const Tick& tick) {
        ++tick_count_;
        strategy_->on_tick(tick);           // virtual call
        execution_->process_fills(tick);    // virtual call
        for (const auto& fill : execution_->recent_fills()) {
            position_.apply_fill(fill);
            ++fill_count_;
        }
        Signal sig = strategy_->generate_signal(tick);  // virtual call
        if (sig.active) {
            Side side = (sig.direction > 0) ? Side::Buy : Side::Sell;
            Quantity qty = static_cast<Quantity>(base_order_size_ * sig.strength);
            if (qty <= 0) qty = 1;
            qty = risk_->adjust_quantity(qty, position_);           // virtual call
            if (qty > 0 && risk_->check_order(side, tick.mid(), qty, position_)) {  // virtual call
                Price price = (side == Side::Buy) ? tick.ask : tick.bid;
                execution_->submit_order(side, price, qty);         // virtual call
                ++order_count_;
            }
        }
        position_.mark_to_market(tick.mid());
    }

    void print_status(const Tick& tick) const {
        std::cout << "[" << strategy_->name() << " | "
                  << execution_->name() << " | " << risk_->name() << "]\n";
        std::cout << "  Ticks: " << tick_count_
                  << " | Orders: " << order_count_
                  << " | Fills: " << fill_count_
                  << " | Pending: " << execution_->pending_count() << "\n";
        std::cout << "  Last: bid=" << std::fixed << std::setprecision(2) << tick.bid
                  << " ask=" << tick.ask
                  << " mid=" << tick.mid()
                  << " spread=" << std::setprecision(4) << tick.spread() << "\n";
        position_.print();
    }

    const Position& position() const { return position_; }
    uint64_t tick_count() const { return tick_count_; }
    uint64_t fill_count() const { return fill_count_; }
};
