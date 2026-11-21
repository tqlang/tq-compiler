using Tqc.Core;
using Tqc.IR.Nodes;

namespace Tqc.IR.ProgramMembers.Tq;

public class TqFunction(string name, SourceLocation location) : IMethod, ITqMember
{
    public IReadOnlyList<TqAttribute> Attributes { get; set; } = [];
    public MemberAccess Access { get; set; } = MemberAccess.Default;
    public SourceLocation SourceLocation => location;
    public string Name => name;
    public TqParameter[] Parameters { get; set; }
    public IrExpression? ReturnType { get; set; }

    public IrBody? Body { get; set; }

    /// <summary>The struct it is declared in; null for a function of a namespace or of a typedef.</summary>
    public IObjectType? Owner { get; set; }

    public bool IsStatic => Has(BuiltinAttribute.Static);
    public bool IsConstructor => this is TqConstructor;

    /// <summary>No body in an `@abstract` struct or a `protocol`.</summary>
    public bool IsAbstract => Body == null && Owner is { IsAbstract: true };

    public bool CanBeOverridden => IsAbstract || Has(BuiltinAttribute.Virtual) || Has(BuiltinAttribute.Override);

    private bool Has(BuiltinAttribute kind) =>
        Attributes.OfType<TqBuiltinAttribute>().Any(attribute => attribute.Kind == kind);
    
    public override string ToString() => $"func {Name}";
}
