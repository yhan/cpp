#pragma once
#include "market_data.h"
#include <deque>
#include <numeric>
#include <cmath>
#include <algorithm>

// ============================================================================
// CRTP base for trading strategies
// Zero-overhead polymorphism — all calls resolved at compile time
// ============================================================================

template <typename Derived>
class StrategyBase {
public:
    // Public interface — calls into derived at compile time, fully inlined
    void on_tick(const Tick& tick) {
        derived().on_tick_impl(tick);
    }

    Signal generate_signal(const Tick& tick) {
        return derived().generate_signal_impl(tick);
    }

    const char* name() const {
        return derived().name_impl();
    }

    void reset() {
        derived().reset_impl();
    }

private:
    Derived&       derived()       { return static_cast<Derived&>(*this); }
    const Derived& derived() const { return static_cast<const Derived&>(*this); }
};

// ============================================================================
// Strategy 1: Momentum — trades breakouts from a rolling window
// ============================================================================

class MomentumStrategy : public StrategyBase<MomentumStrategy> {
    friend class StrategyBase<MomentumStrategy>;

    std::deque<Price> price_history_;
    size_t            lookback_;
    double            threshold_;    // min % move to trigger
    double            last_signal_ = 0.0;

    void on_tick_impl(const Tick& tick) {
        price_history_.push_back(tick.mid());
        if (price_history_.size() > lookback_) {
            price_history_.pop_front();
        }
    }

    Signal generate_signal_impl(const Tick& tick) {
        if (price_history_.size() < lookback_) return Signal::none();

        Price oldest  = price_history_.front();
        Price current = tick.mid();
        double ret    = (current - oldest) / oldest;

        if (std::abs(ret) < threshold_) return Signal::none();

        double strength = std::min(std::abs(ret) / (threshold_ * 3.0), 1.0);
        last_signal_ = ret;
        return { ret > 0 ? 1.0 : -1.0, strength, true };
    }

    const char* name_impl() const { return "Momentum"; }

    void reset_impl() {
        price_history_.clear();
        last_signal_ = 0.0;
    }

public:
    MomentumStrategy(size_t lookback = 20, double threshold = 0.002)
        : lookback_(lookback), threshold_(threshold) {}
};

// ============================================================================
// Strategy 2: Mean Reversion — fades moves away from rolling average
// ============================================================================

class MeanReversionStrategy : public StrategyBase<MeanReversionStrategy> {
    friend class StrategyBase<MeanReversionStrategy>;

    std::deque<Price> price_history_;
    size_t            lookback_;
    double            entry_zscore_;
    double            running_sum_   = 0.0;
    double            running_sumsq_ = 0.0;

    void on_tick_impl(const Tick& tick) {
        Price p = tick.mid();
        running_sum_ += p;
        running_sumsq_ += p * p;
        price_history_.push_back(p);
        if (price_history_.size() > lookback_) {
            Price old = price_history_.front();
            running_sum_ -= old;
            running_sumsq_ -= old * old;
            price_history_.pop_front();
        }
    }

    Signal generate_signal_impl(const Tick& tick) {
        if (price_history_.size() < lookback_) return Signal::none();

        double n    = static_cast<double>(price_history_.size());
        double mean = running_sum_ / n;
        double var  = (running_sumsq_ / n) - (mean * mean);
        if (var <= 0.0) return Signal::none();

        double stddev  = std::sqrt(var);
        double zscore  = (tick.mid() - mean) / stddev;

        if (std::abs(zscore) < entry_zscore_) return Signal::none();

        // Fade the move: sell when price is high, buy when low
        double direction = (zscore > 0) ? -1.0 : 1.0;
        double strength  = std::min(std::abs(zscore) / (entry_zscore_ * 2.0), 1.0);
        return { direction, strength, true };
    }

    const char* name_impl() const { return "MeanReversion"; }

    void reset_impl() {
        price_history_.clear();
        running_sum_ = 0.0;
        running_sumsq_ = 0.0;
    }

public:
    MeanReversionStrategy(size_t lookback = 50, double entry_zscore = 1.5)
        : lookback_(lookback), entry_zscore_(entry_zscore) {}
};

// ============================================================================
// Strategy 3: Market Making — quotes both sides, earns the spread
// ============================================================================

class MarketMakingStrategy : public StrategyBase<MarketMakingStrategy> {
    friend class StrategyBase<MarketMakingStrategy>;

    std::deque<Price> price_history_;
    size_t            vol_window_;
    double            spread_mult_;
    double            skew_factor_;
    double            volatility_ = 0.0;

    void on_tick_impl(const Tick& tick) {
        price_history_.push_back(tick.mid());
        if (price_history_.size() > vol_window_) {
            price_history_.pop_front();
        }
        // Compute short-term realized volatility
        if (price_history_.size() >= 2) {
            double sum_sq = 0.0;
            for (size_t i = 1; i < price_history_.size(); ++i) {
                double ret = (price_history_[i] - price_history_[i-1]) / price_history_[i-1];
                sum_sq += ret * ret;
            }
            volatility_ = std::sqrt(sum_sq / (price_history_.size() - 1));
        }
    }

    Signal generate_signal_impl(const Tick& /*tick*/) {
        if (price_history_.size() < vol_window_ / 2) return Signal::none();

        // Market makers always want to quote — signal strength = inverse of vol
        // In high vol, reduce size; in low vol, be aggressive
        double strength = std::max(0.1, 1.0 - volatility_ * 100.0);

        // Slight skew based on recent price trend
        double recent_ret = 0.0;
        if (price_history_.size() >= 5) {
            size_t n = price_history_.size();
            recent_ret = (price_history_[n-1] - price_history_[n-5]) / price_history_[n-5];
        }

        // Skew: lean away from recent direction (inventory management)
        double direction = -recent_ret * skew_factor_;

        return { direction, strength, true };
    }

    const char* name_impl() const { return "MarketMaking"; }

    void reset_impl() {
        price_history_.clear();
        volatility_ = 0.0;
    }

public:
    MarketMakingStrategy(size_t vol_window = 30, double spread_mult = 2.0, double skew = 50.0)
        : vol_window_(vol_window), spread_mult_(spread_mult), skew_factor_(skew) {}

    double current_volatility() const { return volatility_; }
};
