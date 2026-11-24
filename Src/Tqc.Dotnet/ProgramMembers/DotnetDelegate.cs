using Mono.Cecil;

namespace Tqc.Dotnet.ProgramMembers;

/// <summary>
/// A delegate of .NET without generic parameters (`Action`, `EventHandler`): a class, so by reference (`*Action`), whose
/// signature is the one of its `Invoke`. A function of Tq with that signature can be made one (`action += f`).
/// </summary>
public sealed class DotnetDelegate(TypeReference typeRef, string name) : DotnetClass(typeRef, name)
{
    /// <summary>What the delegate calls: its parameters and what it returns are the ones a function needs to become it.</summary>
    public DotnetMethod? Invoke =>
        Members != null && Members.TryGetValue("Invoke", out var member) && member is DotnetMethodGroup group
            ? group.Methods.FirstOrDefault(method => !method.IsStatic)
            : null;

    public override string ToString() => $"delegate {Name}";
}
