using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using Abstract.CodeProcess.Core.Language.EvaluationData;
using Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree;
using Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Expressions;
using Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Statements;
using Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Values;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.CodeReferences;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.FieldReferences;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.FunctionReferences;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences.Builtin;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences.Builtin.Integer;
using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Collections;
using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet.Signatures.Types;
using AssemblyDefinition = AsmResolver.DotNet.AssemblyDefinition;
using FieldAttributes = AsmResolver.PE.DotNet.Metadata.Tables.Rows.FieldAttributes;
using FieldDefinition = AsmResolver.DotNet.FieldDefinition;
using MethodAttributes = AsmResolver.PE.DotNet.Metadata.Tables.Rows.MethodAttributes;
using MethodDefinition = AsmResolver.DotNet.MethodDefinition;
using ModuleDefinition = AsmResolver.DotNet.ModuleDefinition;
using TypeAttributes = AsmResolver.PE.DotNet.Metadata.Tables.Rows.TypeAttributes;
using TypeDefinition = AsmResolver.DotNet.TypeDefinition;
using TypeReference = Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences.TypeReference;

namespace Abstract.CodeProcess;

public class Compiler
{
    private Dictionary<NamespaceObject, TypeDefinition> _namespacesMap = [];
    private Dictionary<FunctionObject, IMethodDescriptor> _functionsMap = [];
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

        var systemCore = new AssemblyReference("System.Runtime", new Version(10, 0, 0, 0))
        {
            PublicKeyOrToken = [ 0xb0, 0x3f, 0x5f, 0x7f, 0x11, 0xd5, 0x0a, 0x3a ]
        };
        
        _module = new ModuleDefinition(programName, systemCore);
        
        _assembly.Modules.Add(_module);
        LoadCoreLibResources();
        
        foreach (var m in program.Modules) SearchRecursive(m);
        
        DeclareTypes();
        ResolveContent();
        ImplementMethods();
        
