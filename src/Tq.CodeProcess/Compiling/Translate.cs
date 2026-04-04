using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using Abstract.CodeProcess.Core.EvaluationData.IntermediateTree;
using Abstract.CodeProcess.Core.EvaluationData.IntermediateTree.Expressions;
using Abstract.CodeProcess.Core.EvaluationData.IntermediateTree.Statements;
using Abstract.CodeProcess.Core.EvaluationData.IntermediateTree.Values;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.CodeReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.Dotnet;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.FieldReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.FunctionReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypedefReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin.Integer;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using TypeReference = Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.TypeReference;

namespace Abstract.CodeProcess;

public partial class Compiler
{
    private void CompileIr(IrBlock block, Context ctx)
    {
        foreach (var node in block.Content) CompileIrNodeLoad(node, true, ctx);
    }
    
    private void CompileIrNodeLoad(IrNode node, bool ignoreValue, Context ctx)
    {
        switch (node)
        {
            case IrBlock @b: CompileIr(b, ctx); break;
            
            case IrAssign @ass:
                CompileIrNodeStore(ass.Target, ass.Value, ctx);
                break;

            case IrNewObject @nobj:
            {
                if (ignoreValue) return;
                var type = nobj.InstanceType switch
                {
                    SolvedStructTypeReference structRef => _typesMap[structRef.Struct].Type,
                    DotnetTypeReference dotnetRef => dotnetRef.Reference.Reference,
                    _ => throw new NotImplementedException()
                };
                var typeSignature = type.ToTypeSignature();

                if (type.IsValueType)
                {
                    var local = ctx.AllocTmp(typeSignature);
                    ctx.Gen.Add(CilOpCodes.Ldloca, local);
                    CompileIrNodeCall(nobj.Target, nobj.Arguments, false, ctx);
                    ctx.Gen.Add(CilOpCodes.Ldloc, local);
                    ctx.FreeTmp(typeSignature, local);
                }
                else
                {
                    CompileIrNodeCall(nobj.Target, nobj.Arguments, false, ctx, useNewObj: true);
                }

                if (ignoreValue) ctx.Gen.Add(CilOpCodes.Pop);
                else ctx.StackPush(typeSignature);
            } break;

            case IrIntegerLiteral @intlit:
            {
                if (ignoreValue) return;
                var signed = ((RuntimeIntegerTypeReference)intlit.Type!).Signed;
                switch (intlit.Size)
                {
                    // case <= 8:
                    //     ctx.Gen.Add(CilOpCodes.Ldc_I4_S, (short)intlit.Value);
                    //     ctx.Gen.Add(CilOpCodes.Conv_I1);
                    //     ctx.StackPush(signed ? _corLibFactory.SByte : _corLibFactory.Byte);
                    //     break;
                    // case <= 16:
                    //     ctx.Gen.Add(CilOpCodes.Ldc_I4_S, (short)intlit.Value);
                    //     ctx.Gen.Add(CilOpCodes.Conv_I2);
                    //     ctx.StackPush(signed ? _corLibFactory.Int16 : _corLibFactory.UInt16);
                    //     break;
                    case <= 32:
                        ctx.Gen.Add(CilInstruction.CreateLdcI4((int)intlit.Value));
                        ctx.StackPush(signed ? _corLibFactory.Int32 : _corLibFactory.UInt32);
                        break;
                    case <= 64:
                        ctx.Gen.Add(CilOpCodes.Ldc_I8, unchecked((long)(Int128)intlit.Value));
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
                            ctx.Gen.Add(CilOpCodes.Initobj, (ITypeDefOrRef)largeType.t.ToTypeDefOrRef());
                            ctx.Gen.Add(CilOpCodes.Ldloc, tmp);
                        }
                        else if (intlit.Value <= ulong.MaxValue)
                        {
                            ctx.Gen.Add(CilOpCodes.Ldc_I8, (long)intlit.Value);
                            ctx.Gen.Add(CilOpCodes.Call, (IMethodDescriptor)largeType.m[intlit.Value.Sign < 0 ? "Conv_from_i64" : "Conv_from_u64"]);
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
                            ctx.Gen.Add(CilOpCodes.Call, (IMethodDescriptor)largeType.m["new"]);
                            ctx.Gen.Add(CilOpCodes.Ldloc, tmp);
                        }
                        ctx.StackPush(largeType.t);
                    } break;
                    default: throw new UnreachableException();
                }
                } break;
            case IRBooleanLiteral @boollit:
                if (ignoreValue) return;
                ctx.Gen.Add(boollit.Value ? CilOpCodes.Ldc_I4_1 : CilOpCodes.Ldc_I4_0);
                ctx.StackPush(_corLibFactory.Boolean);
                break;
            case IrCharLiteral @charlit:
                if (ignoreValue) return;
                ctx.Gen.Add(CilOpCodes.Ldc_I4_S, (short)charlit.Data);
                ctx.StackPush(_corLibFactory.Char);
                break;
            case IrStringLiteral @strlit:
                if (ignoreValue) return;
                ctx.Gen.Add(CilOpCodes.Ldstr, strlit.Data);
                ctx.StackPush(_corLibFactory.String);
                break;
            case IrCollectionLiteral @collit:
            {
                if (ignoreValue) return;
                var elmtype = TypeFromRef(collit.ElementType);
                
                ctx.Gen.Add(CilInstruction.CreateLdcI4(collit.Length));
                ctx.Gen.Add(CilOpCodes.Newarr, elmtype.ToTypeDefOrRef());
                ctx.StackPush(TypeFromRef(collit.Type));

                var index = 0;
                foreach (var i in collit.Items)
                {
                    ctx.Gen.Add(CilOpCodes.Dup);
                    ctx.Gen.Add(CilInstruction.CreateLdcI4(index++));
                    CompileIrNodeLoad(i, false, ctx);
                    switch (elmtype)
                    {
                        case CorLibTypeSignature @clt:
                        {
                            switch (clt.ElementType)
                            {
                                case ElementType.I or ElementType.U: ctx.Gen.Add(CilOpCodes.Stelem_I); break;
                                case ElementType.I1 or ElementType.U1: ctx.Gen.Add(CilOpCodes.Stelem_I1); break;
                                case ElementType.I2 or ElementType.U2: ctx.Gen.Add(CilOpCodes.Stelem_I2); break;
                                case ElementType.I4 or ElementType.U4: ctx.Gen.Add(CilOpCodes.Stelem_I4); break;
                                case ElementType.I8 or ElementType.U8: ctx.Gen.Add(CilOpCodes.Conv_I8); break;
                                case ElementType.R4: ctx.Gen.Add(CilOpCodes.Stelem_R4); break;
                                case ElementType.R8: ctx.Gen.Add(CilOpCodes.Stelem_R8); break;
                                default: ctx.Gen.Add(CilOpCodes.Stelem, elmtype.ToTypeDefOrRef()); break;
                            }
                        } break;
                        default: ctx.Gen.Add(CilOpCodes.Stelem, elmtype.ToTypeDefOrRef()); break;
                    }
                    ctx.StackPop();
                }
            } break;
            
