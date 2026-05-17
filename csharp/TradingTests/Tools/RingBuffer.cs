namespace Tools;

public class MyCircularQueue
{
    private readonly int[] buffer;
    private int count = 0; // nb of elements actually stored in ring buffer
    private int head;
    private int tail;
    private readonly int capacity;

    public MyCircularQueue(int k)
    {
        buffer = new int[k];
        count = 0;
        capacity = k;
    }

    public bool EnQueue(int value)
    {
        if (IsFull()) return false;
        // to tail 
        buffer[tail] = value;
        // update tail
        tail = (1 + tail) % capacity;
        count++;
        return true;
    }

    public bool DeQueue()
    {
        if (IsEmpty()) return false;
        // dequeue from head
        head = (head + 1) % capacity;
        count--;
        return true;
    }

    public int Front()
    {
        if (IsEmpty()) return -1;
        return buffer[head];
    }

    public int Rear()
    {
        // last element
        if (IsEmpty()) return -1;
        int x = (tail - 1 + capacity) % capacity;
        return buffer[x];
    }

    public bool IsEmpty()
    {
        return count == 0;
    }

    public bool IsFull()
    {
        return count == capacity;
    }
}

/**
 * Your MyCircularQueue object will be instantiated and called as such:
 * MyCircularQueue obj = new MyCircularQueue(k);
 * bool param_1 = obj.EnQueue(value);
 * bool param_2 = obj.DeQueue();
 * int param_3 = obj.Front();
 * int param_4 = obj.Rear();
 * bool param_5 = obj.IsEmpty();
 * bool param_6 = obj.IsFull();
 */