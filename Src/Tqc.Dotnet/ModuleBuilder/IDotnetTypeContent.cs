using Tqc.Dotnet.ProgramMembers;
using Tqc.IR.References;

namespace Tqc.Dotnet.ModuleBuilder;

/// <summary>
/// Conteúdo de um tipo .NET, anexado pelo builder numa passada final (depois que todos os tipos existem),
/// o que resolve referências circulares (um método que retorna o próprio tipo, por exemplo).
/// Faça DotnetClass, DotnetStruct, DotnetEnum, DotnetInterface e DotnetStaticClass implementarem isto;
/// enquanto não implementarem, o builder simplesmente não anexa nada a eles.
/// </summary>
public interface IDotnetTypeContent
{
    ITypeRef? BaseType { get; set; }
    IReadOnlyList<IDotnetMember> Members { get; set; }
    IReadOnlyList<IDotnetType> NestedTypes { get; set; }
}

// Implementação sugerida, a copiar nas classes acima:
//
//     public ITypeRef? BaseType { get; set; }
//     public IReadOnlyList<IDotnetMember> Members { get; set; } = [];
//     public IReadOnlyList<IDotnetType> NestedTypes { get; set; } = [];
