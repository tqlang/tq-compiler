using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.AttributeReferences;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Control;

namespace Abstract.CodeProcess.Core.Language.EvaluationData.LanguageObjects;

public abstract class LangObject(string[] global, string name)
{
    private readonly List<AttributeReference> _attributes = [];
    private LangObject _parent = null!;
    private readonly List<LangObject> _children = [];
 
    public readonly string[] Global = global;
    public readonly string Name = name;
    public LangObject Parent { get =>_parent; set => _parent = value; }

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
    
    public LangObject[] Children => [.. _children];
    public AttributeReference[] Attributes => [.. _attributes];
    
    public void AppendAttributes(params AttributeReference[] attrs) => _attributes.AddRange(attrs);

    public void AppendChild(LangObject child)
    {
        _children.Add(child);
        child._parent = this;
    }

    public abstract override string ToString();
}

