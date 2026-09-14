using System.Diagnostics;
using System.Numerics;
using Abstract.CodeProcess.Core;
using Abstract.CodeProcess.Core.EvaluationData;
using Abstract.CodeProcess.Core.EvaluationData.IntermediateTree;
using Abstract.CodeProcess.Core.EvaluationData.IntermediateTree.Statements;
using Abstract.CodeProcess.Core.EvaluationData.IntermediateTree.Values;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.CodeReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.FunctionReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.NamespaceReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin.Integer;
using Abstract.CodeProcess.Core.EvaluationData.Misc;
using Tq.CodeProcess.Core.EvaluationData.IntermediateTree.Expressions;
using Tq.CodeProcess.Core.EvaluationData.LanguageObjects;
using Tq.CodeProcess.Core.EvaluationData.LanguageObjects.Attributes;
using Tq.CodeProcess.Core.EvaluationData.LanguageObjects.CodeObjects;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.AttributeReferences;
using Tq.CodeProcess.Core.Language.SyntaxNodes;

namespace Tq.CodeProcess;

/*
 * Stage Four:
 *  Semantic analysis, solving automatic type inference, type conversion,
 *  operator overloading, function overloading, etc.
 */

public partial class Analyser
{
    private void DoSemanticAnalysis()
    {
        List<FunctionObject> funclist = [];
        List<TypedefObject> typedeflist = [];
        List<ConstructorObject> ctorList = [];
        List<DestructorObject> dtorList = [];
        List<FieldObject> fldlist = [];

        var allMembers = _modules.SelectMany(WalkMembers).ToList();

        foreach (var i in allMembers)
        {
            switch (i)
            {
                case TqNamespaceObject nmsp: NamespaceSemaAnal(nmsp); break;
                case TypedefObject t: typedeflist.Add(t); break;
                case FunctionObject f: funclist.Add(f); break;
                case FieldObject f: fldlist.Add(f); break;
                case ConstructorObject c: ctorList.Add(c); break;
                case DestructorObject d: dtorList.Add(d); break;
            }
        }
        
        // Header analysis
        foreach (var obj in allMembers)
        {
            switch (obj)
            {
                case FunctionObject @fun:
                    FunctionSemaAnal(fun);
                    break;
                
                case StructObject @struc:
                    StructureSemaAnal(struc);
                    break;
                
                case FieldObject @field:
                    FieldSemaAnal(field);
                    break;
            }
        }
        
        // Execution analysis
        foreach (var fld in fldlist)
        {
            if (fld.Value == null) continue;
            var ctx = new IrBlockExecutionContextData(fld);
            fld.Value = SolveTypeCast((ITypeReference)fld.Type, (IrExpression)NodeSemaAnal(fld.Value, ctx), true);
        }
        
        foreach (var tdef in typedeflist)
        {
            foreach (var namedEntry in tdef.NamedValues)
            {
                if (namedEntry.Value == null) continue;
                var ctx = new IrBlockExecutionContextData(tdef);
                namedEntry.Value = (IrExpression)NodeSemaAnal(namedEntry.Value, ctx);
            }
        }
        
        foreach (var fun in funclist)
        {
            var ctx = new IrBlockExecutionContextData(fun);
            if (fun.Body != null) BlockSemaAnal(fun.Body, ctx);
        }
        
        foreach (var ctor in ctorList)
        {
            var ctx = new IrBlockExecutionContextData(ctor);
            if (ctor.Body != null) BlockSemaAnal(ctor.Body, ctx);
        }
        
        foreach (var dtor in dtorList)
        {
            var ctx = new IrBlockExecutionContextData(dtor);
            if (dtor.Body != null) BlockSemaAnal(dtor.Body, ctx);
        }
    }

    private void NamespaceSemaAnal(TqNamespaceObject nmsp)
    {
    }
    private void FunctionSemaAnal(FunctionObject function)
    {
        foreach (var i in function.Parameters)
        {
            if (!i.Type.IsSolved) i.Type = (Reference)SolveTypeLazy2(i.Type, null, function);
        }

        foreach (var i in function.Locals)
        {
            if (i.Type == null || i.Type.IsSolved) continue;
            i.Type = (Reference)SolveTypeLazy2(i.Type, null, function);
        }
        
        if (!function.ReturnType.IsSolved)
            function.ReturnType = (Reference)SolveTypeLazy2(function.ReturnType, null, function.Container);
    }
    private void CtorSemaAnal(ConstructorObject ctor)
    {
        foreach (var i in ctor.Parameters)
        {
            if (i.Type.IsSolved) i.Type = (Reference)SolveTypeLazy2(i.Type, null, ctor.Container);
        }

        foreach (var i in ctor.Locals)
        {
            if (i.Type == null || i.Type.IsSolved) i.Type = (Reference)SolveTypeLazy2(i.Type!, null, ctor.Container);
        }

        if (ctor.ReturnTypeOverride is { IsSolved: false })
            ctor.ReturnTypeOverride = (Reference)SolveTypeLazy2(ctor.ReturnTypeOverride, null, ctor.Container);
    }
    private void DtorSemaAnal(DestructorObject dtor)
    {
        foreach (var i in dtor.Parameters)
        {
            if (i.Type.IsSolved) continue;
            i.Type = (Reference)SolveTypeLazy2(i.Type, null, dtor.Container);
        }

        foreach (var i in dtor.Locals)
        {
            if (i.Type == null || i.Type.IsSolved) continue;
            i.Type = (Reference)SolveTypeLazy2(i.Type, null, dtor.Container);
        }
    }
    
