using System.Buffers.Binary;
using System.Diagnostics;
using Abstract.CodeProcess.Core.EvaluationData;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.PE.DotNet.Cil;
using AssemblyDefinition = AsmResolver.DotNet.AssemblyDefinition;
using FieldAttributes = AsmResolver.PE.DotNet.Metadata.Tables.Rows.FieldAttributes;
using FieldDefinition = AsmResolver.DotNet.FieldDefinition;
using MethodAttributes = AsmResolver.PE.DotNet.Metadata.Tables.Rows.MethodAttributes;
using MethodDefinition = AsmResolver.DotNet.MethodDefinition;
using ModuleDefinition = AsmResolver.DotNet.ModuleDefinition;
using TypeAttributes = AsmResolver.PE.DotNet.Metadata.Tables.Rows.TypeAttributes;
using TypeDefinition = AsmResolver.DotNet.TypeDefinition;
using TypeReference = AsmResolver.DotNet.TypeReference;

namespace Abstract.CodeProcess;

public partial class Compiler
{
    private Dictionary<TqNamespaceObject, TypeDefinition> _namespacesMap = [];
    private Dictionary<ICallable, IFunctionData> _functionsMap = [];
    private Dictionary<StructObject, StructData> _typesMap = [];
    private Dictionary<TypedefObject, EnumData> _enumsMap = [];
    private Dictionary<FieldObject, FieldDefinition> _fieldsMap = [];

    private AssemblyDefinition _assembly;
    private ModuleDefinition _module;
    
    private CorLibTypeFactory _corLibFactory;
    private Dictionary<string, (TypeSignature t, Dictionary<string, IMethodDescriptor> m)> _coreLib = [];
    
    public void Compile(ProgramObject program)
    {
        var programName = program.Modules[0].Name;
        
        _assembly = new AssemblyDefinition(programName + ".dll",
            new Version(1, 0, 0, 0));
        
        _module = new ModuleDefinition(programName, program.AssemblyResolver.CorLibReference)
        { MetadataResolver = new DefaultMetadataResolver(program.AssemblyResolver) };
        _assembly.Modules.Add(_module);
        
        LoadCoreLibResources();
        
        foreach (var m in program.Modules) 
            if (!m.ReferenceOnly) SearchRecursive(m);
        
        DeclareTypes();
        ResolveContent();
        ImplementFieldInitializers();
        ImplementMethods();
        
        DumpModule();
        _module.Write($".abs-out/{programName}.dll");
    }
    
    private void SearchRecursive(LangObject obj)
    {
        switch (obj)
        {
            case ModuleObject @a:
                foreach (var i in a.Namespaces) SearchRecursive(i);
                break;

            case TqNamespaceObject @a:
            {
                var attributes = TypeAttributes.AnsiClass
                                 | TypeAttributes.Class
                                 | TypeAttributes.Sealed
                                 | TypeAttributes.Public
                                 | TypeAttributes.Abstract;

                var isroot = string.IsNullOrEmpty(a.Name);
                var name = a.Name + "Static";
                var nmsp = isroot ? obj.Module!.Name : string.Join('.', a.Global[0..^1]);
                
                var moduledef = new TypeDefinition(nmsp, name, attributes, _coreLib["Object"].t.ToTypeDefOrRef());
                _module.TopLevelTypes.Add(moduledef);
                _namespacesMap.Add(a, moduledef);
                
                foreach (var i in a.Fields) SearchRecursive(i);
                foreach (var i in a.Structs) SearchRecursive(i);
                foreach (var i in a.Typedefs) SearchRecursive(i);
                foreach (var i in a.Functions) SearchRecursive(i);
            } break;

            case FieldObject @a:
                _fieldsMap.Add(a, null!);
                break;
            
            case FunctionGroupObject @a:
                foreach (var i in a.Overloads)
                    _functionsMap.Add(i, null!);
                break;

            case StructObject @a:
                _typesMap.Add(a, null!);
                break;
            
            case TypedefObject @a:
                _enumsMap.Add(a, null!);
                break;
            
            default: throw new UnreachableException();
        }
    }
    
