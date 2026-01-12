using System.Diagnostics;
using System.Numerics;
using Abstract.CodeProcess.Core;
using Abstract.CodeProcess.Core.Language.EvaluationData;
using Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree;
using Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Expressions;
using Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Statements;
using Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Values;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.CodeReferences;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.FunctionReferences;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences.Builtin;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences.Builtin.Integer;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Value;

namespace Abstract.CodeProcess;

/*
 * Stage Four:
 *  Semantic analysis, solving automatic type inference, type conversion,
 *  operator overloading, function overloading, etc.
 */

public partial class Analyzer
{
    private void DoSemanticAnalysis()
    {
        List<FunctionObject> funclist = [];
        List<FieldObject> fldlist = [];

        foreach (var (_, i) in _globalReferenceTable)
        {
            switch (i)
            {
                case NamespaceObject nmsp: NamespaceSemaAnal(nmsp); break;
                case FunctionGroupObject group: funclist.AddRange(group.Overloads); break;
                case FunctionObject f: funclist.Add(f); break;
                case FieldObject f: fldlist.Add(f); break;
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
        }
        
        foreach (var fun in funclist)
        {
            var ctx = new IrBlockExecutionContextData(fun);
            if (fun.Body != null) BlockSemaAnal(fun.Body, ctx);
        }
    }

    private void NamespaceSemaAnal(NamespaceObject nmsp)
    {
    }
    private void FunctionSemaAnal(FunctionObject function)
    {

        if (!IsSolved(function.ReturnType))
            function.ReturnType = SolveTypeLazy2(function.ReturnType, null, function);
        
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
    }

    private void StructureSemaAnal(StructObject structure)
    {
        uint currentOffset = 0;
        foreach (var i in structure.Children.OfType<FieldObject>())
        {
            FieldSemaAnal(i);
        }
    }
    private void FieldSemaAnal(FieldObject field)
    {
        if (IsSolved(field.Type)) return;
        field.Type = SolveTypeLazy2(field.Type, null, field);
    }

    
    private void BlockSemaAnal(IRBlock block, IrBlockExecutionContextData ctx, bool newFrame = true)
    {
        if (newFrame) ctx.PushFrame();
        for (var i = 0; i < block.Content.Count; i++) block.Content[i] = NodeSemaAnal(block.Content[i], ctx);
        if (newFrame) ctx.PopFrame();
    }

    private IRNode NodeSemaAnal(IRNode node, IrBlockExecutionContextData ctx)
    {
        return node switch
        {
            IRInvoke @iv => NodeSemaAnal_Invoke(iv, ctx),
            IRAssign @ass => NodeSemaAnal_Assign(ass, ctx),
            IRUnaryExp @ue => NodeSemaAnal_UnExp(ue, ctx),
            IRBinaryExp @be => NodeSemaAnal_BinExp(be, ctx),
            IRCompareExp @ce => NodeSemaAnal_CmpExp(ce, ctx),
            IrLogicalExp @ce => NodeSemaAnal_LogicalExp(ce, ctx),
            IrIndex @ix => NodeSemaAnal_Index(ix, ctx),
            IrConv @tc =>NodeSemaAnal_Conv(tc, ctx),
            IRNewObject @no => NodeSemaAnal_NewObj(no, ctx),
            IRReturn @re => NodeSemaAnal_Return(re, ctx),
            IRIf @iff => NodeSemaAnal_If(iff, ctx),
            IRWhile @iwhile => NodeSemaAnal_While(iwhile, ctx),
            
            IRSolvedReference
                or IrCharLiteral
                or IrStringLiteral
                or IrIntegerLiteral 
                or IRNullLiteral => node,
            
            IRAccess @s => NodeSemaAnal_Access(s, ctx),
            IrCollectionLiteral @c => NodeSemaAnal_Collection(c, ctx),
            IRUnknownReference @u => SolveReferenceLazy(u, ctx, null),
            
            _ => throw new NotImplementedException(),
        };
    }
    
    private IRNode NodeSemaAnal_Invoke(IRInvoke node, IrBlockExecutionContextData ctx)
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

        
        node.Target = NodeSemaAnal_Invoke_GetFunctionOverload(fng, node.Arguments, node.Origin);
        //node.Type = ((FunctionTypeReference)node.Target.Type!).Returns;
        //else throw new NotImplementedException();
        
        if (instance != null) node.Arguments = [(IrExpression)NodeSemaAnal(instance, ctx), ..node.Arguments];
        
        return node;
    }

    
    private IRReference NodeSemaAnal_Invoke_GetFunctionOverload(
        FunctionGroupObject group, IrExpression[] arguments, SyntaxNode origin)
    {
        // Node is a function group and must be analysed to point
        // to the correct or most optimal overload

        var overloads = group.Overloads;
        FunctionObject? betterFound = null;
        var betterFoundInstance = false;
        var betterFoundSum = 0;

        var instanceableStruct = group.Parent is StructObject { Static: false };

        foreach (var ov in overloads)
        {
            var instanceFunc = instanceableStruct && !ov.Static;
            
            if (arguments.Length != ov.Parameters.Length) continue;
            if (ov.Parameters.Length == 0)
            {
                betterFound = ov;
                betterFoundSum = 0;
                continue;
            }
            
            var parameters = ov.Parameters;
            var suitability = new int[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var argt = GetEffectiveTypeReference(arguments[i]);
                var s = (int)CalculateTypeSuitability(parameters[i].Type, argt, true);
                if (s == 0) goto NoSuitability;
                suitability[i] = s;
            }

            var sum = (suitability.Sum() * 100) / parameters.Length;
            if (sum <= betterFoundSum) continue;
            betterFound = ov;
            betterFoundSum = sum;
            betterFoundInstance = instanceFunc;

            NoSuitability: ;
        }

        if (betterFound == null)
            throw new Exception($"CompError: No overload that matches function call {origin}");
        

        for (var i = 0; i < arguments.Length; i++)
            arguments[i] = SolveTypeCast(betterFound.Parameters[i].Type, arguments[i]);
        
        var newref = new IRSolvedReference(origin, new SolvedFunctionReference(betterFound));
        return newref;
    }
    private IRNode NodeSemaAnal_Assign(IRAssign node, IrBlockExecutionContextData ctx)
    {
        var a = node;
        node.Target = (IrExpression)NodeSemaAnal(node.Target, ctx);
        node.Value = (IrExpression)NodeSemaAnal(node.Value, ctx);

        if (node.Target is IRSolvedReference { Reference: LocalReference { Type: null } @l })
        {
            var typefrom = GetEffectiveTypeReference(node.Value);
            if (typefrom is ComptimeIntegerTypeReference) typefrom = new RuntimeIntegerTypeReference(true);
            
            l.Local.Type = typefrom;
        }

        var typeto = GetEffectiveTypeReference(node.Target);
        node.Value = SolveTypeCast(typeto, node.Value);
        node.Value = SolveTypeCast(GetEffectiveTypeReference(node.Target), node.Value);
        
        if (node.Value is IRNewObject @newobj)
        {
            // Object instantiation is just a call with its memory as
            // the first argument
            return new IRNewObject(
                node.Origin,
                newobj.InstanceType,
                [node.Target, ..newobj.Arguments],
                newobj.InlineAssignments);
        }
        
        return node;
    }

