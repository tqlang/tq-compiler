using System.Buffers.Binary;
using System.Diagnostics;
using Abstract.CodeProcess.Core.EvaluationData;
using Abstract.CodeProcess.Core.EvaluationData.IntermediateTree.Values;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.Dotnet;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin.Integer;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Collections;
using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
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

    private string launchConfig = 
        """
        {
            "runtimeOptions": {
                "tfm": "net10.0",
                "framework": {
                    "name": "Microsoft.NETCore.App",
                    "version": "10.0.0"
                },
                "configProperties": {
                    "System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization": false
                }
            }
        }
        """;
    
    public void Compile(ProgramObject program)
    {
        var programName = program.Modules[0].Name;
        
        _assembly = new AssemblyDefinition(programName + ".dll",
            new Version(1, 0, 0, 0));
        
        _module = new ModuleDefinition(programName, program.AssemblyResolver.CorLibReference)
        { MetadataResolver = new DefaultMetadataResolver(program.AssemblyResolver) };
        _assembly.Modules.Add(_module);
        
        LoadCoreLibResources();
        
        foreach (var m in program.Modules) SearchRecursive(m);
        
        DeclareTypes();
        ResolveContent();
        ImplementFieldInitializers();
        ImplementMethods();
        
        DumpModule();
        _module.Write($".abs-out/{programName}.dll");
        File.WriteAllText($".abs-out/{programName}.runtimeconfig.json", launchConfig);
    }
    
    private void SearchRecursive(LangObject obj)
    {
        switch (obj)
        {
            case TqModuleObject @a:
                if (a.Root == null) throw new Exception($"Module '{a.Name}'s root is null");
                SearchRecursive(a.Root);
                break;
            case DotnetModuleObject: return;

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
                
                var moduledef = new TypeDefinition(nmsp, name, attributes, _coreLib["System.Object"].t.ToTypeDefOrRef());
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
            
            case DotnetNamespaceObject:
            case DotnetTypeObject:
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
                
                var sig = MethodSignature.CreateStatic(_corLibFactory.Void);
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
                CompileIrNodeLoad(i.Value, false, ctx);
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
            if (v.Method is not MethodDefinition { HasMethodBody: false } @mDef) continue;
            
            var body = new CilMethodBody(mDef);
            mDef.CilMethodBody = body;
            
            var locals = new CilLocalVariable[k.Locals.Count];
            foreach (var local in k.Locals)
            {
                var l = new CilLocalVariable(TypeFromRef(local.Type));
                locals[local.index] = l;
                body.LocalVariables.Add(l);
            }
            if (locals.Length > 0) body.InitializeLocals = true;

            var args = new Parameter[mDef.GenericParameters.Count + mDef.Parameters.Count];
            mDef.Parameters.ToArray().CopyTo(args, mDef.GenericParameters.Count);
            ITypeDefOrRef? selfType = mDef.Signature!.HasThis ? mDef.DeclaringType : null;
            
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

        var typedef = new TypeDefinition(nmsp, name, attributes, _coreLib["System.ValueType"].t.ToTypeDefOrRef());
        typedef.ClassLayout = new ClassLayout((ushort)structobj.Alignment!.Value.Bytes, (uint)structobj.Length!.Value.Bytes);
        _module.TopLevelTypes.Add(typedef);
        
        return new StructData(typedef);
    }
    private EnumData DeclareTypedef(TypedefObject typedefobj)
    {
        var ns = string.Join('.', typedefobj.Global[0..^1]);
        var name = typedefobj.Name;

        var attributes = TypeAttributes.AnsiClass
                         | TypeAttributes.Sealed;

        bool isPrimitiveType;
        TypeSignature valueType;
        
        switch (typedefobj.BackType)
        {
            case null:
                valueType = _corLibFactory.Int64;
                isPrimitiveType = true;
                break;
            case RuntimeIntegerTypeReference @integer:
            {
                var s = integer.Signed;
                if (integer.BitSize.Bytes == 16)
                {
                    isPrimitiveType = false;
                    valueType = _coreLib[s ? "Int128" : "UInt128"].t;
                    break;
                }
            
                isPrimitiveType = true;
                valueType = integer.BitSize.Bytes switch
                {
                    1 => s ? _corLibFactory.SByte : _corLibFactory.Byte,
                    2 => s ? _corLibFactory.Int16 : _corLibFactory.UInt16,
                    4 => s ? _corLibFactory.Int32 : _corLibFactory.UInt32,
                    8 => s ? _corLibFactory.Int64 : _corLibFactory.UInt64,
                    _ => throw new ArgumentOutOfRangeException(nameof(typedefobj.BackType), typedefobj.BackType, null)
                };
                break;
            }
            case DotnetTypeReference { Reference: { IsEnum: true } @e }:
                isPrimitiveType = true;
                valueType = e.Reference.Fields[0].Signature!.FieldType;
                break;
            default:
                isPrimitiveType = false;
                valueType = TypeFromRef(typedefobj.BackType);
                break;
        }

        if (isPrimitiveType)
        {
            var enumType = new TypeDefinition(ns, name, attributes, _coreLib["System.Enum"].t.ToTypeDefOrRef());
            _module.TopLevelTypes.Add(enumType);

            var valueField = new FieldDefinition(
                "value__",
                FieldAttributes.Public | FieldAttributes.SpecialName | FieldAttributes.RuntimeSpecialName,
                valueType
            );
            enumType.Fields.Add(valueField);

            var enumData = new EnumData(enumType, valueField);

            List<(FieldDefinition, TypedefNamedValue)> fieldsWithDefaultValue = [];
            List<FieldDefinition> fieldsWithoutDefaultValue = [];
            
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
                
                enumType.Fields.Add(itemField);
                enumData.Items.Add(value, itemField);
                
                if (value.Value == null) fieldsWithoutDefaultValue.Add(itemField);
                else fieldsWithDefaultValue.Add((itemField, value));
            }

            var bytes = valueType.ElementType switch
            {
                ElementType.I1 or ElementType.U1 => new byte[1],
                ElementType.I2 or ElementType.U2 => new byte[2],
                ElementType.I4 or ElementType.U4 => new byte[4],
                ElementType.I8 or ElementType.U8 => new byte[8],
                
                _ => throw new ArgumentOutOfRangeException()
            };
            
            HashSet<ulong> usedValues = [];
            foreach (var (d, o) in fieldsWithDefaultValue)
            {
                if (o.Value is IrIntegerLiteral @intlit)
                {
                    var largeVal = (Int128)intlit.Value;
                    switch (valueType.ElementType)
                    {
                        case ElementType.I1 or ElementType.U1: bytes[0] = (byte)largeVal; break;
                        case ElementType.I2: BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(), unchecked((short)largeVal)); break;
                        case ElementType.U2: BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(), unchecked((ushort)largeVal)); break;
                        case ElementType.I4: BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(), unchecked((int)largeVal)); break;
                        case ElementType.U4: BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(), unchecked((uint)largeVal));break;
                        case ElementType.I8: BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(), unchecked((long)largeVal)); break;
                        case ElementType.U8: BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(), unchecked((ulong)largeVal)); break;
                        default: throw new ArgumentOutOfRangeException();
                    }
                    d.Constant = new Constant(valueType.ElementType, new DataBlobSignature(bytes.ToArray()));
                    usedValues.Add(unchecked((ulong)largeVal));
                }
                else
                {
                    var constant = o.Value switch
                    {
                        IrSolvedReference @r => r.Reference switch
                        {
                            DotnetFieldReference @fr => fr.Reference.Reference.Constant!,
                            _ => throw new NotImplementedException(),
                        },
                        _ => throw new NotImplementedException(),
                    };
                    d.Constant = constant;

                    var constBytes = constant.Value!.Data;
                    var entryValue = valueType.ElementType switch
                    {
                        ElementType.I1 or ElementType.U1 => constBytes[0],
                        ElementType.I2 => unchecked((ulong)BinaryPrimitives.ReadInt16LittleEndian(constBytes)),
                        ElementType.U2 => BinaryPrimitives.ReadUInt16LittleEndian(constBytes),
                        ElementType.I4 => unchecked((ulong)BinaryPrimitives.ReadInt32LittleEndian(constBytes)),
                        ElementType.U4 => BinaryPrimitives.ReadUInt32LittleEndian(constBytes),
                        ElementType.I8 => unchecked((ulong)BinaryPrimitives.ReadInt64LittleEndian(constBytes)),
                        ElementType.U8 => BinaryPrimitives.ReadUInt64LittleEndian(constBytes),

                        _ => throw new ArgumentOutOfRangeException()
                    };
                    usedValues.Add(entryValue);
                }
            }

            ulong val = 0;
            foreach (var i in fieldsWithoutDefaultValue)
            {
                while (usedValues.Contains(val)) val++;

                switch (valueType.ElementType)
                {
                    case ElementType.I1 or ElementType.U1: bytes[0] = (byte)val; break;
                    case ElementType.I2: BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(), unchecked((short)val)); break;
                    case ElementType.U2: BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(), unchecked((ushort)val)); break;
                    case ElementType.I4: BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(), unchecked((int)val)); break;
                    case ElementType.U4: BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(), unchecked((uint)val));break;
                    case ElementType.I8: BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(), unchecked((long)val)); break;
                    case ElementType.U8: BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(),val); break;
                    default: throw new ArgumentOutOfRangeException();
                }
                i.Constant = new Constant(valueType.ElementType, new DataBlobSignature(bytes.ToArray()));
            }
        
            return enumData;
        }
        else
        {
            var enumType = new TypeDefinition(ns, name, attributes, _corLibFactory.Object.ToTypeDefOrRef());
            _module.TopLevelTypes.Add(enumType);

            var valueField = new FieldDefinition(
                "value__",
                FieldAttributes.Public | FieldAttributes.SpecialName | FieldAttributes.RuntimeSpecialName,
                valueType
            );
            enumType.Fields.Add(valueField);
            
            MethodAttributes attributes2 = MethodAttributes.HideBySig
                                          | MethodAttributes.SpecialName
                                          | MethodAttributes.RuntimeSpecialName
                                          | MethodAttributes.Static;
            var sig = MethodSignature.CreateInstance(_corLibFactory.Void);
            var staticCtor = new MethodDefinition(".cctor", attributes2, sig);
            enumType.Methods.Add(staticCtor);
            
            var enumData = new EnumData(enumType, valueField);

            Dictionary<TypedefNamedValue, FieldDefinition> namedValues = [];
            
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
                namedValues.Add(value, itemField);
            }

            foreach (var i in namedValues)
            {
                // TODO implement static constructor
                throw new NotImplementedException();
            }
        
            return enumData;
        }
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
        var m = new MethodDefinition(name, attributes, sig) { IsStatic = false };
        foreach (var i in argDefs) m.ParameterDefinitions.Add(i);
        parentTypedef.Methods.Add(m);
        
        return new FunctionData(m);
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
        
        var m = new MethodDefinition(name, attributes, sig) { IsStatic = funcObj.Static};
        foreach (var i in generics) m.GenericParameters.Add(i);
        
        foreach (var i in parameterDefinitions) m.ParameterDefinitions.Add(i);
        parentTypedef.Methods.Add(m);

        if (funcObj.Name == "main") _module.ManagedEntryPointMethod = m;
        return new FunctionData(m);
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
