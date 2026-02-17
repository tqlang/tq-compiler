using System.Diagnostics;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.CodeObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.Containers;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.Imports;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.AttributeReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.Dotnet;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin.Integer;
using Abstract.CodeProcess.Core.Language;
using Abstract.CodeProcess.Core.Language.Module;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Control;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Expression;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Misc;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Value;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures.Types;
using TypeReference = Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.TypeReference;

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
        _namespaces.Clear();
        _globalReferenceTable.Clear();
        _onHoldAttributes.Clear();
        _assemblies.Clear();
        
        // Search tq references
        foreach (var m in modules)
        {
            var module = new ModuleObject(m.name);
            Dictionary<string, TqNamespaceObject> _moduleNamespaces = [];
            foreach (var n in m.Namespaces)
            {
                var a = string.Join('.', n.Identifier);
                List<string> name = [m.name];
                if (n.Identifier.Length > 0) name.AddRange(n.Identifier);
                var obj = new TqNamespaceObject(n.Identifier[0], n);

                if (n.Identifier.Length > 1 || !string.IsNullOrEmpty(n.Identifier[0]))
                {
                    var parent = _moduleNamespaces[string.Join('.', n.Identifier[0..^1])];
                    parent.Namespaces.Add(obj);
                }
                else module.Namespaces.Add(obj);
                _moduleNamespaces.Add(string.Join('.', n.Identifier), obj);
                
                _namespaces.Add(obj);
                SearchNamespaceRecursive(obj);
            }

            LoadGlobalsRecursive(module);
            _modules.Add(module);
        }
        
        // Search dotnet included references
        var dotnetModule = new ModuleObject("Dotnet") { ReferenceOnly = true};
        var dotnetTypesMap = new Dictionary<string, DotnetTypeObject>();
        foreach (var i in includes)
        {
            var asmRef = new AssemblyReference(i, new Version());
            var res = _assemblyResolver.Resolve(asmRef);
            if (res == null) throw new Exception($"Assembly '{i}' not found");
            _assemblies.Add(res);
            
            var manifest = res.ManifestModule;
            if (manifest != null) BuildDotnetAssemblyTree(dotnetModule, manifest, dotnetTypesMap);
        }
        LoadGlobalsRecursive(dotnetModule);
        _modules.Add(dotnetModule);

        foreach (var (ik, iv) in dotnetTypesMap)
            DotnetRescanTypes(iv, dotnetTypesMap);
        
        _modules.TrimExcess();
        _namespaces.TrimExcess();
        _globalReferenceTable.TrimExcess();
        _assemblies.TrimExcess();
        _onHoldAttributes.Clear();
    }

    private void SearchNamespaceRecursive(TqNamespaceObject nmsp)
    {
        _onHoldAttributes.Push([]);
        foreach (var t in nmsp.SyntaxNode.Trees)
        {
            var script = new SourceScript("no path provided bruh");
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
            sourceScript.Imports.Add(new GeneralImportObject(namespaceParts));
        }
        else
        {
            var namespacePartsNode = fromImport.Children[1];
            var namespaceParts = namespacePartsNode is AccessNode @accessNode
                ? (accessNode).StringValues
                : [((IdentifierNode)namespacePartsNode).Value];
            
            var imports = (ImportCollectionNode)fromImport.Children[3];
            if (namespaceParts.Any(string.IsNullOrEmpty)) throw new Exception("Invalid expression inside namespace identifier");

            var importObj = new SpecificImportObject(namespaceParts);
            
            foreach (var i in imports.Content.OfType<ImportItemNode>())
            {
                if (i.Children is [_, TokenNode { Token.type: TokenType.AsKeyword }, _, ..])
                {
                    var original = (IdentifierNode)i.Children[0];
                    var alias = (IdentifierNode)i.Children[2];
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
        if (obj is not ModuleObject) _globalReferenceTable.Add(obj.Global, obj);

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

    
    private void BuildDotnetAssemblyTree(ModuleObject root, ModuleDefinition dotnetModule, Dictionary<string, DotnetTypeObject> typesMap)
    {
        foreach (var type in dotnetModule.ExportedTypes) DotnetInsertType(root, type, typesMap, true);
        foreach (var type in dotnetModule.TopLevelTypes) DotnetInsertType(root, type, typesMap, false);
    }

    private void DotnetInsertType(ModuleObject root, ITypeDescriptor type, Dictionary<string, DotnetTypeObject> map, bool skipable)
    {
        var typedef = type.Resolve() ?? throw new Exception();
        try
        {

            if (typedef.FullName == "<Module>") return;
            if (typedef.IsNotPublic) return;
            
            if (map.ContainsKey(typedef.FullName))
            {
                if (skipable) return;
                throw new Exception();
            }

            var ns = type.Namespace?.Split('.') ?? ["Global"];
            if (ns.Length < 1) throw new Exception();

            ContainerObject current = root;

            if (typedef.IsNested)
            {
                current = map[typedef.DeclaringType!.FullName];
            }
            else
            {
                // Manually search inside namespace
                foreach (var part in ns) current = GetOrCreateNamespace((INamespaceContainer)current, part);
            }

            if (current is not IDotnetTypeContainer @container) throw new Exception();
            var obj = new DotnetTypeObject(type, typedef, type.Name!);
            container.Types.Add(obj);

            map.Add(typedef.FullName, obj);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error importing type '{typedef.FullName}': {e.Message}");
        }
    }

    private void DotnetRescanTypes(DotnetTypeObject obj, Dictionary<string, DotnetTypeObject> map)
    {
        foreach (var i in obj.TypeDefinition.Methods) DotnetTryInsertMethod(obj, i, map);
    }
    
    private void DotnetTryInsertMethod(DotnetTypeObject parent, IMethodDescriptor method,
        Dictionary<string, DotnetTypeObject> typesMap)
    {
        var methodDef = method.Resolve() ?? throw new Exception();
        
        try
        {

            if (methodDef.IsPrivate) return;
            
            var parameters = new ParameterObject[methodDef.GenericParameters.Count + methodDef.Parameters.Count];
            for ( var i = 0; i < methodDef.GenericParameters.Count; i++)
            {
                parameters[i] = new ParameterObject(
                    new TypeTypeReference(null),
                    methodDef.GenericParameters[i].Name!);
            }

            for (var i = 0; i < methodDef.Parameters.Count; i++)
            {
                var idx = methodDef.GenericParameters.Count + i;
                var ptype = methodDef.Parameters[i].ParameterType;
                var pname = methodDef.Parameters[i].Name!;
                
                if (ptype is GenericParameterSignature gp)
                    parameters[idx] = new ParameterObject(new GenericTypeReference(parameters[gp.Index]), pname);
                else
                    parameters[idx] = new ParameterObject(DotnetTypeToRef(ptype, typesMap), pname);
            }

            var obj = new DotnetMethodObject(
                methodDef.Name!,
                method, methodDef,
                DotnetTypeToRef(methodDef.Signature!.ReturnType,  typesMap),
                parameters);
            
            if (methodDef.IsConstructor) parent.Constructors.Add(obj);
            else
            {
                var group = parent.Methods.FirstOrDefault(e => e.Name == method.Name);
                if (group == null)
                {
                    group = new DotnetMethodGroupObject(method.Name!);
                    parent.Methods.Add(group);
                }
                group.Overloads.Add(obj);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error importing method '{methodDef.FullName}': {e.Message}");
        }
    }
    
    private ContainerObject GetOrCreateNamespace(INamespaceContainer container, string name)
    {
        var val = container.Namespaces.FirstOrDefault(e => e.Name == name);
        if (val != null) return val;

        val = new DotnetNamespaceObject(name);
        container.Namespaces.Add(val);
        val.Parent = (LangObject)container;
        return val;
    }

    private TypeReference DotnetTypeToRef(TypeSignature t, Dictionary<string, DotnetTypeObject> typesMap)
    {
        switch (t)
        {
            case ByReferenceTypeSignature byRef:
                return new ReferenceTypeReference(DotnetTypeToRef(byRef.BaseType, typesMap));
            
            case PointerTypeSignature pt:
                return new ReferenceTypeReference(DotnetTypeToRef(pt.BaseType, typesMap));
            
            case SzArrayTypeSignature sza:
                return new SliceTypeReference(DotnetTypeToRef(sza.BaseType, typesMap));
            
            case GenericInstanceTypeSignature { GenericType.FullName: "System.Nullable`1" }:
                throw new NotImplementedException("nullable");
                //return new AnytypeTypeReference();

            case GenericInstanceTypeSignature generic when generic.GenericType.FullName.StartsWith("System.Func"):
                throw new NotImplementedException("lambda");
                //return new FunctionTypeReference(null, []);
            
            case GenericInstanceTypeSignature g:
            {
                var args = new TypeReference[g.TypeArguments.Count];
                for (var i = 0; i < g.TypeArguments.Count; i++)
                    args[i] = DotnetTypeToRef(g.TypeArguments[i], typesMap);
                return new DotnetGenericTypeReference(typesMap[g.GenericType.ToTypeSignature().FullName], args);
            }

            case GenericParameterSignature g:
            {
                return g.ParameterType switch
                {
                    GenericParameterType.Type => new DotnetGenericTypeParamReference(g.Index),
                    GenericParameterType.Method => new DotnetGenericMethodParamReference(g.Index),
                    _ => throw new UnreachableException()
                };
            }
            
            case CorLibTypeSignature corLib:
                return corLib.FullName switch
                {
                    "System.Void" => new VoidTypeReference(),
                    "System.Object" => new AnytypeTypeReference(),
            
                    "System.IntPtr" => new RuntimeIntegerTypeReference(true),
                    "System.UIntPtr" => new RuntimeIntegerTypeReference(false),
                    "System.SByte" => new RuntimeIntegerTypeReference(true, 8),
                    "System.Byte" => new RuntimeIntegerTypeReference(false, 8),
                    "System.Int16" => new RuntimeIntegerTypeReference(true, 16),
                    "System.UInt16" => new RuntimeIntegerTypeReference(false, 16),
                    "System.Int32" => new RuntimeIntegerTypeReference(true, 32),
                    "System.UInt32" => new RuntimeIntegerTypeReference(false, 32),
                    "System.Int64" => new RuntimeIntegerTypeReference(true, 64),
                    "System.UInt64" => new RuntimeIntegerTypeReference(false, 64),
            
                    "System.Single" => new RuntimeIntegerTypeReference(true, 32),
                    "System.Double" => new RuntimeIntegerTypeReference(true, 64),
                    
                    "System.Boolean" => new BooleanTypeReference(),
                    "System.String" => new StringTypeReference(StringEncoding.Utf16),
                    "System.Char" => new CharTypeReference(),
                    "System.Type" => new TypeTypeReference(null),
            
                    _ => throw new NotImplementedException($"core lib type {corLib.FullName} not implemented")
                };
        }
        
        if (t is not TypeDefOrRefSignature) throw new Exception($"{t.FullName} is {t.GetType().FullName}");
        var res = new DotnetTypeReference(typesMap[t.FullName]);
        return t.IsValueType ? res : new ReferenceTypeReference(res);
    }
}
