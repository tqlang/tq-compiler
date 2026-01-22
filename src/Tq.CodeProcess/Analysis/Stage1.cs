using System.Diagnostics;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.Containers;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.AttributeReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.Module;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Control;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Expression;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Value;

namespace Abstract.CodeProcess;

/*
 * Stage One:
 *  Iterates though the syntatic tree and
 *  collects the headers for general metadata
 *  generation. All generated data is organized
 *  in a tree and dumped into `_globalReferenceTable`
 */

public partial class Analyser
{
    private void SearchReferences(Module[] modules)
    {
        _modules.Clear();
        _namespaces.Clear();
        _globalReferenceTable.Clear();
        _onHoldAttributes.Clear();
        
        foreach (var m in modules)
        {
            var module = new ModuleObject(m.name);
            foreach (var n in m.Namespaces)
            {
                List<string> name = [m.name];
                if (n.Identifier.Length > 0) name.AddRange(n.Identifier);
                var obj = new NamespaceObject(n.Identifier[0], n);
                
                module.Namespaces.Add(obj);
                _namespaces.Add(obj);
                SearchNamespaceRecursive(obj);
            }

            LoadGlobalsRecursive(module);
            _modules.Add(module);
        }
        
        _modules.TrimExcess();
        _namespaces.TrimExcess();
        _globalReferenceTable.TrimExcess();
        _onHoldAttributes.Clear();
    }

    private void SearchNamespaceRecursive(NamespaceObject nmsp)
    {
        _onHoldAttributes.Push([]);
        foreach (var t in nmsp.SyntaxNode.Trees)
        {
            var import = new ImportObject();
            foreach (var n in t.Children) SearchGenericScopeRecursive(nmsp, (ControlNode)n, import);
            nmsp.Imports.Add(import);
        }

        var poppedList = _onHoldAttributes.Pop();
        if (poppedList.Count == 0) return;
        
        foreach (var unbinded in poppedList)
        {
            try { throw new Exception($"Attribute {unbinded} not assigned to any member"); }
            catch (Exception e) { _errorHandler.RegisterError(e); }
        }
    }
    private void SearchGenericScopeRecursive(LangObject parent, ControlNode node, ImportObject imports)
    {
        switch (node)
        {
            case AttributeNode @attr:
                _onHoldAttributes.Peek().Add(EvaluateAttribute(attr));
                return;
            
            case FromImportNode @fromimport:
            {
                if (parent is not NamespaceObject @nmsp)
                    throw new Exception("Imports can only be made inside a namespace root");
                
                HandleImport(imports, fromimport);
                nmsp.Imports.Add(imports);
                
                return;
            }
        }

        LangObject obj = node switch
        {
            FunctionDeclarationNode @n when parent is IFunctionContainer @p => RegisterFunction(p, n, imports),
            StructureDeclarationNode @n when parent is IStructContainer @p => RegisterStructure(p, n, imports),
            TypeDefinitionNode @n when parent is ITypedefContainer @p => RegisterTypedef(p, n, imports),
            TopLevelVariableNode @n when parent is IFieldContainer @p => RegisterField(p, n, imports),
            ConstructorDeclarationNode @n when parent is ICtorDtorContainer @p => RegisterCtor(p, n, imports),
            DestructorDeclarationNode @n when parent is ICtorDtorContainer @p => RegisterDtor(p, n, imports),
            
            _ => throw new NotImplementedException()
        };

        if (_onHoldAttributes.Count <= 0) return;
        obj.AppendAttributes([.. _onHoldAttributes.Peek()]);
        _onHoldAttributes.Peek().Clear();

    }
    private void SearchTypedefScopeRecursive(TypedefObject parent, ControlNode node, ImportObject imports)
    {
        if (node is AttributeNode @attr)
        {
            _onHoldAttributes.Peek().Add(EvaluateAttribute(attr));
            return;
        }

        LangObject obj = node switch
        {
            FunctionDeclarationNode @funcnode => RegisterFunction(parent, funcnode, imports),
            TypeDefinitionItemNode @item => RegisterTypedefItem(parent, item, imports),
            _ => throw new NotImplementedException(),
        };

        if (_onHoldAttributes.Count <= 0) return;
        obj.AppendAttributes([.. _onHoldAttributes.Peek()]);
        _onHoldAttributes.Peek().Clear();
    }


