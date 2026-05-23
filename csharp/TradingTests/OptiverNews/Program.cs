using System;
using System.Collections.Generic;
using System.Linq;

namespace Solution
{
    public class Subscription
    {
        public long Id;
        public long MinInterest;
        public long MaxNewsPerSecond;
        public HashSet<string> Topics = new();
        public readonly Queue<double> DeliveryTimestamps = new(); // last [pubtime-1s, pubtime]
    }

    public class NewsItem
    {
        public long Id;
        public double Timestamp;
        public long Interest;
        public HashSet<string> Topics = new();

// Subscriber ids that have ever received this news. Persists across
// unsubscribe/resubscribe so a subscriber id never receives the same news twice.
        public readonly HashSet<long> DeliveredTo = new();
    }

    public class NewsProvider
    {
        private readonly Dictionary<long, Subscription> subs = new();
        private readonly Dictionary<long, NewsItem> news = new();
        private readonly SortedSet<NewsItem> sortedNewsByAge = new(new NewestOnHeadComparer());
        private readonly Dictionary<long, int> inCallDeliveries = new();
        private readonly Dictionary<long, int> baselineWindowCount = new();

        private const long MaxId = 1L << 32;
        private const long MaxInterest = 1L << 32;
        private const long MaxNewsPerSec = 1L << 12;
        private const int MaxTopicsLen = 1 << 10;
        private const double MaxAgeLimit = 1L << 32;

        public bool AddSubscription(long id, long minInterest, long maxNewsPerSecond, List<string> topics)
        {
            if (id < 1 || id >= MaxId) return false;
            if (minInterest < 1 || minInterest >= MaxInterest) return false;
            if (maxNewsPerSecond < 1 || maxNewsPerSecond >= MaxNewsPerSec) return false;
            if (topics == null || topics.Count < 1 || topics.Count >= MaxTopicsLen) return false;

            if (subs.TryGetValue(id, out Subscription existing))
            {
                existing.MinInterest = minInterest;
                existing.MaxNewsPerSecond = maxNewsPerSecond;
                existing.Topics = new HashSet<string>(topics);
                return true;
            }

            Subscription sub = new Subscription
            {
                Id = id,
                MinInterest = minInterest,
                MaxNewsPerSecond = maxNewsPerSecond,
                Topics = new HashSet<string>(topics) // compress for overlapping with subscription. TODO : SubsByTopic 
            };
            subs[id] = sub;
            return true;
        }

        public bool RemoveSubscription(long id)
        {
            if (id < 1 || id >= MaxId) return false;
            return subs.Remove(id);
        }

        public bool NewsReceived(long id, double timestamp, long interest, List<string> topics) // 
        {
            if (id < 1 || id >= MaxId) return false;
            if (interest < 1 || interest >= MaxInterest) return false;
            if (topics == null || topics.Count < 1 || topics.Count >= MaxTopicsLen) return false;
            if (news.ContainsKey(id)) return false;

            var newsItem = new NewsItem
            {
                Id = id,
                Timestamp = timestamp,
                Interest = interest,
                Topics = new HashSet<string>(topics)
            };
            news[id] = newsItem;
            sortedNewsByAge.Add(newsItem); // news never removed, do not need housekeeping 
            return true;
        }

        public Dictionary<long, List<long>> Publish(double publishTimestamp, double maxAge) //  a map of subscription ids by news ids
        {
            Dictionary<long, List<long>> subByNewsId = new Dictionary<long, List<long>>();

            if (maxAge <= 0 || maxAge >= MaxAgeLimit) return subByNewsId;
            if (publishTimestamp < 0) return subByNewsId;

            List<NewsItem> candidates = new List<NewsItem>();
            
            foreach (NewsItem news in sortedNewsByAge)
            {
                double age = publishTimestamp - news.Timestamp;
                if (age < 0) continue; // future news, skip
                if (age > maxAge) break; // too old; rest are older too
                candidates.Add(news);
            }
            candidates.Sort((NewsItem a, NewsItem b) => 
            {
                if (a.Interest != b.Interest) return b.Interest.CompareTo(a.Interest); // highest first
                if (a.Timestamp != b.Timestamp) return a.Timestamp.CompareTo(b.Timestamp);//oldest first
                return b.Id.CompareTo(a.Id); // highest first
            });

            inCallDeliveries.Clear();
            baselineWindowCount.Clear();
            foreach (Subscription s in subs.Values)
            {
                while (s.DeliveryTimestamps.TryPeek(out double ts) 
                    && ts < (publishTimestamp - 1d))
                {
                    s.DeliveryTimestamps.Dequeue(); // drop all pub_ts recorded older than now-1s
                }
                baselineWindowCount[s.Id] = s.DeliveryTimestamps.Count;
                inCallDeliveries[s.Id] = 0;
            }

            foreach (NewsItem news in candidates)
            { // TODO select subscriptions: news=> list of topic => Map<topic, subscription>
                foreach (Subscription sub in subs.Values)
                {
                    if (news.DeliveredTo.Contains(sub.Id)) continue;
                    if (news.Interest < sub.MinInterest) continue;
                    if (!HasTopicOverlap(news.Topics, sub.Topics)) continue;
                    if (baselineWindowCount[sub.Id] + inCallDeliveries[sub.Id] >= sub.MaxNewsPerSecond)
                        continue;

                    news.DeliveredTo.Add(sub.Id);
                    sub.DeliveryTimestamps.Enqueue(publishTimestamp);
                    inCallDeliveries[sub.Id]++;

                    if (!subByNewsId.TryGetValue(news.Id, out List<long>? subs))
                    {
                        subs = new List<long>();
                        subByNewsId[news.Id] = subs; // require news id => list of subscription 
                    }

                    subs.Add(sub.Id);
                }
            }

            return subByNewsId;
        }

