using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;

namespace Tqc.Semantics;

/// <summary>
/// `const` is a modifier of the type: `const T x` is `let Const(T) x` and `const x` is `let Const(AnyType) x`.
/// The values of a constant are the ones of `T` (which, for the second form, comes from its value); what the
/// type says is that it cannot be assigned.
/// </summary>
internal sealed partial class TypeChecker
{
    /// <summary>The types that fields declared with `const x = v` got from their values.</summary>
    private readonly Dictionary<TqField, ITypeRef> _inferredConstants = [];

    private static bool IsConstInstance(IStruct structure, out IrExpression argument)
    {
        argument = null!;
        if (structure is not GenericInstance { Arguments: [var only] } instance
            || !ReferenceEquals(instance.Definition, CompilerStd.Types.Const)) return false;

        argument = only;
        return true;
    }

    private static bool IsConst(ITypeRef? type, out IrExpression argument)
    {
        argument = null!;
        return type is StructTypeReference { Struct: var structure } && IsConstInstance(structure, out argument);
    }

    /// <summary>`Const(AnyType)`: the type of the constant is the type of its value.</summary>
    private static bool IsInferredArgument(IrExpression argument) =>
        argument is IrReference { Reference: TypeMetaReference { Type: var type } } && ReferenceEquals(type, CompilerStd.Types.AnyType);

    /// <summary>The type expression of `T` in `Const(T)`; any other type expression as it is.</summary>
    private static IrExpression? WithoutConst(IrExpression? typeExpression) =>
        typeExpression is IrReference { Reference: TypeMetaReference { Type: IStruct structure } } && IsConstInstance(structure, out var argument)
            ? argument
            : typeExpression;

    private static bool IsConstDeclaration(IrExpression? typeExpression) =>
        typeExpression != null && IsConst(TypeFromExpression(typeExpression), out _);

    /// <summary>A declaration takes its type from the value when it has no type or has `Const(AnyType)`.</summary>
    private static bool NeedsInferredType(IrLocal local)
    {
        if (local.type == null) return true;
        return IsConst(TypeFromExpression(local.type), out var argument) && IsInferredArgument(argument);
    }

    /// <summary>The type of the values of a declaration, which is not `Const(T)` but `T`.</summary>
    private ITypeRef? ValueTypeOf(ITypeRef? declared, ITypeRef? inferred)
    {
        if (!IsConst(declared, out var argument)) return declared;
        return IsInferredArgument(argument) ? inferred : TypeFromExpression(argument);
    }

    private ITypeRef? LocalValueType(IrLocal local)
    {
        var inferred = types.Get(local);
        return local.type == null ? inferred : ValueTypeOf(TypeFromExpression(local.type), inferred);
    }

    private ITypeRef? FieldValueType(TqField field) =>
        field.Type == null ? null : ValueTypeOf(TypeFromExpression(field.Type), _inferredConstants.GetValueOrDefault(field));

    /// <summary>A literal has no size: a variable that takes its type from one gets the default.</summary>
    private static ITypeRef DefaultTypeOf(ITypeRef sourceType) => sourceType switch
    {
        ComptimeIntegerTypeRef => new StructTypeReference(CompilerStd.Types.Natural(true)),
        ComptimeDecimalTypeRef => new StructTypeReference(CompilerStd.Types.FloatOf(64)),
        _ => sourceType,
    };

    /// <summary>The name of the constant that an assignment would write to, if the target is one.</summary>
    private static string? ConstantNameOf(IrExpression target) => target switch
    {
        IrReference { Reference: LocalReference { Local: { type: { } type } local } } when IsConstDeclaration(type) => local.name,
        IrReference { Reference: FieldReference { Field: TqField { Type: { } type } field } } when IsConstDeclaration(type) => field.Name,
        IrAccess { access.Reference: FieldReference { Field: TqField { Type: { } type } field } } when IsConstDeclaration(type) => field.Name,
        _ => null,
    };
}