    private void StructureSemaAnal(StructObject structure)
    {
        foreach (var i in structure.Fields) FieldSemaAnal(i);
        foreach (var i in structure.Constructors) CtorSemaAnal(i);
        foreach (var i in structure.Destructors) DtorSemaAnal(i);

        if (structure is { Abstract: false, Constructors.Count: 0 })
        {
            var defaultCtor = new ConstructorObject(null!, null!)
            {
                Body = new IrBlock(null!),
            };
            structure.Constructors.Add(defaultCtor);
        }
    }
    private void FieldSemaAnal(FieldObject field)
    {
        if (!field.Type.IsSolved) field.Type = (Reference)SolveTypeLazy2(field.Type, null, field);
    }
    
    private void BlockSemaAnal(IrBlock block, IrBlockExecutionContextData ctx, bool newFrame = true)
    {
        if (newFrame) ctx.PushFrame();
        for (var i = 0; i < block.Content.Count; i++) block.Content[i] = NodeSemaAnal(block.Content[i], ctx);
        if (newFrame) ctx.PopFrame();
    }

    private IrNode NodeSemaAnal(IrNode node, IrBlockExecutionContextData ctx)
    {
        // try
        // {
            return node switch
            {
                IrCall @iv => NodeSemaAnal_Invoke(iv, ctx),
                IrAssign @ass => NodeSemaAnal_Assign(ass, ctx),
                IRUnaryExp @ue => NodeSemaAnal_UnExp(ue, ctx),
                IrBinaryExp @be => NodeSemaAnal_BinExp(be, ctx),
                IrCompareExp @ce => NodeSemaAnal_CmpExp(ce, ctx),
                IrLogicalExp @ce => NodeSemaAnal_LogicalExp(ce, ctx),
                IrTernary @te => NodeSemaAnal_TernaryExp(te, ctx),
                IrIndex @ix => NodeSemaAnal_Index(ix, ctx),
                IrConv @tc =>NodeSemaAnal_Conv(tc, ctx),
                IrNewObject @no => NodeSemaAnal_NewObj(no, ctx),
                IrReturn @re => NodeSemaAnal_Return(re, ctx),
                IRIf @iff => NodeSemaAnal_If(iff, ctx),
                IRWhile @iwhile => NodeSemaAnal_While(iwhile, ctx),
                IrPatternMatch @match => NodeSemaAnal_Match(match, ctx),
                IrReference { IsSolved: true } @re => NodeSemaAnal_Reference(re, ctx),
                
                IrCharLiteral
                or IrStringLiteral
                or IrIntegerLiteral 
                or IRBooleanLiteral
                or IRNullLiteral => node,
                
                IrAccess @s => NodeSemaAnal_Access(s, ctx),
                IrCollectionLiteral @c => NodeSemaAnal_Collection(c, ctx),
                IrReference { IsSolved: false } @u => SolveReference(u, ctx, null),
                
                _ => throw new NotImplementedException(),
            };
        // }
        // catch (Exception e)
        // {
        //     _errorHandler.SetFile(ctx.Parent.SourceScript);
        //     _errorHandler.RegisterError(e);
        //     return node;
        // }
    }