        private static bool HasTopicOverlap(HashSet<string> a, HashSet<string> b)
        {
            HashSet<string> small = a.Count <= b.Count ? a : b;
            HashSet<string> big = a.Count <= b.Count ? b : a;
            foreach (string t in small)
                if (big.Contains(t))
                    return true;
            return false;
        }
    }

    internal class NewestOnHeadComparer : IComparer<NewsItem>
    {
        public int Compare(NewsItem? x, NewsItem? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (y is null) return 1;
            if (x is null) return -1;

            var compareTo = y.Timestamp.CompareTo(x.Timestamp);
            if (compareTo == 0)
            {
                return x.Id.CompareTo(y.Id);
            }
            return compareTo;
        }
    }

    public class Solution
    {
        private static string Print(bool b) => b ? "True" : "False";

        private static string Print(Dictionary<long, List<long>> map)
        {
            if (map == null || map.Count == 0) return "";
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            List<long> keys = new List<long>(map.Keys);
            keys.Sort();
            foreach (long key in keys)
            {
                List<long> ids = new List<long>(map[key]);
                ids.Sort();
                sb.Append(" - news=").Append(key).Append(" to [");
                for (int i = 0; i < ids.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(ids[i]);
                }

                sb.Append("]\n");
            }

            return sb.ToString();
        }

        public static void Main(string[] args)
        {
            NewsProvider provider = new NewsProvider();
            string? line;
            while ((line = Console.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length == 0) continue;
                string[] tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string keyword = tokens[0];

                if (keyword == "subscribe")
                {
                    if (tokens.Length < 5)
                    {
                        Console.Error.WriteLine("Malformed input: " + line);
                        continue;
                    }

                    long id = long.Parse(tokens[1]);
                    long minInterest = long.Parse(tokens[2]);
                    long maxNewsPerSecond = long.Parse(tokens[3]);
                    List<string> topics = tokens.Skip(4).ToList();

                    bool subscribed = provider.AddSubscription(id, minInterest, maxNewsPerSecond, topics);
                    Console.WriteLine("subscribed=" + Print(subscribed));
                }
                else if (keyword == "unsubscribe")
                {
                    if (tokens.Length != 2)
                    {
                        Console.Error.WriteLine("Malformed input: " + line);
                        continue;
                    }

                    long id = long.Parse(tokens[1]);
                    bool unsubscribed = provider.RemoveSubscription(id);
                    Console.WriteLine("unsubscribed=" + Print(unsubscribed));
                }
                else if (keyword == "news")
                {
                    if (tokens.Length < 5)
                    {
                        Console.Error.WriteLine("Malformed input: " + line);
                        continue;
                    }

                    long id = long.Parse(tokens[1]);
                    double timestamp = double.Parse(tokens[2]);
                    long interest = long.Parse(tokens[3]);
                    List<string> topics = tokens.Skip(4).ToList();

                    bool newsReceived = provider.NewsReceived(id, timestamp, interest, topics);
                    Console.WriteLine("news_received=" + Print(newsReceived));
                }
                else if (keyword == "publish")
                {
                    if (tokens.Length != 3)
                    {
                        Console.Error.WriteLine("Malformed input: " + line);
                        continue;
                    }

                    double timestamp = double.Parse(tokens[1]);
                    double maxAgeInMs = double.Parse(tokens[2]);

                    Dictionary<long, List<long>> subscribersPerNews = provider.Publish(timestamp, maxAgeInMs);
                    Console.WriteLine("publish:");
                    Console.Write(Print(subscribersPerNews));
                }
                else
                {
                    Console.Error.WriteLine("Malformed input! " + keyword);
                }
            }
        }
    }
}