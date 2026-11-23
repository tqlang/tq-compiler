using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Tq;
using Mono.Cecil;

namespace Tqc.Dotnet.ProgramMembers;

/// <summary>
/// An interface of .NET with generic parameters (`IEnumerable&lt;T&gt;`): a constructor of types, used as `IEnumerable(i32)`.
/// Like a generic class it is applied to arguments (a `GenericInstance`), and a value of it is by reference.
/// </summary>
public class DotnetGenericInterface(TypeReference typeRef, string name) : DotnetInterface(typeRef, name), IStruct
{
    public IReadOnlyList<TqParameter> Parameters { get; init; } = [];

    public override bool IsReference => true;
}
