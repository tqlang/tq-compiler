using System.Diagnostics;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.Containers;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.Imports;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.AttributeReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.Module;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Control;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Expression;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Misc;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Value;
using AsmResolver.DotNet;

namespace Abstract.CodeProcess;

/*
 * Stage One:
 *  Iterates though the syntactic tree and
 *  collects the headers for general metadata
 *  generation. All generated data is organized
 *  in a tree and dumped into `_globalReferenceTable`
 */

public partial class Analyser
{
    private void SearchReferences(Module[] modules, string[] includes)
    {
        _modules.Clear();
        _assemblies.Clear();
        _namespaces.Clear();
        _onHoldAttributes.Clear();
        _globalReferenceTable.Clear();
        
        // Search tq references
        foreach (var m in modules)
        {
            Dictionary<string, TqNamespaceObject> moduleNamespaces = [];
            var module = new TqModuleObject(m.name);
            
            foreach (var n in m.Namespaces)
            {
                var obj = new TqNamespaceObject(n.Identifier[0], n);
                if (n.Identifier.Length == 1 && string.IsNullOrEmpty(n.Identifier[0])) module.Root = obj;
                else
                {
                    obj = new TqNamespaceObject(n.Identifier[0], n);
                    if (n.Identifier.Length > 1 || !string.IsNullOrEmpty(n.Identifier[0]))
                    {
                        var parent = moduleNamespaces[string.Join('.', n.Identifier[0..^1])];
                        parent.Namespaces.Add(obj);
                    }
                }
                
                moduleNamespaces.Add(string.Join('.', n.Identifier), obj);
                _namespaces.Add(obj);
                SearchNamespaceRecursive(obj);
            }

            LoadGlobalsRecursive(module);
            _modules.Add(module);
        }
        
        // Search dotnet included references
        var dotnetModule = new DotnetModuleObject("Dotnet");
        foreach (var (_, i) in _assemblyResolver.Assemblies)
        {
            var res = _assemblyResolver.Resolve(i);
            if (res?.ManifestModule != null) dotnetModule.AddModule(res.ManifestModule);
        }
        foreach (var i in includes)
        {
            var asmRef = new AssemblyReference(i, new Version());
            var res = _assemblyResolver.Resolve(asmRef);
            if (res == null) throw new Exception($"Assembly '{i}' not found");
            _assemblies.Add(res);
            
            if (res.ManifestModule != null) dotnetModule.AddModule(res.ManifestModule);
        }
        LoadGlobalsRecursive(dotnetModule);
        _modules.Add(dotnetModule);
        
        _modules.TrimExcess();
        _assemblies.TrimExcess();
        _namespaces.TrimExcess();
        _onHoldAttributes.Clear();
    }

    private void SearchNamespaceRecursive(TqNamespaceObject nmsp)
    {
        _onHoldAttributes.Push([]);
        foreach (var t in nmsp.SyntaxNode.Trees)
        {
            var script = new SourceScript(t.Path);
            foreach (var n in t.Children) SearchGenericScopeRecursive(nmsp, (ControlNode)n, script);
            nmsp.Scripts.Add(script);
        }

        var poppedList = _onHoldAttributes.Pop();
        if (poppedList.Count == 0) return;
        
        foreach (var unbounded in poppedList)
        {
            try { throw new Exception($"Attribute {unbounded} not assigned to any member"); }
            catch (Exception e) { _errorHandler.RegisterError(e); }
        }
    }
    private void SearchGenericScopeRecursive(LangObject parent, ControlNode node, SourceScript script)
    {
        switch (node)
        {
            case AttributeNode @attr:
                _onHoldAttributes.Peek().Add(EvaluateAttribute(attr));
                return;
            
            case FromImportNode fromImport:
            {
                if (parent is not TqNamespaceObject)
                    throw new Exception("Imports can only be made inside a namespace root");
                
                HandleImport(script, fromImport);
                return;
            }
        }

        LangObject obj = node switch
        {
            FunctionDeclarationNode @n when parent is IFunctionContainer @p => RegisterFunction(p, n, script),
            StructureDeclarationNode @n when parent is IStructContainer @p => RegisterStructure(p, n, script),
            TypeDefinitionNode @n when parent is ITypedefContainer @p => RegisterTypedef(p, n, script),
            TopLevelVariableNode @n when parent is IFieldContainer @p => RegisterField(p, n, script),
            ConstructorDeclarationNode @n when parent is ICtorDtorContainer @p => RegisterCtor(p, n, script),
            DestructorDeclarationNode @n when parent is ICtorDtorContainer @p => RegisterDtor(p, n, script),
            
            _ => throw new NotImplementedException()
        };

        if (_onHoldAttributes.Count <= 0) return;
        obj.AppendAttributes([.. _onHoldAttributes.Peek()]);
        _onHoldAttributes.Peek().Clear();

    }
    private void SearchTypedefScopeRecursive(TypedefObject parent, ControlNode node, SourceScript script)
    {
        if (node is AttributeNode @attr)
        {
            _onHoldAttributes.Peek().Add(EvaluateAttribute(attr));
            return;
        }

        LangObject obj = node switch
        {
            FunctionDeclarationNode @funcnode => RegisterFunction(parent, funcnode, script),
            TypeDefinitionItemNode @item => RegisterTypedefItem(parent, item),
            _ => throw new NotImplementedException(),
        };

        if (_onHoldAttributes.Count <= 0) return;
        obj.AppendAttributes([.. _onHoldAttributes.Peek()]);
        _onHoldAttributes.Peek().Clear();
    }