    private void DeclareTypes()
    {
        foreach (var (k, v) in _typesMap)
        {
            var newstruct = DeclareType(k);
            _typesMap[k] = newstruct;
        }

        foreach (var (k, v) in _enumsMap)
        {
            var enumdata = DeclareTypedef(k);
            _enumsMap[k] = enumdata;
        }
    }

    private void ResolveContent()
    {
        foreach (var (k, _) in _functionsMap)
        {
            var parent = _namespacesMap[((LangObject)k).Namespace!];
            var fun = k switch
            {
                FunctionObject @f => DeclareFunction(f, parent),
                ConstructorObject @c => DeclareCtor(c, parent), 
                _ => throw new NotImplementedException(),
            };
            _functionsMap[k] = fun;
        }

        foreach (var (k, _) in _fieldsMap)
        {
            var newfield = DeclareField(k, _namespacesMap[k.Namespace!]);
            _fieldsMap[k] = newfield;
        }

        foreach (var (k, v) in _typesMap)
        {
            if (v.Type is TypeDefinition typedef && k.Extends != null)
            {
                FieldAttributes attributes = FieldAttributes.Public | FieldAttributes.SpecialName;
                var sig = new FieldSignature(CallingConventionAttributes.Default, TypeFromRef(k.Extends));
                var f = new FieldDefinition("base", attributes, sig) { FieldOffset = 0 };
                typedef.Fields.Add(f);
            }

            foreach (var i in k.Fields)
            {
                var f = DeclareField(i, v.Type);
                _fieldsMap.Add(i, f);
            }
            
            foreach (var i in k.Constructors)
            {
                var f = DeclareCtor(i, v.Type);
                _functionsMap.Add(i, f);
            }
            
            foreach (var i in k.Destructors)
            {
                // TODO
            }

            foreach (var i in k.Functions)
            {
                foreach (var j in i.Overloads)
                {
                    var f = DeclareFunction(j, v.Type);
                    _functionsMap.Add(j, f);
                }
            }
        }
    }

    private void ImplementFieldInitializers()
    {
        foreach (var (k, v) in _namespacesMap)
        {
            MethodDefinition staticCtor;
            {
                MethodAttributes attributes = MethodAttributes.HideBySig
                                              | MethodAttributes.SpecialName
                                              | MethodAttributes.RuntimeSpecialName
                                              | MethodAttributes.Static;
                
                var sig = MethodSignature.CreateInstance(_corLibFactory.Void);
                staticCtor = new MethodDefinition(".cctor", attributes, sig);
            }
            
            var body = new CilMethodBody(staticCtor);
            staticCtor.CilMethodBody = body;
            
            foreach (var i in k.Fields)
            {
                if (i.Value == null) continue;
                var fieldDef = _fieldsMap[i];

                fieldDef.HasDefault = true;
                var ctx = new Context(null, body, _module.DefaultImporter, [], []);
                CompileIrNodeLoad(i.Value, ctx);
                body.Instructions.Add(CilOpCodes.Stsfld, fieldDef);
            }

            if (body.Instructions.Count <= 0) continue;
            body.Instructions.Add(CilOpCodes.Ret);
            v.Methods.Add(staticCtor);
        }
    }
    private void ImplementMethods()
    {
        foreach (var (k, _v) in _functionsMap)
        {
            if (_v is not ConcreteFunctionData @v) continue;
            if (k.Body == null) continue;
            if (v.Def.HasMethodBody) continue;
            
            var body = new CilMethodBody(v.Def);
            v.Def.CilMethodBody = body;
            
            var locals = new CilLocalVariable[k.Locals.Count];
            foreach (var local in k.Locals)
            {
                var l = new CilLocalVariable(TypeFromRef(local.Type));
                locals[local.index] = l;
                body.LocalVariables.Add(l);
            }
            if (locals.Length > 0) body.InitializeLocals = true;
            
            var args = v.Def.Parameters.ToArray();
            ITypeDefOrRef? selfType = !v.IsStatic ? v.Def.DeclaringType : null;
            
            var ctx = new Context(selfType, body, _module.DefaultImporter, args, locals);
            
            CompileIr(k.Body!, ctx);
            if (body.Instructions.Count == 0 || body.Instructions[^1].OpCode != CilOpCodes.Ret)
            {
                body.Instructions.Add(CilOpCodes.Ret);
                if (v.ReturnsValue) ctx.StackPop();
            }

            var arr = GC.AllocateArray<uint>(25);
            
            //if (ctx.Stack.Count != 0) throw new UnreachableException();
            //body.ComputeMaxStack();
            body.Instructions.CalculateOffsets();
        }
    }


