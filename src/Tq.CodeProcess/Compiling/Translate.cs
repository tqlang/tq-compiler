using System.Diagnostics;
using System.Numerics;
using Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree;
using Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Expressions;
using Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Statements;
using Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Values;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.CodeReferences;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.FieldReferences;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.FunctionReferences;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypedefReferences;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences.Builtin;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences.Builtin.Integer;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.PE.DotNet.Cil;

namespace Abstract.CodeProcess;

public partial class Compiler
{
    private void CompileIr(IRBlock block, Context ctx)
    {
        foreach (var node in block.Content) CompileIrNodeLoad(node, ctx);
    }
    
    private void CompileIrNodeLoad(IRNode node, Context ctx)
    {
        switch (node)
        {
            case IRAssign @ass:
                CompileIrNodeStore(ass.Target, ass.Value, ctx);
                break;

            case IRNewObject @nobj:
            {
                var type = _typesMap[((SolvedStructTypeReference)nobj.InstanceType).Struct];
                var ctor = type.Def.Methods.First(e => e.Name == ".ctor");

                if (type.Def.IsValueType)
                {
                    CompileIrNodeLoadAsRef(nobj.Arguments[0], ctx);
                    foreach (var i in nobj.Arguments[1..]) CompileIrNodeLoad(i, ctx);
                    ctx.Gen.Add(CilOpCodes.Call, ctor);
                    ctx.StackPop(nobj.Arguments.Length);
                }
                else
                {
                    foreach (var i in nobj.Arguments[1..]) CompileIrNodeLoad(i, ctx);
                    ctx.Gen.Add(CilOpCodes.Newobj, ctor);
                    ctx.StackPop(nobj.Arguments.Length);
                    CompileIrNodeStore(nobj.Arguments[0], null, ctx);
                }
            } break;

            case IrIntegerLiteral @intlit:
            {
                var signed = ((RuntimeIntegerTypeReference)intlit.Type!).Signed;
                switch (intlit.Size)
                {
                    case <= 32:
                        ctx.Gen.Add(CilInstruction.CreateLdcI4((int)intlit.Value));
                        ctx.StackPush(signed ? _corLibFactory.Int32 : _corLibFactory.UInt32);
                        break;
                    case <= 64:
                        ctx.Gen.Add(CilOpCodes.Ldc_I8, unchecked((long)(UInt128)intlit.Value));
                        ctx.StackPush(signed ? _corLibFactory.Int64 : _corLibFactory.UInt64);
                        break;
                    case <= 128:
                    {
                        var largeType = signed ? _coreLib["Int128"] : _coreLib["UInt128"];
                        
                        if (intlit.Value == 0)
                        {
                            var tmp = new CilLocalVariable(largeType.t);
                            ctx.Gen.Owner.LocalVariables.Add(tmp);
                            ctx.Gen.Add(CilOpCodes.Ldloca, tmp);
                            ctx.Gen.Add(CilOpCodes.Initobj, largeType.t.ToTypeDefOrRef());
                            ctx.Gen.Add(CilOpCodes.Ldloc, tmp);
                        }
                        else if (intlit.Value <= ulong.MaxValue)
                        {
                            ctx.Gen.Add(CilOpCodes.Ldc_I8, (long)intlit.Value);
                            ctx.Gen.Add(CilOpCodes.Call, largeType.m[intlit.Value.Sign < 0 ? "Conv_from_i64" : "Conv_from_u64"]);
                        }
                        else
                        {
                            var tmp = new CilLocalVariable(largeType.t);
                            ctx.Gen.Owner.LocalVariables.Add(tmp);
                            ctx.Gen.Add(CilOpCodes.Ldloca, tmp);
                            var mask = (BigInteger.One << 64) - BigInteger.One;
                            var hi = (ulong)((intlit.Value >> 64) & mask);
                            var lo = (ulong)(intlit.Value & mask);
                            ctx.Gen.Add(CilOpCodes.Ldc_I8, unchecked((long)hi));
                            ctx.Gen.Add(CilOpCodes.Ldc_I8, unchecked((long)lo));
                            ctx.Gen.Add(CilOpCodes.Call, largeType.m["new"]);
                            ctx.Gen.Add(CilOpCodes.Ldloc, tmp);
                        }
                        ctx.StackPush(largeType.t);
                    } break;
                    default: throw new UnreachableException();
                }
                
            } break;
            case IrCharLiteral @charlit:
                ctx.Gen.Add(CilOpCodes.Ldc_I4_S, (short)charlit.Data);
                ctx.StackPush(_corLibFactory.Char);
                break;
            case IrStringLiteral @strlit:
                ctx.Gen.Add(CilOpCodes.Ldstr, strlit.Data);
                ctx.StackPush(_corLibFactory.String);
                break;
            case IrCollectionLiteral @collit:
            {
                var elmtype = TypeFromRef(collit.ElementType);
                
                ctx.Gen.Add(CilInstruction.CreateLdcI4(collit.Length));
                ctx.Gen.Add(CilOpCodes.Newarr, elmtype.ToTypeDefOrRef());
                ctx.StackPush(TypeFromRef(collit.Type));

                var index = 0;
                foreach (var i in collit.Items)
                {
                    ctx.Gen.Add(CilOpCodes.Dup);
                    ctx.Gen.Add(CilInstruction.CreateLdcI4(index++));
                    CompileIrNodeLoad(i,  ctx);
                    ctx.Gen.Add(CilOpCodes.Stelem, elmtype.ToTypeDefOrRef());
                    ctx.StackPop();
                }
            } break;
            
            case IRSolvedReference @solv:
            {
                switch (solv.Reference)
                {
                    case LocalReference @lr:
                        ctx.Gen.Add(CilOpCodes.Ldloc, ctx.GetLoc(lr.Local.index));
                        ctx.StackPush(ctx.GetLoc(lr.Local.index).VariableType);
                        break;
    
                    case ParameterReference @pr:
                        ctx.Gen.Add(CilOpCodes.Ldarg, ctx.GetArg(pr.Parameter.index));
                        ctx.StackPush(ctx.GetArg(pr.Parameter.index).ParameterType);
                        break;

                    case SolvedFieldReference @fr:
                    {
                        var field = _fieldsMap[fr.Field];
                        
                        if (!field.IsStatic && !ctx.Stack[^1].IsAssignableTo(field.DeclaringType!.ToTypeSignature()))
                            ctx.Gen.Add(CilOpCodes.Conv_U);
                        
                        ctx.Gen.Add(field.IsStatic ? CilOpCodes.Ldsfld : CilOpCodes.Ldfld, field);
                        if (!field.IsStatic) ctx.StackPop(); 
                        ctx.StackPush(_fieldsMap[fr.Field].Signature!.FieldType);
                    } break;

                    case SolvedTypedefNamedFieldReference @tn:
                    {
                        var item = tn.Item;
                        var typedef = (TypedefObject)item.Parent;
                        
                        var enumdata = _enumsMap[typedef];
                        var enumfield = enumdata.GetItem(item);

                        ctx.Gen.Add(CilOpCodes.Ldsfld, enumfield);
                        ctx.StackPush(enumfield.Signature!.FieldType);
                    } break;
                    
                    default: throw new UnreachableException();
                }
            } break;
    
            case IRAccess @acc:
            {
                CompileIrNodeLoadAsRef(acc.A, ctx);
                CompileIrNodeLoadAsRef(acc.B, ctx);
                
                var t = TypeFromRef(acc.Type);
                ctx.Gen.Add(CilOpCodes.Ldobj, t.ToTypeDefOrRef());
                ctx.StackPush(t);
            } break;
    
            case IRInvoke @iv:
            {
                foreach (var i in iv.Arguments) CompileIrNodeLoad(i, ctx);
                CompileIrNodeCall(iv.Target, ctx);
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
                        CompileIrNodeLoad(c.Expression, ctx);
                        ctx.StackPop();
                        ctx.Gen.Add(CilOpCodes.Box, baseTypeRef.ToTypeDefOrRef());
                        ctx.Gen.Add(CilOpCodes.Callvirt, _coreLib["Object"].m["ToString"]);
                        ctx.StackPush(_corLibFactory.String);
                    } break;

                    case RuntimeIntegerTypeReference @targt:
                    {
                        CompileIrNodeLoad(c.Expression, ctx);
                        ctx.StackPop();
                        
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
                                    ctx.Gen.Add(CilOpCodes.Call, baset.m[s ? "Conv_to_i8" : "Conv_to_u8"]);
                                    ctx.StackPush(s ? _corLibFactory.SByte :  _corLibFactory.Byte);
                                    break;
                                case <= 16:
                                    ctx.Gen.Add(CilOpCodes.Call, baset.m[s ? "Conv_to_i16" : "Conv_to_u16"]);
                                    ctx.StackPush(s ? _corLibFactory.Int16 : _corLibFactory.UInt16);
                                    break;
                                case <= 32:
                                    ctx.Gen.Add(CilOpCodes.Call, baset.m[s ? "Conv_to_i32" : "Conv_to_u32"]);
                                    ctx.StackPush(s ? _corLibFactory.Int32 : _corLibFactory.UInt32);
                                    break;
                                case <= 64:
                                    ctx.Gen.Add(CilOpCodes.Call, baset.m[s ? "Conv_to_i64" : "Conv_to_u64"]);
                                    ctx.StackPush(s ? _corLibFactory.Int64 : _corLibFactory.UInt64);
                                    break;
                                default: throw new UnreachableException();
                            }
                            return;
                        }
                        
                        switch (bitsize)
                        {
                            case 0:
                                ctx.Gen.Add(s ? CilOpCodes.Conv_I : CilOpCodes.Conv_U);
                                ctx.StackPush(s ? _corLibFactory.IntPtr : _corLibFactory.UIntPtr);
                                break;
                            
                            case <= 8:
                                ctx.Gen.Add(s ? CilOpCodes.Conv_I1 : CilOpCodes.Conv_U1);
                                ctx.StackPush(s ? _corLibFactory.SByte : _corLibFactory.Byte);
                                break;
                            case <= 16:
                                ctx.Gen.Add(s ? CilOpCodes.Conv_I2 : CilOpCodes.Conv_U2);
                                ctx.StackPush(s ? _corLibFactory.Int16 : _corLibFactory.UInt16);
                                break;
                            case <= 32:
                                ctx.Gen.Add(s ? CilOpCodes.Conv_I4 : CilOpCodes.Conv_U4);
                                ctx.StackPush(s ? _corLibFactory.Int32 : _corLibFactory.UInt32);
                                break;
                            case <= 64:
                                ctx.Gen.Add(s ? CilOpCodes.Conv_I8 : CilOpCodes.Conv_U8);
                                ctx.StackPush(s ? _corLibFactory.Int64 : _corLibFactory.UInt64);
                                break;

                            case <= 128:
                            {
                                var baset = srs ? _coreLib["Int128"] : _coreLib["UInt128"];
                                
                                switch (srbitsize)
                                {
                                    case <= 8:ctx.Gen.Add(CilOpCodes.Call, baset.m[s ? "Conv_from_i8" : "Conv_from_u8"]); break;
                                    case <= 16: ctx.Gen.Add(CilOpCodes.Call, baset.m[s ? "Conv_from_i16" : "Conv_from_u16"]); break;
                                    case <= 32: ctx.Gen.Add(CilOpCodes.Call, baset.m[s ? "Conv_from_i32" : "Conv_from_u32"]); break;
                                    case <= 64: ctx.Gen.Add(CilOpCodes.Call, baset.m[s ? "Conv_from_i64" : "Conv_from_u64"]); break;
                                    default: throw new UnreachableException();
                                }

                                ctx.StackPush(baset.t);
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
                RuntimeIntegerTypeReference it;
                var bs = 0;
                
                switch (ue.Operation)
                {
                    case IRUnaryExp.UnaryOperation.Reference:
                        CompileIrNodeLoadAsRef(ue.Value, ctx);
                        break;

                    case IRUnaryExp.UnaryOperation.BitwiseNot:
                    {
                        var isSigned = ((RuntimeIntegerTypeReference)ue.Type!).Signed;
                        var is128 = ((RuntimeIntegerTypeReference)ue.Type!).BitSize == 128;
                        
                        CompileIrNodeLoad(ue.Value, ctx);
                        if (is128) ctx.Gen.Add(CilOpCodes.Call, _coreLib[isSigned ? "Int128" : "UInt128"].m["BitwiseNot"]);
                        else ctx.Gen.Add(CilOpCodes.Not); ;
                    } break;

                    case IRUnaryExp.UnaryOperation.PreIncrement:
                        CompileIrNodeLoad(ue.Value, ctx);
                        
                        it = (RuntimeIntegerTypeReference)ue.Value.Type;
                        bs = it.BitSize.Bits;
                        switch (bs)
                        {
                            case <= 32: CilInstruction.CreateLdcI4(1); break;
                            case <= 64: ctx.Gen.Add(CilOpCodes.Ldc_I8, (long)1); break;
                            default: throw new UnreachableException();
                        }
                        
                        ctx.Gen.Add(CilOpCodes.Add);
                        ctx.Gen.Add(CilOpCodes.Dup);
                        CompileIrNodeStore(ue.Value, null, ctx);
                        ctx.Stack.Add(TypeFromRef(ue.Type));
                        break;
                    
                    case IRUnaryExp.UnaryOperation.PostIncrement:
                        CompileIrNodeLoad(ue.Value, ctx);
                        ctx.Gen.Add(CilOpCodes.Dup);
                        
                        it = (RuntimeIntegerTypeReference)ue.Value.Type;
                        bs = it.BitSize.Bits;
                        switch (bs)
                        {
                            case <= 32: CilInstruction.CreateLdcI4(1); break;
                            case <= 64: ctx.Gen.Add(CilOpCodes.Ldc_I8, (long)1); break;
                            default: throw new UnreachableException();
                        }
                        
                        ctx.Gen.Add(CilOpCodes.Add);
                        CompileIrNodeStore(ue.Value, null, ctx);
                        ctx.Stack.Add(TypeFromRef(ue.Type));
                        break;
                    
                    default: throw new ArgumentOutOfRangeException();
                }
            } break;
            case IRBinaryExp @bin:
            {
                CompileIrNodeLoad(bin.Left, ctx);
                CompileIrNodeLoad(bin.Right, ctx);
                
                switch (bin.Left.Type)
                {
                    case RuntimeIntegerTypeReference @originType:
                    {
                        var isSigned = originType.Signed;
                        var is128 = originType.BitSize == 128;

                        switch (bin.Operator)
                        {
                            case IRBinaryExp.Operators.Add:
                                if (is128) ctx.Gen.Add(CilOpCodes.Call, _coreLib[isSigned ? "Int128" : "UInt128"].m["Add"]);
                                else ctx.Gen.Add(CilOpCodes.Add);
                                break;

                            case IRBinaryExp.Operators.AddWarpAround:
                                if (is128) ctx.Gen.Add(CilOpCodes.Call, _coreLib[isSigned ? "Int128" : "UInt128"].m["AddOvf"]);
                                else ctx.Gen.Add(isSigned ? CilOpCodes.Add_Ovf : CilOpCodes.Add_Ovf_Un);
                                break;


                            case IRBinaryExp.Operators.Subtract:
                                if (is128) ctx.Gen.Add(CilOpCodes.Call, _coreLib[isSigned ? "Int128" : "UInt128"].m["Sub"]);
                                else ctx.Gen.Add(CilOpCodes.Sub);
                                break;

                            case IRBinaryExp.Operators.SubtractWarpAround:
                                if (is128) ctx.Gen.Add(CilOpCodes.Call, _coreLib[isSigned ? "Int128" : "UInt128"].m["SubOvf"]);
                                else ctx.Gen.Add(isSigned ? CilOpCodes.Sub_Ovf : CilOpCodes.Sub_Ovf_Un);
                                break;

                            case IRBinaryExp.Operators.Multiply:
                                if (is128) ctx.Gen.Add(CilOpCodes.Call, _coreLib[isSigned ? "Int128" : "UInt128"].m["Mul"]);
                                else ctx.Gen.Add(isSigned ? CilOpCodes.Mul_Ovf : CilOpCodes.Mul_Ovf_Un);
                                break;

                            case IRBinaryExp.Operators.Divide:
                                if (is128) ctx.Gen.Add(CilOpCodes.Call, _coreLib[isSigned ? "Int128" : "UInt128"].m["Div"]);
                                else ctx.Gen.Add(isSigned ? CilOpCodes.Div : CilOpCodes.Div_Un);
                                break;

                            case IRBinaryExp.Operators.Reminder:
                                if (is128) ctx.Gen.Add(CilOpCodes.Call, _coreLib[isSigned ? "Int128" : "UInt128"].m["Rem"]);
                                else ctx.Gen.Add(isSigned ? CilOpCodes.Rem : CilOpCodes.Rem_Un);
                                break;

                            case IRBinaryExp.Operators.BitwiseAnd:
                                if (is128)
                                    ctx.Gen.Add(CilOpCodes.Call, _coreLib[isSigned ? "Int128" : "UInt128"].m["BitwiseAnd"]);
                                else ctx.Gen.Add(CilOpCodes.And);
                                break;

                            case IRBinaryExp.Operators.BitwiseOr:
                                if (is128)
                                    ctx.Gen.Add(CilOpCodes.Call, _coreLib[isSigned ? "Int128" : "UInt128"].m["BitwiseOr"]);
                                else ctx.Gen.Add(CilOpCodes.Or);
                                break;

                            case IRBinaryExp.Operators.BitwiseXor:
                                if (is128)
                                    ctx.Gen.Add(CilOpCodes.Call, _coreLib[isSigned ? "Int128" : "UInt128"].m["BitwiseXor"]);
                                else ctx.Gen.Add(CilOpCodes.Xor);
                                break;

                            case IRBinaryExp.Operators.LeftShift:
                                //ctx.Gen.Add(CilOpCodes.Conv_I4);
                                if (is128)
                                    ctx.Gen.Add(CilOpCodes.Call, _coreLib[isSigned ? "Int128" : "UInt128"].m["LeftShift"]);
                                else ctx.Gen.Add(CilOpCodes.Shl);
                                break;

                            case IRBinaryExp.Operators.RightShift:
                                //ctx.Gen.Add(CilOpCodes.Conv_I4);
                                if (is128)
                                    ctx.Gen.Add(CilOpCodes.Call, _coreLib[isSigned ? "Int128" : "UInt128"].m["RightShift"]);
                                else ctx.Gen.Add(CilOpCodes.Shr_Un);
                                break;
                            
                            case IRBinaryExp.Operators.AddOnBounds:
                            case IRBinaryExp.Operators.SubtractOnBounds:
                            case IRBinaryExp.Operators.DivideFloor:
                            case IRBinaryExp.Operators.DivideCeil:
                            default: throw new ArgumentOutOfRangeException();
                        }
                        break;
                    }
                    
                    default:
                        throw new UnreachableException();
                }
                
                ctx.StackPop();
            } break;
            case IRCompareExp @cmp:
            {
                CompileIrNodeLoad(cmp.Left, ctx);
                CompileIrNodeLoad(cmp.Right, ctx);
                
                switch (cmp.Left.Type)
                {
                    case RuntimeIntegerTypeReference @originType:
                    {
                        var isSigned = originType.Signed;
                        var is128 = originType.BitSize == 128;

                        switch (cmp.Operator)
                        {

                            case IRCompareExp.Operators.Equality:
                                ctx.Gen.Add(CilOpCodes.Ceq);
                                break;
                        
                            case IRCompareExp.Operators.Inequality:
                                ctx.Gen.Add(CilOpCodes.Ceq);
                                ctx.Gen.Add(CilOpCodes.Ldc_I4_0);
                                ctx.Gen.Add(CilOpCodes.Ceq);
                                break;
                        
                            case IRCompareExp.Operators.LessThan: ctx.Gen.Add(isSigned ? CilOpCodes.Clt : CilOpCodes.Clt_Un); break;
                            case IRCompareExp.Operators.GreaterThan: ctx.Gen.Add(isSigned ? CilOpCodes.Cgt : CilOpCodes.Cgt_Un); break;
                            
                            case IRCompareExp.Operators.LessThanOrEqual:
                                ctx.Gen.Add(isSigned ? CilOpCodes.Cgt : CilOpCodes.Cgt_Un);
                                ctx.Gen.Add(CilOpCodes.Ldc_I4_0);
                                ctx.Gen.Add(CilOpCodes.Ceq);
                                break;
                            case IRCompareExp.Operators.GreaterThanOrEqual:
                                ctx.Gen.Add(isSigned ? CilOpCodes.Clt : CilOpCodes.Clt_Un);
                                ctx.Gen.Add(CilOpCodes.Ldc_I4_0);
                                ctx.Gen.Add(CilOpCodes.Ceq);
                                break;
                            
                            default: throw new ArgumentOutOfRangeException();
                        }
                        break;
                    }
                    
                    case BooleanTypeReference:
                        switch (cmp.Operator)
                        {
                            case IRCompareExp.Operators.Equality:
                                ctx.Gen.Add(CilOpCodes.Ceq);
                                break;

                            case IRCompareExp.Operators.Inequality:
                                ctx.Gen.Add(CilOpCodes.Ceq);
                                ctx.Gen.Add(CilOpCodes.Ldc_I4_1);
                                ctx.Gen.Add(CilOpCodes.Xor);
                                break;

                            default: throw new ArgumentOutOfRangeException();
                        }
                        break;

                    case CharTypeReference:
                    {
                        switch (cmp.Operator)
                        {
                            case IRCompareExp.Operators.Equality:
                                ctx.Gen.Add(CilOpCodes.Ceq);
                                break;

                            case IRCompareExp.Operators.Inequality:
                                ctx.Gen.Add(CilOpCodes.Ceq);
                                ctx.Gen.Add(CilOpCodes.Ldc_I4_0);
                                ctx.Gen.Add(CilOpCodes.Ceq);
                                break;
                            
                            default: throw new ArgumentOutOfRangeException();
                        }
                    } break;

                    case StringTypeReference:
                    {
                        switch (cmp.Operator)
                        {
                            case IRCompareExp.Operators.Equality:
                                ctx.Gen.Add(CilOpCodes.Call, _coreLib["String"].m["Equals"]);
                                break;

                            case IRCompareExp.Operators.Inequality:
                                ctx.Gen.Add(CilOpCodes.Call, _coreLib["String"].m["Equals"]);
                                ctx.Gen.Add(CilOpCodes.Ldc_I4_0);
                                ctx.Gen.Add(CilOpCodes.Ceq);
                                break;

                            default: throw new ArgumentOutOfRangeException();
                        }
                    } break;
                }
                
                ctx.StackPop(2);
                ctx.StackPush(_corLibFactory.Boolean);
                
            } break;
            case IrLogicalExp @log:
            {
                if (ctx.GetFrame() is ConditionalExpressionFrame @cef)
                {
                    switch (log.Operator)
                    {
                        case IrLogicalExp.Operators.And:
                            CompileIrNodeLoad(log.Left, ctx);
                            if (log.Left is not IrLogicalExp)
                            {
                                ctx.Gen.Add(CilOpCodes.Brfalse, cef.IfFalse);
                                ctx.StackPop();
                            }
                            CompileIrNodeLoad(log.Right, ctx);
                            if (log.Right is not IrLogicalExp)
                            {
                                ctx.Gen.Add(CilOpCodes.Brfalse, cef.IfFalse);
                                ctx.StackPop();
                            }
                            break;
                        
                        case IrLogicalExp.Operators.Or:
                            CompileIrNodeLoad(log.Left, ctx);
                            ctx.Gen.Add(CilOpCodes.Brtrue, cef.IfTrue);
                            ctx.StackPop();
                            CompileIrNodeLoad(log.Right, ctx);
                            ctx.Gen.Add(CilOpCodes.Brtrue, cef.IfTrue);
                            ctx.StackPop();
                            break;
                        
                        default: throw new ArgumentOutOfRangeException();
                    }
                }
                else
                {
                    CompileIrNodeLoad(log.Left, ctx);
                    CompileIrNodeLoad(log.Right, ctx);

                    switch (log.Operator)
                    {
                        case IrLogicalExp.Operators.And:
                            ctx.Gen.Add(CilOpCodes.And);
                            break;
                        
                        case IrLogicalExp.Operators.Or:
                            ctx.Gen.Add(CilOpCodes.Or);
                            break;
                        
                        default: throw new ArgumentOutOfRangeException();
                    }
                    
                    ctx.StackPop(2);
                    ctx.StackPush(_corLibFactory.Boolean);
                }
            } break;
            
            case IrIndex @idx:
            {
                var elmtype = TypeFromRef(idx.ResultType);
                CompileIrNodeLoad(idx.Value, ctx);
                CompileIrNodeLoad(idx.Indices[0], ctx);

                if (ctx.Stack[^2] is SzArrayTypeSignature)
                {
                    if (ctx.Stack[^1] is CorLibTypeSignature @clts && IsExplicitInteger(clts, out var sig, out var len))
                        if (len > 4) ctx.Gen.Add(CilOpCodes.Conv_I4);
                    ctx.Gen.Add(CilOpCodes.Ldelem, elmtype.ToTypeDefOrRef());
                }
                else if (ctx.Stack[^2] == _corLibFactory.String)
                {
                    if (ctx.Stack[^1] is CorLibTypeSignature @clts && IsExplicitInteger(clts, out var sig, out var len))
                        if (len > 4) ctx.Gen.Add(CilOpCodes.Conv_I4);
                    ctx.Gen.Add(CilOpCodes.Call, _coreLib["String"].m["charAt"]);
                }
                else throw new UnreachableException();
                
                ctx.StackPop();
                ctx.Stack[^1] = elmtype;
            } break;
            case IrLenOf @lenof:
            {
                CompileIrNodeLoad(lenof.OfValue, ctx);
                ctx.Gen.Add(CilOpCodes.Ldlen);
                ctx.Stack[^1] = TypeFromRef(lenof.Type);
            } break;
            
            case IRIf @if:
            {
                var thisConditionLabel = new CilInstructionLabel();
                var nextConditionLabel = new CilInstructionLabel();
                var breakLabel = new CilInstructionLabel();
                
                ctx.FramePush(new ConditionalExpressionFrame(thisConditionLabel, nextConditionLabel));
                CompileIrNodeLoad(@if.Condition, ctx);
                if (@if.Condition is not IrLogicalExp)
                {
                    ctx.Gen.Add(CilOpCodes.Brfalse, nextConditionLabel);
                    ctx.StackPop();
                }
                ctx.FramePop();

                ctx.MarkLabel(thisConditionLabel);
                CompileIr(@if.Then, ctx);
                ctx.Gen.Add(CilOpCodes.Br, breakLabel);
                
                var currentElse = @if.Else;
                while (currentElse != null)
                {
                    var anchor = ctx.Gen.Add(CilOpCodes.Nop);
                    nextConditionLabel.Instruction = anchor;

                    if (currentElse is IRIf @elseIf)
                    {
                        thisConditionLabel = new CilInstructionLabel();
                        nextConditionLabel = new CilInstructionLabel();
                        
                        ctx.FramePush(new ConditionalExpressionFrame(thisConditionLabel, nextConditionLabel));
                        CompileIrNodeLoad(@elseIf.Condition, ctx);
                        ctx.Gen.Add(CilOpCodes.Brfalse, nextConditionLabel);
                        ctx.FramePop();
                        ctx.StackPop();
                        
                        ctx.MarkLabel(thisConditionLabel);
                        CompileIr(@elseIf.Then, ctx);
                        ctx.Gen.Add(CilOpCodes.Br, breakLabel);
            
                        currentElse = @elseIf.Else;
                    }
                    else if (currentElse is IRElse @else)
                    {
                        CompileIr(@else.Then, ctx);
                        currentElse = null; 
                    }
                }
                
                var finalNop = ctx.Gen.Add(CilOpCodes.Nop);
                
                breakLabel.Instruction = finalNop;
                if (nextConditionLabel.Instruction == null) 
                    nextConditionLabel.Instruction = finalNop;

            } break;
    
            case IRWhile @while:
            {
                var checkLabel = new CilInstructionLabel();
                var bodyLabel = new CilInstructionLabel();
                
                // Jump to check
                ctx.Gen.Add(CilOpCodes.Br,  checkLabel);
                
                // Body
                var lastIdx = ctx.Gen.Count;
                CompileIr(@while.Process, ctx);
                bodyLabel.Instruction = ctx.Gen[lastIdx];
                
                // Step
                if (@while.Step != null) CompileIr(@while.Step, ctx);
                
                // Check
                lastIdx = ctx.Gen.Count;
                CompileIrNodeLoad(@while.Condition, ctx);
                ctx.Gen.Add(CilOpCodes.Brtrue, bodyLabel);
                ctx.StackPop();
                checkLabel.Instruction = ctx.Gen[lastIdx];
            } break;
            
            case IRReturn @ret:
                if (ret.Value != null)
                {
                    CompileIrNodeLoad(ret.Value, ctx);
                    ctx.StackPop();
                }
                ctx.Gen.Add(CilOpCodes.Ret);
                break;
            
            default: throw new UnreachableException();
        }
    }
    
