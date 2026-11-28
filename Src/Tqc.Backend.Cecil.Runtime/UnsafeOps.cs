using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Tq;

/// <summary>
/// What `Std.Memory.Unsafe` does that a few instructions cannot: the compiler calls these. Nothing here checks what a pointer
/// points to; that is what unsafe means.
/// </summary>
public static unsafe class UnsafeOps
{
    /// <summary>
    /// `sliceFromPtr`: a `Slice&lt;T&gt;` over `length` elements at `pointer`. It does not own the memory: whoever
    /// allocated it frees it, and the slice must not be used after that. `T` has to be unmanaged (an array of references
    /// cannot be placed over raw memory), which the runtime checks.
    /// </summary>
    public static Slice<T> SliceFromPtr<T>(void* pointer, nuint length)
    {
        if (length > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(length), "A slice cannot have more than 2147483647 elements");
        
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            throw new ArgumentException($"A slice cannot be placed over raw memory of '{typeof(T)}': it holds references");
        
        if (length == 0)
            return default;
        
        if (pointer == null)
            throw new ArgumentNullException(nameof(pointer), "A slice that is not empty cannot start at the null pointer");

        return new Slice<T>(pointer, (int)length);
    }

    /// <summary>
    /// The element at `index` of the `length` elements at `pointer`, by reference: what `block[i]` is where the array is memory on the stack or in
    /// a field, so a `Slice&lt;T&gt;` does not have to be made for each access. An index outside the array is an `IndexOutOfRangeException`, as in a slice.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T At<T>(T* pointer, int index, int length)
    {
        if ((uint)index >= (uint)length) ThrowIndexOutOfRange();
        return ref pointer[index];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowIndexOutOfRange() => throw new IndexOutOfRangeException();

    /// <summary>
    /// `alignOf`: where a `T` can be placed. .NET does not tell it, so it is the offset of a `T` that follows a byte in a struct
    /// with the layout of the runtime; a type that holds references is aligned as a pointer.
    /// </summary>
    public static nuint AlignOf<T>()
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) return (nuint)sizeof(nint);

        var probe = default(Aligned<T>);
        return (nuint)Unsafe.ByteOffset(
            ref Unsafe.As<byte, byte>(ref probe.Byte),
            ref Unsafe.As<T, byte>(ref probe.Value!)
        );
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Aligned<T>
    {
        public byte Byte;
        public T Value;
    }
}