        DumpModule();
        _module.Write($".abs-out/{programName}.dll");
    }

    private void DumpModule()
    {
        var sb = new StringBuilder();
        foreach (var type in _module.GetAllTypes())
        {
            sb.Append('\n');
            sb.Append(type.IsValueType ? "struct " : "class ");
            sb.AppendLine($"{type.FullName} extends {type.BaseType} {{");

            foreach (var field in type.Fields)
            {
                sb.Append("\tfield ");
                sb.Append(field.IsPublic ? "public " : "private ");
                sb.Append(field.IsStatic ? "static " : "instance ");
                sb.AppendLine($"{field.Signature} {field.Name}");
            }
                
            foreach (var method in type.Methods)
            {
                sb.Append("\n\tmethod ");
                sb.Append(method.IsPublic ? "public " : "private ");
                sb.Append(method.IsStatic ? "static " : "instance ");
                sb.Append($"{method.Name} ({string.Join(", ", method.Parameters)}) ");
                sb.Append($"{method.Signature!.ReturnType} ");
                sb.AppendLine("{");
                if (method.CilMethodBody != null)
                {
                    foreach (var local in method.CilMethodBody.LocalVariables)
                        sb.AppendLine($"\t\t.locals init ({local.VariableType} {local})");
                        
                    foreach (var inst in method.CilMethodBody.Instructions)
                        sb.AppendLine($"\t\t{inst}");
                }
                sb.AppendLine("\t}");
            }
            sb.AppendLine("}");
        }
            
        File.WriteAllText(".abs-cache/debug/dlldump.il", sb.ToString());
    }
    
    private void LoadCoreLibResources()
    {
        var cl = _corLibFactory = _module.CorLibTypeFactory;
        
        Dictionary<string, IMethodDescriptor> methods;
        AsmResolver.DotNet.TypeReference typeref;
        ITypeDefOrRef type;
        TypeSignature self;
        
        methods = [];
        type = _module.DefaultImporter.ImportType(cl.CorLibScope.CreateTypeReference("System", "ValueType"));
        self = _module.DefaultImporter.ImportTypeSignature(type.ToTypeSignature());
        {
            
        }
        methods.TrimExcess();
        _coreLib.Add(type.Name!, (self, []));
        
        methods = [];
        self = cl.Object;
        type = _module.DefaultImporter.ImportType(self.ToTypeDefOrRef());
        {
            methods.Add("ToString", CreateMethodRef(type, "ToString", MethodSignature.CreateInstance(cl.String)));
            methods.Add("MemberwiseClone", CreateMethodRef(type, "MemberwiseClone", MethodSignature.CreateInstance(cl.Object)));
        }
        methods.TrimExcess();
        _coreLib.Add(type.Name!, (self, methods));
        
        methods = [];
        type = _module.DefaultImporter.ImportType(cl.CorLibScope.CreateTypeReference("System", "Enum"));
        self = _module.DefaultImporter.ImportTypeSignature(type.ToTypeSignature());
        {
            
        }
        methods.TrimExcess();
        _coreLib.Add(type.Name!, (self, []));
        
        methods = [];
        type = _module.DefaultImporter.ImportType(cl.CorLibScope.CreateTypeReference("System", "Int128"));
        self = _module.DefaultImporter.ImportTypeSignature(type.ToTypeSignature());
        {
            methods.Add("new", CreateMethodRef(type, ".ctor", MethodSignature.CreateInstance(cl.Void, cl.UInt64, cl.UInt64)));
            
            methods.Add("Parse", CreateMethodRef(type, "Parse", MethodSignature.CreateStatic(self, cl.String)));
            methods.Add("Add_ovf", CreateMethodRef(type, "op_Addition", MethodSignature.CreateStatic(self, self, self)));
            methods.Add("Add", CreateMethodRef(type, "op_CheckedAddition", MethodSignature.CreateStatic(self, self, self)));
            methods.Add("Sub_ovf", CreateMethodRef(type, "op_Subtraction", MethodSignature.CreateStatic(self, self, self)));
            methods.Add("Sub", CreateMethodRef(type, "op_CheckedSubtraction", MethodSignature.CreateStatic(self, self, self)));
            methods.Add("Mul", CreateMethodRef(type, "op_Multiply", MethodSignature.CreateStatic(self, self, self)));
            methods.Add("Div", CreateMethodRef(type, "op_Division", MethodSignature.CreateStatic(self, self, self)));
            methods.Add("Rem", CreateMethodRef(type, "op_Modulus", MethodSignature.CreateStatic(self, self, self)));
            
            methods.Add("BitwiseAnd", CreateMethodRef(type, "op_BitwiseAnd", MethodSignature.CreateStatic(self, self, self)));
            methods.Add("BitwiseOr", CreateMethodRef(type, "op_BitwiseOr", MethodSignature.CreateStatic(self, self, self)));
            methods.Add("BitwiseXor", CreateMethodRef(type, "op_ExclusiveOr", MethodSignature.CreateStatic(self, self, self)));
            methods.Add("BitwiseNot", CreateMethodRef(type, "op_OnesComplement", MethodSignature.CreateStatic(self, self)));
            methods.Add("LeftShift", CreateMethodRef(type, "op_LeftShift", MethodSignature.CreateStatic(self, self, cl.Int32)));
            methods.Add("RightShift", CreateMethodRef(type, "op_RightShift", MethodSignature.CreateStatic(self, self, cl.Int32)));
            
            methods.Add("Conv_from_i8", CreateMethodRef(type, "op_Implicit", MethodSignature.CreateStatic(self, cl.SByte)));
            methods.Add("Conv_from_u8", CreateMethodRef(type, "op_Implicit", MethodSignature.CreateStatic(self, cl.Byte)));
            methods.Add("Conv_from_i16", CreateMethodRef(type, "op_Implicit", MethodSignature.CreateStatic(self, cl.Int16)));
            methods.Add("Conv_from_u16", CreateMethodRef(type, "op_Implicit", MethodSignature.CreateStatic(self, cl.UInt16)));
            methods.Add("Conv_from_i32", CreateMethodRef(type, "op_Implicit", MethodSignature.CreateStatic(self, cl.Int32)));
            methods.Add("Conv_from_u32", CreateMethodRef(type, "op_Implicit", MethodSignature.CreateStatic(self, cl.UInt32)));
            methods.Add("Conv_from_i64", CreateMethodRef(type, "op_Implicit", MethodSignature.CreateStatic(self, cl.Int64)));
            methods.Add("Conv_from_u64", CreateMethodRef(type, "op_Implicit", MethodSignature.CreateStatic(self, cl.UInt64)));
            
            methods.Add("Conv_to_i8", CreateMethodRef(type, "op_Explicit", MethodSignature.CreateStatic(cl.SByte, self)));
            methods.Add("Conv_to_u8", CreateMethodRef(type, "op_Explicit", MethodSignature.CreateStatic(cl.Byte, self)));
            methods.Add("Conv_to_i16", CreateMethodRef(type, "op_Explicit", MethodSignature.CreateStatic(cl.Int16, self)));
            methods.Add("Conv_to_u16", CreateMethodRef(type, "op_Explicit", MethodSignature.CreateStatic(cl.UInt16, self)));
            methods.Add("Conv_to_i32", CreateMethodRef(type, "op_Explicit", MethodSignature.CreateStatic(cl.Int32, self)));
            methods.Add("Conv_to_u32", CreateMethodRef(type, "op_Explicit", MethodSignature.CreateStatic(cl.UInt32, self)));
            methods.Add("Conv_to_i64", CreateMethodRef(type, "op_Explicit", MethodSignature.CreateStatic(cl.Int64, self)));
            methods.Add("Conv_to_u64", CreateMethodRef(type, "op_Explicit", MethodSignature.CreateStatic(cl.UInt64, self)));
        }
        methods.TrimExcess();
        _coreLib.Add(type.Name!, (self, methods));
        
        methods = [];
        type = _module.DefaultImporter.ImportType(cl.CorLibScope.CreateTypeReference("System", "UInt128"));
        self = _module.DefaultImporter.ImportTypeSignature(type.ToTypeSignature());
        {
            methods.Add("new", CreateMethodRef(type, ".ctor", MethodSignature.CreateInstance(cl.Void, cl.UInt64, cl.UInt64)));
            
            methods.Add("Parse", CreateMethodRef(type, "Parse", MethodSignature.CreateStatic(self, cl.String)));
            methods.Add("Add_ovf", CreateMethodRef(type, "op_Addition", MethodSignature.CreateStatic(self, self, self)));
            methods.Add("Add", CreateMethodRef(type, "op_CheckedAddition", MethodSignature.CreateStatic(self, self, self)));
            methods.Add("Sub_ovf", CreateMethodRef(type, "op_Subtraction", MethodSignature.CreateStatic(self, self, self)));
            methods.Add("Sub", CreateMethodRef(type, "op_CheckedSubtraction", MethodSignature.CreateStatic(self, self, self)));
            methods.Add("Mul", CreateMethodRef(type, "op_Multiply", MethodSignature.CreateStatic(self, self, self)));
            methods.Add("Div", CreateMethodRef(type, "op_Division", MethodSignature.CreateStatic(self, self, self)));
            methods.Add("Rem", CreateMethodRef(type, "op_Modulus", MethodSignature.CreateStatic(self, self, self)));
            
            methods.Add("BitwiseAnd", CreateMethodRef(type, "op_BitwiseAnd", MethodSignature.CreateStatic(self, self, self)));
            methods.Add("BitwiseOr", CreateMethodRef(type, "op_BitwiseOr", MethodSignature.CreateStatic(self, self, self)));
            methods.Add("BitwiseXor", CreateMethodRef(type, "op_ExclusiveOr", MethodSignature.CreateStatic(self, self, self)));
            methods.Add("BitwiseNot", CreateMethodRef(type, "op_OnesComplement", MethodSignature.CreateStatic(self, self)));
            methods.Add("LeftShift", CreateMethodRef(type, "op_LeftShift", MethodSignature.CreateStatic(self, self, cl.Int32)));
            methods.Add("RightShift", CreateMethodRef(type, "op_RightShift", MethodSignature.CreateStatic(self, self, cl.Int32)));
            
            methods.Add("Conv_from_i8", CreateMethodRef(type, "op_Explicit", MethodSignature.CreateStatic(self, cl.SByte)));
            methods.Add("Conv_from_u8", CreateMethodRef(type, "op_Implicit", MethodSignature.CreateStatic(self, cl.Byte)));
            methods.Add("Conv_from_i16", CreateMethodRef(type, "op_Explicit", MethodSignature.CreateStatic(self, cl.Int16)));
            methods.Add("Conv_from_u16", CreateMethodRef(type, "op_Implicit", MethodSignature.CreateStatic(self, cl.UInt16)));
            methods.Add("Conv_from_i32", CreateMethodRef(type, "op_Explicit", MethodSignature.CreateStatic(self, cl.Int32)));
            methods.Add("Conv_from_u32", CreateMethodRef(type, "op_Implicit", MethodSignature.CreateStatic(self, cl.UInt32)));
            methods.Add("Conv_from_i64", CreateMethodRef(type, "op_Explicit", MethodSignature.CreateStatic(self, cl.Int64)));
            methods.Add("Conv_from_u64", CreateMethodRef(type, "op_Implicit", MethodSignature.CreateStatic(self, cl.UInt64)));
            
            methods.Add("Conv_to_i8", CreateMethodRef(type, "op_Explicit", MethodSignature.CreateStatic(cl.SByte, self)));
            methods.Add("Conv_to_u8", CreateMethodRef(type, "op_Explicit", MethodSignature.CreateStatic(cl.Byte, self)));
            methods.Add("Conv_to_i16", CreateMethodRef(type, "op_Explicit", MethodSignature.CreateStatic(cl.Int16, self)));
            methods.Add("Conv_to_u16", CreateMethodRef(type, "op_Explicit", MethodSignature.CreateStatic(cl.UInt16, self)));
            methods.Add("Conv_to_i32", CreateMethodRef(type, "op_Explicit", MethodSignature.CreateStatic(cl.Int32, self)));
            methods.Add("Conv_to_u32", CreateMethodRef(type, "op_Explicit", MethodSignature.CreateStatic(cl.UInt32, self)));
            methods.Add("Conv_to_i64", CreateMethodRef(type, "op_Explicit", MethodSignature.CreateStatic(cl.Int64, self)));
            methods.Add("Conv_to_u64", CreateMethodRef(type, "op_Explicit", MethodSignature.CreateStatic(cl.UInt64, self)));
        }
        methods.TrimExcess();
        _coreLib.Add(type.Name!, (self, methods));
        
        methods = [];
        self = cl.String;
        type = _module.DefaultImporter.ImportType(self.ToTypeDefOrRef());
        {
            methods.Add("Concat", CreateMethodRef(type, "Concat", MethodSignature.CreateStatic(cl.String.MakeArrayType(1))));
        }
        methods.TrimExcess();
        _coreLib.Add(type.Name!, (self, methods));
        
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
            var nmsp = string.Join('.', k.Global[0..^1]);
            var name = k.Name;

            var attributes = TypeAttributes.AnsiClass
                             | TypeAttributes.ExplicitLayout;
            
            if (k.Public) attributes |= TypeAttributes.Public;
            if (k.Abstract) attributes |= TypeAttributes.Abstract;
            if (k.Final) attributes |= TypeAttributes.Sealed;
            
            var typedef = new TypeDefinition(nmsp, name, attributes, _coreLib["ValueType"].t.ToTypeDefOrRef());
            typedef.ClassLayout = new ClassLayout((ushort)k.Alignment!.Value.Bytes, (uint)k.Length!.Value.Bytes);
            _module.TopLevelTypes.Add(typedef);
            _typesMap[k] = new StructData(typedef);
        }

        foreach (var (k, v) in _enumsMap)
        {
            var nmsp = string.Join('.', k.Global[0..^1]);
            var name = k.Name;
            
            var attributes = TypeAttributes.AnsiClass
                             //| TypeAttributes.ExplicitLayout
                             | TypeAttributes.Sealed
                             | TypeAttributes.Serializable
                             | TypeAttributes.SpecialName;
            
            var enumType = new TypeDefinition(nmsp, name, attributes, _coreLib["Enum"].t.ToTypeDefOrRef());
            _module.TopLevelTypes.Add(enumType);
            
            var valueField = new FieldDefinition(
                "value__",
                FieldAttributes.Public | FieldAttributes.SpecialName | FieldAttributes.RuntimeSpecialName,
                _corLibFactory.UInt64
            );
            enumType.Fields.Add(valueField);

            ulong i = 0;
            foreach (var value in k.Children.OfType<TypedefItemObject>())
            {
                var zeroField = new FieldDefinition(
                    value.Name,
                    FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal
                    | FieldAttributes.HasDefault,
                    enumType.ToTypeSignature()
                );
                var bytes = new byte[8];
                BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(), i);
                zeroField.Constant = new Constant(_corLibFactory.UInt64.ElementType, new DataBlobSignature(bytes));
                enumType.Fields.Add(zeroField);
                i++;
            }
            
            _enumsMap[k] = new EnumData(enumType, valueField);
        }
    }

    private void ResolveContent()
    {
        foreach (var (k, v) in _functionsMap)
        {
            var fun = DeclareFunction(k, _namespacesMap[k.Namespace!]);
            _functionsMap[k] = fun;
        }
        
        foreach (var (k, v) in _typesMap)
        {
            if (k.Extends != null)
            {
                FieldAttributes attributes = FieldAttributes.Public | FieldAttributes.SpecialName;
                var sig = new FieldSignature(CallingConventionAttributes.Default, TypeFromRef(k.Extends));
                var f = new FieldDefinition("base", attributes, sig) { FieldOffset = 0 };
                v.Type.Fields.Add(f);
            }
            
            {
                var signature = MethodSignature.CreateInstance(_corLibFactory.Void);
                var ctor = new MethodDefinition(".ctor",
                    MethodAttributes.Public
                    | MethodAttributes.HideBySig
                    | MethodAttributes.SpecialName
                    | MethodAttributes.RuntimeSpecialName,
                    signature);

                v.Type.Methods.Add(ctor);
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
            
            if (v.Type.BaseType is { IsValueType: false }) {
                var signature = MethodSignature.CreateInstance(_coreLib["Object"].t);
                var clone = new MethodDefinition("<clone>",
                    MethodAttributes.Public
                    | MethodAttributes.HideBySig,
                    signature);

                v.Type.Methods.Add(clone);
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
                        var f = DeclareField(a, v.Type);
                        _fieldsMap.Add(a, f);
                    } break;
                    
                    //case FunctionGroupObject @fg:
                    //{
                    //    
                    //} break;
                    
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
            if (v is not MethodDefinition method) continue;
            
            var body = new CilMethodBody(method);
            method.CilMethodBody = body;
            
            var locals = new CilLocalVariable[k.Locals.Length];
            foreach (var local in k.Locals)
            {
                var l = new CilLocalVariable(TypeFromRef(local.Type));
                locals[local.index] = l;
                body.LocalVariables.Add(l);
            }
            if (locals.Length > 0) body.InitializeLocals = true;
            
            var args = method.Parameters.ToArray();
            var stack = new List<TypeSignature>();
            
            CompileIr(k.Body!, body.Instructions, stack, args, locals);
            if (body.Instructions.Count == 0 || body.Instructions[^1].OpCode != CilOpCodes.Ret)
            {
                body.Instructions.Add(CilOpCodes.Ret);
                if (method.Signature!.ReturnsValue) stack.RemoveAt(stack.Count - 1);
            }
            
            //if (stack.Count != 0) throw new UnreachableException();
            body.Instructions.CalculateOffsets();
        }
    }
    
    
    private IMethodDescriptor DeclareFunction(FunctionObject funcobj, TypeDefinition parent)
    {
        if (funcobj.DotnetImport != null)
        {
            var asmName = funcobj.DotnetImport.Value.AssemblyName;
            var typeName = funcobj.DotnetImport.Value.ClassName;
            var methodName = funcobj.DotnetImport.Value.MethodName;
            var lastDot = typeName.LastIndexOf('.');
            
            var asmRef =
                _module.AssemblyReferences.FirstOrDefault(e => e.Name == asmName) ??
                         new AssemblyReference(asmName, new Version(10, 0, 0, 0))
                            .ImportWith(_module.DefaultImporter);
            var baseType = asmRef.CreateTypeReference(typeName[0..lastDot], typeName[(lastDot + 1)..]);
            if (baseType == null) throw new Exception("Extern type not found: " + typeName);

            var signature = MethodSignature.CreateStatic(
                TypeFromRef(funcobj.ReturnType), funcobj.Parameters.Select(e => TypeFromRef(e.Type)));

            var method = baseType.CreateMemberReference(methodName, signature);
            if (method == null)
                throw new Exception(
                    $"Extern method '{methodName}" +
                    $"({string.Join(", ", funcobj.Parameters.Select(e => e.Type))})" +
                    $"' not found at base type {baseType.FullName}");
            
            return _module.DefaultImporter.ImportMethod(method);
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
        return m;
    }
    private FieldDefinition DeclareField(FieldObject fieldobj, TypeDefinition parent)
    {
        FieldAttributes attributes = 0;
        
        if (fieldobj.Public) attributes |= FieldAttributes.Public;
        if (fieldobj.Static) attributes |= FieldAttributes.Static;
        
        var sig = new FieldSignature(CallingConventionAttributes.Default, TypeFromRef(fieldobj.Type));
        var f = new FieldDefinition(fieldobj.Name, attributes, sig) { FieldOffset = fieldobj.Offset!.Value.Bytes };
        parent.Fields.Add(f);
        return f;
    }
    
    
    private void CompileIr(IRBlock block, CilInstructionCollection gen, List<TypeSignature> stack, Parameter[] args, CilLocalVariable[] locals)
    {
        foreach (var node in block.Content) CompileIrNodeLoad(node, gen, stack, args, locals);
    }
    
    private void CompileIrNodeLoad(IRNode node, CilInstructionCollection gen, List<TypeSignature> stack, Parameter[] args, CilLocalVariable[] locals)
    {
        switch (node)
        {
            case IRAssign @ass:
                CompileIrNodeStore(ass.Target, ass.Value, gen, stack, args, locals);
                break;

            case IRNewObject @nobj:
            {
                var type = _typesMap[((SolvedStructTypeReference)nobj.InstanceType).Struct].Type;
                var ctor = type.Methods.First(e => e.Name == ".ctor");

                if (type.IsValueType)
                {
                    CompileIrNodeLoadAsRef(nobj.Arguments[0], gen, stack, args, locals);
                    foreach (var i in nobj.Arguments[1..]) CompileIrNodeLoad(i, gen, stack, args, locals);
                    gen.Add(CilOpCodes.Call, ctor);
                    stack.RemoveRange(stack.Count - nobj.Arguments.Length, nobj.Arguments.Length);
                }
                else
                {
                    foreach (var i in nobj.Arguments[1..]) CompileIrNodeLoad(i, gen, stack, args, locals);
                    gen.Add(CilOpCodes.Newobj, ctor);
                    stack.RemoveRange(stack.Count - (nobj.Arguments.Length-1), nobj.Arguments.Length-1);
                    CompileIrNodeStore(nobj.Arguments[0], null, gen, stack, args, locals);
                }
            } break;

            case IrIntegerLiteral @intlit:
            {
                var signed = ((RuntimeIntegerTypeReference)intlit.Type!).Signed;
                switch (intlit.Size)
                {
                    case <= 32:
                        gen.Add(CilInstruction.CreateLdcI4((int)intlit.Value));
                        stack.Add(signed ? _corLibFactory.Int32 : _corLibFactory.UInt32);
                        break;
                    case <= 64:
                        gen.Add(CilOpCodes.Ldc_I8, unchecked((long)(UInt128)intlit.Value));
                        stack.Add(signed ? _corLibFactory.Int64 : _corLibFactory.UInt64);
                        break;
                    case <= 128:
                    {
                        var largeType = signed ? _coreLib["Int128"] : _coreLib["UInt128"];
                        
                        if (intlit.Value == 0)
                        {
                            var tmp = new CilLocalVariable(largeType.t);
                            gen.Owner.LocalVariables.Add(tmp);
                            gen.Add(CilOpCodes.Ldloca, tmp);
                            gen.Add(CilOpCodes.Initobj, largeType.t.ToTypeDefOrRef());
                            gen.Add(CilOpCodes.Ldloc, tmp);
                        }
                        else if (intlit.Value <= ulong.MaxValue)
                        {
                            gen.Add(CilOpCodes.Ldc_I8, (long)intlit.Value);
                            gen.Add(CilOpCodes.Call, largeType.m[intlit.Value.Sign < 0 ? "Conv_from_i64" : "Conv_from_u64"]);
                        }
                        else
                        {
                            var tmp = new CilLocalVariable(largeType.t);
                            gen.Owner.LocalVariables.Add(tmp);
                            gen.Add(CilOpCodes.Ldloca, tmp);
                            var mask = (BigInteger.One << 64) - BigInteger.One;
                            var hi = (ulong)((intlit.Value >> 64) & mask);
                            var lo = (ulong)(intlit.Value & mask);
                            gen.Add(CilOpCodes.Ldc_I8, unchecked((long)hi));
                            gen.Add(CilOpCodes.Ldc_I8, unchecked((long)lo));
                            gen.Add(CilOpCodes.Call, largeType.m["new"]);
                            gen.Add(CilOpCodes.Ldloc, tmp);
                        }
                        stack.Add(largeType.t);
                    } break;
                    default: throw new UnreachableException();
                }
                
            } break;
            case IRStringLiteral @strlit:
                gen.Add(CilOpCodes.Ldstr, strlit.Data);
                stack.Add(_corLibFactory.String);
                break;
            case IrCollectionLiteral @collit:
            {
                var elmtype = TypeFromRef(collit.ElementType);
                
                gen.Add(CilInstruction.CreateLdcI4(collit.Length));
                gen.Add(CilOpCodes.Newarr, elmtype.ToTypeDefOrRef());
                stack.Add(TypeFromRef(collit.Type));

                var index = 0;
                foreach (var i in collit.Items)
                {
                    gen.Add(CilOpCodes.Dup);
                    gen.Add(CilInstruction.CreateLdcI4(index++));
                    CompileIrNodeLoad(i,  gen, stack, args, locals);
                    gen.Add(CilOpCodes.Stelem, elmtype.ToTypeDefOrRef());
                    stack.RemoveAt(stack.Count - 1);
                }
            } break;
            
            case IRSolvedReference @solv:
            {
                switch (solv.Reference)
                {
                    case LocalReference @lr:
                        gen.Add(CilOpCodes.Ldloc, locals[lr.Local.index]);
                        stack.Add(locals[lr.Local.index].VariableType);
                        break;
    
                    case ParameterReference @pr:
                        gen.Add(CilOpCodes.Ldarg, args[pr.Parameter.index]);
                        stack.Add(args[pr.Parameter.index].ParameterType);
                        break;

                    case SolvedFieldReference @fr:
                    {
                        var field = _fieldsMap[fr.Field];
                        
                        if (!field.IsStatic && !stack[^1].IsAssignableTo(field.DeclaringType!.ToTypeSignature()))
                            gen.Add(CilOpCodes.Conv_U);
                        
                        gen.Add(CilOpCodes.Ldflda, field);
                        if (!field.IsStatic) stack.RemoveAt(stack.Count - 1); 
                        stack.Add(_fieldsMap[fr.Field].Signature!.FieldType);
                    } break;

                    default: throw new UnreachableException();
                }
            } break;
    
            case IRAccess @acc:
            {
                CompileIrNodeLoadAsRef(acc.A, gen, stack, args, locals);
                CompileIrNodeLoadAsRef(acc.B, gen, stack, args, locals);
                
                var t = TypeFromRef(acc.Type);
                gen.Add(CilOpCodes.Ldobj, t.ToTypeDefOrRef());
                stack.Add(t);
            } break;
    
            case IRInvoke @iv:
            {
                foreach (var i in iv.Arguments) CompileIrNodeLoad(i, gen, stack, args, locals);
                CompileIrNodeCall(iv.Target, gen, stack, args, locals);
            } break;
            
            case IrConv @c:
            {
                var fromType = c.OriginType;
                var toType = c.Type;

                switch (toType)
                {
                    case StringTypeReference:
                    {
                        var baseTypeRef = TypeFromRef(fromType);
                        CompileIrNodeLoad(c.Expression, gen, stack, args, locals);
                        stack.RemoveAt(stack.Count - 1);
                        gen.Add(CilOpCodes.Box, baseTypeRef.ToTypeDefOrRef());
                        gen.Add(CilOpCodes.Callvirt, _coreLib["Object"].m["ToString"]);
                        stack.Add(_corLibFactory.String);
                    } break;

                    case RuntimeIntegerTypeReference @targt:
                    {
                        CompileIrNodeLoad(c.Expression, gen, stack, args, locals);
                        stack.RemoveAt(stack.Count - 1);
                        
                        var srt = (RuntimeIntegerTypeReference)fromType;
                        var srs = srt.Signed;
                        var srbitsize = srt.BitSize.Bits;
                        
                        var s = targt.Signed;
                        var bitsize = targt.BitSize.Bits;

                        if (srbitsize == 128)
                        {
                            var baset = srs ? _coreLib["Int128"] : _coreLib["UInt128"];
                            switch (bitsize)
                            {
                                case <= 8:
                                    gen.Add(CilOpCodes.Call, baset.m[s ? "Conv_to_i8" : "Conv_to_u8"]);
                                    stack.Add(s ? _corLibFactory.SByte :  _corLibFactory.Byte);
                                    break;
                                case <= 16:
                                    gen.Add(CilOpCodes.Call, baset.m[s ? "Conv_to_i16" : "Conv_to_u16"]);
                                    stack.Add(s ? _corLibFactory.Int16 : _corLibFactory.UInt16);
                                    break;
                                case <= 32:
                                    gen.Add(CilOpCodes.Call, baset.m[s ? "Conv_to_i32" : "Conv_to_u32"]);
                                    stack.Add(s ? _corLibFactory.Int32 : _corLibFactory.UInt32);
                                    break;
                                case <= 64:
                                    gen.Add(CilOpCodes.Call, baset.m[s ? "Conv_to_i64" : "Conv_to_u64"]);
                                    stack.Add(s ? _corLibFactory.Int64 : _corLibFactory.UInt64);
                                    break;
                                default: throw new UnreachableException();
                            }
                            return;
                        }
                        
                        switch (bitsize)
                        {
                            case 0:
                                gen.Add(s ? CilOpCodes.Conv_I : CilOpCodes.Conv_U);
                                stack.Add(s ? _corLibFactory.IntPtr : _corLibFactory.UIntPtr);
                                break;
                            
                            case <= 8:
                                gen.Add(s ? CilOpCodes.Conv_I1 : CilOpCodes.Conv_U1);
                                stack.Add(s ? _corLibFactory.SByte : _corLibFactory.Byte);
                                break;
                            case <= 16:
                                gen.Add(s ? CilOpCodes.Conv_I2 : CilOpCodes.Conv_U2);
                                stack.Add(s ? _corLibFactory.Int16 : _corLibFactory.UInt16);
                                break;
                            case <= 32:
                                gen.Add(s ? CilOpCodes.Conv_I4 : CilOpCodes.Conv_U4);
                                stack.Add(s ? _corLibFactory.Int32 : _corLibFactory.UInt32);
                                break;
                            case <= 64:
                                gen.Add(s ? CilOpCodes.Conv_I8 : CilOpCodes.Conv_U8);
                                stack.Add(s ? _corLibFactory.Int64 : _corLibFactory.UInt64);
                                break;

                            case <= 128:
                            {
                                var baset = srs ? _coreLib["Int128"] : _coreLib["UInt128"];
                                
                                switch (srbitsize)
                                {
                                    case <= 8:gen.Add(CilOpCodes.Call, baset.m[s ? "Conv_from_i8" : "Conv_from_u8"]); break;
                                    case <= 16: gen.Add(CilOpCodes.Call, baset.m[s ? "Conv_from_i16" : "Conv_from_u16"]); break;
                                    case <= 32: gen.Add(CilOpCodes.Call, baset.m[s ? "Conv_from_i32" : "Conv_from_u32"]); break;
                                    case <= 64: gen.Add(CilOpCodes.Call, baset.m[s ? "Conv_from_i64" : "Conv_from_u64"]); break;
                                    default: throw new UnreachableException();
                                }

                                stack.Add(baset.t);
                                break;
                            }

                            default: throw new UnreachableException();
                        }
                    } break;
                    
                    default: throw new UnreachableException();
                }
            } break;
            
            case IRUnaryExp @ue:
            {
                switch (ue.Operation)
                {
                    case IRUnaryExp.UnaryOperation.Reference:
                        CompileIrNodeLoadAsRef(ue.Value, gen, stack, args, locals);
                        break;

                    case IRUnaryExp.UnaryOperation.BitwiseNot:
                    {
                        var isSigned = ((RuntimeIntegerTypeReference)ue.Type!).Signed;
                        var is128 = ((RuntimeIntegerTypeReference)ue.Type!).BitSize == 128;
                        
                        CompileIrNodeLoad(ue.Value, gen, stack, args, locals);
                        if (is128) gen.Add(CilOpCodes.Call, _coreLib[isSigned ? "Int128" : "UInt128"].m["BitwiseNot"]);
                        else gen.Add(CilOpCodes.Not); ;
                    } break;

                    case IRUnaryExp.UnaryOperation.PreIncrement:
                        CompileIrNodeLoad(ue.Value, gen, stack, args, locals);
                        var it = (RuntimeIntegerTypeReference)ue.Value.Type;
                        var bs = it.BitSize.Bits;
                        switch (bs)
                        {
                            case <= 32: CilInstruction.CreateLdcI4(1); break;
                            case <= 64: gen.Add(CilOpCodes.Ldc_I8, (long)1); break;
                            default: throw new UnreachableException();
                        }
                        gen.Add(CilOpCodes.Add);
                        CompileIrNodeStore(ue.Value, null, gen, stack, args, locals);
                        break;
                    
                    default: throw new ArgumentOutOfRangeException();
                }
            } break;
            case IRBinaryExp @bin:
            {
                CompileIrNodeLoad(@bin.Left, gen, stack, args, locals);
                CompileIrNodeLoad(@bin.Right, gen, stack, args, locals);

                if (bin.Left.Type is RuntimeIntegerTypeReference @originType)
                {
                    var isSigned = originType.Signed;
                    var is128 = originType.BitSize == 128;

                    switch (bin.Operator)
                    {
                        case IRBinaryExp.Operators.Add:
                            if (is128) gen.Add(CilOpCodes.Call, _coreLib[isSigned ? "Int128" : "UInt128"].m["Add"]);
                            else gen.Add(CilOpCodes.Add);
                            break;

                        case IRBinaryExp.Operators.AddWarpAround:
                            if (is128) gen.Add(CilOpCodes.Call, _coreLib[isSigned ? "Int128" : "UInt128"].m["AddOvf"]);
                            else gen.Add(isSigned ? CilOpCodes.Add_Ovf : CilOpCodes.Add_Ovf_Un);
                            break;


                        case IRBinaryExp.Operators.Subtract:
                            if (is128) gen.Add(CilOpCodes.Call, _coreLib[isSigned ? "Int128" : "UInt128"].m["Sub"]);
                            else gen.Add(CilOpCodes.Sub);
                            break;

                        case IRBinaryExp.Operators.SubtractWarpAround:
                            if (is128) gen.Add(CilOpCodes.Call, _coreLib[isSigned ? "Int128" : "UInt128"].m["SubOvf"]);
                            else gen.Add(isSigned ? CilOpCodes.Sub_Ovf : CilOpCodes.Sub_Ovf_Un);
                            break;

                        case IRBinaryExp.Operators.Multiply:
                            if (is128) gen.Add(CilOpCodes.Call, _coreLib[isSigned ? "Int128" : "UInt128"].m["Mul"]);
                            else gen.Add(isSigned ? CilOpCodes.Mul_Ovf : CilOpCodes.Mul_Ovf_Un);
                            break;

                        case IRBinaryExp.Operators.Divide:
                            if (is128) gen.Add(CilOpCodes.Call, _coreLib[isSigned ? "Int128" : "UInt128"].m["Div"]);
                            else gen.Add(isSigned ? CilOpCodes.Div : CilOpCodes.Div_Un);
                            break;

                        case IRBinaryExp.Operators.Reminder:
                            if (is128) gen.Add(CilOpCodes.Call, _coreLib[isSigned ? "Int128" : "UInt128"].m["Rem"]);
                            else gen.Add(isSigned ? CilOpCodes.Rem : CilOpCodes.Rem_Un);
                            break;

                        case IRBinaryExp.Operators.BitwiseAnd:
                            if (is128)
                                gen.Add(CilOpCodes.Call, _coreLib[isSigned ? "Int128" : "UInt128"].m["BitwiseAnd"]);
                            else gen.Add(CilOpCodes.And);
                            break;

                        case IRBinaryExp.Operators.BitwiseOr:
                            if (is128)
                                gen.Add(CilOpCodes.Call, _coreLib[isSigned ? "Int128" : "UInt128"].m["BitwiseOr"]);
                            else gen.Add(CilOpCodes.Or);
                            break;

                        case IRBinaryExp.Operators.BitwiseXor:
                            if (is128)
                                gen.Add(CilOpCodes.Call, _coreLib[isSigned ? "Int128" : "UInt128"].m["BitwiseXor"]);
                            else gen.Add(CilOpCodes.Xor);
                            break;

                        case IRBinaryExp.Operators.LeftShift:
                            //gen.Add(CilOpCodes.Conv_I4);
                            if (is128)
                                gen.Add(CilOpCodes.Call, _coreLib[isSigned ? "Int128" : "UInt128"].m["LeftShift"]);
                            else gen.Add(CilOpCodes.Shl);
                            break;

                        case IRBinaryExp.Operators.RightShift:
                            //gen.Add(CilOpCodes.Conv_I4);
                            if (is128)
                                gen.Add(CilOpCodes.Call, _coreLib[isSigned ? "Int128" : "UInt128"].m["RightShift"]);
                            else gen.Add(CilOpCodes.Shr_Un);
                            break;

                        case IRBinaryExp.Operators.Equality:
                            gen.Add(CilOpCodes.Ceq);
                            break;
                        
                        case IRBinaryExp.Operators.Inequality:
                            gen.Add(CilOpCodes.Ceq);
                            gen.Add(CilOpCodes.Ldc_I4_0);
                            gen.Add(CilOpCodes.Ceq);
                            break;
                        
                        case IRBinaryExp.Operators.LessThan: gen.Add(isSigned ? CilOpCodes.Clt : CilOpCodes.Clt_Un); break;
                        case IRBinaryExp.Operators.GreaterThan: gen.Add(isSigned ? CilOpCodes.Cgt : CilOpCodes.Cgt_Un); break;
                        case IRBinaryExp.Operators.LessThanOrEqual:
                            gen.Add(isSigned ? CilOpCodes.Cgt : CilOpCodes.Cgt_Un);
                            gen.Add(CilOpCodes.Ldc_I4_0);
                            gen.Add(CilOpCodes.Ceq);
                            break;
                        case IRBinaryExp.Operators.GreaterThanOrEqual:
                            gen.Add(isSigned ? CilOpCodes.Clt : CilOpCodes.Clt_Un);
                            gen.Add(CilOpCodes.Ldc_I4_0);
                            gen.Add(CilOpCodes.Ceq);
                            break;
                        
                        case IRBinaryExp.Operators.AddOnBounds:
                        case IRBinaryExp.Operators.SubtractOnBounds:
                        case IRBinaryExp.Operators.DivideFloor:
                        case IRBinaryExp.Operators.DivideCeil:
                        default: throw new ArgumentOutOfRangeException();
                    }
                    stack.RemoveAt(stack.Count - 1);
                }
                else if (bin.Left.Type is BooleanTypeReference)
                {
                    switch (bin.Operator)
                    {
                        case IRBinaryExp.Operators.Equality:
                            gen.Add(CilOpCodes.Ceq);
                            break;
                        
                        case IRBinaryExp.Operators.Inequality:
                            gen.Add(CilOpCodes.Ceq);
                            gen.Add(CilOpCodes.Ldc_I4_1);
                            gen.Add(CilOpCodes.Xor);
                            break;
                        
                        case IRBinaryExp.Operators.LogicalAnd: gen.Add(CilOpCodes.And); break;
                        case IRBinaryExp.Operators.LogicalOr: gen.Add(CilOpCodes.Or); break;
                        
                        default: throw new ArgumentOutOfRangeException();
                    }
                    stack.RemoveAt(stack.Count - 1);
                }
                
            } break;
            case IRCompareExp @cmp:
            {
                CompileIrNodeLoad(cmp.Left, gen, stack, args, locals);
                CompileIrNodeLoad(cmp.Right, gen, stack, args, locals);
    
                var sig = ((RuntimeIntegerTypeReference)cmp.Left.Type!).Signed;
                
                switch (cmp.Operator)
                {
                    case IRCompareExp.Operators.GreaterThan: gen.Add(sig ? CilOpCodes.Cgt : CilOpCodes.Cgt_Un); break;
                    case IRCompareExp.Operators.LessThan: gen.Add(sig ? CilOpCodes.Clt : CilOpCodes.Clt_Un); break;
                    
                    case IRCompareExp.Operators.GreaterThanOrEqual:
                        gen.Add(sig ? CilOpCodes.Clt : CilOpCodes.Clt_Un);
                        gen.Add(CilOpCodes.Ldc_I4_0);
                        gen.Add(CilOpCodes.Ceq);
                        break;
                    case IRCompareExp.Operators.LessThanOrEqual:
                        gen.Add(sig ? CilOpCodes.Cgt : CilOpCodes.Cgt_Un);
                        gen.Add(CilOpCodes.Ldc_I4_0);
                        gen.Add(CilOpCodes.Ceq);
                        break;
                    
                    default: throw new ArgumentOutOfRangeException();
                }
                
                stack.Add(_corLibFactory.Boolean);
            } break;
            case IrIndex @idx:
            {
                var elmtype = TypeFromRef(idx.ResultType);
                CompileIrNodeLoad(idx.Value, gen, stack, args, locals);
                CompileIrNodeLoad(idx.Indices[0], gen, stack, args, locals);
                gen.Add(CilOpCodes.Ldelem, elmtype.ToTypeDefOrRef());
                stack.RemoveAt(stack.Count - 1);
                stack[^1] = elmtype;
            } break;
            
            case IRIf @if:
            {
                var breakLabel = new CilInstructionLabel();
                var nextConditionLabel = new CilInstructionLabel();
                
                CompileIrNodeLoad(@if.Condition, gen, stack, args, locals);
                gen.Add(CilOpCodes.Brfalse, nextConditionLabel);
                stack.RemoveAt(stack.Count - 1);
                
                CompileIr(@if.Then, gen, stack, args, locals);
                gen.Add(CilOpCodes.Br, breakLabel);
                
                var currentElse = @if.Else;
                while (currentElse != null)
                {
                    var anchor = gen.Add(CilOpCodes.Nop);
                    nextConditionLabel.Instruction = anchor;

                    if (currentElse is IRIf @elseIf)
                    {
                        nextConditionLabel = new CilInstructionLabel();
            
                        CompileIrNodeLoad(@elseIf.Condition, gen, stack, args, locals);
                        gen.Add(CilOpCodes.Brfalse, nextConditionLabel);
                        stack.RemoveAt(stack.Count - 1);
            
                        CompileIr(@elseIf.Then, gen, stack, args, locals);
                        gen.Add(CilOpCodes.Br, breakLabel);
            
                        currentElse = @elseIf.Else;
                    }
                    else if (currentElse is IRElse @else)
                    {
                        CompileIr(@else.Then, gen, stack, args, locals);
                        currentElse = null; 
                    }
                }
                
                var finalNop = gen.Add(CilOpCodes.Nop);
                
                breakLabel.Instruction = finalNop;
                if (nextConditionLabel.Instruction == null) 
                    nextConditionLabel.Instruction = finalNop;

            } break;
    
            case IRWhile @while:
            {
                var checkLabel = new CilInstructionLabel();
                var bodyLabel = new CilInstructionLabel();
                
                // Jump to check
                gen.Add(CilOpCodes.Br,  checkLabel);
                
                // Body
                var lastIdx = gen.Count;
                CompileIr(@while.Process, gen, stack, args, locals);
                bodyLabel.Instruction = gen[lastIdx];
                
                // Step
                if (@while.Step != null) CompileIr(@while.Step, gen, stack, args, locals);
                
                // Check
                lastIdx = gen.Count;
                CompileIrNodeLoad(@while.Condition, gen, stack, args, locals);
                gen.Add(CilOpCodes.Brtrue, bodyLabel);
                stack.RemoveAt(stack.Count - 1);
                checkLabel.Instruction = gen[lastIdx];
            } break;
            
            case IRReturn @ret:
                if (ret.Value != null)
                {
                    CompileIrNodeLoad(ret.Value, gen, stack, args, locals);
                    stack.RemoveAt(stack.Count - 1);
                }
                gen.Add(CilOpCodes.Ret);
                break;
            
            default: throw new UnreachableException();
        }
    }
    
    private void CompileIrNodeLoadAsRef(IRNode node, CilInstructionCollection gen, List<TypeSignature> stack, Parameter[] args, CilLocalVariable[] locals)
    {
        switch (node)
        {
            case IRSolvedReference @sr:
            {
                switch (sr.Reference)
                {
                    case LocalReference @l:
                    {
                        var t = locals[l.Local.index].VariableType;
                        var byRef = t.IsValueType;
                        gen.Add(byRef ? CilOpCodes.Ldloca : CilOpCodes.Ldloc, locals[l.Local.index]);
                        stack.Add(t.MakeByReferenceType());
                    } break;

                    case ParameterReference @p:
                    {
                        var t = args[p.Parameter.index].ParameterType;
                        var byRef = t.IsValueType;
                        gen.Add(byRef ? CilOpCodes.Ldarga : CilOpCodes.Ldarg, args[p.Parameter.index]);
                        stack.Add(t.MakeByReferenceType());
                    }
                        break;

                    case SolvedFieldReference @f:
                    {
                        var fi = _fieldsMap[f.Field];
                        var byRef = fi.Signature!.FieldType.IsValueType;
                        
                        if (!fi.IsStatic && !stack[^1].IsAssignableTo(fi.DeclaringType!.ToTypeSignature()))
                            gen.Add(CilOpCodes.Conv_U);
                        
                        gen.Add(CilOpCodes.Ldflda, fi);
                        if (fi.IsStatic) stack.RemoveAt(stack.Count - 1);
                        stack.Add(fi.Signature!.FieldType.MakeByReferenceType());
                    } break;

                    default: throw new UnreachableException();
                }
            } break;
            
            case IRAccess @acc:
            {
                CompileIrNodeLoadAsRef(acc.A, gen, stack, args, locals);
                CompileIrNodeLoadAsRef(acc.B, gen, stack, args, locals);
            } break;
            
            default: throw new UnreachableException();
        }
    }
    
    private void CompileIrNodeStore(IRNode node, IRNode? value, CilInstructionCollection gen, List<TypeSignature> stack, Parameter[] args, CilLocalVariable[] locals)
    {
        switch (node)
        {
            case IRSolvedReference @solv:
            {
                switch (solv.Reference)
                {
                    case LocalReference @l:
                    {
                        if (value != null) CompileIrNodeLoad(value, gen, stack, args, locals);
                        gen.Add(CilOpCodes.Stloc, locals[l.Local.index]);
                        stack.RemoveAt(stack.Count - 1);
                    } break;

                    case SolvedFieldReference @f:
                    {
                        var t = _fieldsMap[f.Field];
                        var isstat = t.IsStatic;
                        
                        if (!t.IsStatic && !stack[^1].IsAssignableTo(t.DeclaringType!.ToTypeSignature()))
                            gen.Add(CilOpCodes.Conv_U);
                        
                        if (value != null) CompileIrNodeLoad(value, gen, stack, args, locals);
                        gen.Add(isstat ? CilOpCodes.Stsfld : CilOpCodes.Stfld, t);
                        
                        if (!isstat) stack.RemoveAt(stack.Count - 1);
                        stack.RemoveAt(stack.Count - 1);
                    } break;

                    default: throw new UnreachableException();
                }
            } break;
    
            case IRAccess @access:
            {
                CompileIrNodeLoadAsRef(@access.A, gen, stack, args, locals);
                CompileIrNodeStore(access.B, value, gen, stack, args, locals);
            } break;

            case IrIndex @idx:
            {
                var elmtype = TypeFromRef(idx.ResultType);
                CompileIrNodeLoad(idx.Value, gen, stack, args, locals);
                CompileIrNodeLoad(idx.Indices[0], gen, stack, args, locals);
                CompileIrNodeLoad(value!, gen, stack, args, locals);
                gen.Add(CilOpCodes.Stelem, elmtype.ToTypeDefOrRef());
                stack.RemoveRange(stack.Count - 4, 3);
            } break;
            
            default: throw new UnreachableException();
        }
    }
    
    private void CompileIrNodeCall(IRNode node, CilInstructionCollection gen, List<TypeSignature> stack, Parameter[] args, CilLocalVariable[] locals)
    {
        switch (node)
        {
            case IRSolvedReference solvedReference:
            {
                switch (solvedReference.Reference)
                {
                    case SolvedFunctionReference sfr:
                    {
                        var f = _functionsMap[sfr.Function];
                        var pl = sfr.Function.Parameters.Length;
                        stack.RemoveRange(stack.Count - pl, pl);
                        gen.Add(CilOpCodes.Call, f);
                        if (f.Signature!.ReturnsValue) stack.Add(f.Signature.ReturnType);
                    } break;

                    default: throw new UnreachableException();
                }
            } break;
                
            default: throw new UnreachableException();
        }
    }
    

    private TypeSignature TypeFromRef(TypeReference? typeRef)
    {
        if (typeRef == null) return _corLibFactory.Void;
        switch (typeRef)
        {
            case ReferenceTypeReference @r:
                return TypeFromRef(r.InternalType).MakeByReferenceType();
            
            case SliceTypeReference @s:
                return TypeFromRef(s.InternalType).MakeArrayType(1);
            
            
            case RuntimeIntegerTypeReference @i:
            {
                return i.BitSize.Bits switch
                {
                    <= 8 => i.Signed ? _corLibFactory.SByte : _corLibFactory.Byte,
                    <= 16 => i.Signed ? _corLibFactory.Int16 : _corLibFactory.UInt16,
                    <= 32 => i.Signed ? _corLibFactory.Int32 : _corLibFactory.UInt32,
                    <= 64 => i.Signed ? _corLibFactory.Int64 : _corLibFactory.UInt64,
                    <= 128 => _coreLib[i.Signed ? "Int128" : "UInt128"].t,
                    _ => throw new UnreachableException()
                };
            }
    
            case StringTypeReference: return _corLibFactory.String;
            case BooleanTypeReference: return _corLibFactory.Boolean;
            
            case NoReturnTypeReference:
            case VoidTypeReference: return _corLibFactory.Void;

            case SolvedStructTypeReference @i:
                return _typesMap[i.Struct].ToTypeSignature();
            
            default: throw new UnreachableException();
        }
    }
    
    private IMethodDescriptor CreateMethodRef(ITypeDefOrRef basetype, string name, MethodSignature signature)
    {
        var importedsig = _module.DefaultImporter.ImportMethodSignature(signature);
        var meth = basetype.CreateMemberReference(name, importedsig);
        return _module.DefaultImporter.ImportMethod(meth);
    }
    
    private class StructData(TypeDefinition typedef)
    {
        public readonly TypeDefinition Type = typedef;
        public MethodDefinition PrimaryCtor = null!;
        public MethodDefinition Clone = null!;
        
        public TypeSignature ToTypeSignature() => Type.ToTypeSignature();
    }
    private class EnumData(TypeDefinition typedef, FieldDefinition valueField)
    {
        public readonly TypeDefinition Type = typedef;
        public readonly FieldDefinition Field = valueField;
    }
}
