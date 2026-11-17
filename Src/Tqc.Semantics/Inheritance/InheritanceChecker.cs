using Tqc.Core;
using Tqc.Diagnostics;
using Tqc.Dotnet.ProgramMembers;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Tq;

namespace Tqc.Semantics;

/// <summary>
/// Checks the object-oriented rules of the structs written in Tq once `extends` is bound. What it extends can be
/// written in any language: it is seen through <see cref="IObjectType"/> and <see cref="IMethod"/>, which say
/// whether a type is abstract, an interface or final and whether a method can be overridden, the same way
/// for Tq (attributes) and for .NET (metadata).
/// <list type="bullet">
/// <item>A final type cannot be extended; an `@interface` has no constructors and only extends interfaces.</item>
/// <item>`@virtual` and `@override` belong to functions of a struct that are not `@static`.</item>
/// <item>`@override` replaces a method of the same name and signature that can be overridden, and needs a body.</item>
/// <item>A function that has the signature of a method that can be overridden must say `@override`.</item>
/// <item>A struct that is not abstract implements every abstract method it inherits.</item>
/// </list>
/// A struct extends an interface to implement it: the language has one `extends` per struct.
/// </summary>
internal sealed class InheritanceChecker(DiagnosticBag diagnostics)
{
    public static void Check(TqNamespace root, DiagnosticBag diagnostics) => new InheritanceChecker(diagnostics).CheckNamespace(root);

    private void Error(string message, SourceLocation location) => diagnostics.ReportError(new TypeError(message, location));

    private void CheckNamespace(TqNamespace namespaceNode)
    {
        foreach (var fileScope in namespaceNode.FileScopes)
        foreach (var member in fileScope.Members.Values)
            CheckMember(member, null);

        foreach (var child in namespaceNode.Namespaces.Values) CheckNamespace(child);
    }

    private void CheckMember(IMember member, TqStruct? parent)
    {
        switch (member)
        {
            case TqStruct structure:
                CheckStruct(structure);
                break;

            case TqFunctionGroup group:
                foreach (var function in group) CheckPlacement(function, parent);
                break;

            case TqFunction function:
                CheckPlacement(function, parent);
                break;

            case TqTypedef typedef:
                foreach (var inner in typedef.Members.Values) CheckMember(inner, null);
                break;
        }
    }

    private static bool Has(ITqMember member, BuiltinAttribute kind) =>
        member.Attributes.OfType<TqBuiltinAttribute>().Any(attribute => attribute.Kind == kind);

    /// <summary>Functions that carry `@virtual` or `@override` have to be in a struct and cannot be static.</summary>
    private void CheckPlacement(TqFunction function, TqStruct? parent)
    {
        if (!Has(function, BuiltinAttribute.Virtual) && !Has(function, BuiltinAttribute.Override)) return;

        if (parent == null)
            Error($"'{function.Name}' can only be '@virtual' or '@override' as a function of a struct", function.SourceLocation);
        else if (function.IsStatic)
            Error($"'{function.Name}' is '@static' and cannot be '@virtual' or '@override'", function.SourceLocation);
    }

    // ---- Structs

    private void CheckStruct(TqStruct structure)
    {
        CheckExtends(structure);

        if (structure.IsInterface && (structure.Constructors.Count > 0 || structure.Destructor != null))
            Error($"The interface '{structure.Name}' cannot have a constructor or a destructor", structure.SourceLocation);

        foreach (var member in structure.Members.Values)
        {
            CheckMember(member, structure);

            foreach (var function in MethodsOf(member).OfType<TqFunction>()) CheckOverriding(structure, function);
        }

        if (!structure.IsAbstract) CheckImplemented(structure);
    }

    private void CheckExtends(TqStruct structure)
    {
        if (structure.Base is not IObjectType baseType) return;
        var baseName = NameOf(baseType);

        if (baseType.IsFinal)
            Error($"'{structure.Name}' cannot extend '{baseName}', which is final", structure.SourceLocation);

        if (structure.IsInterface && !baseType.IsInterface)
            Error($"The interface '{structure.Name}' can only extend interfaces, not '{baseName}'", structure.SourceLocation);
    }