    private IrNode NodeSemaAnal_Reference(IrReference re, IrBlockExecutionContextData ctx)
    {
        switch (re.Reference)
        {
            case LocalReference @lr:
                ctx.LocalVariables.Add(lr.Local);
                return re;

            case GenericTypeImplReference @generic:
            {
                var newGeneric = new GenericTypeImplReference(@generic);
                foreach (var i in generic.Arguments)
                    newGeneric.Arguments.Add((IrExpression)NodeSemaAnal(i, ctx));
                return new IrReference(re.Origin, newGeneric);
            }

            default: return re;       
        }
    }
    private IrNode NodeSemaAnal_Invoke(IrCall node, IrBlockExecutionContextData ctx)
    {
        node.Target = (IrExpression)NodeSemaAnal(node.Target, ctx);
        var targetRef = (ReferenceOf(node.Target));

        IrExpression? instance = null;
        if (node.Target is IrAccess @irAccess)
        {
            instance = irAccess.A;
            node.Target = irAccess.B;
        }
        
        for (var i = 0; i < node.Arguments.Length; i++)
            node.Arguments[i] = (IrExpression)NodeSemaAnal(node.Arguments[i], ctx);

        // extreme exceptions, mainly intrinsic shit
        switch (targetRef)
        {
            case SliceCallReference: return new IrCall(node.Origin, node.Target, [instance!, ..node.Arguments]);
        }
        
        var res = targetRef switch
        {
            FunctionGroupReference @r => SolveFunctionOverload(
                r.FunctionGroup.Overloads.ToArray<ICallable>(), node.Arguments, node.Origin),
            
            DotnetMethodGroupReference @r => SolveFunctionOverload(
                r.MethodGroup.Overloads.ToArray<ICallable>(), node.Arguments, node.Origin),
            
            _ => throw new NotImplementedException(),
        };
        
        switch (res)
        {
            case NoOverloadResult: throw new Exception($"Could not find suitable overload for call '{node.Origin}'");

            case SimpleOverloadResult @s:
            {
                var newArgs =  new IrExpression[s.Callable.Parameters.Count];
                for (var i = 0; i < newArgs.Length; i++)
                    newArgs[i] = SolveTypeCast((ITypeReference)s.Callable.Parameters[i].Type, node.Arguments[i]);
                
                node.Arguments = newArgs;
                node.Target = new IrReference(node.Target.Origin, new CallableReference(s.Callable));
                if (instance != null)
                {
                    node.Arguments = instance.Type is not ReferenceTypeReference
                        ? node.Arguments = [new IrRef(instance.Origin, (IrExpression)NodeSemaAnal(instance, ctx)), ..node.Arguments]
                        : node.Arguments = [(IrExpression)NodeSemaAnal(instance, ctx), ..node.Arguments];
                }
                return node;
            }
            
            default: throw new NotImplementedException(res.ToString());
        }
    }
    private IrNode NodeSemaAnal_NewObj(IrNewObject node, IrBlockExecutionContextData ctx)
    {
        node.Target = (IrReference)NodeSemaAnal(node.Target, ctx);
        var instanceTypeRef = GetEffectiveTypeReference(node.Target);
        
        if (instanceTypeRef is IrReference { IsSolved: false }) throw new Exception($"Not able to resolve reference to '{node.Origin}'");
        if (instanceTypeRef is not StructReference and not DotnetTypeReference and not GenericTypeImplReference)
            throw new Exception($"Cannot instantiate type '{node.Target.Origin}' as an object");
        
        node.InstanceType = instanceTypeRef;
        if (instanceTypeRef is DotnetTypeReference { Reference.IsValueType: false }) 
            node.OverrideReturnType = new ReferenceTypeReference((Reference)instanceTypeRef);
        
        for (var i = 0; i < node.Arguments.Length; i++)
            node.Arguments[i] = (IrExpression)NodeSemaAnal(node.Arguments[i], ctx);

        ISolvedOverloadResult res = instanceTypeRef switch
        {
            StructReference structRef => SolveFunctionOverload(
                structRef.Struct.Constructors.ToArray<ICallable>(), node.Arguments, node.Origin),
            
            DotnetTypeReference dotnetRef => SolveFunctionOverload(
                dotnetRef.Reference.Constructors.ToArray<ICallable>(), node.Arguments, node.Origin),
            
            _ => throw new NotImplementedException()
        };

        switch (res)
        {
            case NoOverloadResult: throw new Exception($"Could not find suitable overload for call '{node.Origin}'");

            case SimpleOverloadResult @s:
            {
                var newArgs =  new IrExpression[s.Callable.Parameters.Count];
                for (var i = 0; i < newArgs.Length; i++)
                    newArgs[i] = SolveTypeCast((ITypeReference)s.Callable.Parameters[i].Type, node.Arguments[i]);
                
                node.Arguments = newArgs;
                node.Target = new IrReference(node.Target.Origin, new CallableReference(s.Callable));
            } break;

            // case GenericOverloadResult @g:
            // {
            //     List<IrExpression> newArgs = [];
            //     for (var i = 0; i < node.Arguments.Length; i++)
            //     {
            //         if (g.Types[i] is IgnoreTypeReference) continue;
            //         newArgs.Add(SolveTypeCast(g.Types[i], node.Arguments[i]));
            //     }
            //
            //     node.Arguments = [.. newArgs];
            //     node.Target = new IrSolvedReference(node.Target.Origin, new SolvedCallableReference(g.Callable));
            // } break;
                
            default: throw new NotImplementedException(res.ToString());
        }

        for (var i = 0; i < node.InlineAssignments.Length; i++)
        {
            var v = node.InlineAssignments[i];

            v.Target = (IrExpression)NodeSemaAnal(v.Target, ctx);
            v.Value = (IrExpression)NodeSemaAnal(v.Value, ctx);
            
            v.Value = SolveTypeCast(GetEffectiveTypeReference(v.Target), v.Value);
            
            node.InlineAssignments[i] = v;
        }
        
        return node;
    }
    private IrNode NodeSemaAnal_Assign(IrAssign node, IrBlockExecutionContextData ctx)
    {
        var a = node;
        node.Target = (IrExpression)NodeSemaAnal(node.Target, ctx);
        node.Value = (IrExpression)NodeSemaAnal(node.Value, ctx);

        if (node.Target is IrReference { IsSolved: true, Reference: LocalReference { Type: null } @l })
        {
            var typefrom = GetEffectiveTypeReference(node.Value);
            if (typefrom is ComptimeIntegerTypeReference) typefrom = new RuntimeIntegerTypeReference(true);
            
            l.Local.Type = (Reference)typefrom;
        }

        var typeto = GetEffectiveTypeReference(node.Target);
        node.Value = SolveTypeCast(typeto, node.Value);
        node.Value = SolveTypeCast(GetEffectiveTypeReference(node.Target), node.Value);
        
        return node;
    }

