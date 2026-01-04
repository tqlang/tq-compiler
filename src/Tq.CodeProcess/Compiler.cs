using System.Diagnostics;
using System.Reflection;
using System.Text;
using Abstract.CodeProcess.Core.Language.EvaluationData;
using Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree;
using Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Expresions;
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
    private Dictionary<ModuleObject, TypeDefinition> _modulesMap = [];
    private Dictionary<FunctionObject, MethodDefinition> _functionsMap = [];
    private Dictionary<StructObject, TypeDefinition> _typesMap = [];
    private Dictionary<FieldObject, FieldDefinition> _fieldsMap = [];

    private AssemblyDefinition _assembly;
    private ModuleDefinition _module;
    
    private CorLibTypeFactory corLib = null!;
    private Dictionary<string, (ITypeDefOrRef t, Dictionary<string, IMethodDescriptor> m)> _coreLib = [];
    
    public void Compile(ProgramObject program)
    {
        var programName = program.Modules[0].Name;
        
        _assembly = new AssemblyDefinition(programName + ".dll",
            new Version(1, 0, 0, 0));
        
        _module = new ModuleDefinition(programName);
        _assembly.Modules.Add(_module);

        corLib = _module.CorLibTypeFactory;
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
        Dictionary<string, IMethodDescriptor> methods;
        Type type;
        
        methods = [];
        type = typeof(ValueType);
        {
        }
        _coreLib.Add(type.Name, (_module.DefaultImporter.ImportType(type), methods));
            
        methods = [];
        type = typeof(Int128);
        {
            methods.Add("Parse", _module.DefaultImporter.ImportMethod(type.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, [typeof(string)]) ?? throw new Exception()));
            methods.Add("Add_ovf", _module.DefaultImporter.ImportMethod(type.GetMethod("op_Addition", BindingFlags.Public | BindingFlags.Static, [typeof(Int128), typeof(Int128)]) ?? throw new Exception()));
            methods.Add("Add", _module.DefaultImporter.ImportMethod(type.GetMethod("op_CheckedAddition", BindingFlags.Public | BindingFlags.Static, [typeof(Int128), typeof(Int128)]) ?? throw new Exception()));
            methods.Add("Sub_ovf", _module.DefaultImporter.ImportMethod(type.GetMethod("op_Subtraction", BindingFlags.Public | BindingFlags.Static, [typeof(Int128), typeof(Int128)]) ?? throw new Exception()));
            methods.Add("Sub", _module.DefaultImporter.ImportMethod(type.GetMethod("op_CheckedSubtraction", BindingFlags.Public | BindingFlags.Static, [typeof(Int128), typeof(Int128)]) ?? throw new Exception()));
            
            methods.Add("Conv_from_i8", _module.DefaultImporter.ImportMethod(type.GetMethod("op_Implicit", BindingFlags.Public | BindingFlags.Static, [typeof(sbyte)]) ?? throw new Exception()));
            methods.Add("Conv_from_u8", _module.DefaultImporter.ImportMethod(type.GetMethod("op_Implicit", BindingFlags.Public | BindingFlags.Static, [typeof(byte)]) ?? throw new Exception()));
            methods.Add("Conv_from_i16", _module.DefaultImporter.ImportMethod(type.GetMethod("op_Implicit", BindingFlags.Public | BindingFlags.Static, [typeof(short)]) ?? throw new Exception()));
            methods.Add("Conv_from_u16", _module.DefaultImporter.ImportMethod(type.GetMethod("op_Implicit", BindingFlags.Public | BindingFlags.Static, [typeof(ushort)]) ?? throw new Exception()));
            methods.Add("Conv_from_i32", _module.DefaultImporter.ImportMethod(type.GetMethod("op_Implicit", BindingFlags.Public | BindingFlags.Static, [typeof(int)]) ?? throw new Exception()));
            methods.Add("Conv_from_u32", _module.DefaultImporter.ImportMethod(type.GetMethod("op_Implicit", BindingFlags.Public | BindingFlags.Static, [typeof(uint)]) ?? throw new Exception()));
            methods.Add("Conv_from_i64", _module.DefaultImporter.ImportMethod(type.GetMethod("op_Implicit", BindingFlags.Public | BindingFlags.Static, [typeof(long)]) ?? throw new Exception()));
            methods.Add("Conv_from_u64", _module.DefaultImporter.ImportMethod(type.GetMethod("op_Implicit", BindingFlags.Public | BindingFlags.Static, [typeof(ulong)]) ?? throw new Exception()));
            
            methods.Add("Conv_to_i8", _module.DefaultImporter.ImportMethod(type.GetMethods().First(e => e.Name == "op_Explicit" && e.ReturnType == typeof(sbyte)) ?? throw new Exception()));
            methods.Add("Conv_to_u8", _module.DefaultImporter.ImportMethod(type.GetMethods().First(e => e.Name == "op_Explicit" && e.ReturnType == typeof(byte)) ?? throw new Exception()));
            methods.Add("Conv_to_i16", _module.DefaultImporter.ImportMethod(type.GetMethods().First(e => e.Name == "op_Explicit" && e.ReturnType == typeof(short)) ?? throw new Exception()));
            methods.Add("Conv_to_u16", _module.DefaultImporter.ImportMethod(type.GetMethods().First(e => e.Name == "op_Explicit" && e.ReturnType == typeof(ushort)) ?? throw new Exception()));
            methods.Add("Conv_to_i32", _module.DefaultImporter.ImportMethod(type.GetMethods().First(e => e.Name == "op_Explicit" && e.ReturnType == typeof(int)) ?? throw new Exception()));
            methods.Add("Conv_to_u32", _module.DefaultImporter.ImportMethod(type.GetMethods().First(e => e.Name == "op_Explicit" && e.ReturnType == typeof(uint)) ?? throw new Exception()));
            methods.Add("Conv_to_i64", _module.DefaultImporter.ImportMethod(type.GetMethods().First(e => e.Name == "op_Explicit" && e.ReturnType == typeof(long)) ?? throw new Exception()));
            methods.Add("Conv_to_u64", _module.DefaultImporter.ImportMethod(type.GetMethods().First(e => e.Name == "op_Explicit" && e.ReturnType == typeof(ulong)) ?? throw new Exception()));
        }
        _coreLib.Add(type.Name, (_module.DefaultImporter.ImportType(type), methods));
        
        methods = [];
        type = typeof(UInt128);
        {
            methods.Add("Parse", _module.DefaultImporter.ImportMethod(type.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, [typeof(string)]) ?? throw new Exception()));
            methods.Add("Add_ovf", _module.DefaultImporter.ImportMethod(type.GetMethod("op_Addition", BindingFlags.Public | BindingFlags.Static, [typeof(UInt128), typeof(UInt128)]) ?? throw new Exception()));
            methods.Add("Add", _module.DefaultImporter.ImportMethod(type.GetMethod("op_CheckedAddition", BindingFlags.Public | BindingFlags.Static, [typeof(UInt128), typeof(UInt128)]) ?? throw new Exception()));
            methods.Add("Sub_ovf", _module.DefaultImporter.ImportMethod(type.GetMethod("op_Subtraction", BindingFlags.Public | BindingFlags.Static, [typeof(UInt128), typeof(UInt128)]) ?? throw new Exception()));
            methods.Add("Sub", _module.DefaultImporter.ImportMethod(type.GetMethod("op_CheckedSubtraction", BindingFlags.Public | BindingFlags.Static, [typeof(UInt128), typeof(UInt128)]) ?? throw new Exception()));
            
            methods.Add("Conv_from_i8", _module.DefaultImporter.ImportMethod(type.GetMethod("op_Explicit", BindingFlags.Public | BindingFlags.Static, [typeof(sbyte)]) ?? throw new Exception()));
            methods.Add("Conv_from_u8", _module.DefaultImporter.ImportMethod(type.GetMethod("op_Implicit", BindingFlags.Public | BindingFlags.Static, [typeof(byte)]) ?? throw new Exception()));
            methods.Add("Conv_from_i16", _module.DefaultImporter.ImportMethod(type.GetMethod("op_Explicit", BindingFlags.Public | BindingFlags.Static, [typeof(short)]) ?? throw new Exception()));
            methods.Add("Conv_from_u16", _module.DefaultImporter.ImportMethod(type.GetMethod("op_Implicit", BindingFlags.Public | BindingFlags.Static, [typeof(ushort)]) ?? throw new Exception()));
            methods.Add("Conv_from_i32", _module.DefaultImporter.ImportMethod(type.GetMethod("op_Explicit", BindingFlags.Public | BindingFlags.Static, [typeof(int)]) ?? throw new Exception()));
            methods.Add("Conv_from_u32", _module.DefaultImporter.ImportMethod(type.GetMethod("op_Implicit", BindingFlags.Public | BindingFlags.Static, [typeof(uint)]) ?? throw new Exception()));
            methods.Add("Conv_from_i64", _module.DefaultImporter.ImportMethod(type.GetMethod("op_Explicit", BindingFlags.Public | BindingFlags.Static, [typeof(long)]) ?? throw new Exception()));
            methods.Add("Conv_from_u64", _module.DefaultImporter.ImportMethod(type.GetMethod("op_Implicit", BindingFlags.Public | BindingFlags.Static, [typeof(ulong)]) ?? throw new Exception()));
            
            methods.Add("Conv_to_i8", _module.DefaultImporter.ImportMethod(type.GetMethods().First(e => e.Name == "op_Explicit" && e.ReturnType == typeof(sbyte)) ?? throw new Exception()));
            methods.Add("Conv_to_u8", _module.DefaultImporter.ImportMethod(type.GetMethods().First(e => e.Name == "op_Explicit" && e.ReturnType == typeof(byte)) ?? throw new Exception()));
            methods.Add("Conv_to_i16", _module.DefaultImporter.ImportMethod(type.GetMethods().First(e => e.Name == "op_Explicit" && e.ReturnType == typeof(short)) ?? throw new Exception()));
            methods.Add("Conv_to_u16", _module.DefaultImporter.ImportMethod(type.GetMethods().First(e => e.Name == "op_Explicit" && e.ReturnType == typeof(ushort)) ?? throw new Exception()));
            methods.Add("Conv_to_i32", _module.DefaultImporter.ImportMethod(type.GetMethods().First(e => e.Name == "op_Explicit" && e.ReturnType == typeof(int)) ?? throw new Exception()));
            methods.Add("Conv_to_u32", _module.DefaultImporter.ImportMethod(type.GetMethods().First(e => e.Name == "op_Explicit" && e.ReturnType == typeof(uint)) ?? throw new Exception()));
            methods.Add("Conv_to_i64", _module.DefaultImporter.ImportMethod(type.GetMethods().First(e => e.Name == "op_Explicit" && e.ReturnType == typeof(long)) ?? throw new Exception()));
            methods.Add("Conv_to_u64", _module.DefaultImporter.ImportMethod(type.GetMethods().First(e => e.Name == "op_Explicit" && e.ReturnType == typeof(ulong)) ?? throw new Exception()));
        }
        _coreLib.Add(type.Name, (_module.DefaultImporter.ImportType(type), methods));
    }
    
    private void SearchRecursive(LangObject obj)
    {
        switch (obj)
        {
            case ModuleObject @a:
            {
                var attributes = TypeAttributes.AnsiClass
                                 | TypeAttributes.Class
                                 | TypeAttributes.Sealed
                                 | TypeAttributes.Public
                                 | TypeAttributes.Abstract;

                var moduledef = new TypeDefinition(a.Name, "Statics", attributes, corLib.Object.Type);
                _module.TopLevelTypes.Add(moduledef);
                _modulesMap.Add(a, moduledef);
                
                foreach (var i in a.Children) SearchRecursive(i);
            } break;

            case NamespaceObject @a:
                foreach (var i in a.Children) SearchRecursive(i);
                break;
            
            case FunctionGroupObject @a:
                foreach (var i in a.Overloads)
                    _functionsMap.Add(i, null!);
                break;

            case StructObject @a:
                _typesMap.Add(a, null!);
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

            var typedef = new TypeDefinition(nmsp, name, attributes, _coreLib["ValueType"].t);
            _module.TopLevelTypes.Add(typedef);
            _typesMap[k] = typedef;
        }
    }

    private void ResolveContent()
    {
        foreach (var (k, v) in _functionsMap)
        {
            var fun = DeclareFunction(k, _modulesMap[k.Module!]);
            _functionsMap[k] = fun;
        }
        
        foreach (var (k, v) in _typesMap)
        {
            {
                var signature = MethodSignature.CreateInstance(corLib.Void);
                var ctor = new MethodDefinition(".ctor",
                    MethodAttributes.Public
                    | MethodAttributes.HideBySig
                    | MethodAttributes.SpecialName
                    | MethodAttributes.RuntimeSpecialName,
                    signature);

                v.Methods.Add(ctor);

                var body = new CilMethodBody(ctor);
                ctor.CilMethodBody = body;

                var objectCtor = _module.DefaultImporter.ImportMethod(typeof(object).GetConstructors()[0]);
                var baseCtorRef = _module.DefaultImporter.ImportMethod(objectCtor);
                
                body.Instructions.Add(CilOpCodes.Ldarg_0);
                body.Instructions.Add(CilOpCodes.Initobj, _typesMap[k]);
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
                        var f = DeclareField(a, v);
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
        foreach (var (k, method) in _functionsMap)
        {
            if (k.Body == null) continue;
            
            var body = new CilMethodBody(method);
            method.CilMethodBody = body;
            
            var locals = new CilLocalVariable[k.Locals.Length];
            foreach (var local in k.Locals)
            {
                var l = new CilLocalVariable(TypeFromRef(local.Type));
                locals[local.index] = l;
                body.LocalVariables.Add(l);
            }

            var args = method.Parameters.ToArray();
                
            CompileIr(k.Body!, body.Instructions, args, locals);
            
            if (body.Instructions.Count == 0 || body.Instructions[^1].OpCode != CilOpCodes.Ret)
                body.Instructions.Add(CilOpCodes.Ret);
            
            body.Instructions.OptimizeMacros();
        }
    }
    
    private MethodDefinition DeclareFunction(FunctionObject funcobj, TypeDefinition parent)
    {
        var nmsp = string.Join('.', funcobj.Global[0..^1]);
        var name = funcobj.Name;

        MethodAttributes attributes = 0;
            
        if (funcobj.Abstract) attributes |= MethodAttributes.Abstract;
        if (funcobj.Public) attributes |= MethodAttributes.Public;
        if (funcobj.Static) attributes |= MethodAttributes.Static;
        
        
        var argTypes = funcobj.Parameters
            .Select(p => TypeFromRef(p.Type));
        var argDefs = funcobj.Parameters
            .Select((p, i) => new ParameterDefinition((ushort)(i+1), p.Name, 0));
        
        MethodSignature sig = funcobj.Static switch
        {
            true => MethodSignature.CreateStatic(TypeFromRef(funcobj.ReturnType), argTypes),
            false => MethodSignature.CreateInstance(TypeFromRef(funcobj.ReturnType), argTypes),
        };
        
        var m = new MethodDefinition(name, attributes, sig);
        foreach (var i in argDefs) m.ParameterDefinitions.Add(i);
        parent.Methods.Add(m);
        return m;
    }
    private FieldDefinition DeclareField(FieldObject fieldobj, TypeDefinition parent)
    {
        FieldAttributes attributes = 0;
        
        if (fieldobj.Public) attributes |= FieldAttributes.Public;
        if (fieldobj.Static) attributes |= FieldAttributes.Static;
        
        var sig = new FieldSignature(CallingConventionAttributes.Default, TypeFromRef(fieldobj.Type));
        var f = new FieldDefinition(fieldobj.Name, attributes, sig);
        parent.Fields.Add(f);
        return f;
    }
    
    
    
    private void CompileIr(IRBlock block, CilInstructionCollection gen, Parameter[] args, CilLocalVariable[] locals)
    {
        foreach (var node in block.Content)
            CompileIrNodeLoad(node, gen, args, locals, ignoreValue: true);
    }
    
    private void CompileIrNodeLoad(IRNode node, CilInstructionCollection gen, Parameter[] args, CilLocalVariable[] locals, bool ignoreValue = false)
    {
        switch (node)
        {
            case IRAssign @ass:
                CompileIrNodeStore(ass.Target, ass.Value, gen, args, locals);
                break;
            
            case IRNewObject @nobj:
                
                CompileIrNodeLoadAsRef(nobj.Arguments[0],  gen, args, locals);
                foreach (var i in nobj.Arguments[1..]) CompileIrNodeLoad(i,  gen, args, locals);
                
                var ctor = _typesMap[((SolvedStructTypeReference)nobj.InstanceType).Struct]
                    .Methods.First(e => e.Name == ".ctor");
                gen.Add(CilOpCodes.Call, ctor);
                break;
    
            case IrIntegerLiteral @intlit:
            {
                var signed = ((RuntimeIntegerTypeReference)intlit.Type!).Signed;
                
                switch (intlit.Size)
                {
                    case <= 32: gen.Add(CilInstruction.CreateLdcI4((int)intlit.Value)); break;
                    case <= 64: gen.Add(CilOpCodes.Ldc_I8, unchecked((long)(UInt128)intlit.Value)); break;
                    case <= 128:
                        gen.Add(CilOpCodes.Ldstr, intlit.Value.ToString());
                        gen.Add(CilOpCodes.Call, (signed ? _coreLib["Int128"] : _coreLib["UInt128"]).m["Parse"]);
                        break;
                    default: throw new UnreachableException();
                }
            } break;
            case IRStringLiteral @strlit:
                gen.Add(CilOpCodes.Ldstr, strlit.Data);
                break;
                
            case IRSolvedReference @solv:
            {
                switch (solv.Reference)
                {
                    case LocalReference @lr:
                        gen.Add(CilOpCodes.Ldloc, locals[lr.Local.index]); break;
    
                    case ParameterReference @pr:
                        gen.Add(CilOpCodes.Ldarg, args[pr.Parameter.index]); break;
                    
                    case SolvedFieldReference @fr:
                        gen.Add(CilOpCodes.Ldflda, _fieldsMap[fr.Field]);
                        break;
                    
                    default: throw new UnreachableException();
                }
            } break;
    
            case IRAccess @acc:
            {
                CompileIrNodeLoad(acc.A, gen, args, locals);
                CompileIrNodeLoad(acc.B, gen, args, locals);
            } break;
    
            case IRInvoke @iv:
            {
                foreach (var i in iv.Arguments)
                    CompileIrNodeLoad(i, gen, args, locals);
                CompileIrNodeCall(iv.Target, gen, args, locals);
                if (ignoreValue && ((FunctionTypeReference)iv.Target.Type).Returns is not VoidTypeReference)
                    gen.Add(CilOpCodes.Pop);
            } break;
    
            case IRIntCast ic:
            {
                CompileIrNodeLoad(ic.Expression, gen, args, locals);
                
                var srt = (RuntimeIntegerTypeReference)ic.TargetType;
                var srs = srt.Signed;
                var srbitsize = srt.PtrSized ? 0 : srt.BitSize;
    
                var targt = (RuntimeIntegerTypeReference)ic.Type!;
                var s = targt.Signed;
                var bitsize = targt.PtrSized ? 0 : targt.BitSize;

                if (srbitsize == 128)
                {
                    var baset = srs ? _coreLib["Int128"] : _coreLib["UInt128"];
                    switch (bitsize)
                    {
                        case <= 8: gen.Add(CilOpCodes.Call, baset.m[s ? "Conv_to_i8" : "Conv_to_u8"]); break;
                        case <= 16: gen.Add(CilOpCodes.Call, baset.m[s ? "Conv_to_i16" : "Conv_to_u16"]); break;
                        case <= 32: gen.Add(CilOpCodes.Call, baset.m[s ? "Conv_to_i32" : "Conv_to_u32"]); break;
                        case <= 64: gen.Add(CilOpCodes.Call, baset.m[s ? "Conv_to_i64" : "Conv_to_u64"]); break;
                        default: throw new UnreachableException();
                    }
                    return;
                }
                
                switch (bitsize)
                {
                    case 0: gen.Add(s ? CilOpCodes.Conv_I : CilOpCodes.Conv_U); break;
                    
                    case <= 8: gen.Add(s ? CilOpCodes.Conv_I1 : CilOpCodes.Conv_U1); break;
                    case <= 16: gen.Add(s ? CilOpCodes.Conv_I2 : CilOpCodes.Conv_U2); break;
                    case <= 32: gen.Add(s ? CilOpCodes.Conv_I4 : CilOpCodes.Conv_U4); break;
                    case <= 64: gen.Add(s ? CilOpCodes.Conv_I8 : CilOpCodes.Conv_U8); break;

                    case <= 128:
                    {
                        var baset = srs ? _coreLib["Int128"] : _coreLib["UInt128"];
                        
                        switch (srbitsize)
                        {
                            case <= 8: gen.Add(CilOpCodes.Call, baset.m[s ? "Conv_from_i8" : "Conv_from_u8"]); break;
                            case <= 16: gen.Add(CilOpCodes.Call, baset.m[s ? "Conv_from_i16" : "Conv_from_u16"]); break;
                            case <= 32: gen.Add(CilOpCodes.Call, baset.m[s ? "Conv_from_i32" : "Conv_from_u32"]); break;
                            case <= 64: gen.Add(CilOpCodes.Call, baset.m[s ? "Conv_from_i64" : "Conv_from_u64"]); break;
                            default: throw new UnreachableException();
                        }

                        break;
                    }

                    default: throw new UnreachableException();
                }
            } break;
    
            case IRUnaryExp @ue:
            {
                switch (ue.Operation)
                {
                    case IRUnaryExp.UnaryOperation.Reference:
                        CompileIrNodeLoadAsRef(ue.Value, gen, args, locals);
                        break;
                    
                    case IRUnaryExp.UnaryOperation.PreIncrement:
                        CompileIrNodeLoad(ue.Value, gen, args, locals);
                        var it = (RuntimeIntegerTypeReference)ue.Value.Type;
                        var bs = it.PtrSized ? 0 : it.BitSize;
                        switch (bs)
                        {
                            case <= 32: CilInstruction.CreateLdcI4(1); break;
                            case <= 64: gen.Add(CilOpCodes.Ldc_I8, (long)1); break;
                            default: throw new UnreachableException();
                        }
                        gen.Add(CilOpCodes.Add);
                        CompileIrNodeStore(ue.Value, null, gen, args, locals);
                        break;
                    
                    default: throw new ArgumentOutOfRangeException();
                }
            } break;
            case IRBinaryExp @bin:
            {
                CompileIrNodeLoad(@bin.Left, gen, args, locals);
                CompileIrNodeLoad(@bin.Right, gen, args, locals);
                
                var isSigned = ((RuntimeIntegerTypeReference)bin.Type!).Signed;
                var is128 = ((RuntimeIntegerTypeReference)bin.Type!).BitSize == 128;
    
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
                        if (is128) gen.Add(CilOpCodes.Call, _coreLib[isSigned ? "Int128" : "UInt128"].m["CheckedSub"]);
                        else gen.Add(CilOpCodes.Sub);
                        break;
                    
                    case IRBinaryExp.Operators.SubtractWarpAround:
                        if (is128) gen.Add(CilOpCodes.Call, _coreLib[isSigned ? "Int128" : "UInt128"].m["Sub"]);
                        else gen.Add(isSigned ? CilOpCodes.Sub_Ovf : CilOpCodes.Sub_Ovf_Un);
                        break;
                    
                    case IRBinaryExp.Operators.AddOnBounds:
                    case IRBinaryExp.Operators.SubtractOnBounds:
                    case IRBinaryExp.Operators.Multiply:
                    case IRBinaryExp.Operators.Divide:
                    case IRBinaryExp.Operators.DivideFloor:
                    case IRBinaryExp.Operators.DivideCeil:
                    case IRBinaryExp.Operators.Reminder:
                    case IRBinaryExp.Operators.BitwiseAnd:
                    case IRBinaryExp.Operators.BitwiseOr:
                    case IRBinaryExp.Operators.BitwiseXor:
                    case IRBinaryExp.Operators.LeftShift:
                    case IRBinaryExp.Operators.RightShift:
                    case IRBinaryExp.Operators.LogicalAnd:
                    case IRBinaryExp.Operators.LogicalOr:
                    default: throw new ArgumentOutOfRangeException();
                }
            } break;
            case IRCompareExp @cmp:
            {
                CompileIrNodeLoad(cmp.Left, gen, args, locals);
                CompileIrNodeLoad(cmp.Right, gen, args, locals);
    
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
            } break;
            
            case IRIf @if:
            {
                var breakLabel = new CilInstructionLabel();
                var nextConditionLabel = new CilInstructionLabel();
                
                CompileIrNodeLoad(@if.Condition, gen, args, locals);
                gen.Add(CilOpCodes.Brfalse, nextConditionLabel);
                
                CompileIr(@if.Then, gen, args, locals);
                gen.Add(CilOpCodes.Br, breakLabel);
    
                var currentElse = @if.Else;
                while (currentElse != null)
                {
                    var anchor = gen.Add(CilOpCodes.Nop);
                    nextConditionLabel.Instruction = anchor;

                    if (currentElse is IRIf @elseIf)
                    {
                        nextConditionLabel = new CilInstructionLabel();
            
                        CompileIrNodeLoad(@elseIf.Condition, gen, args, locals);
                        gen.Add(CilOpCodes.Brfalse, nextConditionLabel);
            
                        CompileIr(@elseIf.Then, gen, args, locals);
                        gen.Add(CilOpCodes.Br, breakLabel);
            
                        currentElse = @elseIf.Else;
                    }
                    else if (currentElse is IRElse @else)
                    {
                        CompileIr(@else.Then, gen, args, locals);
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
                var breakLabel = new CilInstructionLabel();
                
                // Jump to check
                gen.Add(CilOpCodes.Br,  checkLabel);
                
                // Body
                var lastIdx = gen.Count;
                CompileIr(@while.Process,  gen, args, locals);
                bodyLabel.Instruction = gen[lastIdx];
                
                // Step
                if (@while.Step != null) CompileIr(@while.Step, gen, args, locals);
                
                // Check
                lastIdx = gen.Count;
                CompileIrNodeLoad(@while.Condition, gen, args, locals);
                gen.Add(CilOpCodes.Brtrue, bodyLabel);
                checkLabel.Instruction = gen[lastIdx];
            } break;
            
            case IRReturn @ret:
                if (ret.Value != null) CompileIrNodeLoad(ret.Value, gen, args, locals);
                gen.Add(CilOpCodes.Ret);
                break;
            
            default: throw new UnreachableException();
        }
    }
    
    private void CompileIrNodeLoadAsRef(IRNode node, CilInstructionCollection gen, Parameter[] args, CilLocalVariable[] locals)
    {
        switch (node)
        {
            case IRSolvedReference @sr:
            {
                switch (sr.Reference)
                {
                    case LocalReference @l: gen.Add(CilOpCodes.Ldloca, locals[l.Local.index]); break;
                    
                    default: throw new UnreachableException();
                }
            } break;
            
            default: throw new UnreachableException();
        }
    }
    
    private void CompileIrNodeStore(IRNode node, IRNode? value, CilInstructionCollection gen, Parameter[] args, CilLocalVariable[] locals)
    {
        switch (node)
        {
            case IRSolvedReference @solv:
            {
                switch (solv.Reference)
                {
                    case LocalReference @l:
                        if (value != null) CompileIrNodeLoad(value, gen, args, locals);
                        gen.Add(CilOpCodes.Stloc, locals[l.Local.index]);
                        break;
                    
                    case SolvedFieldReference @f:
                        if (value != null) CompileIrNodeLoad(value, gen, args, locals);
                        gen.Add(f.Field.Static ? CilOpCodes.Stsfld : CilOpCodes.Stfld, _fieldsMap[f.Field]);
                        break;
                    
                    default: throw new UnreachableException();
                }
            } break;
    
            case IRAccess @access:
            {
                CompileIrNodeLoad(@access.A, gen, args, locals);
                CompileIrNodeStore(access.B, value, gen, args, locals);
            } break;
            
            default: throw new UnreachableException();
        }
    }
    
    private void CompileIrNodeCall(IRNode node, CilInstructionCollection gen, Parameter[] args, CilLocalVariable[] locals)
    {
        switch (node)
        {
            case IRSolvedReference solvedReference:
            {
                switch (solvedReference.Reference)
                {
                    case SolvedFunctionReference sfr:
                        gen.Add(CilOpCodes.Call, _functionsMap[sfr.Function]);
                        break;
                        
                    default: throw new UnreachableException();
                }
            } break;
                
            default: throw new UnreachableException();
        }
    }
    

    private TypeSignature TypeFromRef(TypeReference? typeRef)
    {
        if (typeRef == null) return corLib.Void;
        if (typeRef is ReferenceTypeReference tref)
        {
            var baset = TypeRefOf(@tref.InternalType);
            return baset.IsValueType ? baset.MakeByReferenceType() : baset.ToTypeSignature();
        }
        
        return typeRef switch
        {
            NoReturnTypeReference or VoidTypeReference => corLib.Void,
            
            _ => new TypeDefOrRefSignature(TypeRefOf(typeRef))
        };
    }
    private ITypeDefOrRef TypeRefOf(TypeReference? typeRef)
    {
        if (typeRef == null) return corLib.Void.Type;
        switch (typeRef)
        {
            case RuntimeIntegerTypeReference @i:
            {
                if (i.PtrSized) return (i.Signed ? corLib.IntPtr : corLib.UIntPtr).Type;
                
                switch (i.BitSize)
                {
                    case <= 8: return (i.Signed ? corLib.SByte : corLib.Byte).Type;
                    case <= 16: return (i.Signed ?  corLib.Int16 : corLib.UInt16).Type;
                    case <= 32: return (i.Signed ?  corLib.Int32 : corLib.UInt32).Type;
                    case <= 64: return (i.Signed ?  corLib.Int64 : corLib.UInt64).Type;
                    case <= 128: return _coreLib[i.Signed ? "Int128" : "UInt128"].t;
                }
                throw new UnreachableException();
            }
    
            case StringTypeReference: return corLib.String.Type;
            case BooleanTypeReference: return corLib.Boolean.Type;
            
            case NoReturnTypeReference:
            case VoidTypeReference: return corLib.Void.Type;
            
            case SolvedStructTypeReference @i: return _typesMap[i.Struct].ToTypeReference();
            
            default: throw new UnreachableException();
        }
    }
    
}
