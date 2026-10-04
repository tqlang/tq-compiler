using System.Diagnostics;
using Mono.Cecil;
using Tqc.Dotnet.ModuleBuilder;
using Tqc.Dotnet.ProgramMembers;
using Tqc.IR.References;

namespace Tqc.Dotnet;

internal enum TypeKind { Class, StaticClass, Struct, Enum, Interface, Delegate }

/// <summary>
/// Um tipo conhecido pelo builder.
/// <see cref="Reference"/> é como o tipo é endereçado "de fora" (é ela que vai para o IL / para os Dotnet*)
/// e <see cref="Definition"/> é onde o tipo realmente está definido (usada só para inspecionar a estrutura).
/// As duas são independentes: num tipo exportado (type forward), Reference.Scope é o assembly que exporta
/// e Definition.Module.Assembly é o que define.
/// </summary>
sealed internal class TypeNode(string name, TypeKind kind, TypeReference reference, TypeDefinition definition, bool isExternal)
{
    public string Name { get; } = name;
    public TypeKind Kind { get; } = kind;

    public TypeReference Reference { get; } = reference;
    public TypeDefinition Definition { get; } = definition;

    /// <summary>True quando o tipo não pertence aos assemblies do módulo e só foi descoberto por referência.</summary>
    public bool IsExternal { get; } = isExternal;

    /// <summary>Null para delegates (ainda não suportados).</summary>
    public IDotnetType? DotnetType { get; init; }

    public ITypeRef? BaseType { get; set; }
    public List<TypeNode> Nested { get; } = [];

    /// <summary>Campos, métodos, construtores e propriedades visíveis (public/protected). Vazio em tipos externos.</summary>
    public List<IDotnetMember> Members { get; } = [];
}

sealed internal class NamespaceNode(string name)
{
    public string Name { get; } = name;
    public Dictionary<string, NamespaceNode> Namespaces { get; } = new(StringComparer.Ordinal);
    public List<TypeNode> Types { get; } = [];

    public NamespaceNode GetOrAddNamespace(string childName)
    {
        if (Namespaces.TryGetValue(childName, out var child)) return child;

        child = new NamespaceNode(childName);
        Namespaces.Add(childName, child);
        return child;
    }
}