    private IrNode NodeSemaAnal_UnExp(IRUnaryExp node, IrBlockExecutionContextData ctx)
    {
        node.Value = (IrExpression)NodeSemaAnal(node.Value, ctx);

        if (node is { Value: IrIntegerLiteral @valInt })
        {
            return new IrIntegerLiteral(node.Origin, node.Operation switch
            {
                IRUnaryExp.UnaryOperation.Plus => valInt.Value,
                IRUnaryExp.UnaryOperation.Minus => BigInteger.Negate(valInt.Value),
                IRUnaryExp.UnaryOperation.Not => ~valInt.Value,
                
                IRUnaryExp.UnaryOperation.PreIncrement => valInt.Value + BigInteger.One,
                IRUnaryExp.UnaryOperation.PreDecrement => valInt.Value - BigInteger.One,
                IRUnaryExp.UnaryOperation.PostIncrement or
                    IRUnaryExp.UnaryOperation.PostDecrement => valInt.Value,
                
                _ => throw new UnreachableException(),
            }, (IntegerTypeReference)GetEffectiveTypeReference(node.Value));
        }
        
        return node;
    }
    private IrNode NodeSemaAnal_BinExp(IrBinaryExp node, IrBlockExecutionContextData ctx)
    {
        node.Left = (IrExpression)NodeSemaAnal(node.Left, ctx);
        node.Right = (IrExpression)NodeSemaAnal(node.Right, ctx);
        var leftTypeRef = GetEffectiveTypeReference(node.Left);
        ITypeReference ftype = new VoidTypeReference();

        if (leftTypeRef is ComptimeIntegerTypeReference
            && GetEffectiveTypeReference(node.Right) is ComptimeIntegerTypeReference)
        {
            ftype = new ComptimeIntegerTypeReference();
            goto skipTypeCheck;
        }

        // TODO solve operator overloading

        switch (node.Operator)
        {

            case IrBinaryExp.Operators.LeftShift:
            case IrBinaryExp.Operators.RightShift:
                node.Right = SolveTypeCast(new RuntimeIntegerTypeReference(false), node.Right);
                break;
            
            default:
                node.Right = SolveTypeCast(leftTypeRef, node.Right);
                break;
        }

        var ltype = ftype = GetEffectiveTypeReference(node.Left);
        var rtype = GetEffectiveTypeReference(node.Right);
        
        switch (ltype)
        {
            case RuntimeIntegerTypeReference @left when
                rtype is RuntimeIntegerTypeReference @right:
            {
                if (left.BitSize >= right.BitSize) ftype = left;
                else if (left.BitSize < right.BitSize) ftype = right;
            } break;
            
            case RuntimeIntegerTypeReference @left2 when
                rtype is ComptimeIntegerTypeReference:
            {
                ftype = left2;
                node.Right = new IrIntegerLiteral(node.Right.Origin, ((IrIntegerLiteral)node.Right).Value, left2);
            } break;
            
            case RuntimeIntegerTypeReference left when
                rtype is TypedefReference { Typedef.BackType: RuntimeIntegerTypeReference right }:
            {
                if (left.BitSize >= right.BitSize) ftype = left;
                else if (left.BitSize < right.BitSize) ftype = right;
            } break;

            case RuntimeIntegerTypeReference left when
                rtype is CharTypeReference:
            {
                ftype = rtype;
            } break;
            
            case ComptimeIntegerTypeReference @left3 when
                rtype is RuntimeIntegerTypeReference @right3:
            {
                node.Left = new IrIntegerLiteral(node.Left.Origin, ((IrIntegerLiteral)node.Left).Value, right3);
                ftype = right3;
            } break;

            case ComptimeIntegerTypeReference when
                rtype is ComptimeIntegerTypeReference:
            {
                ftype = new ComptimeIntegerTypeReference();
            } break;
            
            case ComptimeIntegerTypeReference when
                rtype is TypedefReference { Typedef.BackType: RuntimeIntegerTypeReference right }:
            {
                ftype = right.Type;
            } break;
            
            case StringTypeReference @sl when
                rtype is StringTypeReference @sr:
            {
                if (sl.Encoding == sr.Encoding) ftype = new StringTypeReference(sl.Encoding);
                else throw new Exception("Cannot automatically concatenate strings with different encoding");
            } break;

            case CharTypeReference when
                rtype is CharTypeReference:
            {
                ftype = new CharTypeReference();
            } break;
            
            default: throw new NotImplementedException();
        }

        node.ResultType = ftype;
        
        skipTypeCheck:
        // Operate literals at comptime
        // FIXME it surely would be better if in a stage 5 or something
        return node switch
        {
            { Left: IrIntegerLiteral @leftInt, Right: IrIntegerLiteral @rightInt } => node.Operator switch
            {
                _ => new IrIntegerLiteral(node.Origin, node.Operator switch
                    {
                        IrBinaryExp.Operators.Add => leftInt.Value + rightInt.Value,
                        IrBinaryExp.Operators.AddOnBounds => AddOnBounds(leftInt.Value, rightInt.Value, (IntegerTypeReference)ftype),
                        IrBinaryExp.Operators.AddWrapAround => AddWithOverflow(leftInt.Value, rightInt.Value, (IntegerTypeReference)ftype),
                    
                        IrBinaryExp.Operators.Subtract => leftInt.Value - rightInt.Value,
                        IrBinaryExp.Operators.SubtractOnBounds => SubOnBounds(leftInt.Value, rightInt.Value, (IntegerTypeReference)ftype),
                        IrBinaryExp.Operators.SubtractWrapAround => SubWithOverflow(leftInt.Value, rightInt.Value, (IntegerTypeReference)ftype),
                        
                        IrBinaryExp.Operators.Multiply => leftInt.Value * rightInt.Value,
                        IrBinaryExp.Operators.Divide => leftInt.Value / rightInt.Value,
                        IrBinaryExp.Operators.Reminder => leftInt.Value % rightInt.Value,

                        IrBinaryExp.Operators.Pow => BigInteger.Pow(leftInt.Value, (int)rightInt.Value),
                        
                        IrBinaryExp.Operators.BitwiseAnd => leftInt.Value & rightInt.Value,
                        IrBinaryExp.Operators.BitwiseOr => leftInt.Value | rightInt.Value,
                        IrBinaryExp.Operators.BitwiseXor => leftInt.Value ^ rightInt.Value,
                        IrBinaryExp.Operators.LeftShift => leftInt.Value << (int)rightInt.Value,
                        IrBinaryExp.Operators.RightShift => leftInt.Value >> (int)rightInt.Value,

                        _ => throw new NotImplementedException(),
                    }, (IntegerTypeReference)ftype),
            },
            
            { Left: IrStringLiteral @leftStr, Right: IrStringLiteral @rightStr } => new IrStringLiteral(node.Origin,
                node.Operator switch
                {
                    IrBinaryExp.Operators.Add => leftStr.Data + rightStr.Data,
                    _ => throw new UnreachableException()
                }),
            _ => node
        };
        
        BigInteger AddOnBounds(BigInteger left, BigInteger right, IntegerTypeReference type)
        {
            var result = left + right;
            if (type is RuntimeIntegerTypeReference @runtimeInt)
            {
                BigInteger min;
                BigInteger max;
                
                if (runtimeInt.Signed)
                {
                    var limit = BigInteger.One << (runtimeInt.BitSize.Bits - 1);
                    min = -limit;
                    max = limit - 1;
                }
                else
                {
                    min = BigInteger.Zero;
                    max = (BigInteger.One << runtimeInt.BitSize.Bits) - 1;
                }

                if (result > max) return max;
                if (result < min) return min;
            }
            return result;
        }
        BigInteger AddWithOverflow(BigInteger left, BigInteger right, IntegerTypeReference type)
        {
            var result = left + right;
            if (type is RuntimeIntegerTypeReference @runtimeInt)
            {
                BigInteger max;
                
                if (runtimeInt.Signed)
                {
                    var limit = BigInteger.One << (runtimeInt.BitSize.Bits - 1);
                    max = limit - 1;
                }
                else
                {
                    max = (BigInteger.One << runtimeInt.BitSize.Bits) - 1;
                }

                return result % max;
            }
            return result;
        }
        BigInteger SubOnBounds(BigInteger left, BigInteger right, IntegerTypeReference type)
        {
            var result = left - right;
            if (type is RuntimeIntegerTypeReference @runtimeInt)
            {
                BigInteger min;
                BigInteger max;
                
                if (runtimeInt.Signed)
                {
                    var limit = BigInteger.One << (runtimeInt.BitSize.Bits - 1);
                    min = -limit;
                    max = limit - 1;
                }
                else
                {
                    min = BigInteger.Zero;
                    max = (BigInteger.One << runtimeInt.BitSize.Bits) - 1;
                }

                if (result > max) return max;
                if (result < min) return min;
            }
            return result;
        }
        BigInteger SubWithOverflow(BigInteger left, BigInteger right, IntegerTypeReference type)
        {
            var result = left - right;

            if (type is RuntimeIntegerTypeReference runtimeInt)
            {
                var bits = runtimeInt.BitSize.Bits;
                var mod = BigInteger.One << bits;

                result %= mod;
                if (result < 0) result += mod;

                if (runtimeInt.Signed)
                {
                    var half = BigInteger.One << (bits - 1);
                    if (result >= half) result -= mod;
                }
            }

            return result;
        }
    }
    private IrNode NodeSemaAnal_CmpExp(IrCompareExp node, IrBlockExecutionContextData ctx)
    {
        node.Left = (IrExpression)NodeSemaAnal(node.Left, ctx);
        var leftTypeRef = GetEffectiveTypeReference(node.Left);
        node.Right = SolveTypeCast(leftTypeRef, (IrExpression)NodeSemaAnal(node.Right, ctx));
        
        switch (node)
        {
            case { Left: IrIntegerLiteral @leftInt, Right: IrIntegerLiteral @rightInt }:
                return new IRBooleanLiteral(node.Origin, node.Operator switch
                {
                    IrCompareExp.Operators.GreaterThan => leftInt.Value > rightInt.Value,
                    IrCompareExp.Operators.LessThan => leftInt.Value < rightInt.Value,
                    IrCompareExp.Operators.LessThanOrEqual => leftInt.Value <= rightInt.Value,
                    IrCompareExp.Operators.GreaterThanOrEqual => leftInt.Value >= rightInt.Value,
                    _ => throw new UnreachableException()
                });
        }
        
        return node;
    }
    private IrNode NodeSemaAnal_LogicalExp(IrLogicalExp node, IrBlockExecutionContextData ctx)
    {
        node.Left = (IrExpression)NodeSemaAnal(node.Left, ctx);
        var leftTypeRef = GetEffectiveTypeReference(node.Left);
        node.Right = SolveTypeCast(leftTypeRef, (IrExpression)NodeSemaAnal(node.Right, ctx));
        
        switch (node)
        {
            case { Left: IRBooleanLiteral @leftBool, Right: IRBooleanLiteral @rightBool }:
                return new IRBooleanLiteral(node.Origin, node.Operator switch
                {
                    IrLogicalExp.Operators.And => leftBool.Value && rightBool.Value,
                    IrLogicalExp.Operators.Or => leftBool.Value || rightBool.Value,
                    _ => throw new UnreachableException()
                });
        }

        return node;
    }
    private IrNode NodeSemaAnal_TernaryExp(IrTernary node, IrBlockExecutionContextData ctx)
    {
        node.Condition = (IrExpression)NodeSemaAnal(node.Condition, ctx);
        node.TrueExpression = (IrExpression)NodeSemaAnal(node.TrueExpression, ctx);
        node.FalseExpression = (IrExpression)NodeSemaAnal(node.FalseExpression, ctx);

        throw new NotImplementedException();
    }
    
