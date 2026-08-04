using System.Collections;

namespace Tqc.Logging;

public sealed class RingBuffer<T> : IEnumerable<T>
{
    private readonly T[] _buffer;
    private int _head;
    private int _count;

    public int Capacity => _buffer.Length;
    public int Count => _count;

    public RingBuffer(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _buffer = new T[capacity];
    }

    public void Add(T value)
    {
        _buffer[_head] = value;
        _head = (_head + 1) % Capacity;
        if (_count < Capacity) _count++;
    }

    public T this[int index]
    {
        get
        {
            if ((uint)index >= (uint)_count) throw new ArgumentOutOfRangeException(nameof(index));
            var position = (_head - _count + index + Capacity) % Capacity;
            return _buffer[position];
        }
    }
    
    public IEnumerator<T> GetEnumerator()
    {
        for (var i = 0; i < _count; i++) yield return this[i];
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
