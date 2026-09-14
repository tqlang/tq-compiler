using Tq.CodeProcess.Core.EvaluationData.LanguageObjects.Containers;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.AttributeReferences;

namespace Tq.CodeProcess.Core.EvaluationData.LanguageObjects;

public abstract class LangObject(SourceScript sourceScript, string name) : IMember, IFormattable
{
    private readonly List<AttributeReference> _attributes = [];
    private LangObject? _parent;
    private Dictionary<BuiltinAttributes, object?> _encapsilation = [];

    public string Name => name;
    public LangObject Parent { get =>_parent!; set => _parent = value; }
    public string[] Global => string.IsNullOrEmpty(Name) ? [.._parent?.Global ?? []] : [.._parent?.Global ?? [], Name];
    public IDictionary<BuiltinAttributes, object?> Encapsulation => _encapsilation;

    

    public ContainerObject? Container
    {
        get
        {
            if (_parent == null) return null;
            if (_parent is ContainerObject @container) return @container;
            return _parent.Container;
        }
    }
    public TqNamespaceObject? Namespace
    {
        get
        {
            if (_parent == null) return null;
            if (_parent is TqNamespaceObject @nmsp) return nmsp;
            return _parent.Namespace;
        }
    }
    public readonly SourceScript SourceScript = sourceScript;
    
    public BaseModuleObject? Module
    {
        get
        {
            if (_parent == null) return null;
            if (_parent is BaseModuleObject @mod) return mod;
            return _parent.Module;
        }
    }
    
    public AttributeReference[] Attributes => [.. _attributes];

    public abstract LangObject? SearchChild(string name, SearchChildMode mode = SearchChildMode.All);
    public void AppendAttributes(params AttributeReference[] attrs) => _attributes.AddRange(attrs);


    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return format switch
        {
            "sig" => ToSignature(),
            _ => ToString(),
        };
    }
    public override abstract string ToString();
    public abstract string ToSignature();
}

public enum SearchChildMode
{
    All,
    OnlyStatic,
    OnlyInstance
}