    private IrNode NodeSemaAnal_Index(IrIndex node, IrBlockExecutionContextData ctx)
    {
        node.Value = (IrExpression)NodeSemaAnal(node.Value, ctx);
        for (var i = 0; i < node.Indices.Length; i++)
        {
            node.Indices[i] = SolveTypeCast(new RuntimeIntegerTypeReference(false),
                (IrExpression)NodeSemaAnal(node.Indices[i], ctx));
        }

        var expTypeRef = GetEffectiveTypeReference(node.Value);

        switch (expTypeRef)
        { 
            case SliceTypeReference @s:
            {
                // FIXME put message here
                if (node.Indices.Length != 1) throw new Exception("too much indices for this op");
                node.Indices[0] = SolveTypeCast(new RuntimeIntegerTypeReference(false), node.Indices[0]);
                node.ResultType = (ITypeReference)s.ElementType;
            } break;
            
            case StringTypeReference:
                node.ResultType = new CharTypeReference();
                break;
            
            default: throw new UnreachableException();
        }
        
        return node;
    }
    
    private IrNode NodeSemaAnal_Conv(IrConv node, IrBlockExecutionContextData ctx)
    {
        node.Expression = (IrExpression)NodeSemaAnal(node.Expression, ctx);
        return SolveTypeCast(node.Type, node.Expression, node, true);
    }
    private IrNode NodeSemaAnal_Return(IrReturn node, IrBlockExecutionContextData ctx)
    {
        if (node.Value == null) return node;
        if (ctx.Parent is not FunctionObject function) throw new InvalidCastException();
        
        node.Value = (IrExpression)NodeSemaAnal(node.Value, ctx);
        node.Value = SolveTypeCast((ITypeReference)function.ReturnType!, node.Value, false);
        return node;
    }
    private IrNode NodeSemaAnal_If(IRIf node, IrBlockExecutionContextData ctx)
    {
        node.Condition = (IrExpression)NodeSemaAnal(node.Condition, ctx);
        if (node.Else != null)
            node.Else = (IIfElse)(node.Else is IRIf @if
                ? NodeSemaAnal_If(@if, ctx) : NodeSemaAnal_Else((IRElse)node.Else, ctx));
        BlockSemaAnal(node.Then, ctx);
        return node;
    }
    private IrNode NodeSemaAnal_Else(IRElse node, IrBlockExecutionContextData ctx)
    {
        BlockSemaAnal(node.Then, ctx);
        return node;
    }
    private IrNode NodeSemaAnal_Match(IrPatternMatch node, IrBlockExecutionContextData ctx)
    {
        var patterMatch = new IrPatternMatch(node.Origin);
        patterMatch.Expression = (IrExpression)NodeSemaAnal(node.Expression, ctx);

        foreach (var i in node.Cases)
        {
            var @case = new IrPatternMatchCase(i.Origin)
            {
                Pattern = (IrExpression)NodeSemaAnal(i.Pattern!, ctx),
                Action = NodeSemaAnal(i.Action!, ctx),
            };
            patterMatch.Cases.Add(@case);
        }
        if (node.Default is {} @j)
        {
            var @default = new IrPatternMatchCase(j.Origin)
            {
                Pattern = null,
                Action = NodeSemaAnal(j.Action!, ctx),
            };
            patterMatch.Default = @default;
        }
        
        return patterMatch;
    }
    
