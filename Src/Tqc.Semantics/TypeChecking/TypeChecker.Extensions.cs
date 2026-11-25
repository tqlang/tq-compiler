using System.Collections.Immutable;
using Tqc.Dotnet.ProgramMembers;
using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers;
using Tqc.IR.References;

namespace Tqc.Semantics;

/// <summary>
/// The extension methods of .NET, by name: static methods whose first parameter is the value they extend
/// (`WindowExtensions.Run(window)` is also `window.Run()`). Only the ones without type parameters of their own.
/// </summary>
internal sealed class ExtensionIndex
{
    private readonly Dictionary<string, DotnetMethodGroup> _groups = [];

    public static ExtensionIndex Build(IEnumerable<INamespace> roots)
    {
        var index = new ExtensionIndex();
        foreach (var root in roots) index.Visit(root);
        return index;
    }

    /// <summary>All the extension methods of that name, as a group of overloads whose first parameter is the receiver.</summary>
    public DotnetMethodGroup? Named(string name) => _groups.GetValueOrDefault(name);

    private void Visit(INamespace? node)
    {
        switch (node)
        {
            case DotnetNamespace dotnetNamespace:
                foreach (var type in dotnetNamespace.Types.Values) Visit(type as INamespace);
                foreach (var child in dotnetNamespace.Namespaces.Values) Visit(child);
                break;

            case DotnetStaticClass staticClass:
                foreach (var member in staticClass.Members.Values.OfType<DotnetMethodGroup>())
                foreach (var method in member.Methods)
                {
                    if (method.TypeParameters.Count > 0 || !method.IsExtension) continue;

                    if (!_groups.TryGetValue(method.Name, out var group)) _groups[method.Name] = group = new DotnetMethodGroup(method.Name);
                    group.Methods = group.Methods.Add(method);
                }

                foreach (var nested in staticClass.NestedStaticClasses.Values) Visit(nested);
                break;
        }
    }
}

/// <summary>`value.name` where `name` is not a member of the type of `value` but an extension method of .NET: the call decides.</summary>
internal sealed record ExtensionReference(DotnetMethodGroup Group) : Ref;

internal sealed partial class TypeChecker
{
    /// <summary>Set by a call for its callee, which is read for what it is called on: a member that is not there may be an extension.</summary>
    private bool _asCallee;

    private bool TakeCallee()
    {
        var value = _asCallee;
        _asCallee = false;
        return value;
    }

    /// <summary>
    /// `x.f(args)` where the type of `x` has no `f`: the extension methods `f` of .NET with `x` as the first argument,
    /// chosen as any overload is. The call becomes a call of the static method.
    /// </summary>
    private (IrExpression, ITypeRef?) VisitExtensionCall(IrCall call, IrExpression receiver, ExtensionReference extension, Expectation? expected)
    {
        var group = extension.Group;
        var rewritten = call with
        {
            callee = new IrReference(call.callee.Location, new FunctionGroupTypeReference(group)),
            ArgumentsList = new IrArgumentsList(call.ArgumentsList.location, [receiver, ..call.ArgumentsList.arguments]),
        };

        var resolved = ResolveOverload(rewritten, group.Name, OverloadsOf(group, viaValue: false), expected);
        if (resolved == null) return (call, null);

        return (resolved, ReturnTypeOf(resolved.callee, resolved.ArgumentsList.arguments));
    }
}
