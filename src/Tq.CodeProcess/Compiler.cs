using System.Buffers.Binary;
using System.Diagnostics;
using Abstract.CodeProcess.Core.EvaluationData;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Collections;
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

namespace Abstract.CodeProcess;

public partial class Compiler
{
    private Dictionary<TqNamespaceObject, TypeDefinition> _namespacesMap = [];
    private Dictionary<ICallable, FunctionData> _functionsMap = [];
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
                foreach (var i in a.Namespaces) SearchRecursive(i);
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
        foreach (var (k, v) in _functionsMap)
        {
            if (k.Body == null) continue;
            if (v.Definition!.HasMethodBody) continue;
            
            var body = new CilMethodBody(v.Definition);
            v.Definition.CilMethodBody = body;
            
            var locals = new CilLocalVariable[k.Locals.Count];
            foreach (var local in k.Locals)
            {
                var l = new CilLocalVariable(TypeFromRef(local.Type));
                locals[local.index] = l;
                body.LocalVariables.Add(l);
            }
            if (locals.Length > 0) body.InitializeLocals = true;

            var args = new Parameter[v.Definition.GenericParameters.Count + v.Definition.Parameters.Count];
            v.Definition.Parameters.ToArray().CopyTo(args, v.Definition.GenericParameters.Count);
            ITypeDefOrRef? selfType = !v.IsStatic ? v.Definition.DeclaringType : null;
            
            var ctx = new Context(selfType, body, _module.DefaultImporter, args, locals);
            
            CompileIr(k.Body!, ctx);
            if (body.Instructions.Count == 0 || body.Instructions[^1].OpCode != CilOpCodes.Ret)
            {
                body.Instructions.Add(CilOpCodes.Ret);
                if (v.ReturnsValue) ctx.StackPop();
            }
            
            //if (ctx.Stack.Count != 0) throw new UnreachableException();
            //body.ComputeMaxStack();
            body.Instructions.CalculateOffsets();
        }
    }


    private StructData DeclareType(StructObject structobj)
    {
        var nmsp = string.Join('.', structobj.Global[0..^1]);
        var name = structobj.Name;

        var attributes = TypeAttributes.AnsiClass;

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
        
    private FunctionData DeclareCtor(ConstructorObject ctorobj, ITypeDefOrRef parent)
    {
        if (parent is not TypeDefinition @parentTypedef) throw new ArgumentNullException(nameof(parent));
        
        var name = ".ctor";

        var attributes = MethodAttributes.HideBySig
                         | MethodAttributes.SpecialName
                         | MethodAttributes.RuntimeSpecialName;
        
        var argTypes = ctorobj.Parameters.Select(p => TypeFromRef(p.Type));
        var argDefs = ctorobj.Parameters
            .Select((p, i) => new ParameterDefinition((ushort)(i + 1), p.Name, 0));

        var sig = MethodSignature.CreateInstance(_corLibFactory.Void, argTypes);
        
        var m = new MethodDefinition(name, attributes, sig);
        foreach (var i in argDefs) m.ParameterDefinitions.Add(i);
        parentTypedef.Methods.Add(m);
        
        return new FunctionData(m, sig);
    }
    private FunctionData DeclareFunction(FunctionObject funcObj, ITypeDefOrRef parent)    
    {
        if (parent is not TypeDefinition @parentTypedef) throw new ArgumentNullException(nameof(parent));
        
        var name = funcObj.Name;

        MethodAttributes attributes = 0;

        if (funcObj.Abstract) attributes |= MethodAttributes.Abstract;
        if (funcObj.Public) attributes |= MethodAttributes.Public;
        if (funcObj.Static) attributes |= MethodAttributes.Static;
        
        List<TypeSignature> parameterTypes = [];
        List<ParameterDefinition> parameterDefinitions = [];
        List<GenericParameter> generics = [];
        TypeSignature returnType;
        
        foreach (var (i, p) in funcObj.Parameters.Index())
        {
            if (p.IsGeneric)
            {
                generics.Add(new GenericParameter(p.Name));
                continue;
            }
            parameterTypes.Add(TypeFromRef(p.Type));
            parameterDefinitions.Add(new ParameterDefinition((ushort)(parameterTypes.Count), p.Name, 0));
        }
        returnType = TypeFromRef(funcObj.ReturnType);
        
        var sig = funcObj.Static switch
        {
            true => MethodSignature.CreateStatic(returnType, parameterTypes),
            false => MethodSignature.CreateInstance(returnType, parameterTypes),
        };
        sig.GenericParameterCount = generics.Count;
        sig.IsGeneric = generics.Count > 0;
        
        var m = new MethodDefinition(name, attributes, sig);
        foreach (var i in generics) m.GenericParameters.Add(i);
        
        foreach (var i in parameterDefinitions) m.ParameterDefinitions.Add(i);
        parentTypedef.Methods.Add(m);

        if (funcObj.Export == "main") _module.ManagedEntryPointMethod = m;
        return new FunctionData(m, m.Signature!);
    }
    private FieldDefinition DeclareField(FieldObject fieldobj, ITypeDefOrRef parent)
    {
        if (parent is not TypeDefinition @parentTypedef) throw new ArgumentNullException(nameof(parent));
        
        FieldAttributes attributes = 0;
        
        if (fieldobj.Public) attributes |= FieldAttributes.Public;
        if (fieldobj.Static) attributes |= FieldAttributes.Static;
        
        var sig = new FieldSignature(CallingConventionAttributes.Default, TypeFromRef(fieldobj.Type));
        var f = new FieldDefinition(fieldobj.Name, attributes, sig);
        parentTypedef.Fields.Add(f);
        return f;
    }
    
}