    private static void HandleImport(SourceScript sourceScript, FromImportNode fromImport)
    {
        if (fromImport.Children.Length < 4)
        {
            var namespaceParts = ((AccessNode)fromImport.Children[1]).StringValues;

            if (namespaceParts.Any(string.IsNullOrEmpty)) throw new Exception("Invalid expression inside namespace identifier");
            sourceScript.Imports.Add(new GeneralImportObject(fromImport, namespaceParts));
        }
        else
        {
            var namespacePartsNode = fromImport.Children[1];
            var namespaceParts = namespacePartsNode is AccessNode @accessNode
                ? accessNode.StringValues
                : [((IdentifierNode)namespacePartsNode).Value];
            
            var imports = (ImportCollectionNode)fromImport.Children[3];
            if (namespaceParts.Any(string.IsNullOrEmpty)) throw new Exception("Invalid expression inside namespace identifier");

            var importObj = new SpecificImportObject(fromImport, namespaceParts);
            
            foreach (var i in imports.Content.OfType<ImportItemNode>())
            {
                if (i.Children[0] is TypeCastNode @tc)
                {
                    var original = tc.Value as IdentifierNode ?? throw new Exception("Invalid expression inside namespace identifier");
                    var alias = tc.TargetType as IdentifierNode ?? throw new Exception("Invalid expression inside namespace alias");
                    importObj.Imports.Add(alias.Value, (original.Value, null!));
                }
                else
                {
                    var reference = (IdentifierNode)i.Children[0];
                    importObj.Imports.Add(reference.Value, (reference.Value, null!));
                }
            }
            
            sourceScript.Imports.Add(importObj);
        }
    }
    
    private FunctionObject RegisterFunction(IFunctionContainer parent, FunctionDeclarationNode funcnode, SourceScript script)
    {
        var funcg = parent.Functions.FirstOrDefault(e =>  e.Name == funcnode.Identifier.Value);
        if (funcg == null)
        {
            funcg = new FunctionGroupObject(script, funcnode.Identifier.Value);
            parent.Functions.Add(funcg);
        }

        var f = new FunctionObject(script, funcnode.Identifier.Value, funcnode);
        funcg.Overloads.Add(f);
        
        return f;
    }
    private StructObject RegisterStructure(IStructContainer parent, StructureDeclarationNode structnode, SourceScript script)
    {
        var struc = new StructObject(script, structnode.Identifier.Value, structnode);
        parent.Structs.Add(struc);
        
        do
        {
            _onHoldAttributes.Push([]);
            
            foreach (var i in structnode.Body.Content)
                SearchGenericScopeRecursive(struc, (ControlNode)i, script);
            
            var poppedList = _onHoldAttributes.Pop();
            if (poppedList.Count == 0) break;
        
            foreach (var unbinded in poppedList)
            {
                try { throw new Exception($"Attribute {unbinded} not assigned to any member"); }
                catch (Exception e) { _errorHandler.RegisterError(e); }
            }
        } while (false);

        return struc;
    }
    private TypedefObject RegisterTypedef(ITypedefContainer parent, TypeDefinitionNode typedef, SourceScript script)
    {
        var typdef = new TypedefObject(script, typedef.Identifier.Value, typedef);
        parent.Typedefs.Add(typdef);

        do
        {
            _onHoldAttributes.Push([]);
            
            foreach (var node in typdef.syntaxNode.Body.Content.OfType<ControlNode>())
                SearchTypedefScopeRecursive(typdef, node, script);
            
            var poppedList = _onHoldAttributes.Pop();
            if (poppedList.Count == 0) break;

            foreach (var unbinded in poppedList)
            {
                try { throw new Exception($"Attribute {unbinded} not assigned to any member"); }
                catch (Exception e) { _errorHandler.RegisterError(e); }
            }
        } while (false);

        return typdef;
    }
    private TypedefNamedValue RegisterTypedefItem(TypedefObject parent, TypeDefinitionItemNode typedefitem)
    {
        switch (typedefitem)
        {
            case TypeDefinitionNumericItemNode @num:
            {
                throw new NotImplementedException();
            } 

            case TypeDefinitionNamedItemNode @named:
            {
                var nval = new TypedefNamedValue(named, named.Key.Value) { Parent = parent };
                parent.NamedValues.Add(nval);
                return nval;       
            }
            default: throw new UnreachableException();
        }
    }
    private FieldObject RegisterField(IFieldContainer parent, TopLevelVariableNode variable, SourceScript script)
    {
        var fieldType = new UnsolvedTypeReference(variable.Type);
        var field = new FieldObject(script, variable.Identifier.Value, variable, fieldType) { Constant = variable.IsConstant };
        parent.Fields.Add(field);

        return field;
    }
    private ConstructorObject RegisterCtor(ICtorDtorContainer parent, ConstructorDeclarationNode node, SourceScript script)
    {
        var ctor = new ConstructorObject(script, node);
        parent.Constructors.Add(ctor);
        return ctor;
    }
    private DestructorObject RegisterDtor(ICtorDtorContainer parent, DestructorDeclarationNode node, SourceScript script)
    {
        var dtor = new DestructorObject(script, node);
        parent.Destructors.Add(dtor);
        return dtor;
    }

