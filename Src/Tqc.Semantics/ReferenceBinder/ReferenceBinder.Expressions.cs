using Tqc.Core;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR;
using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers;
using Tqc.IR.References;

namespace Tqc.Semantics;

internal static partial class ReferenceBinder
{
    private static IrExpression BindExpression(BindingContext ctx, IrExpression expression)
    {
        switch (expression)
        {
            case IrReference { Reference: UnsolvedRef unsolved } reference:
                return BindName(ctx, reference, unsolved.Value);

            case IrIdentifier identifier:
                var asReference = new IrReference(identifier.Location, new UnsolvedRef(identifier.identifier));
                return BindName(ctx, asReference, identifier.identifier);

            case IrLocal local:
                return BindLocal(ctx, local);

            case IrAssign assign:
                return BindAssign(ctx, assign);

            case IrAccess access:
                return BindAccess(ctx, access);

            case IrCall call:
                return BindCall(ctx, call);

            case IrNew creation:
                return creation with
                {
                    Type = BindExpression(ctx, creation.Type),
                    ArgumentsList = BindArguments(ctx, creation.ArgumentsList),
                };

            case IrConvert conversion:
                return conversion with
                {
                    Expression = BindExpression(ctx, conversion.Expression),
                    toType = BindExpression(ctx, conversion.toType),
                };

            case IrBinary binary:
                return binary with { Left = BindExpression(ctx, binary.Left), Right = BindExpression(ctx, binary.Right) };

            case IrUnary unary:
                return BindUnary(ctx, unary);

            case IrIndex index:
                return index with
                {
                    Target = BindExpression(ctx, index.Target),
                    Indices = [.. index.Indices.Select(element => BindExpression(ctx, element))],
                };

            case IrArray array:
                return array with { Elements = [.. array.Elements.Select(element => BindExpression(ctx, element))] };

            case IrSliceTypeModifier slice:
                return slice with { subtype = BindExpression(ctx, slice.subtype) };

            case IrArrayTypeModifier arrayType:
                return arrayType with { Length = BindExpression(ctx, arrayType.Length), subtype = BindExpression(ctx, arrayType.subtype) };

            case IrMatch match:
                return match with
                {
                    Subject = BindExpression(ctx, match.Subject),
                    Arms =
                    [
                        ..match.Arms.Select(arm =>
                        {
                            // `case Foo f`: `f` is seen by the body of the arm only.
                            var patterns = arm.Patterns.Select(pattern => BindExpression(ctx, pattern)).ToArray();
                            ctx.PushFrame();
                            if (arm.Binding != null) ctx.Add(arm.Binding.name, new LocalReference(arm.Binding));
                            var body = arm.Body is IrBody block ? BindBranch(ctx, block) : (IrNode)BindExpression(ctx, (IrExpression)arm.Body);
                            ctx.PopFrame();
                            return arm with { Patterns = patterns, Body = body };
                        }),
                    ],
                };

            case IrRange range:
                return range with { From = BindExpression(ctx, range.From), To = BindExpression(ctx, range.To) };

            case IrAddressOf addressOf:
                return addressOf with { Operand = BindExpression(ctx, addressOf.Operand) };

            case IrStringConcat concat:
                return concat with { Parts = [.. concat.Parts.Select(part => BindExpression(ctx, part))] };

            default:
                return expression;
        }
    }

    /// <summary>
    /// `!` and `?` before a type are `Failable(T)` and `Nullable(T)`; before a value they are the operators.
    /// What the operand is only shows once it is bound, so this is decided here, as for a call.
    /// </summary>
    private static IrExpression BindUnary(BindingContext ctx, IrUnary unary)
    {
        var operand = BindExpression(ctx, unary.Operand);

        if (!unary.IsPostfix && CompilerStd.Types.PseudotypeOf(unary.Operator) is { } definition && TypeExpressions.Raw(operand) != null)
            return new IrReference(unary.Location, GenericInstanceTable.Shared.GetOrAdd(definition, [operand]).GetReference());

        return unary with { Operand = operand };
    }

    /// <summary>
    /// Applying a generic struct (`Integer(true, 32)`) creates an entry in the instance table, and the expression
    /// becomes a reference to that entry. The arguments are evaluated first, so `Integer(true, 8 * 4)` and
    /// `Integer(true, 32)` are the same entry, and each one has to fit its parameter. An argument that is not
    /// a constant (a type, a name) is kept as written.
    /// </summary>
    private static IrExpression BindCall(BindingContext ctx, IrCall call)
    {
        var callee = BindExpression(ctx, call.callee);
        var arguments = BindArguments(ctx, call.ArgumentsList);

        if (callee is IrReference { Reference: TypeMetaReference { Type: IStruct { Parameters.Count: > 0 } definition } })
        {
            var evaluated = CheckGenericArguments(ctx, definition, arguments, call.Location);
            if (evaluated == null) return call with { callee = callee, ArgumentsList = arguments };

            var instance = GenericInstanceTable.Shared.GetOrAdd(definition, [..evaluated]);
            return new IrReference(call.Location, instance.GetReference());
        }

        return call with { callee = callee, ArgumentsList = arguments };
    }

