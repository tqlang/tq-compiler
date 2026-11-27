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
    
    public static implicit operator Slice<T>(Span<T> span) => new(span);
    public static implicit operator Slice<T>(Memory<T> memory) => new(memory);
    
    public Span<T> Span => IsUnmanaged ? new Span<T>(_pointer, Length) : _memory.Span;
    public Memory<T> Memory => IsUnmanaged ? _memory : throw new InvalidOperationException("Slice points to unmanaged memory");
}