    private void HandleImport(ImportObject importobj, FromImportNode fromImport)
    {
        if (fromImport.Children.Length < 4)
        {
            var namespaceParts = ((AccessNode)fromImport.Children[1]).StringValues;

            if (namespaceParts.Any(string.IsNullOrEmpty)) throw new Exception("Invalid expression inside namespace identifier");
            importobj.Raw.Add(namespaceParts.Append("*").ToArray());
        }
        else throw new UnreachableException();
    }
    
    private FunctionObject RegisterFunction(IFunctionContainer parent, FunctionDeclarationNode funcnode, ImportObject imports)
    {
        var funcg = parent.Functions.FirstOrDefault(e =>  e.Name == funcnode.Identifier.Value);
        if (funcg == null)
        {
            funcg = new FunctionGroupObject(funcnode.Identifier.Value);
            parent.Functions.Add(funcg);
        }

        var f = new FunctionObject(funcnode.Identifier.Value, funcnode) { Imports = imports};
        funcg.Overloads.Add(f);
        
        return f;
    }
    private StructObject RegisterStructure(IStructContainer parent, StructureDeclarationNode structnode, ImportObject imports)
    {
        var struc = new StructObject(structnode.Identifier.Value, structnode) {Imports = imports };
        parent.Structs.Add(struc);
        
        do
        {
            _onHoldAttributes.Push([]);
            
            foreach (var i in structnode.Body.Content)
                SearchGenericScopeRecursive(struc, (ControlNode)i, imports);
            
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
    private TypedefObject RegisterTypedef(ITypedefContainer parent, TypeDefinitionNode typedef, ImportObject imports)
    {
        var typdef = new TypedefObject(typedef.Identifier.Value, typedef) { Imports = imports };
        parent.Typedefs.Add(typdef);

        do
        {
            _onHoldAttributes.Push([]);
            
            foreach (var node in typdef.syntaxNode.Body.Content.OfType<ControlNode>())
                SearchTypedefScopeRecursive(typdef, node, imports);
            
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
    private TypedefNamedValue RegisterTypedefItem(TypedefObject parent, TypeDefinitionItemNode typedefitem, ImportObject imports)
    {
        switch (typedefitem)
        {
            case TypeDefinitionNumericItemNode @num:
            {
                throw new NotImplementedException();
            } 

            case TypeDefinitionNamedItemNode @named:
            {
                var nval = new TypedefNamedValue(named.Identifier.Value) { Parent = parent };
                parent.NamedValues.Add(nval);
                return nval;       
            }
            default: throw new UnreachableException();
        }
    }
    private FieldObject RegisterField(IFieldContainer parent, TopLevelVariableNode variable, ImportObject imports)
    {
        var fieldType = new UnsolvedTypeReference(variable.Type);
        var field = new FieldObject(variable.Identifier.Value, variable, fieldType)
        {
            Imports = imports,
            Constant = variable.IsConstant,
        };
        parent.Fields.Add(field);

        return field;
    }
    private ConstructorObject RegisterCtor(ICtorDtorContainer parent, ConstructorDeclarationNode node, ImportObject imports)
    {
        var ctor = new ConstructorObject(node) { Imports = imports };
        parent.Constructors.Add(ctor);
        return ctor;
    }
    private DestructorObject RegisterDtor(ICtorDtorContainer parent, DestructorDeclarationNode node, ImportObject imports)
    {
        var dtor = new DestructorObject(node) { Imports = imports };
        parent.Destructors.Add(dtor);
        return dtor;
    }

    private void LoadGlobalsRecursive(LangObject obj)
    {
        if (obj is not ModuleObject) _globalReferenceTable.Add(obj.Global, obj);
        
        if (obj is INamespaceContainer @nc)
            foreach (var i in nc.Namespaces)
            {
                i.Parent = obj;
                LoadGlobalsRecursive(i);
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
            "dotnetImport" => BuiltinAttributes.DotnetImport,
            
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