    private IrNode NodeSemaAnal_While(IRWhile node, IrBlockExecutionContextData ctx)
    {
        ctx.PushFrame();
        if (node.Define != null) BlockSemaAnal(node.Define, ctx, false);
        node.Condition = (IrExpression)NodeSemaAnal(node.Condition, ctx);
        if (node.Step != null) BlockSemaAnal(node.Step, ctx);
        BlockSemaAnal(node.Process, ctx);
        ctx.PopFrame();
        
        return node;
    }

    private IrNode NodeSemaAnal_Access(IrAccess node, IrBlockExecutionContextData ctx)
    {
        if (node.B is not IrReference { IsSolved: false } b) return node;
        return SolveAccessInExpression(node.Origin, (IrExpression)NodeSemaAnal(node.A, ctx), b);
    }
    private IrNode NodeSemaAnal_Collection(IrCollectionLiteral node, IrBlockExecutionContextData ctx)
    {
        var items = node.Items.Select(i => (IrExpression)NodeSemaAnal(i, ctx)).ToArray();
        return new IrCollectionLiteral(node.Origin, node.ElementType, items);
    }

    private ISolvedOverloadResult SolveFunctionOverload(ICallable[] options, IrExpression[] arguments, SyntaxNode origin)
    {
        ICallable? bestOverload = null;
        var bestScore = -1;
        
        Dictionary<ParameterObject, ITypeReference?> bestGenerics = null; 

        foreach (var overload in options)
        {
            if (arguments.Length != overload.Parameters.Count) continue;
            
            if (overload.Parameters.Count == 0)
            {
                if (100 > bestScore)
                {
                    bestOverload = overload;
                    bestScore    = 100;
                    bestGenerics = [];
                }
                continue;
            }
            
            var (isSuitable, score, inferredGenerics) = EvaluateOverload(overload, arguments);

            if (isSuitable && score > bestScore)
            {
                bestOverload = overload;
                bestScore    = score;
                bestGenerics = inferredGenerics; 
            }
        }

        if (bestOverload == null) return new NoOverloadResult();
        
        return new SimpleOverloadResult(bestOverload);
    }
    
