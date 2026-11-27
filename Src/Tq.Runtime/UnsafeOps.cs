using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Tq;

/// <summary>
/// What `Std.Memory.Unsafe` does that a few instructions cannot: the compiler calls these. Nothing here checks what a pointer
/// points to; that is what unsafe means.
/// </summary>
public static unsafe class UnsafeOps
{
    /// <summary>
    /// `sliceFromPtr`: a slice (a `Memory&lt;T&gt;`) over `length` elements at `pointer`. It does not own the memory: whoever
    /// allocated it frees it, and the slice must not be used after that. `T` has to be unmanaged (an array of references
    /// cannot be placed over raw memory), which the runtime checks.
    /// </summary>
    public static Memory<T> SliceFromPtr<T>(void* pointer, nuint length)
    {
        if (length > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(length), "A slice cannot have more than 2147483647 elements");
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) throw new ArgumentException($"A slice cannot be placed over raw memory of '{typeof(T)}': it holds references");
        if (length == 0) return Memory<T>.Empty;
        if (pointer == null) throw new ArgumentNullException(nameof(pointer), "A slice that is not empty cannot start at the null pointer");

        return new UnmanagedMemoryManager<T>(pointer, (int)length).Memory;
    }

    /// <summary>
    /// `alignOf`: where a `T` can be placed. .NET does not tell it, so it is the offset of a `T` that follows a byte in a struct
    /// with the layout of the runtime; a type that holds references is aligned as a pointer.
    /// </summary>
    public static nuint AlignOf<T>()
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) return (nuint)sizeof(nint);

        var probe = default(Aligned<T>);
        return (nuint)Unsafe.ByteOffset(ref Unsafe.As<byte, byte>(ref probe.Byte), ref Unsafe.As<T, byte>(ref probe.Value));
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct Aligned<T>
    {
        public byte Byte;
        public T Value;
    }

    private sealed class UnmanagedMemoryManager<T>(void* pointer, int length) : MemoryManager<T>
    {
        public override Span<T> GetSpan() => new(pointer, length);

        public override MemoryHandle Pin(int elementIndex = 0)
        {
            if ((uint)elementIndex > (uint)length) throw new ArgumentOutOfRangeException(nameof(elementIndex));
            return new MemoryHandle((byte*)pointer + (nint)elementIndex * Unsafe.SizeOf<T>());
        }

        public override void Unpin() { }

        protected override void Dispose(bool disposing) { }
    }
}
