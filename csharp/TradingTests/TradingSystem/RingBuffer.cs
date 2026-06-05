using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace LowLatency.Buffers
{
    /// <summary>
    /// Single-producer, single-consumer lock-free ring buffer.
    /// Capacity MUST be a power of two so we can mask instead of modulo.
    /// One slot is sacrificed to distinguish full from empty (head == tail == empty).
    /// </summary>
    public sealed class SpscRingBuffer<T>
    {
        private readonly T[] _buffer;
        private readonly int _mask;

        // Padded to avoid false sharing: head and tail must live on separate cache lines,
        // otherwise the producer writing _tail invalidates the consumer's line holding _head.
        private PaddedLong _head; // next index to READ  (owned by consumer)
        private PaddedLong _tail; // next index to WRITE (owned by producer)

        public SpscRingBuffer(int capacity)
        {
            if (capacity < 2 || (capacity & (capacity - 1)) != 0)
                throw new ArgumentException("Capacity must be a power of two >= 2.", nameof(capacity));

            _buffer = new T[capacity];
            _mask = capacity - 1;
        }

        /// <summary>Producer side. Returns false if full.</summary>
        public bool TryEnqueue(T item)
        {
            long tail = Volatile.Read(ref _tail.Value);
            long head = Volatile.Read(ref _head.Value);

            // Full when advancing tail would collide with head.
            if (tail - head >= _buffer.Length)
                return false;

            _buffer[tail & _mask] = item;

            // Publish: the slot write above MUST be visible before the tail bump.
            // Volatile.Write gives us the release barrier that guarantees that ordering.
            Volatile.Write(ref _tail.Value, tail + 1);
            return true;
        }

        /// <summary>Consumer side. Returns false if empty.</summary>
        public bool TryDequeue(out T item)
        {
            long head = Volatile.Read(ref _head.Value);
            long tail = Volatile.Read(ref _tail.Value);

            if (head == tail) // empty
            {
                item = default!;
                return false;
            }

            item = _buffer[head & _mask];
            _buffer[head & _mask] = default!; // release reference so GC can reclaim (skip for value types)

            Volatile.Write(ref _head.Value, head + 1);
            return true;
        }

        public bool IsEmpty => Volatile.Read(ref _head.Value) == Volatile.Read(ref _tail.Value);
    }

    // Cache-line padding (64 bytes typical). Keeps head and tail off the same line.
    [StructLayout(LayoutKind.Explicit, Size = 128)]
    internal struct PaddedLong
    {
        [FieldOffset(64)] public long Value;
    }
}