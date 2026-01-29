using System.Diagnostics;
using System.Numerics;
using Abstract.CodeProcess.Core;
using Abstract.CodeProcess.Core.EvaluationData;
using Abstract.CodeProcess.Core.EvaluationData.IntermediateTree;
using Abstract.CodeProcess.Core.EvaluationData.IntermediateTree.Expressions;
using Abstract.CodeProcess.Core.EvaluationData.IntermediateTree.Statements;
using Abstract.CodeProcess.Core.EvaluationData.IntermediateTree.Values;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.Attributes;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.CodeObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.CodeReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.FunctionReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin.Integer;
using Abstract.CodeProcess.Core.EvaluationData.Misc;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Value;

namespace Abstract.CodeProcess;

/*
 * Stage Four:
 *  Semantic analysis, solving automatic type inference, type conversion,
 *  operator overloading, function overloading, etc.
 */

public partial class Analyser
{
    private HashSet<LangObject> _analyzedObjects = [];
    
    private void DoSemanticAnalysis()
    {
        List<FunctionObject> funclist = [];
        List<ConstructorObject> ctorList = [];
        List<DestructorObject> dtorList = [];
        List<FieldObject> fldlist = [];

        foreach (var (_, i) in _globalReferenceTable)
        {
            switch (i)
            {
                case NamespaceObject nmsp: NamespaceSemaAnal(nmsp); break;
                case FunctionGroupObject group: funclist.AddRange(group.Overloads); break;
                case FunctionObject f: funclist.Add(f); break;
                case FieldObject f: fldlist.Add(f); break;
                case StructObject s:
                    ctorList.AddRange(s.Constructors);
                    dtorList.AddRange(s.Destructors);
                    break;
            }
        }
        
        // Header analysis
        foreach (var obj in _globalReferenceTable.Values)
        {
            switch (obj)
            {
                case FunctionGroupObject @functionGroup:
                    foreach (var fun in @functionGroup.Overloads) FunctionSemaAnal(fun);
                    break;
                
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
        
        // Excution analysis
        foreach (var fld in fldlist)
        {
            if (fld.Value == null) continue;
            var ctx = new IrBlockExecutionContextData(fld);
            fld.Value = (IrExpression)NodeSemaAnal(fld.Value, ctx);
            _analyzedObjects.Add(fld);
        }
        
        foreach (var fun in funclist)
        {
            var ctx = new IrBlockExecutionContextData(fun);
            if (fun.Body != null) BlockSemaAnal(fun.Body, ctx);
            _analyzedObjects.Add(fun);
        }

        foreach (var ctor in ctorList)
        {
            var ctx = new IrBlockExecutionContextData(ctor);
            if (ctor.Body != null) BlockSemaAnal(ctor.Body, ctx);
            _analyzedObjects.Add(ctor);
        }
        
        foreach (var dtor in dtorList)
        {
            var ctx = new IrBlockExecutionContextData(dtor);
            if (dtor.Body != null) BlockSemaAnal(dtor.Body, ctx);
            _analyzedObjects.Add(dtor);
        }
    }

    private void NamespaceSemaAnal(NamespaceObject nmsp)
    {
    }
    private void FunctionSemaAnal(FunctionObject function)
    {
        foreach (var i in function.Parameters)
        {
            if (IsSolved(i.Type)) continue;
            i.Type = SolveTypeLazy2(i.Type, null, function);
        }

        foreach (var i in function.Locals)
        {
            if (i.Type == null || IsSolved(i.Type)) continue;
            i.Type = SolveTypeLazy2(i.Type, null, function);
        }
        
        if (!IsSolved(function.ReturnType))
            function.ReturnType = SolveTypeLazy2(function.ReturnType, null, function.Container);
    }
    private void CtorSemaAnal(ConstructorObject ctor)
    {
        foreach (var i in ctor.Parameters)
        {
            if (IsSolved(i.Type)) continue;
            i.Type = SolveTypeLazy2(i.Type, null, ctor.Container);
        }

        foreach (var i in ctor.Locals)
        {
            if (i.Type == null || IsSolved(i.Type)) continue;
            i.Type = SolveTypeLazy2(i.Type, null, ctor.Container);
        }
        
        if (!IsSolved(ctor.ReturnTypeOverride))
            ctor.ReturnTypeOverride = SolveTypeLazy2(ctor.ReturnTypeOverride, null, ctor.Container);
    }
    private void DtorSemaAnal(DestructorObject dtor)
    {
        foreach (var i in dtor.Parameters)
        {
            if (IsSolved(i.Type)) continue;
            i.Type = SolveTypeLazy2(i.Type, null, dtor.Container);
        }

        foreach (var i in dtor.Locals)
        {
            if (i.Type == null || IsSolved(i.Type)) continue;
            i.Type = SolveTypeLazy2(i.Type, null, dtor.Container);
        }
    }
    
    private void StructureSemaAnal(StructObject structure)
    {
        foreach (var i in structure.Fields) FieldSemaAnal(i);
        foreach (var i in structure.Constructors) CtorSemaAnal(i);
        foreach (var i in structure.Destructors) DtorSemaAnal(i);

        if (structure is { Abstract: false, Constructors.Count: 0 })
        {
            var defaultCtor = new ConstructorObject(null!)
            {
                Body = new IrBlock(null!),
            };
            structure.Constructors.Add(defaultCtor);
        }
    }
    private void FieldSemaAnal(FieldObject field)
    {
        if (IsSolved(field.Type)) return;
        field.Type = SolveTypeLazy2(field.Type, null, field);
    }

    
    private void BlockSemaAnal(IrBlock block, IrBlockExecutionContextData ctx, bool newFrame = true)
    {
        if (newFrame) ctx.PushFrame();
        for (var i = 0; i < block.Content.Count; i++) block.Content[i] = NodeSemaAnal(block.Content[i], ctx);
        if (newFrame) ctx.PopFrame();
    }

    private IrNode NodeSemaAnal(IrNode node, IrBlockExecutionContextData ctx)
    {
        return node switch
        {
            IrInvoke @iv => NodeSemaAnal_Invoke(iv, ctx),
            IrDotnetInvoke @iv => NodeSemaAnal_DotnetInvoke(@iv, ctx),
            IRAssign @ass => NodeSemaAnal_Assign(ass, ctx),
            IRUnaryExp @ue => NodeSemaAnal_UnExp(ue, ctx),
            IRBinaryExp @be => NodeSemaAnal_BinExp(be, ctx),
            IRCompareExp @ce => NodeSemaAnal_CmpExp(ce, ctx),
            IrLogicalExp @ce => NodeSemaAnal_LogicalExp(ce, ctx),
            IrIndex @ix => NodeSemaAnal_Index(ix, ctx),
            IrConv @tc =>NodeSemaAnal_Conv(tc, ctx),
            IrNewObject @no => NodeSemaAnal_NewObj(no, ctx),
            IrReturn @re => NodeSemaAnal_Return(re, ctx),
            IRIf @iff => NodeSemaAnal_If(iff, ctx),
            IRWhile @iwhile => NodeSemaAnal_While(iwhile, ctx),
            IrSolvedReference @re => NodeSemaAnal_SolvedRef(re, ctx),
            
            IrCharLiteral
            or IrStringLiteral
            or IrIntegerLiteral 
            or IRBooleanLiteral
            or IRNullLiteral => node,
            
            IRAccess @s => NodeSemaAnal_Access(s, ctx),
            IrCollectionLiteral @c => NodeSemaAnal_Collection(c, ctx),
            IRUnknownReference @u => SolveReferenceLazy(u, ctx, null),
            
            _ => throw new NotImplementedException(),
        };
    }

    private IrNode NodeSemaAnal_SolvedRef(IrSolvedReference re, IrBlockExecutionContextData ctx)
    {
        switch (re.Reference)
        {
            case LocalReference @lr:
                ctx.LocalVariables.Add(lr.Local);
                return re;
            
            default: return re;       
        }
    }
    private IrNode NodeSemaAnal_Invoke(IrInvoke node, IrBlockExecutionContextData ctx)
    {
        node.Target = (IrExpression)NodeSemaAnal(node.Target, ctx);
        var fng = ((SolvedFunctionGroupReference)ReferenceOf(node.Target)).FunctionGroup;

        IrExpression? instance = null;
        if (node.Target is IRAccess @irAccess)
        {
            instance = irAccess.A;
            node.Target = irAccess.B;
        }
        
        for (var i = 0; i < node.Arguments.Length; i++)
            node.Arguments[i] = (IrExpression)NodeSemaAnal(node.Arguments[i], ctx);

        var res = SolveFunctionOverload(
            fng.Overloads.ToArray<ICallable>(), node.Arguments, node.Origin);
        switch (res)
        {
            case NoOverloadResult: throw new Exception($"Could not find suitable overload for call '{node.Origin}'");

            case SimpleOverloadResult @s:
            {
                var newArgs =  new IrExpression[s.Callable.Parameters.Count];
                for (var i = 0; i < newArgs.Length; i++)
                    newArgs[i] = SolveTypeCast(s.Callable.Parameters[i].Type, node.Arguments[i]);
                
                node.Arguments = newArgs;
                node.Target = new IrSolvedReference(node.Target.Origin, new SolvedCallableReference(s.Callable));
                if (instance != null) node.Arguments = [(IrExpression)NodeSemaAnal(instance, ctx), ..node.Arguments];
                return node;
            }

            case GenericOverloadResult @g:
            {
                List<IrExpression> newArgs = [];
                for (var i = 0; i < node.Arguments.Length; i++)
                {
                    if (g.Types[i] is IgnoreTypeReference) continue;
                    newArgs.Add(SolveTypeCast(g.Types[i], node.Arguments[i]));
                }

                node.Arguments = [.. newArgs];
                node.Target = new IrSolvedReference(node.Target.Origin, new SolvedCallableReference(g.Callable));
                if (instance != null) node.Arguments = [(IrExpression)NodeSemaAnal(instance, ctx), ..node.Arguments];
                return node;
            }

            case DotnetOverloadResult @d:
            {
                var args = new IrExpression[d.Parameters.Length];
                for (var i = d.Generics.Length; i < node.Arguments.Length; i++)
                    args[i - d.Generics.Length] = SolveTypeCast(d.Parameters[i - d.Generics.Length], node.Arguments[i]);
                
                return new IrDotnetInvoke(
                    node.Target.Origin,
                    new IrSolvedReference(node.Target.Origin, new SolvedCallableReference(d.Callable)),
                    d.Generics,
                    args);
            }
            
            default: throw new NotImplementedException(res.ToString());
        }
    }
    private IrNode NodeSemaAnal_DotnetInvoke(IrDotnetInvoke node, IrBlockExecutionContextData ctx)
    {
        node.Target = (IrExpression)NodeSemaAnal(node.Target, ctx);
        
        for (var i = 0; i < node.Arguments.Length; i++)
            node.Arguments[i] = (IrExpression)NodeSemaAnal(node.Arguments[i], ctx);

        return node;
    }
    private IrNode NodeSemaAnal_NewObj(IrNewObject node, IrBlockExecutionContextData ctx)
    {
        node.Target = (IrReference)NodeSemaAnal(node.Target, ctx);
        var instanceTypeRef = GetEffectiveTypeReference(node.Target);
        if (instanceTypeRef is UnsolvedTypeReference) throw new Exception($"Not able to resolve reference to '{node.Origin}'");
        if (instanceTypeRef is not SolvedStructTypeReference @r) throw new Exception($"Cannot instantiate type {node.Origin} as an object");
        node.InstanceType = r.Struct;
        
        for (var i = 0; i < node.Arguments.Length; i++)
            node.Arguments[i] = (IrExpression)NodeSemaAnal(node.Arguments[i], ctx);
        
        var res = SolveFunctionOverload(
            node.InstanceType.Constructors.ToArray<ICallable>(), node.Arguments, node.Origin);
        switch (res)
        {
            case NoOverloadResult: throw new Exception($"Could not find suitable overload for call '{node.Origin}'");

            case SimpleOverloadResult @s:
            {
                var newArgs =  new IrExpression[s.Callable.Parameters.Count];
                for (var i = 0; i < newArgs.Length; i++)
                    newArgs[i] = SolveTypeCast(s.Callable.Parameters[i].Type, node.Arguments[i]);
                
                node.Arguments = newArgs;
                node.Target = new IrSolvedReference(node.Target.Origin, new SolvedCallableReference(s.Callable));
            } break;

            case GenericOverloadResult @g:
            {
                List<IrExpression> newArgs = [];
                for (var i = 0; i < node.Arguments.Length; i++)
                {
                    if (g.Types[i] is IgnoreTypeReference) continue;
                    newArgs.Add(SolveTypeCast(g.Types[i], node.Arguments[i]));
                }

                node.Arguments = [.. newArgs];
                node.Target = new IrSolvedReference(node.Target.Origin, new SolvedCallableReference(g.Callable));
            } break;
                
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
    private IrNode NodeSemaAnal_Assign(IRAssign node, IrBlockExecutionContextData ctx)
    {
        var a = node;
        node.Target = (IrExpression)NodeSemaAnal(node.Target, ctx);
        node.Value = (IrExpression)NodeSemaAnal(node.Value, ctx);

        if (node.Target is IrSolvedReference { Reference: LocalReference { Type: null } @l })
        {
            var typefrom = GetEffectiveTypeReference(node.Value);
            if (typefrom is ComptimeIntegerTypeReference) typefrom = new RuntimeIntegerTypeReference(true);
            
            l.Local.Type = typefrom;
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
    private IrNode NodeSemaAnal_BinExp(IRBinaryExp node, IrBlockExecutionContextData ctx)
    {
        node.Left = (IrExpression)NodeSemaAnal(node.Left, ctx);
        node.Right = (IrExpression)NodeSemaAnal(node.Right, ctx);
        var leftTypeRef = GetEffectiveTypeReference(node.Left);
        TypeReference ftype = new VoidTypeReference();

        if (leftTypeRef is ComptimeIntegerTypeReference
            && GetEffectiveTypeReference(node.Right) is ComptimeIntegerTypeReference)
        {
            ftype = new ComptimeIntegerTypeReference();
            goto skipTypeCheck;
        }

        // TODO solve operator overloading

        switch (node.Operator)
        {

            case IRBinaryExp.Operators.LeftShift:
            case IRBinaryExp.Operators.RightShift:
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

            case StringTypeReference @sl when
                rtype is StringTypeReference @sr:
            {
                if (sl.Encoding == sr.Encoding) ftype = new StringTypeReference(sl.Encoding);
                else throw new Exception("Cannot automatically concatenate strings with different encoding");
            } break;

            default: throw new NotImplementedException();
        }

        node.ResultType = ftype;
        
        skipTypeCheck:
        // Operate literals at comptime
        return node switch
        {
            { Left: IrIntegerLiteral @leftInt, Right: IrIntegerLiteral @rightInt } => node.Operator switch
            {
                _ => new IrIntegerLiteral(node.Origin, node.Operator switch
                    {
                        IRBinaryExp.Operators.Add => leftInt.Value + rightInt.Value,
                        IRBinaryExp.Operators.Subtract => leftInt.Value - rightInt.Value,
                        IRBinaryExp.Operators.Multiply => leftInt.Value * rightInt.Value,
                        IRBinaryExp.Operators.Divide => leftInt.Value / rightInt.Value,
                        IRBinaryExp.Operators.Reminder => leftInt.Value % rightInt.Value,

                        IRBinaryExp.Operators.BitwiseAnd => leftInt.Value & rightInt.Value,
                        IRBinaryExp.Operators.BitwiseOr => leftInt.Value | rightInt.Value,
                        IRBinaryExp.Operators.BitwiseXor => leftInt.Value ^ rightInt.Value,
                        IRBinaryExp.Operators.LeftShift => leftInt.Value << (int)rightInt.Value,
                        IRBinaryExp.Operators.RightShift => leftInt.Value >> (int)rightInt.Value,

                        _ => throw new NotImplementedException(),
                    }, (IntegerTypeReference)ftype),
            },
            
            { Left: IrStringLiteral @leftStr, Right: IrStringLiteral @rightStr } => new IrStringLiteral(node.Origin,
                node.Operator switch
                {
                    IRBinaryExp.Operators.Add => leftStr.Data + rightStr.Data,
                    _ => throw new UnreachableException()
                }),
            _ => node
        };
    }
    private IrNode NodeSemaAnal_CmpExp(IRCompareExp node, IrBlockExecutionContextData ctx)
    {
        node.Left = (IrExpression)NodeSemaAnal(node.Left, ctx);
        var leftTypeRef = GetEffectiveTypeReference(node.Left);
        node.Right = SolveTypeCast(leftTypeRef, (IrExpression)NodeSemaAnal(node.Right, ctx));
        
        switch (node)
        {
            case { Left: IrIntegerLiteral @leftInt, Right: IrIntegerLiteral @rightInt }:
                return new IRBooleanLiteral(node.Origin, node.Operator switch
                {
                    IRCompareExp.Operators.GreaterThan => leftInt.Value > rightInt.Value,
                    IRCompareExp.Operators.LessThan => leftInt.Value < rightInt.Value,
                    IRCompareExp.Operators.LessThanOrEqual => leftInt.Value <= rightInt.Value,
                    IRCompareExp.Operators.GreaterThanOrEqual => leftInt.Value >= rightInt.Value,
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
                node.ResultType = s.InternalType;
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
        node.Value = SolveTypeCast(function.ReturnType!, node.Value, false);
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

    private IrNode NodeSemaAnal_While(IRWhile node, IrBlockExecutionContextData ctx)
    {
        if (node.Define != null) BlockSemaAnal(node.Define, ctx, false);
        node.Condition = (IrExpression)NodeSemaAnal(node.Condition, ctx);
        if (node.Step != null) BlockSemaAnal(node.Step, ctx);
        BlockSemaAnal(node.Process, ctx);
        
        return node;
    }

    private IrNode NodeSemaAnal_Access(IRAccess node, IrBlockExecutionContextData ctx)
    {
        return SolveAccessInExpression(node.Origin, (IrExpression)NodeSemaAnal(node.A, ctx), (IRUnknownReference)node.B);
    }
    private IrNode NodeSemaAnal_Collection(IrCollectionLiteral node, IrBlockExecutionContextData ctx)
    {
        var items = node.Items.Select(i => (IrExpression)NodeSemaAnal(i, ctx)).ToArray();
        return new IrCollectionLiteral(node.Origin, node.ElementType, items);
    }

    /// <summary>
    /// Will search between the provided callables for the best fit based on the provided argument
    /// expressions.
    /// If the best option is a generic function, it will request that this generic gets baked and
    /// will return the new non-static version of it.
    /// It also returns an array of type references, that represents the types that must be used when
    /// calling the resulted callable. In some cases, it may return `IgnoreTypeReference`, that represents
    /// a argument that must be skipped when generating the final invocation IR node.
    /// </summary>
    /// <param name="options"> The callable overloads to search </param>
    /// <param name="arguments"> The arguments provided in the call </param>
    /// <returns>
    /// Item1 = better option found, null if no option;
    /// Item2 = final argument types or ignored arguments
    /// </returns>
    private ISolvedOverloadResult SolveFunctionOverload(ICallable[] options, IrExpression[] arguments, SyntaxNode origin)
    {
        ICallable? betterFound = null;
        var betterFoundSum = 0;
        Dictionary<ParameterObject, TypeReference>? betterFoundGenerics = null;
        TypeReference[]? betterFoundArgTypes = null;
        
        foreach (var ov in options)
        {
            if (arguments.Length != ov.Parameters.Count) continue;
            if (ov.Parameters.Count == 0)
            {
                betterFound = ov;
                betterFoundSum = 0;
                betterFoundArgTypes = [];
                continue;
            }
            
            var parameters = ov.Parameters;
            var argTypes = new TypeReference[parameters.Count];
            var generics = new Dictionary<ParameterObject, TypeReference?>();
            var suitability = new int[parameters.Count];

            for (var i = 0; i < parameters.Count; i++)
            {
                var argt = GetEffectiveTypeReference(arguments[i], (LangObject)ov);
                argTypes[i] = argt;

                switch (parameters[i].Type)
                {
                    case TypeTypeReference when argt is not GenericTypeReference:
                        generics[parameters[i]] = ((TypeTypeReference)argt).ReferencedType;
                        suitability[i] = (int)Suitability.NeedsSoftCast;
                        break;

                    case TypeTypeReference when argt is GenericTypeReference @argtg:
                    {
                        generics[parameters[i]] = argtg;
                        suitability[i] = (int)Suitability.NeedsSoftCast;
                    } break;
                    
                    case GenericTypeReference @gen:
                    {
                        var s = (int)CalculateTypeSuitability(generics[gen.Parameter]!, argt, true);
                        if (s == 0) goto NoSuitability;
                        suitability[i] = s;
                    } break;

                    default:
                    {
                        var s = (int)CalculateTypeSuitability(parameters[i].Type, argt, true);
                        if (s == 0) goto NoSuitability;
                        suitability[i] = s;
                    } break;
                }
            }

            var sum = (suitability.Sum() * 100) / parameters.Count;
            if (sum <= betterFoundSum) continue;
            betterFound = ov;
            betterFoundSum = sum;
            betterFoundArgTypes = argTypes;
            betterFoundGenerics = generics;

            NoSuitability: ;
        }

        if (betterFound == null || betterFoundArgTypes == null) return new NoOverloadResult();
        if (!betterFound.IsGeneric) return new SimpleOverloadResult(betterFound);

        if (betterFound is FunctionObject { DotnetImport: not null } or ConstructorObject { DotnetImport: not null })
        {
            var generics = new List<TypeReference>();

            for (var i = 0; i < arguments.Length; i++)
            {
                if (betterFound.Parameters[i].Type is TypeTypeReference)
                {
                    generics.Add(betterFoundArgTypes[i] is TypeTypeReference @ttr
                        ? ttr.ReferencedType ?? new TypeTypeReference(null)
                        : betterFoundArgTypes[i]);
                }
                else break;
            }
            
            var firstParam = betterFound.Parameters.FindIndex(e => e.Type is not TypeTypeReference);
            var argtypes = betterFound.Parameters[firstParam..].Select(e => e.Type);

            return new DotnetOverloadResult(betterFound, [.. generics], [.. argtypes]);
        }
        
        var inputTypes = new TypeReference[betterFound.Parameters.Count];
        for (var i = 0; i < betterFound.Parameters.Count; i++)
        {
            var srcType = betterFound.Parameters[i].Type;
            inputTypes[i] = srcType is TypeTypeReference
                ? ((TypeTypeReference)betterFoundArgTypes[i]).ReferencedType!
                : srcType;
        }

        return GenerateSolvedGenericCallable(betterFound,  inputTypes);
    }
    private IrExpression SolveAccessInExpression(SyntaxNode origin, IrExpression accessBase, IRUnknownReference accessMember)
    {
        var baseRef = ReferenceOf(accessBase);
        var accessName = ((IdentifierNode)accessMember.Origin).Value;
        var typeref = baseRef.Type;

        return typeref switch
        {
            SliceTypeReference @sliceBuiltin => new IrLenOf(origin, accessBase),
            
            TypeTypeReference @tt => tt.ReferencedType switch {
                SolvedTypedefTypeReference @solvedTypedef => solvedTypedef.Typedef.SearchChild(accessName) is {} @refe
                    ? new IrSolvedReference(origin, GetObjectReference(refe))
                    : new IRUnknownReference(origin),
                
                _ => throw new NotImplementedException(),
            },
            
            _ => throw new NotImplementedException(),
        };
    }
    private IrNode SolveReferenceLazy(IRUnknownReference node, IrBlockExecutionContextData? ctx, LangObject? reference)
    {
        var syntaxNode = node.Origin;
        var parent = reference;
        if (ctx == null && parent == null) throw new UnreachableException();
        if (parent == null && ctx != null) parent = ctx.Parent;
        
        switch (syntaxNode)
        {
            case IdentifierNode @idnode:
            {
                // Search in local variables and parameters
                if (ctx != null)
                {
                    var r = ctx.LocalVariables.FirstOrDefault(e => e.Name == idnode.Value);
                    if (r != null) return new IrSolvedReference(syntaxNode, new LocalReference(r));
                    
                    var r2 = (ctx.Parent as ICallable)?.Parameters.FirstOrDefault(e => e.Name == idnode.Value);
                    if (r2 != null) return new IrSolvedReference(syntaxNode, new ParameterReference(r2));
                }
                
                // Search in inherited
                if (parent?.Container is StructObject @structObject)
                {
                    LangObject? curr2 = structObject;
                    do
                    {
                        var r3 = curr2.SearchChild(idnode.Value);
                        if (r3 != null)
                        {
                            var refeNode = new IrSolvedReference(syntaxNode, GetObjectReference(r3));
                            return r3 is IStaticModifier { Static: false }
                                ? new IRAccess(syntaxNode, new IrSolvedReference(syntaxNode, new SelfReference()), refeNode) : refeNode;
                        }
                        curr2 = ((curr2 as StructObject)?.Extends as SolvedStructTypeReference)?.Struct;
                    } while (curr2 != null && curr2 is not NamespaceObject);
                }
                
                // Search inside namespace
                var r4 = parent?.Namespace?.SearchChild(idnode.Value);
                if (r4 != null) return new IrSolvedReference(syntaxNode, GetObjectReference(r4));

                // Search inside imports
                var r5 = parent?.Imports?.References.FirstOrDefault(e => e.Name == idnode.Value);
                if (r5 != null) return new IrSolvedReference(syntaxNode, GetObjectReference(r5));
                
                // Search global references
                var r6 = _globalReferenceTable.FirstOrDefault(e => e.Key.Length == 1 && e.Key[0] == idnode.Value);
                if (r6.Key != null) return new IrSolvedReference(syntaxNode, GetObjectReference(r6.Value));

                if (parent is NamespaceObject @nmsp)
                {
                    string[] name = [.. nmsp.Global, idnode.Value];
                    var r7 = Enumerable.FirstOrDefault<KeyValuePair<string[], LangObject>>(_globalReferenceTable, e => IdentifierComparer.IsEquals(e.Key, name));
                    if (r7.Key != null) return new IrSolvedReference(syntaxNode, GetObjectReference(r7.Value));
                }
                
                throw new Exception($"Cannot find reference to {idnode:pos}");
            }
            
            default: throw new UnreachableException();
        }
    }
    private ISolvedOverloadResult GenerateSolvedGenericCallable(ICallable baseCallable, TypeReference[] inputArgTypes)
    {
        switch (baseCallable)
        {
            case FunctionObject @function:
            {
                var newFunc = new FunctionObject($"{function.Name}__impl", function.SyntaxNode)
                {
                    IsGeneric = false,
                    Static = function.Static,
                    ConstExp = function.ConstExp,
                    Abstract = function.Abstract,
                    Override = function.Override,
                    Virtual = function.Virtual,
                    Public = function.Public,
                    Internal = function.Internal,
                };
                
                var genericTypes = new List<TypeReference?>();
                var parameters = new List<ParameterObject?>();
                var argTypes = new List<TypeReference>();
                
                foreach (var i in function.Parameters)
                {
                     ParameterObject? p;
                     switch (i.Type)
                     {
                         case TypeTypeReference:
                             genericTypes.Add(inputArgTypes[i.Index]);
                             argTypes.Add(new IgnoreTypeReference());
                             parameters.Add(null);
                             continue;
                         
                         case GenericTypeReference @gen: p = new ParameterObject(inputArgTypes[gen.Parameter.Index], i.Name); break;
                         default: p = new ParameterObject(i.Type, i.Name); break;
                     }
                     
                     newFunc.Parameters.Add(p);
                     parameters.Add(p);
                     
                     genericTypes.Add(null);
                     argTypes.Add(p.Type);
                }
                
                foreach (var i in function.Locals)
                {
                    switch (i.Type)
                    {
                        case GenericTypeReference @gen:
                            newFunc.Locals.Add(new LocalVariableObject(inputArgTypes[gen.Parameter.Index], i.Name));
                            break;
                        
                        default:
                            newFunc.Locals.Add(new LocalVariableObject(i.Type, i.Name));
                            break;
                    }
                }
                newFunc.ReturnType = SolveGenericOrDefault(function.ReturnType, inputArgTypes);
                
                function.ParentGroup.Overloads.Add(newFunc);
                newFunc.Parent = function.Parent;
                newFunc.ParentGroup = function.ParentGroup;
                newFunc.Imports = function.Imports;
                
                if (!_analyzedObjects.Contains(function))
                {
                    var ctx = new IrBlockExecutionContextData(function);
                    BlockSemaAnal(function.Body!, ctx);
                    _analyzedObjects.Add(function);
                }
                _analyzedObjects.Add(newFunc);
                
                newFunc.Body = (IrBlock)RebuildGenericImplTreeRecursive(
                    function.Body!,
                    [..genericTypes],
                    [..parameters!],
                    [..newFunc.Locals]);
                
                return new GenericOverloadResult(newFunc, [..argTypes]);

                TypeReference SolveGenericOrDefault(TypeReference type, TypeReference?[] genTypes)
                {
                    switch (type)
                    {
                        case SliceTypeReference @slice:
                            return new SliceTypeReference(SolveGenericOrDefault(slice.InternalType, genTypes));
                        
                        case ReferenceTypeReference @refe:
                            return new ReferenceTypeReference(SolveGenericOrDefault(@refe.Type, genTypes));
                    }
                    
                    if (type is GenericTypeReference @g)
                        return genTypes[g.Parameter.Index]!;

                    return type;
                }
            }
            
            default: throw new NotImplementedException();
        }
    }

    
    private IrNode RebuildGenericImplTreeRecursive(
        IrNode node,
        TypeReference?[] genericTypes,
        ParameterObject[] parameters,
        LocalVariableObject[] locals)
    {
        switch (node)
        {
            case IrBlock @block:
            {
                var b = new IrBlock(block.Origin);
                foreach (var i in block.Content)
                    b.Content.Add(RebuildGenericImplTreeRecursive(i, genericTypes, parameters, locals));
                return b;
            }

            case IrReturn @ret:
                return new IrReturn(ret.Origin, ret.Value != null
                    ? (IrExpression)RebuildGenericImplTreeRecursive(ret.Value, genericTypes, parameters, locals)
                    : null);

            case IrConv @conv:
            {
                var ty = conv.Type switch
                {
                    GenericTypeReference @gen => genericTypes[gen.Parameter.Index]!,
                    _ => conv.Type
                };
                
                return new IrConv(conv.Origin,
                    (IrExpression)RebuildGenericImplTreeRecursive(conv.Expression, genericTypes, parameters, locals),
                    ty);
            }

            case IrInvoke @invoke:
            {
             return new IrInvoke(invoke.Origin,
                 (IrExpression)RebuildGenericImplTreeRecursive(invoke.Target, genericTypes, parameters, locals),
                 invoke.Arguments.Select(e => (IrExpression)RebuildGenericImplTreeRecursive(e, genericTypes, parameters, locals)).ToArray());
            }
            case IrDotnetInvoke @invoke:
            {
                var generics = new TypeReference[invoke.Generics.Length];
                for (var i = 0; i < generics.Length; i++)
                {
                    if (invoke.Generics[i] is GenericTypeReference @gen)
                        generics[i] = genericTypes[gen.Parameter.Index]!;
                    else generics[i] = invoke.Generics[i];
                }
                
                return new IrDotnetInvoke(invoke.Origin,
                    (IrExpression)RebuildGenericImplTreeRecursive(invoke.Target, genericTypes, parameters, locals),
                    [.. generics],
                    invoke.Arguments.Select(e => (IrExpression)RebuildGenericImplTreeRecursive(e, genericTypes, parameters, locals)).ToArray());
            }
            
            case IrSolvedReference @r:
            {
                return r.Reference switch
                {
                    LocalReference @a => new IrSolvedReference(r.Origin, new LocalReference(locals[a.Local.index])),
                    ParameterReference @a => a.Type is TypeTypeReference
                        ? new IrSolvedReference(r.Origin, new TypeTypeReference(a.Type))
                        : new IrSolvedReference(r.Origin, new ParameterReference(parameters[a.Parameter.Index])),
                    SolvedCallableReference @c => new IrSolvedReference(r.Origin, c),
                    _ => throw new NotImplementedException()
                };
            } break;
            
            case IRBooleanLiteral @b: return new IRBooleanLiteral(b.Origin, b.Value);
            
            default: throw new NotImplementedException();
        }
    }
    
    
    private LanguageReference ReferenceOf(IrNode node) => node switch
        {
            IRAccess @acc => ReferenceOf(acc.B),
            IrSolvedReference @sr => sr.Reference,
            IrInvoke @iv => iv.Type!,
            _ => throw new UnreachableException(),
        };
    private TypeReference SolveTypeLazy2(TypeReference typeref, IrBlockExecutionContextData? ctx, LangObject? obj)
    {
        switch (typeref)
        {
            case UnsolvedTypeReference @unsolved: typeref = SolveTypeLazy(new UnsolvedTypeReference(unsolved.SyntaxNode), null, obj); break;
            case SliceTypeReference @slice: slice.InternalType = SolveTypeLazy2(@slice.InternalType, ctx, obj); break;
            case ReferenceTypeReference @refer: refer.InternalType = SolveTypeLazy2(@refer.InternalType, ctx, obj); break;
            case NullableTypeReference @nullable: nullable.InternalType = SolveTypeLazy2(@nullable.InternalType, ctx, obj); break;
        }
        
        return typeref;
    }
    
}
