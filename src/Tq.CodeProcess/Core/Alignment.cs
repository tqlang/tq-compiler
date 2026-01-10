using System.Diagnostics.CodeAnalysis;

namespace Abstract.CodeProcess.Core;

public readonly struct Alignment(int bitlen, int nativelen)
{
    public const int NATIVE_SIZE = 64;
    
    public readonly int FineLength = bitlen;
    public readonly int CoarseLength = nativelen;
    
    public int Bits => FineLength + CoarseLength * NATIVE_SIZE;
    public int Bytes => Bits/8;
    public static implicit operator Alignment(int i) => new (i, 0);
    public static implicit operator Alignment(uint i) => new ((int)i, 0);

    public static Alignment operator +(Alignment a, Alignment b) => new (a.FineLength + b.FineLength, a.CoarseLength + b.CoarseLength);
    public static bool operator ==(Alignment a, Alignment b) => a.Bits == b.Bits;
    public static bool operator !=(Alignment a, Alignment b) => a.Bits != b.Bits;
    public static bool operator <(Alignment a, Alignment b) => a.Bits < b.Bits;
    public static bool operator <=(Alignment a, Alignment b) => a.Bits <= b.Bits;
    public static bool operator >(Alignment a, Alignment b) => a.Bits > b.Bits;
    public static bool operator >=(Alignment a, Alignment b) => a.Bits >= b.Bits;
    
    public static Alignment Align(Alignment value, Alignment alignment)
    {
        var val = value.Bits;
        var alig = alignment.Bits;
        
        return alig == 0
            ? new Alignment(val, 0)
            : new Alignment((val + alig - 1) / alig * alig, 0);
    }
    public static Alignment Max(Alignment a, Alignment b) => a > b ? a : b;

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is not Alignment other) return false;
        return Bits == other.Bits;
    }

    public override int GetHashCode() => HashCode.Combine(FineLength, CoarseLength);

    public override string ToString() => $"{FineLength} * {CoarseLength}n";
}
