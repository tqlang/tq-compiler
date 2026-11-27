using Tqc.Diagnostics;
using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;

namespace Tqc.Semantics;

/// <summary>
/// Two functions of the same name in the same namespace, struct or typedef (or two constructors of a struct) cannot take
/// the same parameters and return the same type: no call could choose between them. Parameter names and attributes do not
/// tell them apart. Overloads that only differ in what they return are allowed.
///
/// A type that is not known here is never the same as another, so no error is reported without certainty.
/// </summary>
internal sealed class DuplicateFunctionChecker(DiagnosticBag diagnostics)
{
    public static void Check(TqNamespace root, DiagnosticBag diagnostics) =>
        new DuplicateFunctionChecker(diagnostics).CheckNamespace(root);

    private void CheckNamespace(TqNamespace namespaceNode)
    {
        foreach (var fileScope in namespaceNode.FileScopes)
        foreach (var member in fileScope.Members.Values)
            CheckMember(member);

        foreach (var child in namespaceNode.Namespaces.Values) CheckNamespace(child);
    }

    private void CheckMember(IMember member)
    {
        switch (member)
        {
            case TqFunctionGroup group:
                CheckOverloads(group.Functions, $"The function '{group.Name}'");
                break;

            case TqStruct structure:
                CheckOverloads(structure.Constructors, $"A constructor of '{structure.Name}'");
                foreach (var inner in structure.Members.Values) CheckMember(inner);
                break;

            case TqTypedef typedef:
                foreach (var inner in typedef.Members.Values) CheckMember(inner);
                break;
        }
    }

    private void CheckOverloads(IReadOnlyList<TqFunction> functions, string subject)
    {
        for (var i = 1; i < functions.Count; i++)
        {
            var function = functions[i];
            var first = functions.Take(i).FirstOrDefault(earlier => HaveSameSignature(earlier, function));
            if (first == null) continue;

            var signature = FunctionSignature.Of(function)!;
            var written = function is TqConstructor
                ? $"({string.Join(", ", signature.Parameters.Select(TypeFormatter.Format))})"
                : signature.ToString();

            diagnostics.ReportError(new TypeError(
                $"{subject} is already declared with the signature {written}, at {first.SourceLocation}",
                function.SourceLocation));
        }
    }

    private static bool HaveSameSignature(TqFunction a, TqFunction b)
    {
        if (a.Parameters.Length != b.Parameters.Length) return false;

        for (var i = 0; i < a.Parameters.Length; i++)
            if (!SameType(a.Parameters[i].Type, b.Parameters[i].Type, a, b))
                return false;

        if (a.ReturnType == null && b.ReturnType == null) return true;

        // No return type and `void` written are the same.
        var first = FunctionSignature.Of(a)!.Return;
        var second = FunctionSignature.Of(b)!.Return;
        return first != null && second != null && SameType(first, second, a, b);
    }

    private static bool SameType(IrExpression a, IrExpression b, TqFunction first, TqFunction second) =>
        TypeChecker.TypeFromExpression(a) is { } left && TypeChecker.TypeFromExpression(b) is { } right
        && SameType(left, right, first, second);

    private static bool SameType(ITypeRef a, ITypeRef b, TqFunction first, TqFunction second)
    {
        switch (a, b)
        {
            // `f(type T, T value)` and `f(type U, U value)` are the same: a type parameter is told by its position.
            case (GenericParameterTypeRef left, GenericParameterTypeRef right):
                var position = Array.IndexOf(first.Parameters, left.Parameter);
                return position == Array.IndexOf(second.Parameters, right.Parameter)
                       && (position >= 0 || left.Equals(right));

            case (SliceTypeRef left, SliceTypeRef right):
                return SameType(left.Element, right.Element, first, second);

            case (ArrayTypeRef left, ArrayTypeRef right):
                return left.Length == right.Length && SameType(left.Element, right.Element, first, second);

            case (StructTypeReference { Struct: GenericInstance left }, StructTypeReference { Struct: GenericInstance right }):
                return ReferenceEquals(left.Definition, right.Definition)
                       && left.Arguments.Length == right.Arguments.Length
                       && left.Arguments.Zip(right.Arguments).All(pair => SameArgument(pair.First, pair.Second, first, second));

            default:
                return a.Equals(b);
        }
    }

    /// <summary>An argument of a generic type: a type, or a value known while compiling.</summary>
    private static bool SameArgument(IrExpression a, IrExpression b, TqFunction first, TqFunction second) => (a, b) switch
    {
        (IrComptimeInt left, IrComptimeInt right) => left.BigInteger == right.BigInteger,
        (IrComptimeBool left, IrComptimeBool right) => left.Value == right.Value,
        (IrStringConstant left, IrStringConstant right) => left.Value == right.Value,
        _ => TypeExpressions.Raw(a) is { } left && TypeExpressions.Raw(b) is { } right && SameType(left, right, first, second),
    };
}
