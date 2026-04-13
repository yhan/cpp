#pragma once
#include "market_data.h"
#include <random>
#include <cmath>

class MarketSimulator {
    std::mt19937                     rng_;
    std::normal_distribution<double> norm_{0.0, 1.0};
    Price    price_;
    double   volatility_;
    double   base_vol_;
    double   vol_mean_revert_;
    double   vol_of_vol_;
    double   drift_;
    double   spread_bps_;
    uint64_t timestamp_ = 0;
    uint64_t tick_interval_ns_;

public:
    MarketSimulator(Price initial_price = 100.0, double volatility = 0.0015,
                    double drift = 0.0, double spread_bps = 2.0,
                    uint64_t tick_interval_ns = 1'000'000, uint32_t seed = 42)
        : rng_(seed), price_(initial_price), volatility_(volatility), base_vol_(volatility),
          vol_mean_revert_(0.05), vol_of_vol_(0.3), drift_(drift),
          spread_bps_(spread_bps), tick_interval_ns_(tick_interval_ns) {}

    Tick next_tick() {
        double vol_shock = norm_(rng_);
        volatility_ += vol_mean_revert_ * (base_vol_ - volatility_) + vol_of_vol_ * volatility_ * vol_shock;
        volatility_ = std::max(0.0001, volatility_);
        double price_shock = norm_(rng_);
        double ret = drift_ + volatility_ * price_shock;
        price_ *= (1.0 + ret);
        price_ = std::max(0.01, price_);
        double half_spread = price_ * spread_bps_ * 0.0001 * 0.5;
        std::uniform_int_distribution<Quantity> size_dist(10, 500);
        std::uniform_int_distribution<Quantity> last_size_dist(1, 50);
        timestamp_ += tick_interval_ns_;
        Tick tick;
        tick.timestamp_ns = timestamp_;
        tick.bid      = price_ - half_spread;
        tick.ask      = price_ + half_spread;
        tick.last     = price_ + (norm_(rng_) * half_spread * 0.5);
        tick.bid_size = size_dist(rng_);
        tick.ask_size = size_dist(rng_);
        tick.last_size = last_size_dist(rng_);
        return tick;
    }

    Price current_price() const { return price_; }
    double current_vol() const { return volatility_; }
};
