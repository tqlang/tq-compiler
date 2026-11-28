using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Tq;

public readonly unsafe struct Slice<T>
{
    private readonly Memory<T> _memory;
    private readonly void* _pointer;
    public readonly int Length;
    public readonly bool IsUnmanaged;

    public Slice(Slice<T> copy)
    {
        _memory      = copy._memory;
        _pointer     = copy._pointer;
        Length      = copy.Length;
        IsUnmanaged = copy.IsUnmanaged;
    }
    
    public Slice(Memory<T> memory)
    {
        _memory      = memory;
        _pointer     = null;
        Length      = memory.Length;
        IsUnmanaged = false;
    }
    
    public Slice(Span<T> span)
    {
        _memory      = default;
        _pointer     = Unsafe.AsPointer(ref MemoryMarshal.GetReference(span));
        Length      = span.Length;
        IsUnmanaged = true;
    }

    public Slice(void* pointer, int length)
    {
        _memory      = default;
        _pointer     = pointer;
        Length      = length;
        IsUnmanaged = true;
    }
    
    /// <summary>A slice over the whole array (what the compiler does with a `T[]` of .NET).</summary>
    public Slice(T[] array) : this(new Memory<T>(array)) { }

    public static implicit operator Slice<T>(Span<T> span) => new(span);
    public static implicit operator Slice<T>(Memory<T> memory) => new(memory);
    public static implicit operator Slice<T>(T[] array) => new(array);

    public static implicit operator Span<T>(Slice<T> slice) => slice.Span;
    public static implicit operator ReadOnlySpan<T>(Slice<T> slice) => slice.Span;
    public static implicit operator ReadOnlyMemory<T>(Slice<T> slice) => slice.Memory;

    public Span<T> Span => IsUnmanaged ? new Span<T>(_pointer, Length) : _memory.Span;

    /// <summary>The `Memory&lt;T&gt;` of a slice of managed memory; memory that is not managed has none.</summary>
    public Memory<T> Memory => IsUnmanaged ? throw new InvalidOperationException("Slice points to unmanaged memory") : _memory;

    /// <summary>Copies the elements of the slice into `destination` (what `a = b` does with two arrays).</summary>
    public void CopyTo(Slice<T> destination) => Span.CopyTo(destination.Span);

    /// <summary>The element at `index`, by reference (so it is read and written in place).</summary>
    public ref T this[int index] => ref Span[index];

    /// <summary>`length` elements from `start` (what `slice[a .. b]` is).</summary>
    public Slice<T> Part(int start, int length)
    {
        if ((uint)start > (uint)Length || (uint)length > (uint)(Length - start))
            throw new ArgumentOutOfRangeException(nameof(start), "The part is not inside the slice");

        if (!IsUnmanaged) return new Slice<T>(_memory.Slice(start, length));
        return new Slice<T>(Unsafe.AsPointer(ref Unsafe.Add(ref Unsafe.AsRef<T>(_pointer), start)), length);
    }

    /// <summary>
    /// The `T[]` of a slice: the array itself when the slice is all of one (what .NET does with it is seen by the slice, and nothing is
    /// copied), and a copy when it is a part of one or does not live in an array.
    /// </summary>
    public T[] AsArray()
    {
        if (!IsUnmanaged && MemoryMarshal.TryGetArray<T>(_memory, out var segment)
            && segment.Offset == 0 && segment.Count == segment.Array!.Length)
            return segment.Array;

        return Span.ToArray();
    }
}
