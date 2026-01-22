using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.AttributeReferences;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;

public abstract class LangObject(string name) : IFormattable
{
    private readonly List<AttributeReference> _attributes = [];
    private LangObject? _parent = null!;

    public string[] Global => string.IsNullOrEmpty(Name) ? [.._parent?.Global ?? []] : [.._parent?.Global ?? [], Name];

    public readonly string Name = name;
    public LangObject Parent { get =>_parent!; set => _parent = value; }

    public ContainerObject? Container
    {
        get
        {
            if (_parent == null) return null;
            if (_parent is ContainerObject @container) return @container;
            return _parent.Container;
        }
    }
    public NamespaceObject? Namespace
    {
        get
        {
            if (_parent == null) return null;
            if (_parent is NamespaceObject @nmsp) return nmsp;
            return _parent.Namespace;
        }
    }
    public ModuleObject? Module
    {
        get
        {
            if (_parent == null) return null;
            if (_parent is ModuleObject @mod) return mod;
            return _parent.Module;
        }
    }
    public ImportObject? Imports = null;
    
    public AttributeReference[] Attributes => [.. _attributes];
    
    public void AppendAttributes(params AttributeReference[] attrs) => _attributes.AddRange(attrs);

    public virtual LangObject? SearchChild(string name) => null;
    
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return format switch
        {
            "sig" => ToSignature(),
            _ => ToString(),
        };
    }
    public abstract override string ToString();
    public abstract string ToSignature();
}

