using System.Buffers.Binary;
using System.Diagnostics;
using Abstract.CodeProcess.Core.Language.EvaluationData;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageObjects;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.IO;
using AsmResolver.PE.DotNet.Cil;
using AssemblyDefinition = AsmResolver.DotNet.AssemblyDefinition;
using FieldAttributes = AsmResolver.PE.DotNet.Metadata.Tables.Rows.FieldAttributes;
using FieldDefinition = AsmResolver.DotNet.FieldDefinition;
using MethodAttributes = AsmResolver.PE.DotNet.Metadata.Tables.Rows.MethodAttributes;
using MethodDefinition = AsmResolver.DotNet.MethodDefinition;
using ModuleDefinition = AsmResolver.DotNet.ModuleDefinition;
using TypeAttributes = AsmResolver.PE.DotNet.Metadata.Tables.Rows.TypeAttributes;
using TypeDefinition = AsmResolver.DotNet.TypeDefinition;

namespace Abstract.CodeProcess;

public partial class Compiler
{
    private Dictionary<NamespaceObject, TypeDefinition> _namespacesMap = [];
    private Dictionary<FunctionObject, FunctionData> _functionsMap = [];
    private Dictionary<StructObject, StructData> _typesMap = [];
    private Dictionary<TypedefObject, EnumData> _enumsMap = [];
    private Dictionary<FieldObject, FieldDefinition> _fieldsMap = [];

    private AssemblyDefinition _assembly;
    private ModuleDefinition _module;
    
    private CorLibTypeFactory _corLibFactory;
    private Dictionary<string, (TypeSignature t, Dictionary<string, IMethodDescriptor> m)> _coreLib = [];
    private static Version ZeroVersion = new Version(0, 0, 0, 0);
    
    public void Compile(ProgramObject program)
    {
        var programName = program.Modules[0].Name;
        
        _assembly = new AssemblyDefinition(programName + ".dll",
            new Version(1, 0, 0, 0));

        var runtimeVersion = new Version(10, 0, 0, 0);
        var systemCore = new AssemblyReference("System.Runtime", new Version(10, 0, 0, 0))
        { PublicKeyOrToken = [ 0xb0, 0x3f, 0x5f, 0x7f, 0x11, 0xd5, 0x0a, 0x3a ] };

        _module = new ModuleDefinition(programName, systemCore)
        { MetadataResolver = new DefaultMetadataResolver(new CustomAssemblyResolver(runtimeVersion)) };
        _assembly.Modules.Add(_module);
        
        LoadCoreLibResources();
        
        foreach (var m in program.Modules) SearchRecursive(m);
        
        DeclareTypes();
        ResolveContent();
        ImplementMethods();
        
        DumpModule();
        _module.Write($".abs-out/{programName}.dll");
    }
    
