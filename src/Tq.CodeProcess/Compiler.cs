using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using Abstract.CodeProcess.Core.Language.EvaluationData;
using Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree;
using Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Expresions;
using Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Macros;
using Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Values;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.CodeReferences;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.FieldReferences;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.FunctionReferences;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences.Builtin;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences.Builtin.Integer;

namespace Abstract.CodeProcess;

public class Compiler
{

    private List<StructObject> _programStructs = [];
    private List<FunctionObject> _programStaticFunctions = [];

    private Dictionary<StructObject, TypeInfo> _structTable = [];
    private Dictionary<FunctionObject, MethodInfo> _funcTable = [];
    private Dictionary<FieldObject, FieldInfo> _fieldTable = [];
    
    private ModuleBuilder _mbuilder;
    private TypeBuilder _staticContainer;
    
    private TypeInfo ValueType => typeof(ValueType).GetTypeInfo();
    
    public void Compile(ProgramObject program)
    {
        var programName = program.Modules[0].Name;
        
        var asmName = new AssemblyName(programName);
        var asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
        
        _mbuilder = asmBuilder.DefineDynamicModule(programName);
        _staticContainer = _mbuilder.DefineType("<static_container>", TypeAttributes.Public);

        foreach (var m in program.Modules) SearchRecursive(m);

        DeclareTypes();
        DeclareContent();
        ImplementMethods();
    }

    private void SearchRecursive(LangObject obj)
    {
        switch (obj)
        {
            case ModuleObject @a:
                foreach (var i in a.Children) SearchRecursive(i); 
                break;
            
            case NamespaceObject @a:
                foreach (var i in a.Children) SearchRecursive(i);
                break;
            
            case FunctionGroupObject @a:
                foreach (var i in a.Overloads) _programStaticFunctions.Add(i);
                break;

            case StructObject @a:
                _programStructs.Add(a);
                break;
            
            default: throw new UnreachableException();
        }
    }

    private void DeclareTypes()
    {
        foreach (var obj in _programStructs)
        {
            var typeName = string.Join(".", obj.Global);
            if (obj.Extends == null)
            {
                var typebuilder = _mbuilder.DefineType(
                    typeName,
                    TypeAttributes.Public
                    | TypeAttributes.Sealed
                    | TypeAttributes.ExplicitLayout);
                _structTable.Add(obj, typebuilder);
            }
            else
            {
                var typebuilder = _mbuilder.DefineType(
                    typeName,
                    TypeAttributes.Public
                    | TypeAttributes.ExplicitLayout);
                _structTable.Add(obj, typebuilder);
            }
        }
    }

    private void DeclareContent()
    {
        foreach (var f in _programStaticFunctions)
        {
            var methodName = string.Join(".", f.Global);
            var method = _staticContainer.DefineMethod(
                methodName,
                MethodAttributes.Public | MethodAttributes.Static,
                CallingConventions.Standard);
            _funcTable.Add(f, method);
        }

        foreach (var (obj, typeInfo) in _structTable)
        {
            foreach (var i in obj.Children)
            {
                switch (i)
                {
                    case FunctionObject @a:
                    {
                        var methodName = string.Join(".", a.Global);
                        MethodBuilder method;
                        if (a.Static)
                        {
                            method = ((TypeBuilder)typeInfo).DefineMethod(
                                methodName,
                                MethodAttributes.Public
                                | MethodAttributes.Static,
                                CallingConventions.Standard);
                        }
                        else
                        {
                            method = ((TypeBuilder)typeInfo).DefineMethod(
                                methodName,
                                MethodAttributes.Public,
                                CallingConventions.Standard);
                        }
                    
                        List<TypeInfo> paramTypes = [];
                        paramTypes.AddRange(a.Parameters.Select(param => TypeFromRef(param.Type)));
                        TypeInfo returnType = TypeFromRef(a.ReturnType);
                    
                        method.SetParameters([.. paramTypes]);
                        method.SetReturnType(returnType);
                    
                        _funcTable.Add(a, method);
                    } break;

                    case FieldObject @a:
                    {
                        var fieldName = string.Join(".", a.Global);
                        var fieldBuilder = ((TypeBuilder)typeInfo).DefineField(
                            fieldName,
                            TypeFromRef(a.Type),
                            FieldAttributes.Public);
                        _fieldTable.Add(a, fieldBuilder);
                    } break;
                
                    default: throw new UnreachableException();
                }
            }
        }
    }