    private (bool, int, Dictionary<ParameterObject, ITypeReference?> generics) EvaluateOverload(ICallable overload, IrExpression[] arguments)
    {
        var parameters = overload.Parameters;
        var generics = new Dictionary<ParameterObject, ITypeReference?>();
        var totalSuitability = 0;

        for (var i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];
            var argType = GetEffectiveTypeReference(arguments[i], (LangObject)overload);
            
            var suitabilityScore = 0;

            switch (parameter.Type)
            {
                case TypeTypeReference:
                {
                    if (argType is GenericTypeReference genericArg)
                    {
                        generics[parameter] = genericArg;
                    }
                    else
                    {
                        generics[parameter] = ((TypeTypeReference)argType).ReferencedType;
                    }
                    suitabilityScore = (int)Suitability.NeedsSoftCast;
                    break;
                }
                
                case ITypeReference { IsGeneric: true } genericParam:
                {
                    var concreteType = ConcretizeGeneric(genericParam, generics);
                    suitabilityScore = (int)CalculateTypeSuitability(concreteType, argType, true);
                    break;
                }
                
                default: suitabilityScore = (int)CalculateTypeSuitability((ITypeReference)parameter.Type, argType, true); break;
            }
            
            if (suitabilityScore == 0) return (false, 0, generics);

            totalSuitability += suitabilityScore;
        }