    private void CompileIrNodeLoadAsRef(IRNode node,  Context ctx)
    {
        switch (node)
        {
            case IRSolvedReference @sr:
            {
                switch (sr.Reference)
                {
                    case LocalReference @l:
                    {
                        var t = ctx.GetLoc(l.Local.index).VariableType;
                        var byRef = t.IsValueType;
                        ctx.Gen.Add(byRef ? CilOpCodes.Ldloca : CilOpCodes.Ldloc, ctx.GetLoc(l.Local.index));
                        ctx.StackPush(t.MakeByReferenceType());
                    } break;

                    case ParameterReference @p:
                    {
                        var t = ctx.GetArg(p.Parameter.index).ParameterType;
                        var byRef = t.IsValueType;
                        ctx.Gen.Add(byRef ? CilOpCodes.Ldarga : CilOpCodes.Ldarg, ctx.GetArg(p.Parameter.index));
                        ctx.StackPush(t.MakeByReferenceType());
                    } break;

                    case SolvedFieldReference @f:
                    {
                        var fi = _fieldsMap[f.Field];
                        var byRef = fi.Signature!.FieldType.IsValueType;
                        
                        if (!fi.IsStatic && !ctx.Stack[^1].IsAssignableTo(fi.DeclaringType!.ToTypeSignature()))
                            ctx.Gen.Add(CilOpCodes.Conv_U);
                        
                        ctx.Gen.Add(CilOpCodes.Ldflda, fi);
                        if (fi.IsStatic) ctx.StackPop();
                        ctx.StackPush(fi.Signature!.FieldType.MakeByReferenceType());
                    } break;

                    default: throw new UnreachableException();
                }
            } break;
            
            case IRAccess @acc:
            {
                CompileIrNodeLoadAsRef(acc.A, ctx);
                CompileIrNodeLoadAsRef(acc.B, ctx);
            } break;
            
            default: throw new UnreachableException();
        }
    }
    