    private IRNode NodeSemaAnal_UnExp(IRUnaryExp node, IrBlockExecutionContextData ctx)
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
    private IRNode NodeSemaAnal_BinExp(IRBinaryExp node, IrBlockExecutionContextData ctx)
    {
        node.Left = (IrExpression)NodeSemaAnal(node.Left, ctx);
        node.Right = (IrExpression)NodeSemaAnal(node.Right, ctx);
        var leftTypeRef = GetEffectiveTypeReference(node.Left);
        TypeReference ftype = new VoidTypeReference();
        
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
    private IRNode NodeSemaAnal_CmpExp(IRCompareExp node, IrBlockExecutionContextData ctx)
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
    private IRNode NodeSemaAnal_LogicalExp(IrLogicalExp node, IrBlockExecutionContextData ctx)
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
    
    private IRNode NodeSemaAnal_Index(IrIndex node, IrBlockExecutionContextData ctx)
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
    
    private IRNode NodeSemaAnal_Conv(IrConv node, IrBlockExecutionContextData ctx)
    {
        node.Expression = (IrExpression)NodeSemaAnal(node.Expression, ctx);
        return SolveTypeCast(node.Type, node.Expression, node, true);
    }
    private IRNode NodeSemaAnal_NewObj(IRNewObject node, IrBlockExecutionContextData ctx)
    {
        // TODO i prefer handle constructor overloading when
        //  we have actual constructable structures working

        node.InstanceType = SolveTypeLazy2(node.Type, ctx, null);

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
    private IRNode NodeSemaAnal_Return(IRReturn node, IrBlockExecutionContextData ctx)
    {
        if (node.Value == null) return node;
        if (ctx.Parent is not FunctionObject function) throw new InvalidCastException();
        
        node.Value = (IrExpression)NodeSemaAnal(node.Value, ctx);
        node.Value = SolveTypeCast(function.ReturnType!, node.Value, false);
        return node;
    }
    private IRNode NodeSemaAnal_If(IRIf node, IrBlockExecutionContextData ctx)
    {
        node.Condition = (IrExpression)NodeSemaAnal(node.Condition, ctx);
        if (node.Else != null)
            node.Else = (IIfElse)(node.Else is IRIf @if
                ? NodeSemaAnal_If(@if, ctx) : NodeSemaAnal_Else((IRElse)node.Else, ctx));
        BlockSemaAnal(node.Then, ctx);
        return node;
    }
    private IRNode NodeSemaAnal_Else(IRElse node, IrBlockExecutionContextData ctx)
    {
        BlockSemaAnal(node.Then, ctx);
        return node;
    }

    private IRNode NodeSemaAnal_While(IRWhile node, IrBlockExecutionContextData ctx)
    {
        if (node.Define != null) BlockSemaAnal(node.Define, ctx, false);
        node.Condition = (IrExpression)NodeSemaAnal(node.Condition, ctx);
        if (node.Step != null) BlockSemaAnal(node.Step, ctx);
        BlockSemaAnal(node.Process, ctx);
        
        return node;
    }

    private IRNode NodeSemaAnal_Access(IRAccess node, IrBlockExecutionContextData ctx)
    {
        return SolveAccessInExpression(node.Origin, (IrExpression)NodeSemaAnal(node.A, ctx), (IRUnknownReference)node.B);
    }
    private IRNode NodeSemaAnal_Collection(IrCollectionLiteral node, IrBlockExecutionContextData ctx)
    {
        var items = node.Items.Select(i => (IrExpression)NodeSemaAnal(i, ctx)).ToArray();
        return new IrCollectionLiteral(node.Origin, node.ElementType, items);
    }


    private IrExpression SolveAccessInExpression(SyntaxNode origin, IrExpression accessBase, IRUnknownReference accessMember)
    {
        var baseRef = ReferenceOf(accessBase);
        var typeref = baseRef.Type;

        return typeref switch
        {
            SliceTypeReference @sliceBuiltin => new IrLenOf(origin, accessBase),
            _ => throw new UnreachableException(),
        };
    }
    private IRSolvedReference SolveReferenceLazy(IRUnknownReference node, IrBlockExecutionContextData? ctx, LangObject? parent)
    {
        var syntaxNode = node.Origin;
        if (ctx == null && parent == null) throw new UnreachableException();
        if (parent == null && ctx != null) parent = ctx.Parent;

        switch (syntaxNode)
        {
            case IdentifierNode @idnode:
            {
                // Search in local variables
                var r = (parent as FunctionObject)?.Locals.FirstOrDefault(e => e.Name == idnode.Value);
                if (r != null) return new IRSolvedReference(syntaxNode, new LocalReference(r));

                // Search in parameters
                var r2 = (parent as FunctionObject)?.Parameters.FirstOrDefault(e => e.Name == idnode.Value);
                if (r2 != null) return new IRSolvedReference(syntaxNode, new ParameterReference(r2));
                
                // Search in parent tree
                var curr = parent;
                while (curr != null && curr is not NamespaceObject)
                {
                    var r3 = curr.Children.FirstOrDefault(e => e.Name == idnode.Value);
                    if (r3 != null) return new IRSolvedReference(syntaxNode, GetObjectReference(r3));
                    curr = curr.Parent;
                }
                
                // Search in inherited tree
                if (parent is StructObject { Extends: SolvedStructTypeReference } @structObject)
                {
                    LangObject? curr2 = ((SolvedStructTypeReference)structObject.Extends).Struct;
                    while (curr2 != null && curr2 is not NamespaceObject)
                    {
                        var r3 = curr2.Children.FirstOrDefault(e => e.Name == idnode.Value);
                        if (r3 != null) return new IRSolvedReference(syntaxNode, GetObjectReference(r3));
                        curr2 = curr2.Parent;
                    }
                }

                // Search inside namespace
                var r4 = parent.Namespace.Children.FirstOrDefault(e => e.Name == idnode.Value);
                if (r4 != null) return new IRSolvedReference(syntaxNode, GetObjectReference(r4));

                // Search inside imports
                var r5 = parent.Imports?.References.FirstOrDefault(e => e.Name == idnode.Value);
                if (r5 != null) return new IRSolvedReference(syntaxNode, GetObjectReference(r5));
                
                // Search global references
                var r6 = _globalReferenceTable.FirstOrDefault(e => e.Key.Length == 1 && e.Key[0] == idnode.Value);
                if (r6.Key != null) return new IRSolvedReference(syntaxNode, GetObjectReference(r6.Value));

                if (parent is NamespaceObject @nmsp)
                {
                    string[] name = [.. nmsp.Global, idnode.Value];
                    var r7 = _globalReferenceTable.FirstOrDefault(e => IdentifierComparer.IsEquals(e.Key, name));
                    if (r7.Key != null) return new IRSolvedReference(syntaxNode, GetObjectReference(r7.Value));
                }
                
                throw new Exception($"Cannot find reference to {idnode:pos}");
            }
            
            default: throw new UnreachableException(); break;
        }
    }

    private LanguageReference ReferenceOf(IRNode node) => node switch
        {
            IRAccess @acc => ReferenceOf(acc.B),
            IRSolvedReference @sr => sr.Reference,
            IRInvoke @iv => iv.Type!,
            _ => throw new UnreachableException(),
        };
    
    private TypeReference SolveTypeLazy2(TypeReference typeref, IrBlockExecutionContextData? ctx, LangObject? obj)
    {
        switch (typeref)
        {
            case UnsolvedTypeReference @unsolved: typeref = (TypeReference)SolveReferenceLazy(new IRUnknownReference(unsolved.syntaxNode), ctx, obj).Reference; break;
            case SliceTypeReference @slice: slice.InternalType = SolveTypeLazy2(@slice.InternalType, ctx, obj); break;
            case ReferenceTypeReference @refer: refer.InternalType = SolveTypeLazy2(@refer.InternalType, ctx, obj); break;
            case NullableTypeReference @nullable: nullable.InternalType = SolveTypeLazy2(@nullable.InternalType, ctx, obj); break;
        }
        
        return typeref;
    }
}
