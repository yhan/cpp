#pragma once
#include "market_data.h"
#include "strategy.h"
#include "execution.h"
#include <iostream>
#include <iomanip>
#include <string>

// ============================================================================
// CRTP Trading Engine — wires Strategy + Execution + Risk together
// All dispatch is compile-time. No virtual calls in the hot path.
// ============================================================================

template <typename Strategy, typename Execution, typename RiskManager>
class TradingEngine {
    Strategy     strategy_;
    Execution    execution_;
    RiskManager  risk_;
    Position     position_;
    Quantity     base_order_size_;
    uint64_t     tick_count_ = 0;
    uint64_t     order_count_ = 0;
    uint64_t     fill_count_ = 0;

public:
    TradingEngine(Strategy strategy, Execution execution, RiskManager risk,
                  Quantity base_size = 10)
        : strategy_(std::move(strategy))
        , execution_(std::move(execution))
        , risk_(std::move(risk))
        , base_order_size_(base_size)
    {}

    // ---- Hot path: called every tick ----
    void on_tick(const Tick& tick) {
        ++tick_count_;

        // 1. Feed tick to strategy (updates internal state)
        strategy_.on_tick(tick);

        // 2. Process any pending fills from the exchange
        execution_.process_fills(tick);
        for (const auto& fill : execution_.recent_fills()) {
            position_.apply_fill(fill);
            ++fill_count_;
        }

        // 3. Generate signal
        Signal sig = strategy_.generate_signal(tick);

        // 4. If signal is active, attempt to trade
        if (sig.active) {
            Side side = (sig.direction > 0) ? Side::Buy : Side::Sell;
            Quantity qty = static_cast<Quantity>(base_order_size_ * sig.strength);
            if (qty <= 0) qty = 1;

            // Risk check (compile-time dispatched)
            qty = risk_.adjust_quantity(qty, position_);
            if (qty > 0 && risk_.check_order(side, tick.mid(), qty, position_)) {
                Price price = (side == Side::Buy) ? tick.ask : tick.bid;
                execution_.submit_order(side, price, qty);
                ++order_count_;
            }
        }

        // 5. Mark to market
        position_.mark_to_market(tick.mid());
    }

    // ---- Reporting ----
    void print_status(const Tick& tick) const {
        std::cout << "[" << strategy_.name() << " | "
                  << execution_.name() << " | " << risk_.name() << "]\n";
        std::cout << "  Ticks: " << tick_count_
                  << " | Orders: " << order_count_
                  << " | Fills: " << fill_count_
                  << " | Pending: " << execution_.pending_count() << "\n";
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
