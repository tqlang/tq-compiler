using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.ProgramMembers.Tq;

namespace Tqc.Semantics;

/// <summary>A function of Tq and the struct or typedef it is declared in, if any.</summary>
internal readonly record struct ScannedFunction(TqFunction Function, IMemberContainer? Owner);

/// <summary>Finds every function of Tq below a namespace, including the ones inside structs and typedefs.</summary>
internal static class FunctionScanner
{
    public static IEnumerable<ScannedFunction> All(INamespace root)
    {
        // Only Tq code has attributes; the namespaces of .NET modules can be huge and have none.
        if (root is not (TqNamespace or PrimitiveNamespace)) yield break;

        foreach (var member in root.GetAllMembers())
        foreach (var function in Of(member, null))
            yield return function;
    }

    private static IEnumerable<ScannedFunction> Of(IMember member, IMemberContainer? owner)
    {
        switch (member)
        {
            case INamespace child:
                foreach (var function in All(child)) yield return function;
                break;

            case TqFunctionGroup group:
                foreach (var function in group) yield return new ScannedFunction(function, owner);
                break;

            case TqFunction function:
                yield return new ScannedFunction(function, owner);
                break;

            case TqStruct structure:
                foreach (var inner in structure.Members.Values)
                foreach (var function in Of(inner, structure))
                    yield return function;
                break;

            case TqTypedef typedef:
                foreach (var inner in typedef.Members.Values)
                foreach (var function in Of(inner, typedef))
                    yield return function;
                break;
        }
    }
}