            case IrSolvedReference @solv:
            {
                if (ignoreValue) return;
                switch (solv.Reference)
                {
                    case LocalReference @lr:
                        ctx.Gen.Add(CilOpCodes.Ldloc, ctx.GetLoc(lr.Local.index));
                        ctx.StackPush(ctx.GetLoc(lr.Local.index).VariableType);
                        break;
    
                    case ParameterReference @pr:
                        if (pr.Parameter.IsGeneric)
                        {
                            var genericParam = ctx.Body.Owner.GenericParameters[pr.Parameter.Index];
                            var typeSig = new GenericParameterSignature(GenericParameterType.Method, genericParam.Number);
                            ctx.Gen.Add(CilOpCodes.Ldtoken, ctx.Importer.ImportTypeSignature(typeSig).ToTypeDefOrRef());
                            ctx.Gen.Add(CilOpCodes.Call, _coreLib["Type"].m["GetTypeFromHandle"]);
                            ctx.StackPush(typeSig);
                        }
                        else
                        {
                            ctx.Gen.Add(CilOpCodes.Ldarg, ctx.GetArg(pr.Parameter.Index));
                            ctx.StackPush(ctx.GetArg(pr.Parameter.Index).ParameterType);
                        }
                        break;

                    case SolvedFieldReference @fr:
                    {
                        var field = _fieldsMap[fr.Field];

                        if (field.Constant != null)
                        {
                            CompileIrFieldConstant(field.Constant, ctx);
                            return;
                        }
                        
                        if (!field.IsStatic && !ctx.Stack[^1].IsAssignableTo(field.DeclaringType!.ToTypeSignature()))
                            ctx.Gen.Add(CilOpCodes.Conv_U);
                        
                        ctx.Gen.Add((CilOpCode)(field.IsStatic ? CilOpCodes.Ldsfld : CilOpCodes.Ldfld), (IFieldDescriptor)field);
                        if (!field.IsStatic) ctx.StackPop(); 
                        ctx.StackPush(_fieldsMap[fr.Field].Signature!.FieldType);
                    } break;

                    case SolvedTypedefNamedValueReference @tn:
                    {
                        var item = tn.NamedValue;
                        var typedef = (TypedefObject)item.Parent;
                        
                        var enumdata = _enumsMap[typedef];
                        var enumfield = enumdata.GetItem(item);

                        CompileIrFieldConstant(enumfield.Resolve()!.Constant!, ctx);
                    } break;
                    
                    default: throw new UnreachableException();
                }
            } break;
    
            case IRAccess @acc:
            {
                if (ignoreValue) return;
                CompileIrNodeLoadAsRef(acc.A, ctx);
                CompileIrNodeLoadAsRef(acc.B, ctx);
                
                var t = TypeFromRef(acc.Type);
                ctx.Gen.Add(CilOpCodes.Ldobj, t.ToTypeDefOrRef());
                ctx.StackPush(t);
            } break;
    
            case IrInvoke @iv: CompileIrNodeCall(iv.Target, iv.Arguments, ignoreValue, ctx); break;
            