    private void CompileIrNodeStore(IRNode node, IRNode? value,  Context ctx)
    {
        switch (node)
        {
            case IRSolvedReference @solv:
            {
                switch (solv.Reference)
                {
                    case LocalReference @l:
                    {
                        if (value != null) CompileIrNodeLoad(value, ctx);
                        ctx.Gen.Add(CilOpCodes.Stloc, ctx.GetLoc(l.Local.index));
                        ctx.StackPop();
                    } break;

                    case SolvedFieldReference @f:
                    {
                        var t = _fieldsMap[f.Field];
                        var isstat = t.IsStatic;
                        
                        if (!t.IsStatic && !ctx.Stack[^1].IsAssignableTo(t.DeclaringType!.ToTypeSignature()))
                            ctx.Gen.Add(CilOpCodes.Conv_U);
                        
                        if (value != null) CompileIrNodeLoad(value, ctx);
                        ctx.Gen.Add(isstat ? CilOpCodes.Stsfld : CilOpCodes.Stfld, t);
                        
                        if (!isstat) ctx.StackPop();
                        ctx.StackPop();
                    } break;

                    default: throw new UnreachableException();
                }
            } break;
    
            case IRAccess @access:
            {
                CompileIrNodeLoadAsRef(@access.A, ctx);
                CompileIrNodeStore(access.B, value, ctx);
            } break;

            case IrIndex @idx:
            {
                var elmtype = TypeFromRef(idx.ResultType);
                CompileIrNodeLoad(idx.Value, ctx);
                CompileIrNodeLoad(idx.Indices[0], ctx);
                CompileIrNodeLoad(value!, ctx);
                ctx.Gen.Add(CilOpCodes.Stelem, elmtype.ToTypeDefOrRef());
                ctx.StackPop(3);
            } break;
            
            default: throw new UnreachableException();
        }
    }
    
    private void CompileIrNodeCall(IRNode node,  Context ctx)
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

                        if (f.InstanceType != null)
                        {
                            var currentInstanceTypeSig = ctx.Stack[^pl];
                        }
                        
                        ctx.Gen.Add(CilOpCodes.Call, f.Function);
                        ctx.StackPop(pl);
                        
                        if (f.ReturnsValue) ctx.StackPush(f.ReturnType);
                    } break;

                    default: throw new UnreachableException();
                }
            } break;
                
            default: throw new UnreachableException();
        }
    }
    
}