    // ---- Members of any type

    private static string NameOf(IMemberContainer container) => (container as IMember)?.Name ?? "?";

    private static IEnumerable<IMethod> MethodsOf(IMember member) => member switch
    {
        TqFunctionGroup group => group.Cast<IMethod>(),
        DotnetMethodGroup group => group.Methods.Cast<IMethod>(),
        IMethod method => [method],
        _ => [],
    };

    /// <summary>
    /// What a member implements: its methods and, for a property, its accessors. An abstract property is
    /// implemented by a property of the same name that has the accessor.
    /// </summary>
    private static IEnumerable<IMethod> ImplementationsOf(IMember member) => member switch
    {
        TqProperty property => new[] { property.GetterFunction, property.SetterFunction }.OfType<IMethod>(),
        _ => MethodsOf(member),
    };

    /// <summary>The instance methods of a container with this name, whatever it is written in.</summary>
    private static IEnumerable<IMethod> MethodsNamed(IMemberContainer container, string name) => container.OwnMembers
        .SelectMany(ImplementationsOf)
        .Where(method => method.Name == name && !method.IsStatic && !method.IsConstructor);

    /// <summary>The ancestors of the struct, nearest first, without repeating one (a cycle was reported already).</summary>
    private static IEnumerable<IMemberContainer> Ancestors(TqStruct structure)
    {
        HashSet<IMemberContainer> visited = [structure];
        for (var current = structure.Base; current != null && visited.Add(current); current = current.Base)
            yield return current;
    }

    // ---- Overriding

    private void CheckOverriding(TqStruct structure, TqFunction function)
    {
        var signature = FunctionSignature.Of(function)!;
        var found = FindInAncestors(structure, function.Name, signature);

        if (Has(function, BuiltinAttribute.Override))
        {
            if (found == null)
            {
                Error($"'{function.Name}' is marked '@override' but nothing that '{structure.Name}' extends has a function " +
                      $"named '{function.Name}' with the parameters {signature}", function.SourceLocation);
                return;
            }

            if (!found.Value.Method.CanBeOverridden)
                Error($"'{function.Name}' overrides a function of '{NameOf(found.Value.Owner)}' that is not '@virtual'",
                    function.SourceLocation);

            if (function.Body == null)
                Error($"'{function.Name}' overrides a function and needs a body", function.SourceLocation);

            return;
        }

        if (found is { Method.CanBeOverridden: true } hidden)
            Error($"'{function.Name}' has the signature of '{NameOf(hidden.Owner)}.{function.Name}', which can be overridden; " +
                  "mark it '@override'", function.SourceLocation);
    }

    /// <summary>The nearest method of what the struct extends that has this name and the same signature.</summary>
    private static (IMethod Method, IMemberContainer Owner)? FindInAncestors(
        TqStruct structure, string name, FunctionSignature signature)
    {
        foreach (var ancestor in Ancestors(structure))
        foreach (var candidate in MethodsNamed(ancestor, name))
        {
            if (FunctionSignature.Of(candidate) is { } other && other.IsSameAs(signature)) return (candidate, ancestor);
        }

        return null;
    }

    // ---- Implementing

    /// <summary>A struct that can be created has an implementation for every abstract method it inherits.</summary>
    private void CheckImplemented(TqStruct structure)
    {
        List<IMemberContainer> chain = [structure];

        foreach (var ancestor in Ancestors(structure))
        {
            var required = ancestor.OwnMembers
                .SelectMany(ImplementationsOf)
                .Where(method => !method.IsStatic && !method.IsConstructor && method.IsAbstract);

            foreach (var method in required)
            {
                var signature = FunctionSignature.Of(method);
                if (signature == null) continue;

                var implemented = chain.Any(descendant => MethodsNamed(descendant, method.Name)
                    .Any(candidate => !candidate.IsAbstract && FunctionSignature.Of(candidate)!.IsSameAs(signature)));

                if (!implemented)
                    Error($"'{structure.Name}' must implement '{method.Name}{signature}' of '{NameOf(ancestor)}', or be '@abstract'",
                        structure.SourceLocation);
            }

            chain.Add(ancestor);
        }
    }
}