    private StructData DeclareType(StructObject structobj)
    {
        if (structobj.DotnetImport != null)
        {
            var asmName = structobj.DotnetImport.Value.AssemblyName;
            var typeName = structobj.DotnetImport.Value.ClassName;
            var lastDot = typeName.LastIndexOf('.');

            var asmRef = SolveAssemblyReference(asmName);
            var typeRef = SolveTypeReference(asmRef, typeName[0..lastDot], typeName[(lastDot+1)..]);
            
            return new StructData(typeRef);
        }

        var nmsp = string.Join('.', structobj.Global[0..^1]);
        var name = structobj.Name;

        var attributes = TypeAttributes.AnsiClass
                         | TypeAttributes.ExplicitLayout;

        if (structobj.Public) attributes |= TypeAttributes.Public;
        if (structobj.Abstract) attributes |= TypeAttributes.Abstract;
        if (structobj.Final) attributes |= TypeAttributes.Sealed;

        var typedef = new TypeDefinition(nmsp, name, attributes, _coreLib["ValueType"].t.ToTypeDefOrRef());
        typedef.ClassLayout = new ClassLayout((ushort)structobj.Alignment!.Value.Bytes, (uint)structobj.Length!.Value.Bytes);
        _module.TopLevelTypes.Add(typedef);
        
        return new StructData(typedef);
    }
    private EnumData DeclareTypedef(TypedefObject typedefobj)
    {
        if (typedefobj.DotnetImport != null)
        {
            var asmName = typedefobj.DotnetImport.Value.AssemblyName;
            var typeName = typedefobj.DotnetImport.Value.ClassName;
            var lastDot = typeName.LastIndexOf('.');
            
            var asmRef = SolveAssemblyReference(asmName);
            var importedType = SolveTypeReference(asmRef, typeName[0..lastDot], typeName[(lastDot + 1)..]);
            var enumTypeSignature = importedType.ToTypeSignature();
            
            var valueFieldSignature = new FieldSignature(enumTypeSignature);
            var vfr = importedType.CreateMemberReference("value__", valueFieldSignature);
            var valueField = _module.DefaultImporter.ImportField(vfr);
            
            var enumData = new EnumData(importedType, valueField);

            foreach (var value in typedefobj.NamedValues)
            {
                var fieldSignature = new FieldSignature(enumTypeSignature);
                var fr = importedType.CreateMemberReference(value.Name, fieldSignature);
                var resolved = (FieldDefinition?)fr.Resolve();
                if (resolved == null) throw new UnreachableException(); ;
                enumData.Items.Add(value, _module.DefaultImporter.ImportField(resolved).Resolve()!);
            }

            return enumData;
        }
        else
        {
            var nmsp = string.Join('.', typedefobj.Global[0..^1]);
            var name = typedefobj.Name;

            var attributes = TypeAttributes.AnsiClass
                             | TypeAttributes.Sealed;

            var enumType = new TypeDefinition(nmsp, name, attributes, _coreLib["Enum"].t.ToTypeDefOrRef());
            _module.TopLevelTypes.Add(enumType);

            var valueField = new FieldDefinition(
                "value__",
                FieldAttributes.Public | FieldAttributes.SpecialName | FieldAttributes.RuntimeSpecialName,
                _corLibFactory.UInt64
            );
            enumType.Fields.Add(valueField);

            var enumdata = new EnumData(enumType, valueField);

            ulong i = 0;
            foreach (var value in typedefobj.NamedValues)
            {
                var itemField = new FieldDefinition(
                    value.Name,
                    FieldAttributes.Public
                    | FieldAttributes.Static
                    | FieldAttributes.Literal
                    | FieldAttributes.HasDefault,
                    enumType.ToTypeSignature()
                );
                var bytes = new byte[8];
                BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(), i);
                itemField.Constant = new Constant(_corLibFactory.UInt64.ElementType, new DataBlobSignature(bytes));
                enumType.Fields.Add(itemField);
                enumdata.Items.Add(value, itemField);
                i++;
            }

            return enumdata;
        }
    }

    private IFunctionData DeclareCtor(ConstructorObject ctorobj, ITypeDefOrRef parent)
    {
        if (ctorobj.DotnetImport != null)
        {
            ITypeDefOrRef baseType;
            if (ctorobj.DotnetImport.Value.AssemblyName == null && ctorobj.DotnetImport.Value.ClassName == null)
            {
                baseType = parent;
            }
            else
            {
                var asmName = ctorobj.DotnetImport.Value.AssemblyName!;
                var typeName = ctorobj.DotnetImport.Value.ClassName!;
                var lastDot = typeName.LastIndexOf('.');
            
                var asmRef = SolveAssemblyReference(asmName!);
                baseType = SolveTypeReference(asmRef, typeName[0..lastDot], typeName[(lastDot + 1)..]);
            }
            
            var parameters = ctorobj.Parameters.Select(e => TypeFromRef(e.Type)); 
            var signature = MethodSignature.CreateInstance(_corLibFactory.Void, parameters);

            var baset = baseType;
            var method = baset!.CreateMemberReference(".ctor", signature);
            if (method.Resolve() == null) throw new Exception("Extern constructor reference could not be solved: "
                                                              + baseType!.CreateMemberReference(".ctor", signature));
            var importedMethod = _module.DefaultImporter.ImportMethod(method);
            
            return new ConcreteFunctionData(importedMethod);
        }
        if (parent is not TypeDefinition @parentTypedef) throw new ArgumentNullException(nameof(parent));
        
        var name = ".ctor";

        MethodAttributes attributes = MethodAttributes.HideBySig
                                      | MethodAttributes.SpecialName
                                      | MethodAttributes.RuntimeSpecialName;
        
        var argTypes = ctorobj.Parameters.Select(p => TypeFromRef(p.Type));
        var argDefs = ctorobj.Parameters
            .Select((p, i) => new ParameterDefinition((ushort)(i + 1), p.Name, 0));

        MethodSignature sig = MethodSignature.CreateInstance(_corLibFactory.Void, argTypes);
        
        var m = new MethodDefinition(name, attributes, sig);
        foreach (var i in argDefs) m.ParameterDefinitions.Add(i);
        parentTypedef.Methods.Add(m);
        
        return new ConcreteFunctionData(m);
    }
    private IFunctionData DeclareFunction(FunctionObject funcobj, ITypeDefOrRef parent)    
    {
        if (funcobj.DotnetImport != null)
        {
            ITypeDefOrRef baseType;
            if (funcobj.DotnetImport.Value.AssemblyName == null && funcobj.DotnetImport.Value.ClassName == null) baseType = parent;
            else
            {
                var asmName = funcobj.DotnetImport.Value.AssemblyName;
                var typeName = funcobj.DotnetImport.Value.ClassName!;
                var lastDot = typeName.LastIndexOf('.');
            
                var asmRef = SolveAssemblyReference(asmName!);
                baseType = SolveTypeReference(asmRef, typeName[0..lastDot], typeName[(lastDot + 1)..]);
            }
            
            var methodName = funcobj.DotnetImport.Value.MethodName;
            MethodSignature signature;
            MemberReference method;
            
            if (funcobj.IsGeneric)
            {
                var returnType = TypeFromRef(funcobj.ReturnType);
                var firstParameter = funcobj.Parameters.FindIndex(e => e.Type is not TypeTypeReference);

                var generics = funcobj.Parameters[..firstParameter]
                    .Select(e => TypeFromRef(((TypeTypeReference)e.Type).ReferencedType));
                var parameters = funcobj.Parameters[firstParameter..]
                    .Select(e => TypeFromRef(e.Type));

                signature = new MethodSignature(
                    CallingConventionAttributes.Generic,
                    returnType,
                    parameters)
                { GenericParameterCount = firstParameter };
                
                method = baseType.CreateMemberReference(methodName, signature);
                _module.DefaultImporter.ImportMethodSignature(signature);
                _module.DefaultImporter.ImportMethod(method);
                return new GenericFunctionData(method, signature.ReturnsValue);
            }
            else
            {
                var returnType = TypeFromRef(funcobj.ReturnType);
                var parameters = funcobj.Parameters.Select(e => TypeFromRef(e.Type));
                signature = funcobj.Static
                    ? MethodSignature.CreateStatic(returnType, parameters)
                    : MethodSignature.CreateInstance(returnType, parameters);
                method = baseType.CreateMemberReference(methodName, signature);
                
                if (method.Resolve() == null)
                    throw new Exception($"Extern method reference could not be solved "
                                        + baseType.CreateMemberReference(methodName, signature));
                
                signature = _module.DefaultImporter.ImportMethodSignature(signature);
                IMethodDefOrRef importedMethod = _module.DefaultImporter.ImportMethod(method);
            
                return new ConcreteFunctionData(importedMethod);
            }
        }
        {
            if (parent is not TypeDefinition @parentTypedef) throw new ArgumentNullException(nameof(parent));
            if (funcobj.IsGeneric) return null!;
            
            var name = funcobj.Name;

            MethodAttributes attributes = 0;

            if (funcobj.Abstract) attributes |= MethodAttributes.Abstract;
            if (funcobj.Public) attributes |= MethodAttributes.Public;
            if (funcobj.Static) attributes |= MethodAttributes.Static;

            List<TypeSignature> argTypes = [];
            List<ParameterDefinition> argDefs = [];
            TypeSignature returnType;
            
            foreach (var (i, p) in funcobj.Parameters.Index())
            {
                    argTypes.Add(TypeFromRef(p.Type));
                    argDefs.Add(new ParameterDefinition((ushort)(argTypes.Count - 1), name, 0));
            }
            returnType = TypeFromRef(funcobj.ReturnType);
            
            var sig = funcobj.Static switch
            {
                true => MethodSignature.CreateStatic(returnType, argTypes),
                false => MethodSignature.CreateInstance(returnType, argTypes),
            };

            var m = new MethodDefinition(name, attributes, sig);
            foreach (var i in argDefs) m.ParameterDefinitions.Add(i);
            parentTypedef.Methods.Add(m);

            if (funcobj.Export == "main") _module.ManagedEntryPointMethod = m;
            return new ConcreteFunctionData(m);
        }
    }
    private FieldDefinition DeclareField(FieldObject fieldobj, ITypeDefOrRef parent)
    {
        if (parent is not TypeDefinition @parentTypedef) throw new ArgumentNullException(nameof(parent));
        
        FieldAttributes attributes = 0;
        
        if (fieldobj.Public) attributes |= FieldAttributes.Public;
        if (fieldobj.Static) attributes |= FieldAttributes.Static;
        
        var sig = new FieldSignature(CallingConventionAttributes.Default, TypeFromRef(fieldobj.Type));
        var f = new FieldDefinition(fieldobj.Name, attributes, sig);
        if (fieldobj.Offset.HasValue) f.FieldOffset = fieldobj.Offset!.Value.Bytes;
        parentTypedef.Fields.Add(f);
        return f;
    }
    
    private AssemblyReference SolveAssemblyReference(string? asmName)
    {
        var asmRef = _module.AssemblyReferences.FirstOrDefault(e => e.Name == asmName);
        if (asmRef != null)return asmRef;
        
        asmRef = new AssemblyReference(asmName, new Version()).ImportWith(_module.DefaultImporter);
        return _module.MetadataResolver.AssemblyResolver.Resolve(asmRef) == null
                ? throw new Exception($"Could not resolve assembly reference '{asmName}'")
                : asmRef;
    }
    private TypeReference SolveTypeReference(AssemblyReference assembly, string ns, string name)
    {
        var asm = assembly.Resolve();
        var a = asm.ManifestModule!.ExportedTypes.FirstOrDefault(e => e.Namespace == ns && e.Name == name);
        
        var typeRef = assembly.CreateTypeReference(ns, name);
        typeRef = (TypeReference)_module.DefaultImporter.ImportType(typeRef);
        
        if (typeRef.Resolve() == null) throw new Exception($"Could not resolve type reference '[{assembly.Name}]{typeRef}'");
        return typeRef;
    }
}
