using System.Collections.Generic;

namespace TradingTests;

public class LRUtest
{
    [Test]
    public void LeetCode_Example_Sequence()
    {
        LRUCache cache = new LRUCache(2);

        cache.Put(1, 1); // {1=1}
        cache.Put(2, 2); // {1=1, 2=2}
        Assert.That(cache.Get(1), Is.EqualTo(1)); // returns 1; order now: 1(MRU), 2(LRU)
        cache.Put(3, 3); // evicts 2 (LRU); {1=1, 3=3}
        Assert.That(cache.Get(2), Is.EqualTo(-1)); // 2 was evicted -> miss
        cache.Put(4, 4); // evicts 1 (LRU); {3=3, 4=4}
        Assert.That(cache.Get(1), Is.EqualTo(-1)); // 1 was evicted -> miss
        Assert.That(cache.Get(3), Is.EqualTo(3)); // returns 3
        Assert.That(cache.Get(4), Is.EqualTo(4)); // returns 4
    }

    [Test]
    public void MoveMiddleNode_Capacity3()
    {
        LRUCache cache = new LRUCache(3);
        cache.Put(1, 1); // 1
        cache.Put(2, 2); // 2,1
        cache.Put(3, 3); // 3,2,1   (2 is the middle)
        Assert.That(cache.Get(2), Is.EqualTo(2)); // bump middle → 2,3,1
        cache.Put(4, 4); // full(3): evict LRU=1 → 4,2,3
        Assert.That(cache.Get(1), Is.EqualTo(-1)); // 1 evicted
        Assert.That(cache.Get(3), Is.EqualTo(3)); // 3 still present
        Assert.That(cache.Get(2), Is.EqualTo(2)); // 2 still present
        Assert.That(cache.Get(4), Is.EqualTo(4)); // 4 still present
    }

    [Test]
    public void UpdateExistingKey_RefreshesAndOverwrites()
    {
        LRUCache cache = new LRUCache(2);
        cache.Put(1, 1);
        cache.Put(2, 2); // 2,1
        cache.Put(1, 10); // update 1's value, bump to head → 1,2
        Assert.That(cache.Get(1), Is.EqualTo(10)); // new value
        cache.Put(3, 3); // evict LRU=2 (since 1 was just refreshed) → 3,1
        Assert.That(cache.Get(2), Is.EqualTo(-1)); // 2 evicted
        Assert.That(cache.Get(1), Is.EqualTo(10)); // 1 survived with updated value
    }


    [Test]
    public void SingleNodePutNew()
    {
        LRUCache cache = new LRUCache(1);
        cache.Put(1, 1);
        cache.Put(2, 2); // 2,1
        Assert.That(cache.Get(1), Is.EqualTo(-1)); // new value
    }
    public class LRUCache {
    private int cap;
    private Dictionary<int, Node> map=new ();
    private DoubleLinkedList List = new();
    private int count=0;
    public LRUCache(int capacity) {
        cap = capacity;
    }
    
    public int Get(int key) {
        if(map.TryGetValue(key, out Node n) == false)
           return -1;
        List.MoveToHead(n);
        return n.Value;
    }
    
    public void Put(int key, int value) {
        if(map.TryGetValue(key, out Node n) == false)
        {
            n = new Node(key, value);
            map[key] = n;

            if( count < cap )
            {
                count++;
            }
            else
            {
                Node removed = List.RemoveLast();
                map.Remove(removed.Key);
            }
            List.AddToHead(n);
        }
        else
        {
            n.Value=value;
            List.MoveToHead(n);
        }
    }
}

public class Node 
{
    public int Key; public int Value;
    public Node(int k, int v) {Key=k; Value=v;}
    public Node Prev; public Node Next;
}
public class DoubleLinkedList
{
    public Node Head; public Node Tail;
    private int size=0;
    
    public void MoveToHead(Node n)
    { 
        if(Head == n) return;
        // prev => n => next
        n.Prev.Next = n.Next;
        
        if(n.Next != null) // n is NOT last
            n.Next.Prev = n.Prev;
        else // n is the last
            Tail = n.Prev;
        
        n.Next = Head;
        Head.Prev = n;
        Head = n;
        Head.Prev = null;
    }
    public Node RemoveLast()
    { 
        Node rmv;
        if(size == 1 )
        {
            rmv = Head;
            Head= null; Tail=null;
        }
        else
        {
            // x => tail
            rmv=Tail;
            Tail.Prev.Next= null;
            Tail = Tail.Prev;
        }
        size--;
        return rmv;
    }
    public void AddToHead(Node n)
    {
        if(Head == null)
        {
            Head= n; Tail=n;
        }
        else 
        {
            n.Next = Head;
            Head.Prev=n;
            Head=n;    
        }
        size++;
        
    }
}

/**
 * Your LRUCache object will be instantiated and called as such:
 * LRUCache obj = new LRUCache(capacity);
 * int param_1 = obj.Get(key);
 * obj.Put(key,value);
 */
}