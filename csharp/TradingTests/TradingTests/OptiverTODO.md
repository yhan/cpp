prefix sums
Code pad link:
https://coderpad.io/languages/csharp

# Mock tests

These six span the same surface area as the pack: greedy routing (Q11), windowed risk (none — already covered, but Q9
generalises it), state machines (Q8), graph/topo (none — covered well by Q4), Fenwick + offline (Q7), expiring books
with audits (Q10), and stale-detection (Q6).
Want me to:

Q6 — Cross-Venue Stale Quote Detector
--
Difficulty: Medium | 35–45 min
A consolidated feed receives QUOTE venueId symbol bid ask timestamp events in non-decreasing timestamp order. A quote is
stale at time t if the same venue+symbol pair has not emitted an update in the last S time units (i.e. t - lastUpdate >
S). Process a stream of QUOTE events and CHECK t queries. For each CHECK, output the count of (venue, symbol) pairs that
are currently stale among pairs that have ever been seen.
Edge cases: a pair seen exactly once becomes stale at firstSeen + S + 1; CHECK at the same timestamp as a QUOTE on that
pair ? not stale.
Hint: lazy heap by lastUpdate + S, plus a generation counter per pair.


Q7 — Notional-Bucketed VWAP Reconstruction
--
Difficulty: Medium | 35–45 min
Trades arrive as t price qty in non-decreasing time order. After all trades, answer K offline queries of the form RANGE
t1 t2 minNotional: among trades in [t1, t2] whose individual notional (price * qty) is at least minNotional, output the
VWAP (sum of price×qty / sum of qty) rounded down, or -1 if no trade qualifies.
Edge cases: inclusive bounds on both ends; two-trade tie at boundary; integer division semantics; queries are offline so
you can pre-process.
Hint: offline, sort queries by minNotional descending, sort trades by notional descending, use a Fenwick tree indexed on
time for prefix sums of qty and qty×price.

Plan

````

Queries : sort them descendantly using query[] array sorted by min_notional
sort trades by notional desendantely, trade[] array sorted by notional

compute timestamp index, sorted array timestamp[]

Phase A (preprocessing, trades in input order — already time-sorted):
    Walk trades from index 0 to N-1.
    Maintain a running compressed index that increments when t changes.
    Stamp each trade with its idx.
    Also build times[] for query bound resolution.
    


foreach query :
walk down the trades array insert in fenqty, fennotional
for each trade, you have its timestamp, is stampped in phase A 


update two fenwicks ( qty and notional )
in the query you have t1 t2
do range query in fenwick to get sum(notional)& sum(qty) => compute VWAP


allocate vwap array of size k( k is queries count)
fill vwap[]: vwap[query_idx] = vwap
print vwap[] in order


````

Q8 — Iceberg Order Refresh Engine
--
Difficulty: Medium-hard | 45–55 min
An iceberg order has total quantity T, displayed slice D, and a refresh policy: when the displayed slice is fully
filled, automatically replenish from the hidden reserve up to D, but only if at least R time units have elapsed since
the last refresh. Process events:

NEW orderId T D R t
FILL t orderId qty — fills only consume the currently displayed quantity; if qty exceeds displayed, the excess is
invalid (event rejected, no state change)
QUERY t orderId — output displayed hidden lastRefreshTime or INACTIVE if fully consumed

Edge cases: a fill that exactly empties the displayed slice triggers refresh evaluation at time t (so a same-timestamp
fill afterward sees the refreshed state if R == 0); refresh that finds the reserve empty marks the order INACTIVE; an
order with T < D initially displays T.
Hint: deterministic state machine per order, careful ordering of "fill ? empty ? refresh check" within a single event.

Q9 — Symbol Universe with Tiered Risk Limits
--
Difficulty: Medium-hard | 50–60 min
Each symbol belongs to a tier (1, 2, or 3). The trader has per-tier notional limits L1, L2, L3 and a global limit G.
Process events in time order:

TRADE t symbol qty price — buy if qty > 0, sell if qty < 0
RETIER t symbol newTier — symbol changes tier; existing net position notional moves to the new tier's bucket
QUERY t — output the current headroom for each tier in order: headroom1 headroom2 headroom3 globalHeadroom. Headroom can
be negative if a retier has pushed a tier over its limit; the global headroom is over gross notional across all tiers.

Edge cases: a TRADE that would breach a tier or global limit is rejected (no state change); position notional is
recomputed using the current price at the time of the trade — historical positions retain their original notional until
the next trade in that symbol; retier never rejects.
Hint: per-symbol position + cumulative notional bucket per tier; on retier, move the symbol's contribution between
buckets atomically.

Q10 — Best-Execution Auditor with Late Acks
--
Difficulty: Hard | 60–80 min
For each completed order, you must verify the executed price was no worse than the best price available on any venue at
the order's send time (BUY: lowest ask; SELL: highest bid).
Events in non-decreasing timestamp order, but with a wrinkle: ACKs can arrive late (timestamp ? send time, up to D units
later).

QUOTE t venueId symbol bid ask — venue updates; previous quote replaced
SEND t orderId symbol side qty — order sent at time t
ACK t orderId execPrice — order executed at execPrice (the snapshot to audit is the venue state at the SEND time, not
ACK time)
AUDIT t — for every SEND event whose ACK has arrived and whose send-time was ? t - D, output orderId verdict where
verdict is OK if execPrice was no worse than best, else BAD diff. Each order is reported exactly once across all AUDITs.

Edge cases: an order with no quote on any venue at send time is NO_QUOTE; D can be 0; ACKs may arrive in any order
relative to other ACKs as long as their timestamp is ? the SEND timestamp.
Hint: snapshot best-by-symbol at SEND time using a per-symbol sorted structure (SortedDictionary or two heaps with lazy
deletion); store (symbol, side, snapshotBest) per order; on AUDIT, drain a min-heap of orders keyed by sendTime + D.

Q11 — Latency-Weighted Smart Router with Live Reweighting
--
Difficulty: Hard | 70–90 min
This one is closest in spirit to Q1+Q5 combined.
A router routes child orders. Events:

VENUE venueId latency capacity unitCost — register or update venue properties (capacity is resettable to a new value;
latency and unitCost may also change)
ROUTE t qty maxLatency — route qty units across venues with latency ? maxLatency, minimizing total cost. Output
totalCost and the per-venue allocations sorted by venueId. If full quantity cannot be filled, output IMPOSSIBLE and do
not consume any capacity.
RESTORE t venueId qty — return qty capacity to a venue (e.g. order rejected upstream)

After each successful ROUTE, used capacity is consumed and not returned automatically.
Edge cases: venue updates while it has consumed capacity ? only the available capacity is affected by the new total (
clamped at 0); RESTORE for a venue beyond its original capacity is allowed (overflows into a "reserve" that future
updates respect); ties broken by latency then venueId; multiple ROUTEs at the same timestamp processed in input order.
Hint: SortedDictionary keyed by (unitCost, latency, venueId); on ROUTE, walk the sorted set filtering by latency; on
update, remove and re-insert the venue. Be very careful that a failed ROUTE leaves zero side-effects — build the
allocation plan first, commit only if total qty achievable.