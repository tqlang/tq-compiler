using System.Diagnostics;
using Mono.Cecil;
using Tqc.Dotnet.ProgramMembers;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.References;

namespace Tqc.Dotnet.ModuleBuilder;

internal static class TypeTreeDumper
{
    /// <summary>Quando true, cada membro ganha um comentário com a assinatura do Cecil: [escopo]Tipo.</summary>
    public static bool ShowDotnetSignatures { get; set; }

    [Conditional("DEBUG")]
    public static void Dump(NamespaceNode ns, int depth = 0)
    {
        var pad = new string('\t', depth);
        Console.WriteLine($"{pad}[ns] {ns.Name} (types={ns.Types.Count}, children={ns.Namespaces.Count})");

        foreach (var type in ns.Types) Dump(type, depth + 1);
        foreach (var child in ns.Namespaces.Values) Dump(child, depth + 1);
    }

    private static void Dump(TypeNode node, int depth)
    {
        var pad = new string('\t', depth);
        Console.WriteLine($"{pad}- [{node.Kind}] {node.Name} -> {node.Reference.Scope?.Name} (members={node.Members.Count})");

        foreach (var member in node.Members) Console.WriteLine($"{pad}\t{Describe(member)}");
        foreach (var nested in node.Nested) Dump(nested, depth + 1);
    }

    // ───────────────────────── Membros (assinatura em Tq) ─────────────────────────

    private static string Describe(IDotnetMember member) => member switch
    {
        DotnetField field =>
            $"field {(field.IsStatic ? "static " : "")}{field.Name}: {Tq(field.Type)}"
            + (field.IsConstant ? $" = {field.Constant}" : "")
            + Dotnet($"{Show(field.Reference.FieldType)}"),

        DotnetMethod method =>
            $"{(method.IsConstructor ? "ctor" : "method")} {(method.IsStatic ? "static " : "")}{method.Name}("
            + string.Join(", ", method.ParameterTypes.Select((type, i) => $"{method.Reference.Parameters[i].Name}: {Tq(type)}"))
            + $") -> {(method.ReturnType == null ? "void" : Tq(method.ReturnType))}"
            + Dotnet($"{Show(method.Reference.ReturnType)} ({string.Join(", ", method.Reference.Parameters.Select(p => Show(p.ParameterType)))})"),

        DotnetProperty property =>
            $"property {property.Name}"
            + (property.IsIndexer ? $"[{string.Join(", ", property.IndexParameterTypes.Select(Tq))}]" : "")
            + $": {Tq(property.Type)} {{ {(property.Getter != null ? "get; " : "")}{(property.Setter != null ? "set; " : "")}}}",

        _ => member.ToString() ?? "?",
    };

    // ───────────────────────── Tipos ─────────────────────────

    private static string Tq(ITypeRef type) => type switch
    {
        DotnetCharacterTypeRef               => "char16",
        _ when CompilerStd.Types.IsRef(type, out var referred) => $"ref({Tq(referred)})",
        StructTypeReference { Struct: PrimitiveStruct primitive } => primitive.Name,
        StructTypeReference structure        => NameOf(structure.Struct),
        ProtocolTypeReference protocol       => NameOf(protocol.Protocol),
        UnsupportedTypeRef unsupported       => $"?{unsupported.Name}",
        _                                    => type.ToString() ?? "?",
    };

    // Depende do ToString dos Dotnet* (DotnetClass, DotnetStruct, ...): se eles não o sobrescrevem,
    // isto imprime o nome da classe C# em vez do nome do tipo.
    private static string NameOf(object dotnetType) => dotnetType.ToString() ?? dotnetType.GetType().Name;

    private static string Dotnet(string text) => ShowDotnetSignatures ? $"   // {text}" : "";

    /// <summary>[escopo]NomeCompleto — o escopo é o que vira o AssemblyRef no binário.</summary>
    private static string Show(TypeReference type) =>
        type is GenericParameter ? type.Name : $"[{type.Scope?.Name}]{type.FullName}";
}