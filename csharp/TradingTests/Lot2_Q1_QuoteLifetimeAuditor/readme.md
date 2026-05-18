# Question 1 — Quote Lifetime Auditor

A market-maker submits two-sided quotes on a single instrument. You receive a chronological stream of events. Every
event carries a millisecond timestamp `ts` (monotonic non-decreasing; equal timestamps allowed and processed in input
order).

## Event formats

```
QUOTE  ts q_id bid_px bid_sz ask_px ask_sz ttl_ms
AMEND  ts q_id new_bid_sz new_ask_sz
CANCEL ts q_id
TRADE  ts side px sz
```

- `side ? {BUY, SELL}` is the aggressor side.
- All prices and sizes are positive integers. `ttl_ms ? 0`.

## Quote semantics

- `QUOTE` is purely additive: each `q_id` is a distinct entry. A new quote never cancel-replaces a prior one, regardless
  of price. `q_id` is unique across the stream.
- A quote has two independent sides (bid, ask). Each side is alive iff: not expired, not cancelled, and its size > 0.
- A quote expires at `quote_ts + ttl_ms`. At `now >= expiry_ts` the quote is dead (both sides).
- `AMEND` modifies sizes only; price and TTL are unchanged. Setting a side's size to 0 kills that side permanently — a
  later AMEND raising it does **not** revive it.
- `CANCEL` kills both sides immediately.
- Invalid `AMEND` or `CANCEL` (unknown `q_id`, or quote already fully dead) is silently ignored but counted.

## Trade matching

- Aggressor `BUY` consumes from the best ask among alive quotes at `ts`; aggressor `SELL` from the best bid.
- Best ask = `min(ask_px)`; best bid = `max(bid_px)`. Ties broken by **smallest q_id**.
- A trade fills against **one quote only**, at one price level. No multi-level sweep, no multi-quote sweep at the same
  price.
- Three outcomes:
    - `trade_sz < resting`: partial fill of the quote. Quote's side size decreases by `trade_sz`, stays alive.
      `matched = trade_sz`, `unmatched = 0`.
    - `trade_sz == resting`: full fill. Quote's side size ? 0, side dies. `matched = trade_sz`, `unmatched = 0`.
    - `trade_sz > resting`: full fill of the quote. Quote's side size ? 0, side dies. `matched = resting`,
      `unmatched = trade_sz - resting`.
- If no quote is alive on the aggressed side: `matched = 0`, `unmatched = trade_sz`, no quote touched.
- `px` on the TRADE event is informational; matching is driven purely by best touch.

## Output

For each `TRADE` event in input order, one line:

```
q_id matched_sz unmatched_sz
```

Use `-` for `q_id` when no quote was touched (unmatched-only case).

At end-of-stream:

1. For every `q_id` ever submitted, one line in ascending `q_id` order:

   ```
   q_id bought_from_me_sz sold_to_me_sz
   ```

   where `bought_from_me_sz` is total size filled against this quote's ask (aggressor BUYs hit it), and `sold_to_me_sz`
   is total size filled against its bid (aggressor SELLs hit it).

2. One final line:

   ```
   IGNORED count
   ```

   counting invalid AMEND and CANCEL events.

## Constraints

- Up to 2×10? events total.
- Prices, sizes, timestamps fit in 64-bit signed integers.