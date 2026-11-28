namespace Tq;

/// <summary>`!void`: only the error. `Error` is `object` while there is no `Std.Types.Error`.</summary>
public struct Failable
{
    public object? Error;
}

/// <summary>`!T`: the value, or the error.</summary>
public struct Failable<T>
{
    public T Value;
    public object? Error;
}
