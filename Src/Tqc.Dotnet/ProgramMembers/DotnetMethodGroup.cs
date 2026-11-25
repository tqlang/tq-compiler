using System.Collections.Immutable;
using Tqc.IR.ProgramMembers;

namespace Tqc.Dotnet.ProgramMembers;

public class DotnetMethodGroup(string name) : IFunctionGroup, IDotnetMember
{
    public string Name => name;
    public ImmutableArray<DotnetMethod> Methods = [];

    /// <summary>
    /// The extension methods of this name whose first parameter is this type (or one it extends): `Run(this IView)` is also an
    /// instance method `Run()` of `IView`. They are static methods, kept apart from <see cref="Methods"/>: a call through a value
    /// tries `Methods` first, and the extensions only when none of them takes the arguments.
    /// </summary>
    public ImmutableArray<DotnetMethod> Extensions = [];
    
    public override string ToString() => $"method_group {name}";
}