    /// <summary>The arguments with their constants evaluated; null (after reporting) if they do not fit the struct.</summary>
    private static IrExpression[]? CheckGenericArguments(
        BindingContext ctx, IStruct definition, IrArgumentsList list, SourceLocation location)
    {
        var parameters = definition.Parameters;
        if (list.arguments.Length != parameters.Count)
        {
            ctx.Error($"'{definition.Name}' takes {parameters.Count} argument(s), not {list.arguments.Length}", location);
            return null;
        }

        var result = new IrExpression[list.arguments.Length];
        var valid = true;

        for (var i = 0; i < result.Length; i++)
        {
            var argument = list.arguments[i];
            var constant = ComptimeEvaluator.TryEvaluate(argument, out var error);

            if (error != null)
            {
                ctx.Error(error, argument.Location);
                valid = false;
            }

            result[i] = constant ?? argument;

            if (constant != null && !FitsParameter(constant, parameters[i], out var expected))
            {
                ctx.Error($"The argument '{parameters[i].Name}' of '{definition.Name}' must be {expected}", argument.Location);
                valid = false;
            }
        }

        return valid ? result : null;
    }

    /// <summary>Whether a constant is what a parameter asks for; parameters of a type this cannot tell are accepted.</summary>
    private static bool FitsParameter(IrExpression constant, TqParameter parameter, out string expected)
    {
        expected = "";
        var type = TypeChecker.TypeFromExpression(parameter.Type);
        if (type == null) return true;

        switch (constant)
        {
            case IrComptimeBool:
                expected = $"'{TypeFormatter.Format(type)}'";
                return ConversionRules.IsStruct(type, CompilerStd.Types.Bool);

            case IrComptimeInt:
                expected = $"'{TypeFormatter.Format(type)}' and fit it";
                return ConversionRules.ClassifyLiteral(constant, type).Kind != MatchKind.None;

            case IrStringConstant:
                expected = $"'{TypeFormatter.Format(type)}'";
                return ConversionRules.IsStruct(type, CompilerStd.Types.String);

            default:
                expected = $"'{TypeFormatter.Format(type)}'";
                return false;
        }
    }

    private static IrArgumentsList BindArguments(BindingContext ctx, IrArgumentsList list) =>
        list with { arguments = [.. list.arguments.Select(argument => BindExpression(ctx, argument))] };

    private static IrExpression BindName(BindingContext ctx, IrReference reference, string name)
    {
        // Keyword types are checked before any scope, so they cannot be shadowed.
        var builtin = BuiltinTypes.TryResolve(name);
        if (builtin != null) return reference with { Reference = builtin };

        var resolved = ctx.TryGet(name, out var candidates);
        if (resolved != null)
        {
            ctx.CheckAccess(name, resolved, reference.Location);
            return reference with { Reference = resolved };
        }

        ctx.Error(candidates.Count > 1
                ? $"Reference to '{name}' is ambiguous. It could be: {string.Join(", ", candidates)}"
                : $"The name '{name}' does not exist in the current scope",
            reference.Location);
        return reference;
    }

    /// <summary>
    /// `a.b` where `a` is a namespace or a type with members becomes a direct reference to the member.
    /// Where `a` is a value, `b` depends on the type of `a`, so it stays unresolved for the type checker.
    /// </summary>
    private static IrExpression BindAccess(BindingContext ctx, IrAccess access)
    {
        var baseExpr = BindExpression(ctx, access.baseExpr);

        if (baseExpr is IrReference baseReference
            && baseReference.Reference is not UnsolvedRef
            && access.access.Reference is UnsolvedRef memberName)
        {
            var member = TryGetStaticMember(baseReference.Reference, memberName.Value, out var isContainer);
            if (member != null)
            {
                var memberReference = member.GetReference();
                ctx.CheckAccess(memberName.Value, memberReference, access.access.Location);
                return new IrReference(access.Location, memberReference);
            }

            // A failed base was already reported.
            if (isContainer)
                ctx.Error($"'{baseReference.Reference}' has no member named '{memberName.Value}'", access.access.Location);
        }

        return access with { baseExpr = baseExpr };
    }

    /// <param name="isContainer">
    /// False for values and types that do not list their members yet: a missing member is not an error there.
    /// </param>
    private static IMember? TryGetStaticMember(Ref container, string name, out bool isContainer)
    {
        switch (container)
        {
            case NamespaceReference { Namespace: var @namespace }:
                isContainer = true;
                return @namespace.TryGetMember(name);

            case TypeMetaReference { Type: IMemberContainer members }:
                isContainer = true;
                return members.TryGetMember(name);

            default:
                isContainer = false;
                return null;
        }
    }
}
