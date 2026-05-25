# Question 1

As the provider of a news aggregation service, you aim to provide your customers with a system that is as easy to use as
possible. There are many different news providers, and it is tedious for users to subscribe to all of them manually, so
you want to provide a single subscription that manages all the news providers for each customer.

Each subscriber is only interested in a certain set of topics, and should only receive news about those topics.

Each news item has an interest score and each subscriber nominates their minimum interest score. A subscriber should not
be sent news that has a score lower than the subscriber's minimum.

The system may have to handle a lot of data and subscriptions, so it must be robust and not overload subscribers with
too much data. We then choose to decouple the news input feed from the output towards the subscribers, giving more
control to the system.

## Problem Statement

Complete the functions described below in the `NewsProvider` class. Keep in mind that:

* If any constraint is violated when performing an operation, the operation must fail.
* If a constraint is said to be "guaranteed", you may assume it is never violated.
* Timestamps are represented as number of seconds since the Unix Epoch (Jan 1, 1970 UTC), using floating-point values
  valid to the millisecond precision. They are guaranteed to be positive numbers and fit into 32 bits.

### Guaranteed input constraints:

* `1 < N < 2^15`, where `N` is the total number of instructions given to the program.

---

## AddSubscription(id: integer, minInterest: integer, maxNewsPerSecond: integer, topics: list[string]) -> bool

* Register a new subscription for upcoming news on certain topics. Returns `true` if the operation succeeds, and `false`
  otherwise.
* `id` is a unique identifier of the subscription. If a subscription with the same `id` already exists, the subscription
  must be updated with the new parameters.
* `minInterest` represents the minimum interest score desired (inclusive).
* `maxNewsPerSecond` represents the maximum number of news items this subscription can receive per second. This
  constraint is based on a rolling window, i.e. at any given timestamp `t`, no more than `maxNewsPerSecond` news items
  can be delivered within the time window `[t - 1.0, t]`.
* `topics` is a list of news topics this subscription should consider.

### Input constraints:

* `1 ? id < 2^32`
* `1 ? minInterest < 2^32`
* `1 ? maxNewsPerSecond < 2^12`
* `1 ? length(topics) < 2^10`

---

## RemoveSubscription(id: integer) -> bool

* Removes an existing subscription from the system. Returns `true` if the operation succeeds, and `false` otherwise.
* `id` is the unique identifier of the subscription to be removed. If it doesn't exist, the operation fails.

---

## NewsReceived(id: integer, timestamp: float, interest: integer, topics: list[string]) -> bool

* Indicates news on given topics have been received at a given timestamp. Returns `true` if the operation succeeds, and
  `false` otherwise.
* `id` is a unique identifier of these news. If it has already been used, the operation fails.
* `interest` represents the interest score of these news.

### Input constraints:

* `1 ? id < 2^32`
* `1 ? interest < 2^32`
* `1 ? length(topics) < 2^10`

---

## Publish(timestamp: float, maxAge: float) -> dict[int, list[int]]

* Computes the news to be published at the given timestamp. Returns a map of subscription ids by news ids, i.e. all
  subscriptions to be notified per news id. The output does not need to be sorted in any particular order. If the
  operation fails, returns an empty map.
* If needed, news must be prioritized first by highest interest score, then oldest timestamp and highest id.
* A subscriber must never receive the same news twice.
* `maxAge` represents the maximum age of news to be published at this time. It is represented as a number of seconds,
  using floating-point values valid to the millisecond precision.

### Guaranteed input constraints:

* `timestamp` is ever increasing for calls of this function.

### Input constraints:

* `0 < maxAge < 2^32`

---

# Input Format for Custom Testing

Each line of input begins with a key word followed by one or more parameters separated by whitespace, as per the order
described above. The key words are:

1. `subscribe` — indicates a call to `AddSubscription`.
2. `unsubscribe` — indicates a call to `RemoveSubscription`.
3. `news` — indicates a call to `NewsReceived`.
4. `publish` — indicates a call to `Publish`.

---

# Sample Case 1

## Sample Input

```text
subscribe 1 5 2 radio television
subscribe 2 7 3 cable
news 10 100 4 television
news 11 100 5 television cable
news 12 150 7 radio streaming
publish 200 100
```

## Sample Output

```text
subscribed=True
subscribed=True
news_received=True
news_received=True
news_received=True
publish:
- news=11 to [1]
- news=12 to [1]
```

## Explanation

There are two subscribers:

| Id | Min. Interest Score | Max. News Per Second | Topics            |
| -- | ------------------- | -------------------- | ----------------- |
| 1  | 5                   | 2                    | radio, television |
| 2  | 7                   | 3                    | cable             |

Three news items are received:

| News Id | Timestamp | Interest Score | Topics            |
| ------- | --------- | -------------- | ----------------- |
| 10      | 100       | 4              | television        |
| 11      | 100       | 5              | television, cable |
| 12      | 150       | 7              | radio, streaming  |

Finally, the call to `Publish` specifies:

* `timestamp = 200`
* `maxAge = 100`

So only news with timestamps in `[100, 200]` are considered.

* News `10` is ignored because its interest score `4` is below subscriber `1`'s minimum of `5`.
* News `11` is sent to subscriber `1` because:

    * topic `television` matches
    * interest `5` satisfies the minimum
* News `11` is not sent to subscriber `2` because interest `5 < 7`
* News `12` is sent to subscriber `1` because:

    * topic `radio` matches
    * interest `7` satisfies the minimum