    private void LoadGlobalsRecursive(LangObject obj)
    {
        if (obj is not BaseModuleObject) _globalReferenceTable.Add(obj.Global, obj);
        
        if (obj is TqModuleObject { Root: not null } @m)
        {
            m.Root.Parent = m;
            LoadGlobalsRecursive(m.Root);
        }
        
        if (obj is INamespaceContainer @nc)
        {
            foreach (var i in nc.Namespaces)
            {
                i.Parent = obj;
                LoadGlobalsRecursive(i);
            }
        }

        if (obj is IFieldContainer @fc)
            foreach (var i in fc.Fields)
            {
                i.Parent = obj;
                LoadGlobalsRecursive(i);
            }

        if (obj is IStructContainer @sc)
            foreach (var i in sc.Structs)
            {
                i.Parent = obj;
                LoadGlobalsRecursive(i);
            }

        if (obj is ITypedefContainer @tc)
            foreach (var i in tc.Typedefs)
            {
                i.Parent = obj;
                LoadGlobalsRecursive(i);
            }

        if (obj is IFunctionContainer gc)
            foreach (var i in gc.Functions)
            {
                i.Parent = obj;
                foreach (var j in i.Overloads)
                {
                    j.Parent = obj;
                    j.ParentGroup = i;
                }
                LoadGlobalsRecursive(i);
            }
        
        if (obj is ICtorDtorContainer @cdc)
        {
            foreach (var i in cdc.Constructors) i.Parent = obj;
            foreach (var i in cdc.Destructors) i.Parent = obj;
        }
        
        if (obj is IDotnetTypeContainer dc)
            foreach (var i in dc.Types)
            {
                i.Parent = obj;
                LoadGlobalsRecursive(i);
            }
        
        if (obj is IDotnetFieldContainer fd)
            foreach (var i in fd.Fields)
            {
                i.Parent = obj;
                LoadGlobalsRecursive(i);
            }
        
        if (obj is IDotnetMethodContainer dm)
            foreach (var i in dm.Methods)
            {
                i.Parent = obj;
                foreach (var j in i.Overloads)
                {
                    j.Parent = obj;
                    j.MethodGroup = i;
                }
                LoadGlobalsRecursive(i);
            }

        if (obj is IDotnetCtorDtorContainer c)
        {
            foreach (var i in c.Constructors) i.Parent = obj;
            c.Destructor?.Parent = obj;
        }
    }
    
    private static AttributeReference EvaluateAttribute(AttributeNode node)
    {
        var identifier = (node.Children[1] as IdentifierNode);
        if (identifier == null) goto _default;

        var builtin = identifier.Value switch
        {
            "static" => BuiltinAttributes.Static,
            "align" => BuiltinAttributes.Align,
            "constExp" => BuiltinAttributes.ConstExp,

            "public" => BuiltinAttributes.Public,
            "private" => BuiltinAttributes.Private,
            "internal" => BuiltinAttributes.Internal,
            "final" => BuiltinAttributes.Final,
            "abstract" => BuiltinAttributes.Abstract,
            "interface" => BuiltinAttributes.Interface,
            "virtual" => BuiltinAttributes.Virtual,
            "override" => BuiltinAttributes.Override,
            "allowAccessTo" => BuiltinAttributes.AllowAccessTo,
            "denyAccessTo" => BuiltinAttributes.DenyAccessTo,

            "extern" => BuiltinAttributes.Extern,
            "export" => BuiltinAttributes.Export,
            
            "inline" => BuiltinAttributes.Inline,
            "noinline" => BuiltinAttributes.Noinline,
            "comptime" => BuiltinAttributes.Comptime,
            "runtime" => BuiltinAttributes.Runtime,
            "callconv" => BuiltinAttributes.CallConv,

            "getter" => BuiltinAttributes.Getter,
            "setter" => BuiltinAttributes.Setter,

            "explicitConvert" => BuiltinAttributes.ExplicitConvert,
            "implicitConvert" => BuiltinAttributes.ImplicitConvert,
            "overrideOperator" => BuiltinAttributes.OverrideOperator,
            "indexerGetter" => BuiltinAttributes.IndexerGetter,
            "indexerSetter" => BuiltinAttributes.IndexerSetter,

            _ => BuiltinAttributes._undefined
        };
        if (builtin == BuiltinAttributes._undefined) goto _default;
        return new BuiltInAttributeReference(node, builtin);
        
        _default:
        return new UnsolvedAttributeReference(node);
    }

}
