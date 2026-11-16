using System.Collections.Immutable;
using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;

namespace Tqc.Semantics;

internal static partial class ReferenceBinder
{
    /// <summary>First phase: binds the arguments of the attributes of a member and of what it contains.</summary>
    private static void BindMemberAttributes(BindingContext ctx, IMember member)
    {
        switch (member)
        {
            case TqFunctionGroup group:
                foreach (var function in group) BindAttributes(ctx, function);
                break;

            case TqStruct structure:
                BindAttributes(ctx, structure);

                // Bound this early so that every struct knows what it extends before any body is bound.
                if (structure.Extends is IrReference or IrAccess)
                    structure.Extends = BindExpression(ctx, structure.Extends);

                InsideOf(ctx, structure, () =>
                {
                    ctx.PushFrame();
                    foreach (var inner in structure.Members.Values) ctx.Add(inner);
                    foreach (var inner in structure.Members.Values) BindMemberAttributes(ctx, inner);
                    foreach (var constructor in structure.Constructors) BindAttributes(ctx, constructor);
                    if (structure.Destructor != null) BindAttributes(ctx, structure.Destructor);
                    ctx.PopFrame();
                });
                break;

            case TqTypedef typedef:
                BindAttributes(ctx, typedef);
                InsideOf(ctx, typedef, () =>
                {
                    ctx.PushFrame();
                    foreach (var inner in typedef.Members.Values) ctx.Add(inner);
                    foreach (var inner in typedef.Members.Values) BindMemberAttributes(ctx, inner);
                    ctx.PopFrame();
                });
                break;

            case ITqMember tqMember:
                BindAttributes(ctx, tqMember);
                break;
        }
    }

    /// <summary>Runs `action` with the code counted as being inside `container`, a struct or typedef.</summary>
    private static void InsideOf(BindingContext ctx, IMember container, Action action)
    {
        var outer = ctx.Accessor;
        ctx.Accessor = outer.Inside(container);
        action();
        ctx.Accessor = outer;
    }

    /// <summary>
    /// Binds the arguments of each attribute. `@allowAcessTo` and `@denyAcessTo` take the namespaces and structs
    /// themselves (or an array of them), and the member's access keeps those members, not their names.
    /// </summary>
    private static void BindAttributes(BindingContext ctx, ITqMember member)
    {
        if (member.Attributes.Count == 0) return;

        var access = member.Access;
        List<TqAttribute> bound = [];

        foreach (var attribute in member.Attributes)
        {
            switch (attribute)
            {
                case TqBuiltinAttribute builtin:
                    var arguments = BindAttributeArguments(ctx, builtin.Arguments);

                    if (builtin.Kind is BuiltinAttribute.AllowAccessTo or BuiltinAttribute.DenyAccessTo)
                    {
                        var targets = AccessTargets(ctx, builtin, arguments);
                        access = builtin.Kind == BuiltinAttribute.AllowAccessTo
                            ? access with { Allowed = access.Allowed.AddRange(targets) }
                            : access with { Denied = access.Denied.AddRange(targets) };
                    }

                    if (builtin.Kind == BuiltinAttribute.OverrideOperator) CheckOperatorArgument(ctx, arguments);

                    bound.Add(builtin with { Arguments = arguments });
                    break;

                case TqUnknownAttribute unknown:
                    bound.Add(unknown with { Arguments = BindAttributeArguments(ctx, unknown.Arguments) });
                    break;

                default:
                    bound.Add(attribute);
                    break;
            }
        }

        member.Attributes = bound;
        member.Access = access;
    }

    /// <summary>`@overrideOperator` takes one of the cases of `Std.Math.Operator`.</summary>
    private static void CheckOperatorArgument(BindingContext ctx, ImmutableArray<IrExpression> arguments)
    {
        if (arguments.Length != 1 || arguments[0] is IrReference { Reference: UnsolvedRef }) return;

        if (arguments[0] is not IrReference { Reference: FieldReference { Field: TqNamedCase { Typedef: { } typedef } } }
            || !ReferenceEquals(typedef, StdMath.Operator))
            ctx.Error("'@overrideOperator' takes a case of 'Std.Math.Operator', for example 'Std.Math.Operator.Add'",
                arguments[0].Location);
    }

    private static ImmutableArray<IrExpression> BindAttributeArguments(
        BindingContext ctx, ImmutableArray<IrExpression> arguments) =>
        [..arguments.Select(argument => BindExpression(ctx, argument))];

    /// <summary>The members an access attribute names; each must contain other members.</summary>
    private static List<IMember> AccessTargets(
        BindingContext ctx, TqBuiltinAttribute attribute, ImmutableArray<IrExpression> arguments)
    {
        List<IMember> targets = [];

        void Add(IrExpression argument)
        {
            switch (argument)
            {
                case IrArray array:
                    foreach (var element in array.Elements) Add(element);
                    break;

                case IrReference { Reference: NamespaceReference { Namespace: var @namespace } }:
                    targets.Add(@namespace);
                    break;

                case IrReference { Reference: TypeMetaReference { Type: IMemberContainer and var container } }:
                    targets.Add(container);
                    break;

                // A name that did not resolve was already reported.
                case IrReference { Reference: UnsolvedRef }:
                    break;

                default:
                    ctx.Error($"'@{attribute.Name}' takes namespaces and structs, which contain other members",
                        argument.Location);
                    break;
            }
        }

        foreach (var argument in arguments) Add(argument);
        return targets;
    }
}
