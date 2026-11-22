using Tqc.Core;
using Tqc.IR.Nodes;

namespace Tqc.IR.ProgramMembers.Tq;

/// <summary>
/// `let T name { get; set }`. Each accessor is a function with the signature `() -> T` or `(T) -> void`, and is
/// either written here (`GetterFunction`, `SetterFunction`: `get { }`) or defined by a function that exists
/// (`GetterSource`, `get = f`). An accessor written here without a body is automatic: it reads or writes the
/// storage of the property (in an abstract struct it is abstract instead). Inside an accessor, `field` is that
/// storage. The IR does not declare it: <see cref="HasBackingField"/> says the backend needs to create one.
/// Reading the property calls the getter and assigning to it calls the setter.
/// </summary>
public class TqProperty(IrExpression? type, string name, SourceLocation location) : IMember, ITqMember
{
    private MemberAccess _access = MemberAccess.Default;

    public IReadOnlyList<TqAttribute> Attributes { get; set; } = [];

    /// <summary>The accessors are reached through the property, so they have its access.</summary>
    public MemberAccess Access
    {
        get => _access;
        set
        {
            _access = value;
            if (GetterFunction != null) GetterFunction.Access = value;
            if (SetterFunction != null) SetterFunction.Access = value;
        }
    }

    public SourceLocation SourceLocation => location;
    public string Name => name;

    /// <summary>Null when it is taken from the functions that define the accessors.</summary>
    public IrExpression? Type { get; set; } = type;

    /// <summary>The struct it is declared in.</summary>
    public IObjectType? Owner { get; set; }

    public TqFunction? GetterFunction { get; set; }
    public TqFunction? SetterFunction { get; set; }

    /// <summary>`get = f`: what names the function, until it is resolved to <see cref="Getter"/>.</summary>
    public IrExpression? GetterSource { get; set; }
    public IrExpression? SetterSource { get; set; }

    /// <summary>The functions that run when the property is read or written; set once resolved.</summary>
    public IFunction? Getter { get; set; }
    public IFunction? Setter { get; set; }

    /// <summary>`let name => value` without a type: the checker gives the property the type of the value.</summary>
    public bool InfersType { get; set; }

    /// <summary>Set once an accessor body uses `field`.</summary>
    public bool UsesFieldKeyword { get; set; }

    /// <summary>
    /// Whether the property has storage: an accessor uses `field`, or one of them is automatic (it has no body
    /// and the struct can be created). The backend declares it, with the type of the property.
    /// </summary>
    public bool HasBackingField => UsesFieldKeyword
        || ((IsStatic || Owner is { IsAbstract: false }) && (GetterFunction is { Body: null } || SetterFunction is { Body: null }));

    /// <summary>A property of a namespace, or marked `@static` in a struct: it belongs to no value, so its accessors take no `this`.</summary>
    public bool IsStatic => Owner == null
        || Attributes.OfType<TqBuiltinAttribute>().Any(attribute => attribute.Kind == BuiltinAttribute.Static);

    public bool HasGetter => GetterFunction != null || GetterSource != null;
    public bool HasSetter => SetterFunction != null || SetterSource != null;

    /// <summary>Whether the sources were already looked for; they are resolved once.</summary>
    public bool AccessorsResolved { get; set; }

    public override string ToString() => $"property {Name}";
}
