using Mono.Cecil;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.References;

namespace Tqc.Dotnet.ModuleBuilder;

/// <summary>
/// Formas de tipo .NET que a IR ainda não sabe representar (arrays, ponteiros, generics, delegates...).
/// Não derruba o build: o membro continua existindo e a referência de Cecil dele segue válida para o emissor.
/// </summary>
public sealed record UnsupportedTypeRef(string Name) : Ref, ITypeRef;
public sealed record DotnetCharacterTypeRef() : Ref, ITypeRef;

/// <summary>
/// Único lugar que conhece o mapeamento tipo .NET -> ITypeRef do Tq.
/// <paramref name="lookup"/> devolve o nó do builder para um tipo nomeado (ou null se não resolvível).
/// </summary>
sealed internal class TqTypeTranslator(Func<TypeReference, TypeNode?> lookup)
{
    // Every primitive is a member of Std.Types, created by the compiler. Sizes are in bits.
    private static readonly StdTypes Std = CompilerStd.Types;

    private static readonly Dictionary<string, ITypeRef> Primitives = new()
    {
        ["System.Boolean"] = new StructTypeReference(Std.Bool),

        ["System.SByte"]   = new StructTypeReference(Std.IntegerOf(signed: true,  bitSize: 8)),
        ["System.Byte"]    = new StructTypeReference(Std.IntegerOf(signed: false, bitSize: 8)),
        ["System.Int16"]   = new StructTypeReference(Std.IntegerOf(signed: true,  bitSize: 16)),
        ["System.UInt16"]  = new StructTypeReference(Std.IntegerOf(signed: false, bitSize: 16)),
        ["System.Int64"]   = new StructTypeReference(Std.IntegerOf(signed: true,  bitSize: 64)),
        ["System.UInt64"]  = new StructTypeReference(Std.IntegerOf(signed: false, bitSize: 64)),
        ["System.Int128"]  = new StructTypeReference(Std.IntegerOf(signed: true,  bitSize: 128)),
        ["System.UInt128"] = new StructTypeReference(Std.IntegerOf(signed: false, bitSize: 128)),

        ["System.Int32"]   = new StructTypeReference(Std.Natural(signed: true)),
        ["System.UInt32"]  = new StructTypeReference(Std.Natural(signed: false)),

        ["System.IntPtr"]  = new StructTypeReference(Std.Native(signed: true)),
        ["System.UIntPtr"] = new StructTypeReference(Std.Native(signed: false)),

        ["System.Char"]    = new DotnetCharacterTypeRef(),

        ["System.Half"]    = new StructTypeReference(Std.FloatOf(16)),
        ["System.Single"]  = new StructTypeReference(Std.FloatOf(32)),
        ["System.Double"]  = new StructTypeReference(Std.FloatOf(64)),

        ["System.String"]  = new StructTypeReference(Std.String),

        // Decimal stays out: it remains a struct.
    };

    /// <summary>Tipo de retorno: null significa void (a IR não tem referência para void).</summary>
    public ITypeRef? TranslateReturn(TypeReference type) =>
        type.FullName == "System.Void" ? null : Translate(type);

    /// <summary>Tipo de valor (campo, parâmetro, propriedade). Nunca devolve null.</summary>
    public ITypeRef Translate(TypeReference type) => type switch
    {
        // ref/out: uma referência ao tipo
        ByReferenceType byRef => CompilerStd.Types.RefOf(Translate(byRef.ElementType)),

        // modreq/modopt e pinned só importam para o emissor
        RequiredModifierType modifier => Translate(modifier.ElementType),
        OptionalModifierType modifier => Translate(modifier.ElementType),
        PinnedType pinned             => Translate(pinned.ElementType),

        // Sem representação na IR por enquanto
        GenericParameter or ArrayType or PointerType or GenericInstanceType or FunctionPointerType or SentinelType
            => Unsupported(type),

        _ => TranslateNamed(type),
    };

    private ITypeRef TranslateNamed(TypeReference type)
    {
        if (Primitives.TryGetValue(type.FullName, out var primitive)) return primitive;

        var node = lookup(type);
        if (node?.DotnetType is not { } dotnetType) return Unsupported(type);

        return node.Kind switch
        {
            TypeKind.Class     => CompilerStd.Types.RefOf(new StructTypeReference((IStruct)dotnetType)),
            TypeKind.Struct    => new StructTypeReference((IStruct)dotnetType),
            TypeKind.Enum      => new StructTypeReference((IStruct)dotnetType),
            TypeKind.Interface => new ProtocolTypeReference((IProtocol)dotnetType),

            _ => Unsupported(type), // StaticClass e Delegate não são tipos de valor
        };
    }

    private static UnsupportedTypeRef Unsupported(TypeReference type) =>
        new(type is GenericParameter ? type.Name : type.FullName);
}