            case IrConv @c:
            {
                if (ignoreValue) return;
                var fromType = c.OriginType;
                var toType = c.Type;

                switch (toType)
                {
                    case StringTypeReference:
                    {
                        switch (fromType)
                        {
                            case SliceTypeReference @slice:
                            {
                                var stringBuilder = _coreLib["System.Text.StringBuilder"];
                                var elmType = TypeFromRef(slice.ElementType);
                                
                                var tmpSb = ctx.AllocTmp(stringBuilder.t);
                                var tmpI = ctx.AllocTmp(_corLibFactory.Int32);
                                var tmpLen = ctx.AllocTmp(_corLibFactory.Int32);
                                
                                var sb_new = stringBuilder.m["new"];
                                var sb_append_c = stringBuilder.m["Append_char"];
                                var sb_append_s = stringBuilder.m["Append_str"];
                                var sb_tostr = stringBuilder.m["ToString"];
                                
                                var loopLbl = new CilInstructionLabel();
                                var checkLbl = new CilInstructionLabel();
                                var skipLbl = new CilInstructionLabel();
                                
                                ctx.Gen.Add(CilOpCodes.Newobj, sb_new);
                                ctx.Gen.Add(CilOpCodes.Dup);
                                ctx.Gen.Add(CilOpCodes.Stloc, tmpSb);
                                
                                ctx.Gen.Add(CilOpCodes.Ldc_I4_S, (byte)'[');
                                ctx.Gen.Add(CilOpCodes.Call, sb_append_c);
                                ctx.Gen.Add(CilOpCodes.Pop);
                                
                                ctx.Gen.Add(CilOpCodes.Ldc_I4_0);
                                ctx.Gen.Add(CilOpCodes.Stloc, tmpI);
                                
                                CompileIrNodeLoad(c.Expression, false, ctx);
                                ctx.StackPop();
                                ctx.Gen.Add(CilOpCodes.Ldlen);
                                ctx.Gen.Add(CilOpCodes.Dup);
                                ctx.Gen.Add(CilOpCodes.Stloc, tmpLen);
                                ctx.Gen.Add(CilOpCodes.Brfalse, skipLbl);
                                
                                ctx.Gen.Add(CilOpCodes.Br, checkLbl);
                                // loop body
                                {
                                    loopLbl.Instruction = ctx.Gen.Add(CilOpCodes.Ldloc, tmpSb);
                                    CompileIrNodeLoad(c.Expression, false, ctx);
                                    ctx.StackPop();
                                    ctx.Gen.Add(CilOpCodes.Ldloc, tmpI);
                                    ctx.Gen.Add(CilOpCodes.Ldelem, elmType.ToTypeDefOrRef());
                                    ctx.Gen.Add(CilOpCodes.Box, elmType.ToTypeDefOrRef());
                                    ctx.Gen.Add(CilOpCodes.Callvirt, _coreLib["System.Object"].m["ToString"]);
                                    ctx.Gen.Add(CilOpCodes.Call, sb_append_s);
                                    
                                    ctx.Gen.Add(CilOpCodes.Ldstr, ", ");
                                    ctx.Gen.Add(CilOpCodes.Call, sb_append_s);
                                    ctx.Gen.Add(CilOpCodes.Pop);
                                    
                                    ctx.Gen.Add(CilOpCodes.Ldloc, tmpI);
                                    ctx.Gen.Add(CilOpCodes.Ldc_I4_1);
                                    ctx.Gen.Add(CilOpCodes.Add);
                                    ctx.Gen.Add(CilOpCodes.Stloc, tmpI);
                                }
                                
                                // loop check
                                {
                                    checkLbl.Instruction = ctx.Gen.Add(CilOpCodes.Ldloc, tmpI);
                                    ctx.Gen.Add(CilOpCodes.Ldloc, tmpLen);
                                    ctx.Gen.Add(CilOpCodes.Blt, loopLbl);
                                }
                                
                                ctx.Gen.Add(CilOpCodes.Ldloc, tmpSb);
                                //ctx.Gen.Add(CilOpCodes.Dup);
                                ctx.Gen.Add(CilOpCodes.Dup);
                                ctx.Gen.Add(CilOpCodes.Call, stringBuilder.m["get_Len"]);
                                ctx.Gen.Add(CilOpCodes.Ldc_I4_2);
                                ctx.Gen.Add(CilOpCodes.Sub);
                                ctx.Gen.Add(CilOpCodes.Call, stringBuilder.m["set_Len"]);
                                
                                skipLbl.Instruction = ctx.Gen.Add(CilOpCodes.Ldloc, tmpSb);
                                ctx.Gen.Add(CilOpCodes.Ldc_I4_S, (byte)']');
                                ctx.Gen.Add(CilOpCodes.Call, sb_append_c);
                                
                                ctx.Gen.Add(CilOpCodes.Call, sb_tostr);
                                ctx.StackPush(_corLibFactory.String);

                                ctx.FreeTmp(stringBuilder.t, tmpSb);
                                ctx.FreeTmp(_corLibFactory.Int32, tmpI);
                                ctx.FreeTmp(_corLibFactory.Int32, tmpLen);
                                
                            } break;
                            default:
                            {
                                var baseTypeRef = TypeFromRef(fromType);
                                CompileIrNodeLoad(c.Expression, false, ctx);
                                ctx.StackPop();
                                ctx.Gen.Add(CilOpCodes.Box, baseTypeRef.ToTypeDefOrRef());
                                ctx.Gen.Add(CilOpCodes.Callvirt, _coreLib["System.Object"].m["ToString"]);
                                ctx.StackPush(_corLibFactory.String);
                            } break;
                        }
                    } break;

                    case RuntimeIntegerTypeReference @targt:
                    {
                        CompileIrNodeLoad(c.Expression, false, ctx);
                        
                        switch (c.Expression.Type)
                        {
                            case RuntimeIntegerTypeReference @srt:
                            {
                                var srs = srt.Signed;
                                var srbitsize = srt.BitSize.Bits;
                                
                                var s = targt.Signed;
                                var bitsize = targt.BitSize.Bits;

                                ctx.StackPop();
                                if (srbitsize == 128)
                                {
                                    var baset = srs ? _coreLib["Int128"] : _coreLib["UInt128"];
                                    switch (bitsize)
                                    {
                                        case <= 8:
                                            ctx.Gen.Add(CilOpCodes.Call, (IMethodDescriptor)baset.m[s ? "Conv_to_i8" : "Conv_to_u8"]);
                                            ctx.StackPush(s ? _corLibFactory.SByte :  _corLibFactory.Byte);
                                            break;
                                        case <= 16:
                                            ctx.Gen.Add(CilOpCodes.Call, (IMethodDescriptor)baset.m[s ? "Conv_to_i16" : "Conv_to_u16"]);
                                            ctx.StackPush(s ? _corLibFactory.Int16 : _corLibFactory.UInt16);
                                            break;
                                        case <= 32:
                                            ctx.Gen.Add(CilOpCodes.Call, (IMethodDescriptor)baset.m[s ? "Conv_to_i32" : "Conv_to_u32"]);
                                            ctx.StackPush(s ? _corLibFactory.Int32 : _corLibFactory.UInt32);
                                            break;
                                        case <= 64:
                                            ctx.Gen.Add(CilOpCodes.Call, (IMethodDescriptor)baset.m[s ? "Conv_to_i64" : "Conv_to_u64"]);
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
                                            case <= 8:ctx.Gen.Add(CilOpCodes.Call, (IMethodDescriptor)baset.m[s ? "Conv_from_i8" : "Conv_from_u8"]); break;
                                            case <= 16: ctx.Gen.Add(CilOpCodes.Call, (IMethodDescriptor)baset.m[s ? "Conv_from_i16" : "Conv_from_u16"]); break;
                                            case <= 32: ctx.Gen.Add(CilOpCodes.Call, (IMethodDescriptor)baset.m[s ? "Conv_from_i32" : "Conv_from_u32"]); break;
                                            case <= 64: ctx.Gen.Add(CilOpCodes.Call, (IMethodDescriptor)baset.m[s ? "Conv_from_i64" : "Conv_from_u64"]); break;
                                            default: throw new UnreachableException();
                                        }

                                        ctx.StackPush(baset.t);
                                        break;
                                    }

                                    default: throw new UnreachableException();
                                }
                            } break;

                            case CharTypeReference:
                            {
                                var s = targt.Signed;
                                var bitsize = targt.BitSize.Bits;
                                
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
                                        var baset = _coreLib["UInt128"];
                                        ctx.Gen.Add(CilOpCodes.Call, baset.m[s ? "Conv_from_i32" : "Conv_from_u32"]);
                                        ctx.StackPush(baset.t);
                                        break;
                                    }

                                    default: throw new UnreachableException();
                                }
                            } break;
                            
                            case SolvedTypedefTypeReference:
                            {
                                var fromTypeSig = ctx.Stack[^1];
                                if (fromTypeSig is not CorLibTypeSignature @corlibsig
                                    || !IsExplicitInteger(corlibsig, out var srs, out var srbitsize))
                                    throw new UnreachableException();
                                
                                ctx.StackPop();

                                if (srs == targt.Signed && srbitsize == targt.BitSize) goto forceEnd;
                                switch (targt.BitSize.Bits)
                                {
                                    case <= 8:
                                        ctx.Gen.Add(srs ? CilOpCodes.Conv_I1 : CilOpCodes.Conv_U1);
                                        ctx.StackPush(srs ? _corLibFactory.SByte : _corLibFactory.Byte);
                                        break;
                                    case <= 16:
                                        ctx.Gen.Add(srs ? CilOpCodes.Conv_I2 : CilOpCodes.Conv_U2);
                                        ctx.StackPush(srs ? _corLibFactory.Int16 : _corLibFactory.UInt16);
                                        break;
                                    case <= 32:
                                        ctx.Gen.Add(srs ? CilOpCodes.Conv_I4 : CilOpCodes.Conv_U4);
                                        ctx.StackPush(srs ? _corLibFactory.Int32 : _corLibFactory.UInt32);
                                        break;
                                    case <= 64:
                                        ctx.Gen.Add(srs ? CilOpCodes.Conv_I8 : CilOpCodes.Conv_U8);
                                        ctx.StackPush(srs ? _corLibFactory.Int64 : _corLibFactory.UInt64);
                                        break;
                                    case <= 128:
                                    {
                                        var baseT = srs ? _coreLib["Int128"] : _coreLib["UInt128"];
                                        switch (srbitsize)
                                        {
                                            case <= 8:ctx.Gen.Add(CilOpCodes.Call, (IMethodDescriptor)baseT.m[srs ? "Conv_from_i8" : "Conv_from_u8"]); break;
                                            case <= 16: ctx.Gen.Add(CilOpCodes.Call, (IMethodDescriptor)baseT.m[srs ? "Conv_from_i16" : "Conv_from_u16"]); break;
                                            case <= 32: ctx.Gen.Add(CilOpCodes.Call, (IMethodDescriptor)baseT.m[srs ? "Conv_from_i32" : "Conv_from_u32"]); break;
                                            case <= 64: ctx.Gen.Add(CilOpCodes.Call, (IMethodDescriptor)baseT.m[srs ? "Conv_from_i64" : "Conv_from_u64"]); break;
                                            default: throw new UnreachableException();
                                        }
                                        ctx.StackPush(baseT.t);
                                        break;
                                    }
                                }
                                
                                forceEnd:
                                ctx.StackPush(TypeFromRef(c.Type));
                                
                            } break;
                            
                            default: throw new NotImplementedException();
                        }
                        
                    } break;
                    
                    case GenericTypeReference @generic:
                    {
                        CompileIrNodeLoad(c.Expression, false, ctx);
                        var genericParam = ctx.Body.Owner.GenericParameters[generic.Parameter.Index];
                        var typeSig = new GenericParameterSignature(GenericParameterType.Method, genericParam.Number);
                        ctx.Gen.Add(CilOpCodes.Unbox_Any, ctx.Importer.ImportTypeSignature(typeSig).ToTypeDefOrRef());
                        ctx.StackPush(typeSig);
                    } break;

                    case DotnetTypeReference @dotnet:
                    {
                        
                    } break;

                    case DotnetGenericTypeReference @dotnetGeneric:
                    {
                        switch (dotnetGeneric.Reference.Reference.FullName)
                        {
                            case "System.Span`1" when fromType is SliceTypeReference @sliceTypeRef:
                            {
                                var generic0 = TypeFromRef(dotnetGeneric.GenericArguments[0]);
                                var spanTypeGeneric = _coreLib["System.Span`1"];
                                var spanTypeInstance = new GenericInstanceTypeSignature(spanTypeGeneric.t.ToTypeDefOrRef(), true, generic0);
                                var spanTypeSig = ctx.Importer.ImportTypeSignature(spanTypeInstance);
                                
                                var methodGeneric = _coreLib["System.MemoryExtensions"].m["AsSpan_i32"];
                                var methodInstance = new GenericInstanceMethodSignature(generic0);
                                var methodSpec = new MethodSpecification((IMethodDefOrRef)methodGeneric, methodInstance);

                                CompileIrNodeLoad(c.Expression, false, ctx);
                                ctx.StackPop();
                                ctx.Gen.Add(CilOpCodes.Ldc_I4_0);
                                ctx.Gen.Add(CilOpCodes.Call, methodSpec);
                                
                                ctx.Stack.Add(spanTypeSig);

                            } break;
                            default: throw new UnreachableException();
                        }
                    } break;
                    
                    default: throw new UnreachableException();
                }
            } break;
            
            case IrRef @r: CompileIrNodeLoadAsRef(r.Expression, ctx); break;
            
            case IRUnaryExp @ue:
            {
                RuntimeIntegerTypeReference it;
                var bs = 0;
                
                switch (ue.Operation)
                {
                    case IRUnaryExp.UnaryOperation.BitwiseNot:
                    {
                        if (ignoreValue) return;
                        var isSigned = ((RuntimeIntegerTypeReference)ue.Type!).Signed;
                        var is128 = ((RuntimeIntegerTypeReference)ue.Type!).BitSize == 128;
                        
                        CompileIrNodeLoad(ue.Value, false, ctx);
                        if (is128) ctx.Gen.Add(CilOpCodes.Call, _coreLib[isSigned ? "Int128" : "UInt128"].m["BitwiseNot"]);
                        else ctx.Gen.Add(CilOpCodes.Not); ;
                    } break;

                    case IRUnaryExp.UnaryOperation.PreIncrement:
                        CompileIrNodeLoad(ue.Value, false, ctx);
                        
                        it = (RuntimeIntegerTypeReference)ue.Value.Type;
                        bs = it.BitSize.Bits;
                        switch (bs)
                        {
                            case <= 32: ctx.Gen.Add(CilInstruction.CreateLdcI4(1)); break;
                            case <= 64: ctx.Gen.Add(CilOpCodes.Ldc_I8, (long)1); break;
                            default: throw new UnreachableException();
                        }
                        
                        ctx.Gen.Add(CilOpCodes.Add);
                        if (!ignoreValue) ctx.Gen.Add(CilOpCodes.Dup);
                        CompileIrNodeStore(ue.Value, null, ctx);
                        ctx.Stack.Add(TypeFromRef(ue.Type));
                        break;
                    
                    case IRUnaryExp.UnaryOperation.PostIncrement:
                        CompileIrNodeLoad(ue.Value, false, ctx);
                        if (!ignoreValue) ctx.Gen.Add(CilOpCodes.Dup);
                        
                        it = (RuntimeIntegerTypeReference)ue.Value.Type;
                        bs = it.BitSize.Bits;
                        switch (bs)
                        {
                            case <= 32: ctx.Gen.Add(CilInstruction.CreateLdcI4(1)); break;
                            case <= 64: ctx.Gen.Add(CilOpCodes.Ldc_I8, (long)1); break;
                            default: throw new UnreachableException();
                        }
                        
                        ctx.Gen.Add(CilOpCodes.Add);
                        CompileIrNodeStore(ue.Value, null, ctx);
                        ctx.Stack.Add(TypeFromRef(ue.Type));
                        break;
                    
                    case IRUnaryExp.UnaryOperation.Not:
                        if (ignoreValue) return;
                        CompileIrNodeLoad(ue.Value, false, ctx);
                        ctx.Gen.Add(CilOpCodes.Ldc_I4_0);
                        ctx.Gen.Add(CilOpCodes.Ceq);
                        break;
                    
                    default: throw new ArgumentOutOfRangeException();
                }
            } break;
            case IrBinaryExp @bin:
            {
                if (ignoreValue) return;
                CompileIrNodeLoad(bin.Left, false, ctx);
                CompileIrNodeLoad(bin.Right, false, ctx);
                
                switch (bin.Left.Type)
                {
                    case RuntimeIntegerTypeReference @originType:
                    {
                        var isSigned = originType.Signed;
                        var is128 = originType.BitSize == 128;

                        switch (bin.Operator)
                        {
                            case IrBinaryExp.Operators.Add:
                                if (is128) ctx.Gen.Add(CilOpCodes.Call, (IMethodDescriptor)_coreLib[isSigned ? "Int128" : "UInt128"].m["Add"]);
                                else ctx.Gen.Add(CilOpCodes.Add);
                                break;

                            case IrBinaryExp.Operators.AddWarpAround:
                                if (is128) ctx.Gen.Add(CilOpCodes.Call, (IMethodDescriptor)_coreLib[isSigned ? "Int128" : "UInt128"].m["AddOvf"]);
                                else ctx.Gen.Add(isSigned ? CilOpCodes.Add_Ovf : CilOpCodes.Add_Ovf_Un);
                                break;


                            case IrBinaryExp.Operators.Subtract:
                                if (is128) ctx.Gen.Add(CilOpCodes.Call, (IMethodDescriptor)_coreLib[isSigned ? "Int128" : "UInt128"].m["Sub"]);
                                else ctx.Gen.Add(CilOpCodes.Sub);
                                break;

                            case IrBinaryExp.Operators.SubtractWarpAround:
                                if (is128) ctx.Gen.Add(CilOpCodes.Call, (IMethodDescriptor)_coreLib[isSigned ? "Int128" : "UInt128"].m["SubOvf"]);
                                else ctx.Gen.Add(isSigned ? CilOpCodes.Sub_Ovf : CilOpCodes.Sub_Ovf_Un);
                                break;

                            case IrBinaryExp.Operators.Multiply:
                                if (is128) ctx.Gen.Add(CilOpCodes.Call, (IMethodDescriptor)_coreLib[isSigned ? "Int128" : "UInt128"].m["Mul"]);
                                else ctx.Gen.Add(isSigned ? CilOpCodes.Mul_Ovf : CilOpCodes.Mul_Ovf_Un);
                                break;

                            case IrBinaryExp.Operators.Divide:
                                if (is128) ctx.Gen.Add(CilOpCodes.Call, (IMethodDescriptor)_coreLib[isSigned ? "Int128" : "UInt128"].m["Div"]);
                                else ctx.Gen.Add(isSigned ? CilOpCodes.Div : CilOpCodes.Div_Un);
                                break;

                            case IrBinaryExp.Operators.Reminder:
                                if (is128) ctx.Gen.Add(CilOpCodes.Call, (IMethodDescriptor)_coreLib[isSigned ? "Int128" : "UInt128"].m["Rem"]);
                                else ctx.Gen.Add(isSigned ? CilOpCodes.Rem : CilOpCodes.Rem_Un);
                                break;

                            case IrBinaryExp.Operators.BitwiseAnd:
                                if (is128)
                                    ctx.Gen.Add(CilOpCodes.Call, (IMethodDescriptor)_coreLib[isSigned ? "Int128" : "UInt128"].m["BitwiseAnd"]);
                                else ctx.Gen.Add(CilOpCodes.And);
                                break;

                            case IrBinaryExp.Operators.BitwiseOr:
                                if (is128)
                                    ctx.Gen.Add(CilOpCodes.Call, (IMethodDescriptor)_coreLib[isSigned ? "Int128" : "UInt128"].m["BitwiseOr"]);
                                else ctx.Gen.Add(CilOpCodes.Or);
                                break;

                            case IrBinaryExp.Operators.BitwiseXor:
                                if (is128)
                                    ctx.Gen.Add(CilOpCodes.Call, _coreLib[isSigned ? "Int128" : "UInt128"].m["BitwiseXor"]);
                                else ctx.Gen.Add(CilOpCodes.Xor);
                                break;

                            case IrBinaryExp.Operators.LeftShift:
                                if (is128)
                                    ctx.Gen.Add(CilOpCodes.Call, _coreLib[isSigned ? "Int128" : "UInt128"].m["LeftShift"]);
                                else ctx.Gen.Add(CilOpCodes.Shl);
                                break;

                            case IrBinaryExp.Operators.RightShift:
                                if (is128)
                                    ctx.Gen.Add(CilOpCodes.Call, (IMethodDescriptor)_coreLib[isSigned ? "Int128" : "UInt128"].m["RightShift"]);
                                else ctx.Gen.Add(CilOpCodes.Shr_Un);
                                break;
                            
                            case IrBinaryExp.Operators.AddOnBounds:
                            case IrBinaryExp.Operators.SubtractOnBounds:
                            case IrBinaryExp.Operators.DivideFloor:
                            case IrBinaryExp.Operators.DivideCeil:
                            default: throw new ArgumentOutOfRangeException();
                        }
                        break;
                    }
                    
                    case StringTypeReference when bin.Right.Type is StringTypeReference:
                        ctx.Gen.Add(CilOpCodes.Call, _coreLib["System.String"].m["Concat_s0_s1"]);
                        break;
                    
                    default: throw new UnreachableException();
                }
                
                ctx.StackPop(2);
                ctx.StackPush(TypeFromRef(bin.Type));
            } break;
            case IrCompareExp @cmp:
            {
                if (ignoreValue) return;
                
                switch (cmp.Left.Type)
                {
                    case RuntimeIntegerTypeReference @originType:
                    {
                        CompileIrNodeLoad(cmp.Left, false, ctx);
                        CompileIrNodeLoad(cmp.Right, false, ctx);
                        
                        var isSigned = originType.Signed;
                        var is128 = originType.BitSize == 128;

                        switch (cmp.Operator)
                        {

                            case IrCompareExp.Operators.Equality:
                                ctx.Gen.Add(CilOpCodes.Ceq);
                                break;
                        
                            case IrCompareExp.Operators.Inequality:
                                ctx.Gen.Add(CilOpCodes.Ceq);
                                ctx.Gen.Add(CilOpCodes.Ldc_I4_0);
                                ctx.Gen.Add(CilOpCodes.Ceq);
                                break;
                        
                            case IrCompareExp.Operators.LessThan: ctx.Gen.Add(isSigned ? CilOpCodes.Clt : CilOpCodes.Clt_Un); break;
                            case IrCompareExp.Operators.GreaterThan: ctx.Gen.Add(isSigned ? CilOpCodes.Cgt : CilOpCodes.Cgt_Un); break;
                            
                            case IrCompareExp.Operators.LessThanOrEqual:
                                ctx.Gen.Add(isSigned ? CilOpCodes.Cgt : CilOpCodes.Cgt_Un);
                                ctx.Gen.Add(CilOpCodes.Ldc_I4_0);
                                ctx.Gen.Add(CilOpCodes.Ceq);
                                break;
                            case IrCompareExp.Operators.GreaterThanOrEqual:
                                ctx.Gen.Add(isSigned ? CilOpCodes.Clt : CilOpCodes.Clt_Un);
                                ctx.Gen.Add(CilOpCodes.Ldc_I4_0);
                                ctx.Gen.Add(CilOpCodes.Ceq);
                                break;
                            
                            default: throw new ArgumentOutOfRangeException();
                        }
                        
                        ctx.StackPop(2);
                    } break;

                    case BooleanTypeReference:
                    {
                        CompileIrNodeLoad(cmp.Left, false, ctx);
                        CompileIrNodeLoad(cmp.Right, false, ctx);

                        switch (cmp.Operator)
                        {
                            case IrCompareExp.Operators.Equality:
                                ctx.Gen.Add(CilOpCodes.Ceq);
                                break;

                            case IrCompareExp.Operators.Inequality:
                                ctx.Gen.Add(CilOpCodes.Ceq);
                                ctx.Gen.Add(CilOpCodes.Ldc_I4_1);
                                ctx.Gen.Add(CilOpCodes.Xor);
                                break;

                            default: throw new ArgumentOutOfRangeException();
                        }
                        
                        ctx.StackPop(2);
                    } break;

                    case CharTypeReference:
                    {
                        CompileIrNodeLoad(cmp.Left, false, ctx);
                        CompileIrNodeLoad(cmp.Right, false, ctx);
                        
                        switch (cmp.Operator)
                        {
                            case IrCompareExp.Operators.Equality:
                                ctx.Gen.Add(CilOpCodes.Ceq);
                                break;

                            case IrCompareExp.Operators.Inequality:
                                ctx.Gen.Add(CilOpCodes.Ceq);
                                ctx.Gen.Add(CilOpCodes.Ldc_I4_0);
                                ctx.Gen.Add(CilOpCodes.Ceq);
                                break;
                            
                            case IrCompareExp.Operators.LessThan:
                                ctx.Gen.Add(CilOpCodes.Clt_Un);
                                break;
                            
                            case IrCompareExp.Operators.LessThanOrEqual:
                                ctx.Gen.Add(CilOpCodes.Cgt_Un);
                                ctx.Gen.Add(CilOpCodes.Ldc_I4_0);
                                ctx.Gen.Add(CilOpCodes.Ceq);
                                break;
                            
                            case IrCompareExp.Operators.GreaterThan:
                                ctx.Gen.Add(CilOpCodes.Cgt_Un);
                                break;
                            
                            case IrCompareExp.Operators.GreaterThanOrEqual:
                                ctx.Gen.Add(CilOpCodes.Clt_Un);
                                ctx.Gen.Add(CilOpCodes.Ldc_I4_0);
                                ctx.Gen.Add(CilOpCodes.Ceq);
                                break;
                            
                            default: throw new ArgumentOutOfRangeException();
                        }
                        
                        ctx.StackPop(2);
                    } break;

                    case StringTypeReference:
                    {
                        CompileIrNodeLoad(cmp.Left, false, ctx);
                        CompileIrNodeLoad(cmp.Right, false, ctx);
                        
                        switch (cmp.Operator)
                        {
                            case IrCompareExp.Operators.Equality:
                                ctx.Gen.Add(CilOpCodes.Call, _coreLib["String"].m["Equals"]);
                                break;

                            case IrCompareExp.Operators.Inequality:
                                ctx.Gen.Add(CilOpCodes.Call, (IMethodDescriptor)_coreLib["String"].m["Equals"]);
                                ctx.Gen.Add(CilOpCodes.Ldc_I4_0);
                                ctx.Gen.Add(CilOpCodes.Ceq);
                                break;

                            default: throw new ArgumentOutOfRangeException();
                        }
                        
                        ctx.StackPop(2);
                    } break;

                    case AnytypeTypeReference @anytype:
                    {
                        CompileIrNodeLoad(cmp.Left, false, ctx);
                        CompileIrNodeLoad(cmp.Right, false, ctx);
                        ctx.Gen.Add(CilOpCodes.Box, TypeFromRef(anytype).ToTypeDefOrRef());

                        var equals = _coreLib["System.Object"].m["Equals"];
                        ctx.Gen.Add(CilOpCodes.Callvirt, equals);
                        
                        ctx.StackPop(2);
                    } break;
                }
                ctx.StackPush(_corLibFactory.Boolean);
            } break;
            case IrLogicalExp @log:
            {
                var shortcutMode = false;
                CilInstructionLabel shortcutIfTrueLabel = null!;
                CilInstructionLabel shortcutIfFalseLabel = null!;
                
                if (ctx.GetFrame() is ConditionalExpressionFrame @cef)
                    (shortcutMode, shortcutIfTrueLabel, shortcutIfFalseLabel) = (true, cef.IfTrue, cef.IfFalse);
                else if (ctx.GetFrame() is LoopCheckFrame @lcf)
                    (shortcutMode, shortcutIfTrueLabel, shortcutIfFalseLabel) = (true, lcf.Execute, lcf.Break);
                else if (ctx.GetFrame() is ShortcutFrame @scf)
                    (shortcutMode, shortcutIfTrueLabel, shortcutIfFalseLabel) = (true, scf.IfTrue, scf.IfFalse);
                
                if (shortcutMode)
                {
                    switch (log.Operator)
                    {
                        case IrLogicalExp.Operators.And:
                        {
                            var labelRightSide = new CilInstructionLabel(); 
                            
                            ctx.FramePush(new ShortcutFrame(labelRightSide, shortcutIfFalseLabel));
                            CompileIrNodeLoad(log.Left, false, ctx);
                            ctx.FramePop();
                            
                            if (log.Left is not IrLogicalExp)
                            {
                                ctx.Gen.Add(CilOpCodes.Brfalse, shortcutIfFalseLabel);
                                ctx.StackPop();
                            }

                            labelRightSide.Instruction = ctx.Gen.Add(CilOpCodes.Nop);
                            
                            ctx.FramePush(new ShortcutFrame(shortcutIfTrueLabel, shortcutIfFalseLabel));
                            CompileIrNodeLoad(log.Right, false, ctx);
                            ctx.FramePop();

                            if (log.Right is not IrLogicalExp)
                            {
                                ctx.Gen.Add(CilOpCodes.Brfalse, shortcutIfFalseLabel);
                                ctx.Gen.Add(CilOpCodes.Br, shortcutIfTrueLabel); 
                                ctx.StackPop();
                            }
                        } break;

                        case IrLogicalExp.Operators.Or:
                        {
                            // No OR: Se a esquerda é verdadeira -> pula pro True Global.
                            // Se a esquerda é falsa -> avalia a direita.
                            var labelRightSide = new CilInstructionLabel();

                            ctx.FramePush(new ShortcutFrame(shortcutIfTrueLabel, labelRightSide));
                            CompileIrNodeLoad(log.Left, false, ctx);
                            ctx.FramePop();

                            if (log.Left is not IrLogicalExp)
                            {
                                ctx.Gen.Add(CilOpCodes.Brtrue, shortcutIfTrueLabel);
                                ctx.StackPop();
                            }

                            labelRightSide.Instruction = ctx.Gen.Add(CilOpCodes.Nop);

                            ctx.FramePush(new ShortcutFrame(shortcutIfTrueLabel, shortcutIfFalseLabel));
                            CompileIrNodeLoad(log.Right, false, ctx);
                            ctx.FramePop();

                            if (log.Right is not IrLogicalExp)
                            {
                                ctx.Gen.Add(CilOpCodes.Brtrue, shortcutIfTrueLabel);
                                ctx.Gen.Add(CilOpCodes.Br, shortcutIfFalseLabel);
                                ctx.StackPop();
                            }
                        } break;

                        default: throw new ArgumentOutOfRangeException();
                    }
                }
                else
                {
                    if (ignoreValue) return;
                    CompileIrNodeLoad(log.Left, false, ctx);
                    CompileIrNodeLoad(log.Right, false, ctx);

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
                CompileIrNodeLoad(idx.Value, false, ctx);
                CompileIrNodeLoad(idx.Indices[0], false, ctx);
                
                if (ctx.Stack[^2] is SzArrayTypeSignature)
                {
                    if (ctx.PeekStack() is CorLibTypeSignature @clts && IsExplicitInteger(clts, out var sig, out var len))
                        if (len > 4) ctx.Gen.Add(CilOpCodes.Conv_I4);
                    ctx.Gen.Add(CilOpCodes.Ldelem, elmtype.ToTypeDefOrRef());
                }
                else if (ctx.Stack[^2] == _corLibFactory.String)
                {
                    if (ctx.PeekStack() is CorLibTypeSignature @clts && IsExplicitInteger(clts, out var sig, out var len))
                        if (len > 4) ctx.Gen.Add(CilOpCodes.Conv_I4);
                    ctx.Gen.Add(CilOpCodes.Call, _coreLib["String"].m["charAt"]);
                }
                else throw new NotImplementedException();
                
                ctx.StackPop(2);
                ctx.StackPush(elmtype);
            } break;
            case IrLenOf lenOf:
            {
                CompileIrNodeLoad(lenOf.OfValue, false, ctx);
                ctx.Gen.Add(CilOpCodes.Ldlen);
                ctx.Gen.Add(CilOpCodes.Conv_U8);
                ctx.Stack[^1] = _corLibFactory.UInt64;
            } break;
            
            case IRIf @if:
            {
                var thisConditionLabel = new CilInstructionLabel();
                var nextConditionLabel = new CilInstructionLabel();
                var breakLabel = new CilInstructionLabel();
                
                ctx.FramePush(new ConditionalExpressionFrame(thisConditionLabel, nextConditionLabel));
                CompileIrNodeLoad(@if.Condition, false, ctx);
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
                        CompileIrNodeLoad(@elseIf.Condition, false, ctx);
                        if (@elseIf.Condition is not IrLogicalExp)
                        {
                            ctx.Gen.Add(CilOpCodes.Brfalse, nextConditionLabel);
                            ctx.StackPop();
                        }
                        ctx.FramePop();
                        
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
                var breakLabel = new CilInstructionLabel();
                
                // initialize def
                if (@while.Define != null) CompileIrNodeLoad(@while.Define, true, ctx);
                
                // Jump to check
                ctx.Gen.Add(CilOpCodes.Br,  checkLabel);
                
                // Body
                ctx.FramePush(new LoopBodyFrame(checkLabel, breakLabel));
                var lastIdx = ctx.Gen.Count;
                CompileIr(@while.Process, ctx);
                if (lastIdx == ctx.Gen.Count) ctx.Gen.Add(CilOpCodes.Nop);
                bodyLabel.Instruction = ctx.Gen[lastIdx];
                ctx.FramePop();
                
                // Step
                if (@while.Step != null) CompileIr(@while.Step, ctx);
                
                // Check
                ctx.FramePush(new LoopCheckFrame(bodyLabel, breakLabel));
                var stackBefore = ctx.Stack.Count;
                lastIdx = ctx.Gen.Count;
                CompileIrNodeLoad(@while.Condition, false, ctx);
                ctx.FramePop();

                if (stackBefore != ctx.Stack.Count)
                {
                    ctx.Gen.Add(CilOpCodes.Brtrue, bodyLabel);
                    ctx.StackPop();
                }
                else ctx.Gen.Add(CilOpCodes.Br, bodyLabel);
                
                checkLabel.Instruction = ctx.Gen[lastIdx];
                breakLabel.Instruction = ctx.Gen.Add(CilOpCodes.Nop);
            } break;
            
            case IrReturn @ret:
                if (ret.Value != null)
                {
                    CompileIrNodeLoad(ret.Value, false, ctx);
                    ctx.StackPop();
                }
                ctx.Gen.Add(CilOpCodes.Ret);
                break;
            
            default: throw new UnreachableException();
        }
    }
    
    private void CompileIrNodeLoadAsRef(IrNode node, Context ctx)
    {
        switch (node)
        {
            case IrSolvedReference @sr:
            {
                switch (sr.Reference)
                {
                    case SelfReference @s:
                        ctx.Gen.Add(CilOpCodes.Ldarg_0);
                        ctx.StackPush(ctx.SelfType?.ToTypeSignature() ?? throw new UnreachableException());
                        break;
                    
                    case LocalReference @l:
                    {
                        var t = ctx.GetLoc(l.Local.index).VariableType;
                        var byRef = t.IsValueType;
                        ctx.Gen.Add(byRef ? CilOpCodes.Ldloca : CilOpCodes.Ldloc, ctx.GetLoc(l.Local.index));
                        ctx.StackPush(t.MakeByReferenceType());
                    } break;

                    case ParameterReference @p:
                    {
                        var t = ctx.GetArg(p.Parameter.Index).ParameterType;
                        var byRef = t.IsValueType;
                        ctx.Gen.Add(byRef ? CilOpCodes.Ldarga : CilOpCodes.Ldarg, ctx.GetArg(p.Parameter.Index));
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
    
    private void CompileIrNodeStore(IrNode node, IrNode? value, Context ctx)
    {
        switch (node)
        {
            case IrSolvedReference @solv:
            {
                switch (solv.Reference)
                {
                    case LocalReference @l:
                    {
                        if (value != null) CompileIrNodeLoad(value, false, ctx);
                        ctx.Gen.Add(CilOpCodes.Stloc, ctx.GetLoc(l.Local.index));
                        ctx.StackPop();
                    } break;

                    case SolvedFieldReference @f:
                    {
                        var t = _fieldsMap[f.Field];
                        var isstat = t.IsStatic;
                        
                        if (!t.IsStatic && !ctx.Stack[^1].IsAssignableTo(t.DeclaringType!.ToTypeSignature()))
                            ctx.Gen.Add(CilOpCodes.Conv_U);
                        
                        if (value != null) CompileIrNodeLoad(value, false, ctx);
                        ctx.Gen.Add((isstat ? CilOpCodes.Stsfld : CilOpCodes.Stfld), t);
                        
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
                CompileIrNodeLoad(idx.Value, false, ctx);
                CompileIrNodeLoad(idx.Indices[0], false, ctx);
                SolveLastSoftCast(_corLibFactory.Int32, ctx);
                CompileIrNodeLoad(value!, false, ctx);
                ctx.Gen.Add(CilOpCodes.Stelem, elmtype.ToTypeDefOrRef());
                ctx.StackPop(3);
            } break;
            
            default: throw new UnreachableException();
        }
    }
    
    private void CompileIrNodeCall(IrNode node, IrExpression[] allArgs, bool ignoreValue, Context ctx, bool useNewObj = false)
    {
        switch (node)
        {
            case IrSolvedReference solvedReference:
            {
                switch (solvedReference.Reference)
                {
                    case SolvedCallableReference sfr:
                    {
                        List<TypeReference> generics = [];
                        var argsCount = 0;
                        
                        foreach (var i in allArgs)
                        {
                            switch (i)
                            {
                                case IrSolvedReference { Type: TypeTypeReference, Reference: TypeReference @type } when sfr.Callable.IsGeneric:
                                    generics.Add(type);
                                    break;
                                
                                case IrSolvedReference { Type: TypeTypeReference, Reference: ParameterReference @param } when sfr.Callable.IsGeneric:
                                    generics.Add(new GenericTypeReference(param.Parameter));
                                    break;
                                
                                default:
                                    CompileIrNodeLoad(i, false, ctx);
                                    SolveLastSoftCast(TypeFromRef(i.Type), ctx);
                                    argsCount++; break;
                            }
                        }
                        
                        switch (sfr.Callable)
                        {
                            case DotnetMethodObject dotnetMethod:
                            {
                                var signature = dotnetMethod.MethodDefinition.Signature!;
                                
                                IMethodDescriptor descriptor;
                                if (signature.IsGeneric)
                                {
                                    var genericsSignatures = generics.Select(TypeFromRef).ToArray();
                                    var genericInstance = ((IMethodDefOrRef)dotnetMethod.MethodReference)
                                        .MakeGenericInstanceMethod(genericsSignatures);
                                    descriptor = ctx.Importer.ImportMethod(genericInstance);
                                }
                                else descriptor = ctx.Importer.ImportMethod(dotnetMethod.MethodReference);
                                
                                var opcode = useNewObj
                                    ? CilOpCodes.Newobj
                                    : dotnetMethod.IsVirtual
                                        ? CilOpCodes.Callvirt
                                        : CilOpCodes.Call;
                                
                                ctx.Gen.Add(opcode, descriptor);
                                ctx.StackPop(argsCount); 
                                if (signature.ReturnsValue)
                                {
                                    if (ignoreValue) ctx.Gen.Add(CilOpCodes.Pop);
                                    else ctx.StackPush(signature.ReturnType);
                                }
                            } break;

                            default:
                            {
                                var functionData = _functionsMap[sfr.Callable];
                                var signature = functionData.Method.Signature!;

                                IMethodDescriptor descriptor = functionData.Method;
                                if (signature.IsGeneric)
                                {
                                    var genericsSignatures = generics.Select(TypeFromRef).ToArray();
                                    descriptor = functionData.Method.MakeGenericInstanceMethod(genericsSignatures);
                                }

                                ctx.Gen.Add(useNewObj ? CilOpCodes.Newobj : CilOpCodes.Call, descriptor);
                                ctx.StackPop(argsCount);
                                if (signature.ReturnsValue)
                                {
                                    if (ignoreValue) ctx.Gen.Add(CilOpCodes.Pop);
                                    else ctx.StackPush(signature.ReturnType);
                                }
                            } break;
                        }
                    } break;

                    case SliceCallReference @sliceCall:
                    {
                        var fref = _coreLib["String"].m["Substring"];
                        CompileIrNodeLoad(allArgs[0], false, ctx);
                        
                        CompileIrNodeLoad(allArgs[1], false, ctx);
                        ctx.Gen.Add(CilOpCodes.Conv_Ovf_I4_Un);
                        
                        CompileIrNodeLoad(allArgs[2], false, ctx);
                        CompileIrNodeLoad(allArgs[1], false, ctx);
                        ctx.Gen.Add(CilOpCodes.Sub_Ovf);
                        ctx.Gen.Add(CilOpCodes.Conv_Ovf_I4_Un);

                        ctx.Gen.Add(CilOpCodes.Call, fref);
                    } break;
                    
                    default: throw new UnreachableException();
                }
            } break;
                
            default: throw new UnreachableException();
        }
    }

    private void CompileIrFieldConstant(Constant c, Context ctx)
    {
        switch (c.Type)
        {
            case ElementType.I1:
            case ElementType.U1:
            {
                var val = c.Value!.Data[0];
                ctx.Gen.Add(CilInstruction.CreateLdcI4(val));
            } break;
            case ElementType.I2:
            case ElementType.U2:
            {
                var bytes = c.Value!.Data;
                var val = BinaryPrimitives.ReadInt16LittleEndian(bytes);
                ctx.Gen.Add(CilInstruction.CreateLdcI4(val));
            } break;
                                
            case ElementType.I4:
            case ElementType.U4:
            {
                var bytes = c.Value!.Data;
                var val = BinaryPrimitives.ReadInt32LittleEndian(bytes);
                ctx.Gen.Add(CilInstruction.CreateLdcI4(val));
            } break;

            case ElementType.I8 or ElementType.U8:
            {
                var bytes = c.Value!.Data;
                var val = BinaryPrimitives.ReadInt64LittleEndian(bytes);
                ctx.Gen.Add(CilOpCodes.Ldc_I8, val);
            } break;
            
            default: throw new NotImplementedException();
        }
        
        ctx.StackPush(_corLibFactory.FromElementType(c.Type)!);
    }

    private void SolveLastSoftCast(TypeSignature toType, Context ctx)
    {
        var fromType = ctx.PeekStack();
        switch (toType)
        {
            case CorLibTypeSignature @to when fromType is CorLibTypeSignature @from:
            {
                if (
                    IsExplicitInteger(to, out var toSigned, out var toSize)
                    && IsExplicitInteger(from, out var fromSigned, out var fromSize))
                {
                    switch (toSize)
                    {
                        case null when fromSigned: ctx.Gen.Add(toSigned ? CilOpCodes.Conv_Ovf_I : CilOpCodes.Conv_Ovf_U); break;
                        case null:                 ctx.Gen.Add(toSigned ? CilOpCodes.Conv_Ovf_I_Un : CilOpCodes.Conv_Ovf_U_Un); break;
                        case 1 when fromSigned: ctx.Gen.Add(toSigned ? CilOpCodes.Conv_Ovf_I1 : CilOpCodes.Conv_Ovf_U1); break;
                        case 1:                 ctx.Gen.Add(toSigned ? CilOpCodes.Conv_Ovf_I1_Un : CilOpCodes.Conv_Ovf_U1_Un); break;
                        case 2 when fromSigned: ctx.Gen.Add(toSigned ? CilOpCodes.Conv_Ovf_I2 : CilOpCodes.Conv_Ovf_U2); break;
                        case 2:                 ctx.Gen.Add(toSigned ? CilOpCodes.Conv_Ovf_I2_Un : CilOpCodes.Conv_Ovf_U2_Un); break;
                        case 4 when fromSigned: ctx.Gen.Add(toSigned ? CilOpCodes.Conv_Ovf_I4 : CilOpCodes.Conv_Ovf_U4); break;
                        case 4:                 ctx.Gen.Add(toSigned ? CilOpCodes.Conv_Ovf_I4_Un : CilOpCodes.Conv_Ovf_U4_Un); break;
                        case 8 when fromSigned: ctx.Gen.Add(toSigned ? CilOpCodes.Conv_Ovf_I8 : CilOpCodes.Conv_Ovf_U8); break;
                        case 8:                 ctx.Gen.Add(toSigned ? CilOpCodes.Conv_Ovf_I8_Un : CilOpCodes.Conv_Ovf_U8_Un); break;
                    }
                }
            } break;
        }
    }
}