        var finalScore = totalSuitability * 100 / parameters.Count;
        return (true, finalScore, generics);
    }
    
    private IrExpression SolveAccessInExpression(SyntaxNode origin, IrExpression accessBase, IrReference accessMember)
    {
        if (accessMember.IsSolved) return new IrAccess(origin, accessBase, accessMember);
        
        var baseRef = ReferenceOf(accessBase);
        var accessName = ((IdentifierNode)accessMember.Origin).Value;
        var typeref = baseRef.Type;

        while (true)
        {
            if (typeref is ReferenceTypeReference @r) typeref = (ITypeReference)r.InternalType;
            else break;
        }
        
        return typeref switch
        {
            SliceTypeReference @sliceBuiltin => accessName switch
            {
                "len" => new IrLenOf(origin, accessBase),
                _ =>  throw new NotImplementedException(),
            },
            
            StringTypeReference @stringBuiltin => accessName switch
            {
                "len" => new IrLenOf(origin, accessBase),
                "slice" => new IrAccess(origin, accessBase, new IrReference(origin, new SliceCallReference(stringBuiltin.Encoding))),
                _ =>  throw new NotImplementedException(),
            },
            
            TypeTypeReference { ReferencedType: RuntimeIntegerTypeReference @ri } => accessName switch
            {
                "min" => new IrIntegerLiteral(origin, ty: ri,
                    val: ri.Signed
                        ? -(BigInteger.One << (ri.BitSize.Bits - 1))
                        : BigInteger.Zero),
                "max" => new IrIntegerLiteral(origin, ty: ri,
                    val: ri.Signed
                        ? (BigInteger.One << (ri.BitSize.Bits - 1)) - 1
                        : (BigInteger.One << ri.BitSize.Bits) - 1),
                
                _ =>  throw new NotImplementedException(),
            },
            
            TypeTypeReference staticRef => staticRef.ReferencedType switch {
                TypedefReference @solvedType
                    => solvedType.Typedef.SearchChild(accessName, SearchChildMode.OnlyStatic) is {} @refe
                        ? new IrReference(origin, GetObjectReference(refe))
                        : new IrReference(origin),
                
                DotnetTypeReference @dotnetType
                    => @dotnetType.Reference.SearchChild(accessName, SearchChildMode.OnlyStatic) is {} @refe
                        ? new IrReference(origin, GetObjectReference(refe))
                        : new IrReference(origin),
                
                DotnetGenericImplReference @dotnetGenericType
                    => dotnetGenericType.Reference.SearchChild(accessName, SearchChildMode.OnlyStatic) is {} @refe
                        ? new IrReference(origin, GetObjectReference(refe))
                        : new IrReference(origin),
                
                SolvedNamespaceReference @staticTypedef
                    => staticTypedef.Namespace.SearchChild(accessName, SearchChildMode.OnlyStatic) is {} @refe
                        ? new IrReference(origin, GetObjectReference(refe))
                        : new IrReference(origin),
                
                _ => throw new NotImplementedException(),
            },
            
            DotnetTypeReference @instanceRef
                => instanceRef.Reference.SearchChild(accessName, SearchChildMode.OnlyInstance) is {} @refe
                    ? new IrAccess(origin, accessBase, new IrReference(origin, GetObjectReference(refe)))
                    : new IrReference(origin),
            
            StructReference instanceRef
                => instanceRef.Struct.SearchChild(accessName, SearchChildMode.OnlyInstance) is {} @refe
                    ? new IrAccess(origin, accessBase, new IrReference(origin, GetObjectReference(refe)))
                    : new IrReference(origin),
            
            SolvedNamespaceReference @staticTypedef
                => staticTypedef.Namespace.SearchChild(accessName, SearchChildMode.OnlyStatic) is {} @refe
                    ? new IrReference(origin, GetObjectReference(refe))
                    : new IrReference(origin),
            
            
            _ => throw new NotImplementedException(),
        };
    }
    private IrNode SolveReference(IrReference node, IrBlockExecutionContextData? ctx, LangObject? reference)
    {
        var syntaxNode = node.Origin;
        var parent = reference;
        if (ctx == null && parent == null) throw new UnreachableException();
        if (parent == null && ctx != null) parent = ctx.Parent;

        switch (syntaxNode)
        {
            case IdentifierNode idnode:
            {
                // Search in local variables and parameters
                if (ctx != null)
                {
                    var localVariable = ctx.LocalVariables.FirstOrDefault(e => e.Name == idnode.Value);
                    if (localVariable != null)
                        return new IrReference(syntaxNode, new LocalReference(localVariable));

                    var callableParameter = (ctx.Parent as ICallable)?.Parameters.FirstOrDefault(e => e.Name == idnode.Value);
                    if (callableParameter != null)
                        return new IrReference(syntaxNode, new ParameterReference(callableParameter));
                }

                if (parent?.Container is StructObject structObject)
                {
                    // Search in generic parameters
                    var genericParameter = structObject.Parameters.FirstOrDefault(e => e.Name == idnode.Value);
                    if (genericParameter != null) return new IrReference(syntaxNode, new ParameterReference(genericParameter));

                    // Search in inherited members
                    LangObject? currentStructScope = structObject;
                    do
                    {
                        var inheritedMember = currentStructScope.SearchChild(idnode.Value, SearchChildMode.All);
                        if (inheritedMember != null)
                        {
                            var referenceNode = new IrReference(syntaxNode, GetObjectReference(inheritedMember));

                            // NOTE: previously, a member that wasn't even
                            // IStaticModifier (couldn't ever be static)
                            // fell through to `referenceNode` with no
                            // self-prefix. Now every member answers
                            // HasFlag(Static), so anything never marked
                            // static gets the self-prefix. Double check
                            // this is fine for whatever kinds of members
                            // used to skip IStaticModifier entirely.
                            return !inheritedMember.HasFlag(BuiltinAttributes.Static)
                                ? new IrAccess(syntaxNode, new IrReference(syntaxNode, new SelfReference()), referenceNode)
                                : referenceNode;
                        }

                        currentStructScope = ((currentStructScope as StructObject)?.Extends as StructReference)?.Struct;
                    } while (currentStructScope != null && currentStructScope is not TqNamespaceObject);
                }

                // Search inside namespace
                var staticNamespaceMember = parent?.Namespace?.SearchChild(idnode.Value, SearchChildMode.OnlyStatic);
                if (staticNamespaceMember != null)
                    return new IrReference(syntaxNode, GetObjectReference(staticNamespaceMember));

                // Search inside imports
                if (parent?.SourceScript != null)
                {
                    foreach (var importStatement in parent.SourceScript.Imports)
                    {
                        var importedSymbol = importStatement.SearchReference(idnode.Value);
                        if (importedSymbol != null)
                            return new IrReference(syntaxNode, GetObjectReference(importedSymbol));
                    }
                }

                // Search top-level module members (replaces the flat
                // `_globalReferenceTable` bare-identifier fallback)
                LangObject? topLevelMember = null;
                foreach (var m in _modules)
                {
                    topLevelMember = m switch
                    {
                        TqModuleObject { Root: not null } tq => tq.Root.SearchChild(idnode.Value, SearchChildMode.OnlyStatic),
                        DotnetModuleObject dn => dn.SearchChild(idnode.Value, SearchChildMode.OnlyStatic),
                        _ => null
                    };
                    if (topLevelMember != null) break;
                }
                if (topLevelMember != null)
                    return new IrReference(syntaxNode, GetObjectReference(topLevelMember));

                // Search inside the current namespace itself (replaces
                // the flat, namespaced `_globalReferenceTable` fallback)
                if (parent is TqNamespaceObject currentNamespace)
                {
                    var ownMember = currentNamespace.SearchChild(idnode.Value, SearchChildMode.OnlyStatic);
                    if (ownMember != null)
                        return new IrReference(syntaxNode, GetObjectReference(ownMember));
                }

                throw new Exception($"Cannot find reference to {idnode.Value}");
            }
            
            default: throw new UnreachableException();
        }
    }

    private Reference ReferenceOf(IrNode node) => node switch
        {
            IrReference { IsSolved: true } @sr => sr.Reference,
            IrAccess @acc                      => ReferenceOf(acc.B),
            IrCall @iv                         => (Reference)iv.Type!,
            IrConv @cv                         => (Reference)cv.Type!,
            _                                  => throw new UnreachableException(),
        };
    private ITypeReference SolveTypeLazy2(Reference typeRef, IrBlockExecutionContextData? ctx, LangObject? obj)
    {
        switch (typeRef)
        {
            case UnknownReference @unsolved: return SolveTypeReference(new UnknownReference(unsolved.SyntaxNode), null, obj); break;
            
            case SliceTypeReference @slice: slice.ElementType           = (Reference)SolveTypeLazy2(@slice.ElementType, ctx, obj); break;
            case ReferenceTypeReference @refer: refer.InternalType      = (Reference)SolveTypeLazy2(@refer.InternalType, ctx, obj); break;
            case NullableTypeReference @nullable: nullable.InternalType = (Reference)SolveTypeLazy2(@nullable.InternalType, ctx, obj); break;
        }
        
        return (ITypeReference)typeRef;
    }
    
}
