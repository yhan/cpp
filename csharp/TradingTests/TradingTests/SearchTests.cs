using System;
using System.Collections.Generic;
using System.Formats.Tar;
using NFluent;

namespace TradingTests;

public class SearchTests
{
    // Given a sorted array nums of length
    //     that may contain duplicate elements, return the index of the leftmost occurrence of target.If the array does not contain target, return
    //     .
    [Test]
    public void test_cantfind()
    {
        int leftMost = FindLeftMost(42, [1, 2, 3, 4, 5, 6, 7, 55, 55, 55, 56, 100, 1000]);
        Check.That(leftMost).IsEqualTo(-1);
    }

    [Test]
    public void found_single()
    {
        int leftMost = FindLeftMost(1, [1]);
        Check.That(leftMost).IsEqualTo(0);
    }

    [Test]
    public void test_found()
    {
        int leftMost = FindLeftMost(55, [1, 2, 3, 4, 5, 6, 7, 55, 55, 55, 56, 100, 1000]);
        Check.That(leftMost).IsEqualTo(7);
    }

    private int FindLeftMost(int target, List<int> arr)
    {
        int l = 0, r = arr.Count - 1;
        int idx = -1;
        while (l <= r)
        {
            var mididx = (l + r) >> 1;
            int mid = arr[mididx];
            if (target < mid)
                r = mididx - 1;
            else if (target > mid)
                l = mididx + 1;
            else
            {
                idx = mididx;
                r = mididx - 1;
            }
        }

        return idx;
    }


    public class ListNode
    {
        public int val;
        public ListNode next;

        public ListNode(int val = 0, ListNode next = null)
        {
            this.val = val;
            this.next = next;
        }
    }


    [Test]
    public void test()
    {
        int k = 42;
        if ((k = 12) != 12)
        {
            Console.WriteLine("KO");
        }

        Console.WriteLine("OK");
    }
    /*
     * prev, curr, next
     */
    public ListNode Reverse(ListNode head)
    {
        ListNode prev = null;
        ListNode curr = head;

        while (curr!= null)
        {
            ListNode next = curr.next;
            curr.next = prev;
            prev = curr;
            curr = next;
        }
        return prev;
    }
}