using Tqc.Dotnet.ProgramMembers;
using Tqc.IR.Nodes;
using Tqc.IR.References;

namespace Tqc.Semantics;

internal sealed partial class TypeChecker
{
    /// <summary>
    /// `x.f(args)` where the type of `x` has methods `f` and extension methods `f` of .NET (`DotnetMethodGroup.Extensions`, put there
    /// when the module was built: `Run(this IView)` is also `IView.Run()`). As in C#, the methods of the type come first: the extension
    /// is the call only when none of them takes the arguments. Null when one does, or when no extension does either (the call is then
    /// checked as any other, and says what the methods of the type do not take).
    /// </summary>
    private (IrExpression, ITypeRef?)? ExtensionWhenMembersFail(IrCall call, IrAccess callee, DotnetMethodGroup group, Expectation? expected)
    {
        var members = OverloadsOf(group, viaValue: true);
        if (members.Count > 0 && Quietly(() => ResolveOverload(call, group.Name, members, expected)) != null) return null;

        var extensions = new DotnetMethodGroup(group.Name) { Methods = group.Extensions };
        var attempt = Quietly(() => VisitExtensionCall(call, callee.baseExpr, extensions, expected));
        if (!ReferenceEquals(attempt.Item1, call)) return attempt;

        // A type with no method of that name but the extensions: they are what the error is about.
        return members.Count == 0 ? VisitExtensionCall(call, callee.baseExpr, extensions, expected) : null;
    }

    /// <summary>
    /// The call as a call of the extension methods: static, with the receiver as the first argument, chosen as any overload is
    /// (the receiver is converted like an argument).
    /// </summary>
    private (IrExpression, ITypeRef?) VisitExtensionCall(IrCall call, IrExpression receiver, DotnetMethodGroup extensions, Expectation? expected)
    {
        var rewritten = call with
        {
            callee = new IrReference(call.callee.Location, new FunctionGroupTypeReference(extensions)),
            ArgumentsList = new IrArgumentsList(call.ArgumentsList.location, [receiver, ..call.ArgumentsList.arguments]),
        };

        var resolved = ResolveOverload(rewritten, extensions.Name, OverloadsOf(extensions, viaValue: false), expected);
        if (resolved == null) return (call, null);

        return (resolved, ReturnTypeOf(resolved.callee, resolved.ArgumentsList.arguments));
    }
}
