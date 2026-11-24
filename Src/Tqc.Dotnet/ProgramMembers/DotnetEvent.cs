using Tqc.IR.ProgramMembers;
using Tqc.IR.References;

namespace Tqc.Dotnet.ProgramMembers;

/// <summary>
/// An event of .NET (`window.Load`). It has no value: `+=` and `-=` call its `add` and `remove` accessors with a delegate.
/// </summary>
public class DotnetEvent(string name, ITypeRef type, DotnetMethod? add, DotnetMethod? remove) : IField, IDotnetMember
{
    public string Name => name;

    /// <summary>The type of the delegate that it takes: `Ref(EventHandler)`.</summary>
    public ITypeRef Type => type;

    public DotnetMethod? Add => add;
    public DotnetMethod? Remove => remove;

    public bool IsStatic => (add ?? remove) is { IsStatic: true };

    public override string ToString() => $"event {name}";
}
