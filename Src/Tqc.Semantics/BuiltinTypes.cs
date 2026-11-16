using System.Text.RegularExpressions;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.References;

namespace Tqc.Semantics;

/// <summary>
/// Keywords for types, like `int` in C#. Each one is an alias of a member of `Std.Types`, created by the compiler
/// (the original member needs an explicit import). They are checked before any scope, so they cannot be shadowed.
/// </summary>
internal static class BuiltinTypes
{
    private const int MaxIntegerBits = 256;

    private static readonly Regex SizedInteger = new(@"^([iu])([1-9][0-9]*)$", RegexOptions.Compiled);

    public static Ref? TryResolve(string name)
    {
        var types = CompilerStd.Types;

        switch (name)
        {
            case "bool":   return types.Bool.GetReference();
            case "char":   return types.Char.GetReference();
            case "string": return types.String.GetReference();

            case "int":  return types.Natural(true).GetReference();
            case "uint": return types.Natural(false).GetReference();
            case "iptr": return types.Native(true).GetReference();
            case "uptr": return types.Native(false).GetReference();
            // The size of a byte depends on the target; assumes 8 bits for now.
            case "byte": return types.IntegerOf(false, 8).GetReference();

            case "f16":  return types.FloatOf(16).GetReference();
            case "f32":  return types.FloatOf(32).GetReference();
            case "f64":  return types.FloatOf(64).GetReference();
            case "f80":  return types.FloatOf(80).GetReference();
            case "f128": return types.FloatOf(128).GetReference();

            case "void":     return types.Void.GetReference();
            case "opaque":   return types.Opaque.GetReference();
            case "noreturn": return types.NoReturn.GetReference();
            case "label":    return types.Label.GetReference();

            // Comptime parameter types, e.g. `func assert(type T, anytype other)`
            case "type":    return types.Type.GetReference();
            case "anytype": return types.AnyType.GetReference();
        }

        var match = SizedInteger.Match(name);
        if (match.Success && int.TryParse(match.Groups[2].Value, out var bits) && bits <= MaxIntegerBits)
            return types.IntegerOf(match.Groups[1].Value == "i", bits).GetReference();

        return null;
    }
}
