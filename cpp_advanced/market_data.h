#pragma once
#include <cstdint>
#include <string>
#include <chrono>
#include <iostream>
#include <iomanip>

// ============================================================================
// Core market data types — plain structs, cache-friendly, no virtual
// ============================================================================

using Price    = double;
using Quantity = int64_t;
using OrderId  = uint64_t;

enum class Side : uint8_t { Buy, Sell };

inline const char* to_string(Side s) {
    return s == Side::Buy ? "BUY" : "SELL";
}

struct Tick {
    uint64_t timestamp_ns;   // nanosecond epoch
    Price    bid;
    Price    ask;
    Price    last;
    Quantity bid_size;
    Quantity ask_size;
    Quantity last_size;

    Price mid()    const { return (bid + ask) * 0.5; }
    Price spread() const { return ask - bid; }
};

enum class OrderStatus : uint8_t {
    New, PartialFill, Filled, Cancelled, Rejected
};

struct Order {
    OrderId     id;
    Side        side;
    Price       price;
    Quantity    qty;
    Quantity    filled_qty = 0;
    OrderStatus status     = OrderStatus::New;

    Quantity remaining() const { return qty - filled_qty; }
    bool     is_done()   const {
        return status == OrderStatus::Filled ||
               status == OrderStatus::Cancelled ||
               status == OrderStatus::Rejected;
    }
};

struct Fill {
    OrderId  order_id;
    Price    fill_price;
    Quantity fill_qty;
    Side     side;
};

// Signal produced by a strategy: direction + strength
struct Signal {
    double direction;   // positive = buy, negative = sell
    double strength;    // 0..1 confidence
    bool   active;

    static Signal none() { return {0.0, 0.0, false}; }
};

// Position tracker
struct Position {
    Quantity net_qty    = 0;
    double   avg_price  = 0.0;
    double   realized_pnl = 0.0;
    double   unrealized_pnl = 0.0;

    void apply_fill(const Fill& f) {
        Quantity signed_qty = (f.side == Side::Buy) ? f.fill_qty : -f.fill_qty;
        if ((net_qty >= 0 && signed_qty > 0) || (net_qty <= 0 && signed_qty < 0)) {
            // adding to position
            double total_cost = avg_price * std::abs(net_qty) + f.fill_price * f.fill_qty;
            net_qty += signed_qty;
            avg_price = (net_qty != 0) ? total_cost / std::abs(net_qty) : 0.0;
        } else {
            // reducing / flipping position
            Quantity close_qty = std::min(std::abs(net_qty), f.fill_qty);
            double pnl_per_unit = (f.side == Side::Sell)
                ? (f.fill_price - avg_price)
                : (avg_price - f.fill_price);
            realized_pnl += pnl_per_unit * close_qty;
            net_qty += signed_qty;
            if (net_qty == 0) avg_price = 0.0;
            // if flipped, new avg_price is the fill price
            if ((signed_qty > 0 && net_qty > 0 && f.fill_qty > close_qty) ||
                (signed_qty < 0 && net_qty < 0 && f.fill_qty > close_qty)) {
                avg_price = f.fill_price;
            }
        }
    }

    void mark_to_market(Price current_mid) {
        if (net_qty != 0) {
            unrealized_pnl = (current_mid - avg_price) * net_qty;
        } else {
            unrealized_pnl = 0.0;
        }
    }

    double total_pnl() const { return realized_pnl + unrealized_pnl; }

    void print() const {
        std::cout << "  Position: " << net_qty << " @ "
                  << std::fixed << std::setprecision(2) << avg_price
                  << " | Realized: " << realized_pnl
                  << " | Unrealized: " << unrealized_pnl
                  << " | Total PnL: " << total_pnl() << "\n";
    }
};
