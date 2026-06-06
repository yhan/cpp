namespace TradingTests;

[TestFixture]
public class LinkedListReverseKGroupShould
{
    class ListNode
    {
        public int val;
        public ListNode next;
    }

    private ListNode ReverseKGroup(ListNode head, int k)
    {
        ListNode dummy = new ListNode();
        dummy.next = head;
        ListNode groupPrev = dummy;
        while(true)
        {
            ListNode kth = groupPrev;
            for (int i = 0; i < k && kth != null; i++)
            {
                kth = kth.next;
            }

            if (kth == null) break;
            ListNode groupNext = kth.next; // node after current group
            ListNode groupHead = groupPrev.next; // ... will become group tail

            ListNode prev = groupNext;
            ListNode curr = groupHead;
            while (curr != groupNext)
            {
                ListNode next = curr.next;
                curr.next = prev;
                prev = curr;
                curr = next;
            }

            groupPrev.next = prev; // wire up : the head should point to last visited element of the current group
            groupPrev = groupHead; // previous group's pointer  
        }

        return dummy.next;
    }
}