    private void SearchRecursive(LangObject obj)
    {
        switch (obj)
        {
            case ModuleObject @a:
                foreach (var i in a.Children) SearchRecursive(i);
                break;

            case NamespaceObject @a:
            {
                var attributes = TypeAttributes.AnsiClass
                                 | TypeAttributes.Class
                                 | TypeAttributes.Sealed
                                 | TypeAttributes.Public
                                 | TypeAttributes.Abstract;

                var isroot = string.IsNullOrEmpty(a.Name);
                var name = isroot ? "Static" : a.Name;
                var nmsp = isroot ? obj.Module!.Name : string.Join('.', a.Global[0..^1]);
                
                var moduledef = new TypeDefinition(nmsp, name, attributes, _coreLib["Object"].t.ToTypeDefOrRef());
                _module.TopLevelTypes.Add(moduledef);
                _namespacesMap.Add(a, moduledef);
                
                foreach (var i in a.Children) SearchRecursive(i);
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
            var fun = DeclareFunction(k, _namespacesMap[k.Namespace!]);
            _functionsMap[k] = fun;
        }

        foreach (var (k, _) in _fieldsMap)
        {
            var newfield = DeclareField(k, _namespacesMap[k.Namespace!]);
            _fieldsMap[k] = newfield;
        }
        
        foreach (var (k, v) in _typesMap)
        {
            if (v.Type is not TypeDefinition @typedef) continue;
            
            if (k.Extends != null)
            {
                FieldAttributes attributes = FieldAttributes.Public | FieldAttributes.SpecialName;
                var sig = new FieldSignature(CallingConventionAttributes.Default, TypeFromRef(k.Extends));
                var f = new FieldDefinition("base", attributes, sig) { FieldOffset = 0 };
                typedef.Fields.Add(f);
            }
            
            {
                var signature = MethodSignature.CreateInstance(_corLibFactory.Void);
                var ctor = new MethodDefinition(".ctor",
                    MethodAttributes.Public
                    | MethodAttributes.HideBySig
                    | MethodAttributes.SpecialName
                    | MethodAttributes.RuntimeSpecialName,
                    signature);

                typedef.Methods.Add(ctor);
                v.PrimaryCtor = ctor;

                var body = new CilMethodBody(ctor);
                ctor.CilMethodBody = body;

                //var typeRef = v.Type.BaseType!;
                //var subctor = CreateMethodRef(typeRef, ".ctor",
                //    MethodSignature.CreateInstance(_corLibFactory.Void));
                
                body.Instructions.Add(CilOpCodes.Ldarg_0);
                body.Instructions.Add(CilOpCodes.Initobj, v.Type);
                body.Instructions.Add(CilOpCodes.Ret);
                
                body.Instructions.OptimizeMacros();
                body.ComputeMaxStack();
            }
            
            if (!typedef.IsValueType) {
                var signature = MethodSignature.CreateInstance(_coreLib["Object"].t);
                var clone = new MethodDefinition("<clone>",
                    MethodAttributes.Public
                    | MethodAttributes.HideBySig,
                    signature);

                typedef.Methods.Add(clone);
                v.Clone = clone;

                var body = new CilMethodBody(clone);
                clone.CilMethodBody = body;
                
                body.Instructions.Add(CilOpCodes.Ldarg_0);
                body.Instructions.Add(CilOpCodes.Call, _coreLib["Object"].m["MemberwiseClone"]);
                body.Instructions.Add(CilOpCodes.Ret);
                
                body.Instructions.OptimizeMacros();
                body.ComputeMaxStack();
            }

            foreach (var i in k.Children)
            {
                switch (i)
                {
                    case FieldObject @a:
                    { 
                        var f = DeclareField(a, typedef);
                        _fieldsMap.Add(a, f);
                    } break;
                    
                    case FunctionGroupObject @fg:
                    {
                        foreach (var j in fg.Overloads)
                        {
                            var f = DeclareFunction(j, typedef);
                            _functionsMap.Add(j, f);
                        }
                    } break;
                    
                    default: throw new UnreachableException();
                }
            }
        }
    }

    private void ImplementMethods()
    {
        foreach (var (k, v) in _functionsMap)
        {
            if (k.Body == null) continue;
            if (v.Def.HasMethodBody) continue;
            
            var body = new CilMethodBody(v.Def);
            v.Def.CilMethodBody = body;
            
            var locals = new CilLocalVariable[k.Locals.Length];
            foreach (var local in k.Locals)
            {
                var l = new CilLocalVariable(TypeFromRef(local.Type));
                locals[local.index] = l;
                body.LocalVariables.Add(l);
            }
            if (locals.Length > 0) body.InitializeLocals = true;
            
            var args = v.Def.Parameters.ToArray();
            var stack = new List<TypeSignature>();

            var ctx = new Context(body.Instructions, args, locals);
            CompileIr(k.Body!, ctx);
            if (body.Instructions.Count == 0 || body.Instructions[^1].OpCode != CilOpCodes.Ret)
            {
                body.Instructions.Add(CilOpCodes.Ret);
                if (v.ReturnsValue) ctx.StackPop();
            }
            
            //if (ctx.Stack.Count != 0) throw new UnreachableException();
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
            var typedef = SolveTypeReference(asmRef, typeName[0..lastDot], typeName[(lastDot + 1)..]).Resolve()!;
            var valueField = typedef.Fields.First(e => e.Name == "value__");
            
            var enumdata = new EnumData(typedef, valueField);

            foreach (var value in typedefobj.Children.OfType<TypedefItemObject>())
            {
                var f = typedef.Fields.FirstOrDefault(e => e.Name == value.Name);
                if (f == null) throw new Exception("Extern enum member not found: " + value.Name);
                enumdata.Items.Add(value, f);
            }

            return enumdata;
        }
        else
        {
            var nmsp = string.Join('.', typedefobj.Global[0..^1]);
            var name = typedefobj.Name;

            var attributes = TypeAttributes.AnsiClass
                             | TypeAttributes.ExplicitLayout
                             | TypeAttributes.Sealed
                             | TypeAttributes.Serializable
                             | TypeAttributes.SpecialName;

            var enumType = new TypeDefinition(nmsp, name, attributes, _coreLib["Enum"].t.ToTypeDefOrRef())
                { ClassLayout = new ClassLayout(8, 8) };
            _module.TopLevelTypes.Add(enumType);

            var valueField = new FieldDefinition(
                "value__",
                FieldAttributes.Public | FieldAttributes.SpecialName | FieldAttributes.RuntimeSpecialName,
                _corLibFactory.UInt64
            ) { FieldOffset = 0 };
            enumType.Fields.Add(valueField);

            var enumdata = new EnumData(enumType, valueField);

            ulong i = 0;
            foreach (var value in typedefobj.Children.OfType<TypedefItemObject>())
            {
                var itemField = new FieldDefinition(
                    value.Name,
                    FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal
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

    private FunctionData DeclareFunction(FunctionObject funcobj, TypeDefinition parent)
    {
        if (funcobj.DotnetImport != null)
        {
            TypeDefinition? baseType;
            if (funcobj.DotnetImport.Value.AssemblyName == null && funcobj.DotnetImport.Value.ClassName == null)
            {
                baseType = parent;
            }
            else
            {
                var asmName = funcobj.DotnetImport.Value.AssemblyName;
                var typeName = funcobj.DotnetImport.Value.ClassName;
                var lastDot = typeName.LastIndexOf('.');
            
                var asmRef = SolveAssemblyReference(asmName!);
                baseType = SolveTypeReference(asmRef, typeName[0..lastDot], typeName[(lastDot + 1)..]).Resolve()!;
            }
            
            var methodName = funcobj.DotnetImport.Value.MethodName;

            var returnType = TypeFromRef(funcobj.ReturnType);
            var parameters = funcobj.Parameters.Select(e => TypeFromRef(e.Type)); 
            var signature = funcobj.Static
                ? MethodSignature.CreateStatic(returnType, parameters)
                : MethodSignature.CreateInstance(returnType, parameters);
            signature = _module.DefaultImporter.ImportMethodSignature(signature);

            var baset = baseType;
            var method = baset.CreateMemberReference(methodName, signature);
            IMethodDefOrRef? methoddef = _module.DefaultImporter.ImportMethod(method).Resolve();
            methoddef = _module.DefaultImporter.ImportMethod(methoddef);
            
            return methoddef == null
                ? throw new Exception($"Extern method reference could not be solved: {baseType.CreateMemberReference(methodName, signature)}")
                : new FunctionData(methoddef);
        }

        var nmsp = string.Join('.', funcobj.Global[0..^1]);
        var name = funcobj.Name;

        MethodAttributes attributes = 0;

        if (funcobj.Abstract) attributes |= MethodAttributes.Abstract;
        if (funcobj.Public) attributes |= MethodAttributes.Public;
        if (funcobj.Static) attributes |= MethodAttributes.Static;
        
        var argTypes = funcobj.Parameters
            .Select(p => TypeFromRef(p.Type));
        var argDefs = funcobj.Parameters
            .Select((p, i) => new ParameterDefinition((ushort)(i + 1), p.Name, 0));

        MethodSignature sig = funcobj.Static switch
        {
            true => MethodSignature.CreateStatic(TypeFromRef(funcobj.ReturnType), argTypes),
            false => MethodSignature.CreateInstance(TypeFromRef(funcobj.ReturnType), argTypes),
        };
        
        var m = new MethodDefinition(name, attributes, sig);
        foreach (var i in argDefs) m.ParameterDefinitions.Add(i);
        parent.Methods.Add(m);

        if (funcobj.Export == "main") _module.ManagedEntryPointMethod = m;
        return new FunctionData(m);
    }
    private FieldDefinition DeclareField(FieldObject fieldobj, TypeDefinition parent)
    {
        FieldAttributes attributes = 0;
        
        if (fieldobj.Public) attributes |= FieldAttributes.Public;
        if (fieldobj.Static) attributes |= FieldAttributes.Static;
        
        var sig = new FieldSignature(CallingConventionAttributes.Default, TypeFromRef(fieldobj.Type));
        var f = new FieldDefinition(fieldobj.Name, attributes, sig);
        if (fieldobj.Offset.HasValue) f.FieldOffset = fieldobj.Offset!.Value.Bytes;
        parent.Fields.Add(f);
        return f;
    }


    private AssemblyReference SolveAssemblyReference(string? asmName)
    {
        var asmRef = _module.AssemblyReferences.FirstOrDefault(e => e.Name == asmName);
        if (asmRef != null)return asmRef;
        
        asmRef = new AssemblyReference(asmName, ZeroVersion).ImportWith(_module.DefaultImporter);
        if (asmRef.Resolve() == null) throw new Exception($"Could not resolve assembly reference '{asmName}'");
        
        return asmRef;
    }
    private TypeReference SolveTypeReference(AssemblyReference assembly, string ns, string name)
    {
        var typeRef = assembly.CreateTypeReference(ns, name);
        typeRef = (TypeReference)_module.DefaultImporter.ImportType(typeRef);
        return typeRef.Resolve() != null ? typeRef : throw new Exception($"Could not resolve type reference '[{assembly.Name}]{typeRef}'");
    }

    private class CustomAssemblyResolver : DotNetCoreAssemblyResolver
    {
        private List<string> _resolvingDirectories = [];
        
        public CustomAssemblyResolver(Version runtimeVersion)
            : base(UncachedFileService.Instance, runtimeVersion)
        {
            var paths = new DotNetCorePathProvider().GetRuntimePathCandidates(runtimeVersion);
            _resolvingDirectories.AddRange(paths);
        }
        
        protected override string? ProbeRuntimeDirectories(AssemblyDescriptor assembly)
        {
            var asmName = assembly.Name;
            var asmVersion = assembly.Version;
            
            foreach (var dir in _resolvingDirectories)
            {
                var file = Path.Combine(dir, $"{asmName}.dll");
                if (!File.Exists(file)) continue;
                if (asmVersion == null!
                    || asmVersion == ZeroVersion 
                    || AssemblyDefinition.FromFile(file).Version == asmVersion) return file;
            }
            return null;
        }
        protected override AssemblyDefinition? ResolveImpl(AssemblyDescriptor assembly)
        {
            var path = ProbeRuntimeDirectories(assembly);
            Console.WriteLine($"Resolving {assembly.Name} ({path})");
            return path == null ? null : LoadAssemblyFromFile(path);
        }
    }
}
