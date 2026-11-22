using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.References;

namespace Tqc.Semantics;

/// <summary>Writes types the way the language does, for messages: `i32`, `[]string`.</summary>
internal static class TypeFormatter
{
    public static string Format(ITypeRef? type)
    {
        var types = CompilerStd.Types;

        switch (type)
        {
            case null:
                return "?";

            case ComptimeIntegerTypeRef:
                return "integer literal";

            case ComptimeDecimalTypeRef:
                return "decimal literal";

            case NullTypeRef:
                return "null";

            case var _ when types.IsRef(type, out var referred):
                return "ref " + Format(referred);

            case var _ when types.IsFailable(type, out var result):
                return "!" + Format(result);

            case var _ when types.IsNullable(type, out var value):
                return "?" + Format(value);

            case GenericParameterTypeRef generic:
                return generic.Parameter.Name;

            case SliceTypeRef slice:
                return "[]" + Format(slice.Element);

            case ArrayTypeRef array:
                return $"[{array.Length}]{Format(array.Element)}";

            case StructTypeReference { Struct: GenericInstance instance }:
                return FormatInstance(instance);

            case StructTypeReference { Struct: var structure } when structure == types.Bool: return "bool";
            case StructTypeReference { Struct: var structure } when structure == types.Char: return "char";
            case StructTypeReference { Struct: var structure } when structure == types.String: return "string";
            case StructTypeReference { Struct: var structure } when structure == types.Void: return "void";
            case StructTypeReference { Struct: var structure } when structure == types.AnyType: return "anytype";
            case StructTypeReference { Struct: var structure } when structure == types.Type: return "type";

            case StructTypeReference { Struct: var structure }:
                return structure.Name;

            default:
                return type.ToString() ?? "?";
        }
    }

    private static string FormatInstance(GenericInstance instance)
    {
        var type = new StructTypeReference(instance);

        if (ConversionRules.TryInteger(type, out var signed, out var bits)) return $"{(signed ? "i" : "u")}{bits}";
        if (ConversionRules.TryNatural(type, out signed)) return signed ? "int" : "uint";
        if (ConversionRules.TryNative(type, out signed)) return signed ? "iptr" : "uptr";
        if (ConversionRules.TryFloat(type, out bits)) return $"f{bits}";

        return instance.Name;
    }
}