    private void ImplementMethods()
    {
        foreach (var (obj, methodInfo) in _funcTable)
        {
            if (methodInfo is not MethodBuilder @mbuilder) continue;
            
            var body = obj.Body;
            if (body == null) continue;
            
            var generator = mbuilder.GetILGenerator();
            CompileIr(body, generator);
        }
    }

    private void CompileIr(IRBlock block, ILGenerator gen)
    {
        List<LocalBuilder> locals = [];
        foreach (var node in block.Content) CompileIrNodeLoad(node, gen, locals);
    }

    private void CompileIrNodeLoad(IRNode node, ILGenerator gen, List<LocalBuilder> locals)
    {
        switch (node)
        {
            case IRDefLocal @d: locals.Add(gen.DeclareLocal(TypeFromRef(d.LocalVariable.Type))); break;

            case IRAssign @ass:
                CompileIrNodeStore(ass.Target, ass.Value, gen, locals);
                break;
            
            case IRNewObject @nobj:
                gen.Emit(OpCodes.Newobj, TypeFromRef(nobj.Type));
                break;

            case IRIntegerLiteral @intlit:
            {
                switch (intlit.Size)
                {
                    case <= 32: gen.Emit(OpCodes.Ldc_I4, (uint)intlit.Value); break;
                    case <= 64: gen.Emit(OpCodes.Ldc_I4, (ulong)intlit.Value); break;
                    default: throw new UnreachableException();
                }
            } break;
            case IRStringLiteral @strlit:
                gen.Emit(OpCodes.Ldstr, strlit.Data);
                break;
                
            case IRSolvedReference @solv:
            {
                switch (solv.Reference)
                {
                    case LocalReference @lr:
                    {
                        switch (lr.Local.index)
                        {
                            case 0: gen.Emit(OpCodes.Ldloc_0); break;
                            case 1: gen.Emit(OpCodes.Ldloc_1); break;
                            case 2: gen.Emit(OpCodes.Ldloc_2); break;
                            case 3: gen.Emit(OpCodes.Ldloc_3); break;
                            default: gen.Emit(OpCodes.Ldloc, lr.Local.index); break;
                        }
                    } break;
                    
                    case SolvedFieldReference @fr:
                        gen.Emit(OpCodes.Ldflda, _fieldTable[fr.Field]);
                        break;
                    
                    default: throw new UnreachableException();
                }
            } break;

            case IRAccess @acc:
            {
                CompileIrNodeLoad(acc.A, gen, locals);
                CompileIrNodeLoad(acc.B, gen, locals);
            } break;

            case IRInvoke @iv:
            {
                foreach (var i in iv.Arguments)
                    CompileIrNodeLoad(i, gen, locals);
                CompileIrNodeCall(iv.Target, gen, locals);
            } break;

            case IRIntCast ic:
            {
                var targt = (RuntimeIntegerTypeReference)ic.Type!;
                var s = targt.Signed;
                var bitsize = targt.PtrSized ? 0 : targt.BitSize;
                
                switch (bitsize)
                {
                    case 0: gen.Emit(s ? OpCodes.Conv_I : OpCodes.Conv_U); break;
                    
                    case <= 8: gen.Emit(s ? OpCodes.Conv_I1 : OpCodes.Conv_U1); break;
                    case <= 16: gen.Emit(s ? OpCodes.Conv_I2 : OpCodes.Conv_U2); break;
                    case <= 32: gen.Emit(s ? OpCodes.Conv_I4 : OpCodes.Conv_U4); break;
                    case <= 64: gen.Emit(s ? OpCodes.Conv_I8 : OpCodes.Conv_U8); break;
                    
                    default: throw new UnreachableException();
                }
            } break;
            case IRIntExtend @ie:
            {
                var targt = (RuntimeIntegerTypeReference)ie.Type!;
                var s = targt.Signed;
                var bitsize = targt.PtrSized ? 0 : targt.BitSize;
                
                switch (bitsize)
                {
                    case 0: gen.Emit(s ? OpCodes.Conv_I : OpCodes.Conv_U); break;
                    
                    case <= 8: gen.Emit(s ? OpCodes.Conv_I1 : OpCodes.Conv_U1); break;
                    case <= 16: gen.Emit(s ? OpCodes.Conv_I2 : OpCodes.Conv_U2); break;
                    case <= 32: gen.Emit(s ? OpCodes.Conv_I4 : OpCodes.Conv_U4); break;
                    case <= 64: gen.Emit(s ? OpCodes.Conv_I8 : OpCodes.Conv_U8); break;
                    
                    default: throw new UnreachableException();
                }
            } break;
            case IRIntTrunc @it:
            {
                var targt = (RuntimeIntegerTypeReference)it.Type!;
                var s = targt.Signed;
                var bitsize = targt.PtrSized ? 0 : targt.BitSize;
                
                switch (bitsize)
                {
                    case 0: gen.Emit(s ? OpCodes.Conv_I : OpCodes.Conv_U); break;
                    
                    case <= 8: gen.Emit(s ? OpCodes.Conv_I1 : OpCodes.Conv_U1); break;
                    case <= 16: gen.Emit(s ? OpCodes.Conv_I2 : OpCodes.Conv_U2); break;
                    case <= 32: gen.Emit(s ? OpCodes.Conv_I4 : OpCodes.Conv_U4); break;
                    case <= 64: gen.Emit(s ? OpCodes.Conv_I8 : OpCodes.Conv_U8); break;
                    
                    default: throw new UnreachableException();
                }
            } break;

            case IRUnaryExp @ue:
            {
                switch (ue.Operation)
                {
                    case IRUnaryExp.UnaryOperation.Reference:
                        CompileIrNodeLoadAsRef(ue.Value, gen, locals);
                        break;
                    
                    default: throw new ArgumentOutOfRangeException();
                }
            } break;
            
            default: throw new UnreachableException();
        }
    }

