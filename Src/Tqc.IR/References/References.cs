using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers;

namespace Tqc.IR.References;

public abstract record Ref();
public interface ITypeRef;

public record UnsolvedRef(string Value) : Ref;
public abstract record MemberRef(IMember Member) : Ref;

public record StructTypeReference(IStruct Struct) : Ref, IBuiltinTypeRef
{
    public override string ToString() => Struct.Name;
}

public record ProtocolTypeReference(IProtocol Protocol) : Ref, IBuiltinTypeRef
{
    public override string ToString() => Protocol.Name;
}

public interface IBuiltinTypeRef : ITypeRef;

public record BooleanTypeRef() : Ref, IBuiltinTypeRef;

public record IntegerTypeRef(bool Signed, short Size) : Ref, IBuiltinTypeRef;

public record DefaultIntegerTypeRef(bool Signed) : Ref, IBuiltinTypeRef;

public record NativeIntegerTypeRef(bool Signed) : Ref, IBuiltinTypeRef;
public record FloatingTypeRef(FloatingTypeRef.FloatingPointType Type) : Ref, IBuiltinTypeRef
{
    public enum FloatingPointType { f16, f32, f64, f128 }
}

public record CharacterTypeRef() : Ref, IBuiltinTypeRef;

public record StringTypeRef() : Ref, IBuiltinTypeRef;

public record ReferenceTypeRef(ITypeRef Referred) : Ref, IBuiltinTypeRef;
