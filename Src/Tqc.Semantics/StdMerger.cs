using Tqc.Core;
using Tqc.Diagnostics;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.ProgramMembers.Tq;

namespace Tqc.Semantics;

/// <summary>
/// Merges the `Std` module of the project with the one the compiler creates (CompilerStd).
/// Namespaces with the same name are merged recursively; any other name declared on both sides is an error.
/// </summary>
internal static class StdMerger
{
    public static IModule Merge(IModule userStd, DiagnosticBag diagnostics)
    {
        var root = MergeNamespaces(
            CompilerStd.Module.RootNamespace, userStd.RootNamespace, userStd.RootNamespace.Name, diagnostics);
        return new PrimitiveModule(root);
    }

    private static INamespace MergeNamespaces(
        INamespace compiler, INamespace user, string path, DiagnosticBag diagnostics)
    {
        var members = compiler.GetAllMembers().ToDictionary(member => member.Name);

        foreach (var member in user.GetAllMembers())
        {
            if (!members.TryGetValue(member.Name, out var existing))
            {
                members.Add(member.Name, member);
                continue;
            }

            var qualifiedName = $"{path}.{member.Name}";
            if (existing is INamespace compilerChild && member is INamespace userChild)
            {
                members[member.Name] = MergeNamespaces(compilerChild, userChild, qualifiedName, diagnostics);
                continue;
            }

            diagnostics.ReportError(new BindingError(
                $"'{qualifiedName}' is already declared by the compiler and cannot be redeclared",
                LocationOf(member)));
        }

        return new PrimitiveNamespace(user.Name, members.Values) { Original = user };
    }

    private static SourceLocation? LocationOf(IMember member) => member switch
    {
        TqStruct structure => structure.SourceLocation,
        TqTypedef typedef => typedef.SourceLocation,
        TqFunction function => function.SourceLocation,
        TqField field => field.SourceLocation,
        TqFunctionGroup group => group.FirstOrDefault()?.SourceLocation,
        _ => null,
    };
}