    private void CompileIrNodeLoadAsRef(IRNode node, ILGenerator gen, List<LocalBuilder> locals)
    {
        switch (node)
        {
            case IRSolvedReference @sr:
            {
                switch (sr.Reference)
                {
                    case LocalReference @l: gen.Emit(OpCodes.Ldloca, locals[l.Local.index]); break;
                    
                    default: throw new UnreachableException();
                }
            } break;
            
            default: throw new UnreachableException();
        }
    }
    
    private void CompileIrNodeStore(IRNode node, IRNode value, ILGenerator gen, List<LocalBuilder> locals)
    {
        switch (node)
        {
            case IRSolvedReference @solv:
            {
                switch (solv.Reference)
                {
                    case LocalReference @l:
                        CompileIrNodeLoad(value, gen, locals);
                        gen.Emit(OpCodes.Stloc, locals[l.Local.index]);
                        break;
                    
                    case SolvedFieldReference @f:
                        gen.Emit(f.Field.Static ? OpCodes.Stsfld : OpCodes.Stfld, _fieldTable[f.Field]);
                        break;
                    
                    default: throw new UnreachableException();
                }
            } break;

            case IRAccess @access:
            {
                CompileIrNodeLoad(@access.A, gen, locals);
                CompileIrNodeStore(access.B, value, gen, locals);
            } break;
            
            default: throw new UnreachableException();
        }
    }

    private void CompileIrNodeCall(IRNode node, ILGenerator gen, List<LocalBuilder> locals)
    {
        switch (node)
        {
            case IRSolvedReference solvedReference:
            {
                switch (solvedReference.Reference)
                {
                    case SolvedFunctionReference sfr:
                        gen.Emit(OpCodes.Call, _funcTable[sfr.Function]);
                        break;
                        
                    default: throw new UnreachableException();
                }
            } break;
                
            default: throw new UnreachableException();
        }
    }
    
    private TypeInfo TypeFromRef(TypeReference? typeRef)
    {
        if (typeRef == null) return typeof(void).GetTypeInfo();
        switch (typeRef)
        {
            case RuntimeIntegerTypeReference @i:
            {
                if (i.PtrSized) return i.Signed
                    ? typeof(IntPtr).GetTypeInfo() : typeof(UIntPtr).GetTypeInfo();
                
                switch (i.BitSize)
                {
                    case <= 8: return (i.Signed ? typeof(sbyte) : typeof(byte)).GetTypeInfo();
                    case <= 16: return (i.Signed ? typeof(short) : typeof(ushort)).GetTypeInfo();;
                    case <= 32: return (i.Signed ? typeof(int) : typeof(uint)).GetTypeInfo();;
                    case <= 64: return (i.Signed ? typeof(long) : typeof(ulong)).GetTypeInfo();;
                    case <= 128: return (i.Signed ? typeof(Int128) : typeof(UInt128)).GetTypeInfo();;
                }
                throw new UnreachableException();
            }

            case StringTypeReference: return typeof(string).GetTypeInfo();
            case BooleanTypeReference: return typeof(bool).GetTypeInfo();
            
            case SolvedStructTypeReference @i:
                return _structTable[i.Struct];
            
            default: throw new UnreachableException();
        }
    }
}
