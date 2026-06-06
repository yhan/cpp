using System.Collections.Generic;

namespace TradingTests;

[TestFixture]
public class MergeKSortedLinkedListsShould
{
    class ListNode
    {
        public int val;
        public ListNode next;
    }

    private ListNode Merge(List<ListNode> heads)
    {
        PriorityQueue<ListNode, int> pq = new();
        foreach (var h in heads)
        {
            if(h!=null)
                pq.Enqueue(h, h.val);
        }

        ListNode dummy = new ListNode();
        ListNode tail = dummy;
        while (pq.Count > 0)
        {
            ListNode min = pq.Dequeue();
            tail.next = min;
            tail = min;
            if(min.next != null)
                pq.Enqueue(min.next, min.next.val);
        }

        return dummy.next;
    }
}