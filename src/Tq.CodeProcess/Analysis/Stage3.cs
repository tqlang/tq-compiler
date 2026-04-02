using System.Diagnostics;
using Abstract.CodeProcess.Core;
using Abstract.CodeProcess.Core.EvaluationData;
using Abstract.CodeProcess.Core.EvaluationData.Exceptions;
using Abstract.CodeProcess.Core.EvaluationData.IntermediateTree;
using Abstract.CodeProcess.Core.EvaluationData.IntermediateTree.Expressions;
using Abstract.CodeProcess.Core.EvaluationData.IntermediateTree.Statements;
using Abstract.CodeProcess.Core.EvaluationData.IntermediateTree.Values;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.CodeObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.Containers;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.Imports;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.CodeReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin.Integer;
using Abstract.CodeProcess.Core.Language;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Expression;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Statement;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Value;
using AsmResolver.DotNet;
using TypeReference = Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.TypeReference;

namespace Abstract.CodeProcess;


/*
 * Stage Three:
 *  Processes every function body into a intermediate
 *  representation that will be used for data storage,
 *  compile time execution, runtime evaluation and
 *  high-level optimizations.
 */

public partial class Analyser
{
    private void ScanObjectHeaders()
    {
        foreach (var i in _globalReferenceTable)
        {
            switch (i.Value)
            {
                case FunctionObject @funcobj: ScanFunctionMeta(funcobj); break;
                case StructObject @structobj: ScanStructureMeta(structobj); break;
                case TypedefObject @typedefobj: ScanTypedefMeta(typedefobj); break;
                
                case ConstructorObject @c: ScanCtorMeta(c); break;
                
                case TqNamespaceObject @nmsp:
                    foreach (var i2 in nmsp.Scripts) ScanScriptMeta(i2);
                    break;
                    
                case FunctionGroupObject @funcgroup:
                    foreach (var i2 in funcgroup.Overloads) ScanFunctionMeta(i2);
                    break;
            }
        }

        foreach (var i in _globalReferenceTable.Values.OfType<StructObject>())
        {
            if (i.Extends == null) continue;
            i.Extends = SolveTypeLazy(i.Extends, null, i);
            if (i.Extends is UnsolvedTypeReference) throw new Exception($"Cannot solve type {i.SyntaxNode:pos}");
            if (i.Extends is not SolvedStructTypeReference) throw new Exception("Non-struct types cannot be inherited");
            if (i.Extends is SolvedStructTypeReference { Struct.Static: true }) throw new Exception("Cannot extends static type");
            Console.WriteLine(i.Extends);
        }
        
        var structsSortedList = TopologicalSort(_globalReferenceTable.Values.OfType<StructObject>());
        foreach (var structs in structsSortedList) LazyScanStructureMeta(structs);
    }
    
    
    private void ScanScriptMeta(SourceScript sourceScript)
    {
        foreach (var a in sourceScript.Imports)
        {
            try
            {
                switch (a)
                {
                    case GeneralImportObject @general:
                    {
                        var namespaceObject = TryFindNamespace(general.NamespacePath)
                                              ?? throw new CompilationException(
                                                  $"Reference to '{string.Join('.', general.NamespacePath)}' not found " +
                                                  $"at {general:pos}");
                        general.NamespaceObject = namespaceObject;
                    } break;

                    case SpecificImportObject @specific:
                    {
                        specific.Container = TryFindNamespace(specific.NamespacePath)
                                             ?? throw new CompilationException(
                                                 $"Reference to '{string.Join('.', specific.NamespacePath)}' not found " +
                                                 $"at {specific.Node:pos}");

                        foreach (var (key, value) in specific.Imports)
                        {
                            var r2 = specific.Container.SearchChild(value.path, SearchChildMode.All);
                            if (r2 == null) throw new Exception($"Reference {value.path} not found" +
                                                                $" in base {string.Join('.', specific.NamespacePath)}");
                            specific.Imports[key] = (value.path, r2);
                        }
                    } break;

                    default: throw new Exception();
                }
            }
            catch (CompilationException e)
            {
                _errorHandler.SetFile(sourceScript);
                _errorHandler.RegisterError(e);
            }
        }
    }
    private void ScanStructureMeta(StructObject structure)
    {
        foreach (var i in structure.Fields)
        {
            try
            {
                if (!IsSolved(i.Type)) i.Type = SolveTypeLazy(i.Type, null, structure);
            }
            catch (CompilationException e)
            {
                _errorHandler.SetFile(structure.SourceScript);
                _errorHandler.RegisterError(e);
            }
        }
    }
    private void ScanTypedefMeta(TypedefObject typedef)
    {
        try
        {
            if (typedef.BackType != null && !IsSolved(typedef.BackType))
                typedef.BackType = SolveTypeLazy(typedef.BackType, null, typedef);
        }
        catch (CompilationException e)
        {
            _errorHandler.SetFile(typedef.SourceScript);
            _errorHandler.RegisterError(e);
        }
    }
    private void ScanFunctionMeta(FunctionObject function)
    {
        foreach (var t in function.Parameters)
            if (!IsSolved(t.Type)) t.Type = SolveTypeLazy(t.Type, null, function);
        
        if (!IsSolved(function.ReturnType))
            function.ReturnType = SolveTypeLazy(function.ReturnType, null, function);
    }
    private void ScanCtorMeta(ConstructorObject ctor)
    {
        foreach (var t in ctor.Parameters)
            if (!IsSolved(t.Type)) t.Type = SolveTypeLazy(t.Type, null, ctor);
        
        if (!IsSolved(ctor.ReturnTypeOverride))
            ctor.ReturnTypeOverride = SolveTypeLazy(ctor.ReturnType, null, ctor);
    }
    
    private void ScanObjectBodies()
    {
        foreach (var i in _globalReferenceTable)
        {
            switch (i.Value)
            {
                case FunctionGroupObject @funcgroup:
                {
                    foreach (var i2 in funcgroup.Overloads)
                    {
                        ScanFunctionExecutionBody(i2);
                        foreach (var l in i2.Locals)
                            if (l.Type != null && !IsSolved(l.Type)) l.Type = SolveTypeLazy(l.Type, null, i2);
                    }
                    break;
                }
                
                case FieldObject @fld: ScanFieldValueBody(fld); break;

                case TypedefObject @tdf: ScanTypedefEntriesValueBody(tdf); break;
                
                case StructObject @st:
                {
                    foreach (var j in st.Constructors) ScanCtorExecutionBody(j);
                    foreach (var j in st.Destructors) ScanDtorExecutionBody(j);
                } break;
            }
        }
    }

    private void ScanTypedefEntriesValueBody(TypedefObject typedef)
    {
        foreach (var namedValue in typedef.NamedValues)
        {
            var node = namedValue.syntaxNode;
            if (node.Value != null)
            {
                var ctx = new ExecutionContextData(typedef);
                namedValue.Value = UnwrapExecutionContext_Expression(node.Value, ctx);
            }
        }
    }
    private void ScanFieldValueBody(FieldObject field)
    {
        var nodes = field.SyntaxNode.Children;

        if (nodes is not [_, _, TokenNode { Token.type: TokenType.EqualsChar }, ExpressionNode @value]) return;
        
        var ctx = new ExecutionContextData(field);
        field.Value = UnwrapExecutionContext_Expression(value, ctx);
    }
    private void ScanFunctionExecutionBody(FunctionObject function)
    {
        var body = function.SyntaxNode.GetFunctionBody();
        if (body == null) return;

        var ctx = new ExecutionContextData(function);
        function.Body = UnwrapExecutionContext_Block(ctx, body);
    }

    private void ScanCtorExecutionBody(ConstructorObject ctor)
    {
        var body = ctor.SyntaxNode.Body;
        if (body == null) return;

        var ctx = new ExecutionContextData(ctor);
        ctor.Body = UnwrapExecutionContext_Block(ctx, body);
    }

    private void ScanDtorExecutionBody(DestructorObject dtor)
    {
        var body = dtor.SyntaxNode.Body;
        if (body == null) return;

        var ctx = new ExecutionContextData(dtor);
        dtor.Body = UnwrapExecutionContext_Block(ctx, body);
    }
    
    
    private IrBlock UnwrapExecutionContext_Block(ExecutionContextData ctx, BlockNode block)
    {
        var rootBlock = new IrBlock(block);

        ctx.PushBlock(rootBlock);
        foreach (var i in block.Content)
        {
            var res = UnwrapExecutionContext_Statement(i, ctx);
            ctx.Last = res.node;
            
            if (res is { node: not null, include: true }) rootBlock.Content.Add(res.node);
        }
        ctx.PopBlock();

        return rootBlock;
    }
    private (IrNode? node, bool include) UnwrapExecutionContext_Statement(SyntaxNode node, ExecutionContextData ctx)
    {
        switch (node)
        {
            case LocalVariableNode @localvar:
            {
                // LocalVariableNode is also handled in the expression function,
                // this is because if it is just a declaration, we need to return
                // null to ignore it, but if it is used inside a expression, we
                // must return a reference to the local variable!
                
                var typenode = localvar.TypedIdentifier.Type;
                var name = localvar.TypedIdentifier.Identifier.Value;
                var type = SolveTypeLazy(new UnsolvedTypeReference(localvar.TypedIdentifier.Type), ctx, null);
                
                if (ctx.Locals.Any(e => e.Name == name))
                    throw new Exception($"{localvar:pos} shadows \'{name}\' declaration");
                
                ctx.AppendLocal(new LocalVariableObject(type, name));
                return (null, false);
            }

            case AssignmentExpressionNode @assign:
            {
                var left = UnwrapExecutionContext_Expression(assign.Left, ctx);
                var right = UnwrapExecutionContext_Expression(assign.Right, ctx);
                IrBinaryExp.Operators? op = null;
                
                switch (assign.Operator)
                { 
                    case "=": break;

                    case "+=": op = IrBinaryExp.Operators.Add; break;
                    case "-=": op = IrBinaryExp.Operators.Subtract; break;
                    case "*=": op = IrBinaryExp.Operators.Multiply; break;
                    case "/=": op = IrBinaryExp.Operators.Divide; break;
                    case "%=": op = IrBinaryExp.Operators.Reminder; break;
                    
                    default: throw new NotImplementedException();
                }
                
                if (op != null) right = new IrBinaryExp(assign, op.Value, left, right);
                return (new IrAssign(assign, left, right), true);
            }

            case IfStatementNode @if:
            {
                return (ParseIfElif(@if, @if.Then, @if.Condition), true);
            }
            case ElifStatementNode @elif:
            {
                if (ctx.Last is not IRIf @irif)
                    throw new Exception("elif blocks only allowed after if or elif statements");
                
                var a = ParseIfElif(elif, elif.Then, elif.Condition);
                irif.Else = a;
                return (a, false);
            }
            case ElseStatementNode @else:
            {
                if (ctx.Last is not IRIf irIf)
                    throw new Exception("else blocks only allowed after if or elif statements");
                
                IrBlock then = new IrBlock(@else.Then);
                ctx.PushBlock(then);

                if (@else.Then is BlockNode @block) then = UnwrapExecutionContext_Block(ctx, block);
                else
                {
                    var (res, _) = UnwrapExecutionContext_Statement(@else.Then, ctx);
                    if (res != null) then.Content.Add(res);
                }
                ctx.PopBlock();

                var a = new IRElse(@else, then);
                irIf.Else = a;
                return (a, false);
            }

            case WhileStatementNode @while:
            {
                var clen = @while.Children.Length;
                
                IrBlock? def = null;
                IrBlock? step = null;
                IrBlock then;
                
                var defidx  = clen switch
                {
                    4 => -1,
                    6 => -1,
                    _ => 1
                };
                var conidx = clen switch
                {
                    4 => 1,
                    6 => 1,
                    _ => 3
                };
                var stepidx = clen switch
                {
                    4 => -1,
                    6 => 3,
                    _ => 5
                };
                var bodyidx = clen switch
                {
                    4 => 3,
                    6 => 5,
                    _ => 7
                };
                
                if (defidx != -1)
                {
                    def = new IrBlock(@while.Children[defidx]);
                    ctx.PushBlock(def);
                    var res = UnwrapExecutionContext_Statement(@while.Children[defidx], ctx);
                    if (res is { include: true, node: not null }) def.Content.Add(res.node);
                }
                if (stepidx != -1)
                {
                    step = new IrBlock(@while.Children[stepidx]);
                    step.Content.Add(UnwrapExecutionContext_Expression(@while.Children[stepidx], ctx));
                }
                
                var condition = UnwrapExecutionContext_Expression(@while.Children[conidx], ctx);
                
                var content = @while.Children[bodyidx];
                if (content is BlockNode @bn) then = UnwrapExecutionContext_Block(ctx, bn);
                else
                {
                    then = new IrBlock(content);
                    var n = UnwrapExecutionContext_Statement(content, ctx);
                    if (n is { include: true, node: not null }) then.Content.Add(n.node);
                }

                if (def != null) ctx.PopBlock();
                return (new IRWhile(@while, def, condition, step, then), true);
            }
            
            case ReturnStatementNode @ret:
            {
                var exp = ret.HasExpression ? UnwrapExecutionContext_Expression(ret.Expression, ctx) : null;
                return (new IrReturn(ret, exp), true);
            }
            
            default: return (UnwrapExecutionContext_Expression(node, ctx), true);
        }
        
        IRIf ParseIfElif(SyntaxNode origin, SyntaxNode origin_then, SyntaxNode cond)
        {
            var condition = UnwrapExecutionContext_Expression(cond, ctx);
            IrBlock then = new IrBlock(origin_then);
            
            ctx.PushBlock(then);
            if (origin_then is BlockNode @block) then = UnwrapExecutionContext_Block(ctx, block);
            else
            {
                var (res, _) = UnwrapExecutionContext_Statement(origin_then, ctx);
                if (res != null) then.Content.Add(res);
            }
            ctx.PopBlock();
                
            return new IRIf(@origin, condition, then);
        }
    }
    private IrExpression UnwrapExecutionContext_Expression(SyntaxNode node, ExecutionContextData ctx)
    {
        switch (node)
        {
            case LocalVariableNode @localvar:
            {
                var identifier = localvar.IsImplicitTyped ? localvar.Identifier : localvar.TypedIdentifier.Identifier; 
                var name = identifier.Value;
                
                if (ctx.Locals.Any(e => e.Name == name))
                    throw new Exception($"{localvar:pos} shadows \'{name}\' declaration");
                
                var newLocal = new LocalVariableObject(localvar.IsImplicitTyped ? null
                    : SolveTypeLazy(new UnsolvedTypeReference(localvar.TypedIdentifier.Type), ctx, null), name);
                
                ctx.AppendLocal(newLocal);
                return new IrSolvedReference(identifier, new LocalReference(newLocal));
            }

            case FunctionCallExpressionNode @funccal:
            {
                return new IrInvoke(funccal,
                    UnwrapExecutionContext_Expression(funccal.FunctionReference, ctx),
                    funccal.Arguments.Select(i
                        => UnwrapExecutionContext_Expression(i, ctx)).ToArray());
            }
    
            case BinaryExpressionNode @bexp:
            {
                if (bexp.Operator is ">" or "<" or ">=" or "<=") return new IrCompareExp(bexp,
                    bexp.Operator switch
                    {
                        ">" => IrCompareExp.Operators.GreaterThan,
                        ">=" => IrCompareExp.Operators.GreaterThanOrEqual,
                        "<" => IrCompareExp.Operators.LessThan,
                        "<=" => IrCompareExp.Operators.LessThanOrEqual,
                        _ => throw new UnreachableException(),
                    },
                    UnwrapExecutionContext_Expression(bexp.Left, ctx),
                    UnwrapExecutionContext_Expression(bexp.Right, ctx));

                var l = UnwrapExecutionContext_Expression(bexp.Left, ctx);
                var r = UnwrapExecutionContext_Expression(bexp.Right, ctx);
                
                return bexp.Operator switch
                    {
                        "+" => new IrBinaryExp(bexp, IrBinaryExp.Operators.Add, l, r),
                        "+%" => new IrBinaryExp(bexp, IrBinaryExp.Operators.AddWarpAround, l, r),
                        "+|" => new IrBinaryExp(bexp, IrBinaryExp.Operators.AddOnBounds, l, r),
                        
                        "-" => new IrBinaryExp(bexp, IrBinaryExp.Operators.Subtract, l, r),
                        "-%" =>  new IrBinaryExp(bexp, IrBinaryExp.Operators.SubtractWarpAround, l, r),
                        "-|" => new IrBinaryExp(bexp, IrBinaryExp.Operators.SubtractOnBounds, l, r),
                        
                        "*" => new IrBinaryExp(bexp, IrBinaryExp.Operators.Multiply, l, r),
                        
                        "/" => new IrBinaryExp(bexp, IrBinaryExp.Operators.Divide, l, r),
                        "/_" => new IrBinaryExp(bexp, IrBinaryExp.Operators.DivideFloor, l, r),
                        "/^" => new IrBinaryExp(bexp, IrBinaryExp.Operators.DivideCeil, l, r),
                        
                        "%" => new IrBinaryExp(bexp, IrBinaryExp.Operators.Reminder, l, r),

                        "AND" => new IrBinaryExp(bexp, IrBinaryExp.Operators.BitwiseAnd, l, r),
                        "OR" => new IrBinaryExp(bexp, IrBinaryExp.Operators.BitwiseOr, l, r),
                        "XOR" => new IrBinaryExp(bexp, IrBinaryExp.Operators.BitwiseXor, l, r),

                        "<<" => new IrBinaryExp(bexp, IrBinaryExp.Operators.LeftShift, l, r),
                        ">>" => new IrBinaryExp(bexp, IrBinaryExp.Operators.RightShift, l, r),
                        
                        "==" => new IrCompareExp(bexp, IrCompareExp.Operators.Equality, l, r),
                        "!=" => new IrCompareExp(bexp, IrCompareExp.Operators.Inequality, l, r),
                        "<" => new IrCompareExp(bexp, IrCompareExp.Operators.LessThan, l, r),
                        "<=" => new IrCompareExp(bexp, IrCompareExp.Operators.LessThanOrEqual, l, r),
                        ">" => new IrCompareExp(bexp, IrCompareExp.Operators.GreaterThan, l, r),
                        ">=" => new IrCompareExp(bexp, IrCompareExp.Operators.GreaterThanOrEqual, l, r),
                        
                        "or" => new IrLogicalExp(bexp, IrLogicalExp.Operators.Or, l, r),
                        "and" => new IrLogicalExp(bexp, IrLogicalExp.Operators.And, l, r),

                        _ => throw new UnreachableException(),
                    };
            }
            case UnaryPrefixExpressionNode @uexp:
            {
                return uexp.Operator switch
                {
                    "&" => new IrRef(uexp, UnwrapExecutionContext_Expression(uexp.Expression, ctx)),
                    _ => new IRUnaryExp(uexp, uexp.Operator switch
                    {
                        "+" => IRUnaryExp.UnaryOperation.Plus,
                        "-" => IRUnaryExp.UnaryOperation.Minus,
                        "!" => IRUnaryExp.UnaryOperation.Not,

                        "~" => IRUnaryExp.UnaryOperation.BitwiseNot,

                        "++" => IRUnaryExp.UnaryOperation.PreIncrement,
                        "--" => IRUnaryExp.UnaryOperation.PreDecrement,

                        _ => throw new UnreachableException(),
                    }, UnwrapExecutionContext_Expression(uexp.Expression, ctx))
                };
            }
            case UnaryPostfixExpressionNode @uexp:
            {
                return new IRUnaryExp(uexp, uexp.Operator switch
                    {
                        "++" => IRUnaryExp.UnaryOperation.PostIncrement,
                        "--" => IRUnaryExp.UnaryOperation.PostDecrement,
                    
                        _ => throw new UnreachableException(),
                    },
                    UnwrapExecutionContext_Expression(uexp.Expression, ctx));
            }
            case IndexExpressionNode @iexp:
            {
                return new IrIndex(
                    iexp,
                    UnwrapExecutionContext_Expression(iexp.Target, ctx),
                    iexp.Indexer.Expressions
                        .Select(e => UnwrapExecutionContext_Expression(e, ctx)).ToArray());
            }
            
            case TypeCastNode @tcast:
            {
                return new IrConv(tcast,
                    UnwrapExecutionContext_Expression(tcast.Value, ctx),
                    SolveTypeLazy(new UnsolvedTypeReference(tcast.TargetType), ctx, ctx.Parent));
            }
            
            case AccessNode @identc: return SolveReferenceChain(identc, ctx, null);
            case ImplicitAccessNode @impl: return SolveReferenceChain(impl, ctx, null);
            case IdentifierNode @ident: return SolveReferenceChain(ident, ctx, null);

            case IntegerLiteralNode @intlit:
                return new IrIntegerLiteral(intlit, intlit.Value, new ComptimeIntegerTypeReference());
            case StringLiteralNode @strlit:
            {
                if (strlit.IsSimple) return new IrStringLiteral(strlit, strlit.RawContent);
                throw new NotImplementedException();
            }
            case CharacterLiteralNode @charlit:
                return new IrCharLiteral(charlit, charlit.Value[0]);
            case BooleanLiteralNode @boollit:
                return new IRBooleanLiteral(boollit, boollit.Value);
            case NullLiteralNode @nulllit: 
                return new IRNullLiteral(nulllit);
            
            case NewObjectNode @newobj:
            {
                List<IrAssign> asisgns = [];
                
                if (newobj.Inlined != null)
                {
                    foreach (var i in newobj.Inlined.Content)
                    {
                        if (i is not AssignmentExpressionNode @ass) throw new UnreachableException();
                        asisgns.Add(new IrAssign(ass,
                            new IRUnknownReference(ass.Left),
                            UnwrapExecutionContext_Expression(ass.Right, ctx)));
                    }
                }

                var ctor = new IrNewObject(
                    newobj,
                    (UnwrapExecutionContext_Expression(newobj.Type, ctx) as IrReference) ?? throw new NullReferenceException(),
                    newobj.Arguments.Select(i => UnwrapExecutionContext_Expression(i, ctx)).ToArray(),
                    [..asisgns]);
                
                return ctor;
            }

            case CollectionExpressionNode @c:
            {
                var items = c.Items.Select(i => UnwrapExecutionContext_Expression(i, ctx)).ToArray();
                return new IrCollectionLiteral(c, new UnsolvedTypeReference(null!), items);
            }
            
            case ParenthesisExpressionNode @pa: return UnwrapExecutionContext_Expression(pa.Content, ctx);
            
            default: throw new NotImplementedException();
        };
    }

    
    private void LazyScanStructureMeta(StructObject structure)
    {
        // This function requires that the structure's dependency tree
        // was already scanned!

        try
        {
            var parent = (structure.Extends as SolvedStructTypeReference)?.Struct;
            var virtualCount = structure.Functions.SelectMany(e => e.Overloads).Count(e => e.Abstract || e.Virtual);
            virtualCount += parent?.VirtualTable?.Length ?? 0;

            structure.VirtualTable = new (FunctionObject, FunctionObject?, bool)[virtualCount];
            if (parent is { VirtualTable: not null })
            {
                foreach (var (idx, e) in parent.VirtualTable.Index())
                    structure.VirtualTable[idx].parent = e.overrided ?? e.parent;
            }

            var virtualStartAt = parent?.VirtualTable?.Length ?? 0;

            uint i = 0;
            foreach (var func in structure.Functions.SelectMany(e => e.Overloads))
            {
                // Checking if it is virtual, so a new entries
                // Should be allocated in the vtable
                if (func.Abstract || func.Virtual)
                {
                    structure.VirtualTable[i].parent = func;
                    structure.VirtualTable[i].overrided = func;
                    //func.VirtualIndex = i;
                    i++;
                }
                
                // Solving a override function
                if (func.Override) SolveOverridingFunction(func, structure);
            }

            Alignment fieldOffset = parent != null ? parent.Length!.Value : 0;
            Alignment bestAlignment = parent != null ? parent.Alignment!.Value : 0;

            // Sorting the fields by alignment order
            var fields = structure.Fields.ToArray();
            fields.Sort((a, b) => b.Alignment.Bits - a.Alignment.Bits);
            
            foreach (var field in fields)
            {
                if (!IsSolved(field.Type)) field.Type = SolveTypeLazy(field.Type, null, field);
                var flen = Alignment.Align(field.Type.Length, field.Type.Alignment);
                bestAlignment = Alignment.Max(bestAlignment, flen);
                field.Offset = fieldOffset;
                fieldOffset += flen;
            }
            structure.Length = fieldOffset;
            structure.Alignment = bestAlignment;
        }
        catch (CompilationException e)
        {
            _errorHandler.SetFile(structure.SourceScript);
            _errorHandler.RegisterError(e);
        }
    }
    
    private void SolveOverridingFunction(FunctionObject func, StructObject parent)
    {
        foreach (var (i, e) in parent.VirtualTable.Index())
        {
            var basefunc = e.parent;
            
            // I Suppose it is impossible to override a already
            // overwritten function in the same structure, so skipping
            // here will be quicker
            if (e.overrided != null) continue;
            if (func.Name != basefunc.Name) continue;
            if (func.Parameters.Count != basefunc.Parameters.Count) continue;

            for (var j = 0; j < func.Parameters.Count; j++)
            {
                if (CalculateTypeSuitability(func.Parameters[j].Type, basefunc.Parameters[j].Type, false)
                    != Suitability.Perfect) continue;
            }
            
            parent.VirtualTable[i].overrided = basefunc;
            //func.VirtualIndex = (uint)i;
            return;
        }

        throw new Exception("No virtual function to override");
    }


    private IrExpression SolveReferenceChain(ExpressionNode node, ExecutionContextData? ctx, LangObject? obj)
    {
        return node switch
        {
            AccessNode @access => new IRAccess(node,
                SolveReferenceChain(access.Left, ctx, obj), SolveReferenceChain(access.Right, ctx, obj)),
            
            IdentifierNode @ident => ((Func<IrExpression>)(() =>
            {
                var a = SolveShallowType(ident);
                return a is UnsolvedTypeReference
                    ? new IRUnknownReference(ident)
                    : new IrSolvedReference(ident, a);
            })).Invoke(),
            _ => UnwrapExecutionContext_Expression(node, ctx),
        };
    }
    
    private TypeReference SolveTypeLazy(TypeReference typeRef, ExecutionContextData? ctx, LangObject? obj)
    {
        var scope = obj ?? ctx?.Parent;
        var parent = scope?.Parent ?? ctx?.Parent;
        
        switch (typeRef)
        {
            case ReferenceTypeReference @r:
                r.InternalType = SolveTypeLazy(r.InternalType, ctx, obj);
                return r;
            
            case SliceTypeReference @s:
                s.ElementType = SolveTypeLazy(s.ElementType, ctx, obj);
                return s;
        }
        
        var trySolveShallow = SolveShallowType(((UnsolvedTypeReference)typeRef).SyntaxNode);
        if (trySolveShallow is not UnsolvedTypeReference @unsolv) return trySolveShallow;
        
        var syntaxNode = unsolv.SyntaxNode;
        LangObject langObj;

        switch (syntaxNode)
        {
            case IdentifierNode @idnode:
            {
                // Search generics
                if (scope is ICallable { IsGeneric: true } callable)
                {
                    var param = callable.Parameters.FirstOrDefault(e => e.Name == idnode.Value);
                    if (param != null) return new GenericTypeReference(param);
                }
                
                // Search in parent tree
                var curr = parent;
                while (curr != null && curr is not TqNamespaceObject)
                {
                    var r3 = curr.SearchChild(idnode.Value, SearchChildMode.All);
                    if (r3 != null) return (TypeReference)GetObjectReference(r3);
                    curr = curr.Parent;
                }
                
                // Search in inherited tree
                if (parent is StructObject { Extends: SolvedStructTypeReference } @structObject)
                {
                    LangObject? curr2 = ((SolvedStructTypeReference)structObject.Extends).Struct;
                    while (curr2 != null! && curr2 is not TqNamespaceObject)
                    {
                        var r3 = curr2.SearchChild(idnode.Value, SearchChildMode.All);
                        if (r3 != null) return (TypeReference)GetObjectReference(r3);
                        curr2 = curr2.Parent;
                    }
                }

                // Search inside namespace
                var r4 = obj?.Namespace?.SearchChild(idnode.Value, SearchChildMode.OnlyStatic);
                if (r4 != null) return (TypeReference)GetObjectReference(r4);

                // Search inside imports
                if (obj?.SourceScript != null) {
                    foreach (var i in obj.SourceScript.Imports)
                    {
                        var r5 = i.SearchReference(idnode.Value);
                        if (r5 != null) return (TypeReference)GetObjectReference(r5);
                    }
                }

                // Search global references
                var r6 = _globalReferenceTable
                    .FirstOrDefault(e => e.Key.Length == 1 && e.Key[0] == idnode.Value);
                if (r6.Key != null) return (TypeReference)GetObjectReference(r6.Value);

                if (parent is TqNamespaceObject @nmsp)
                {
                    string[] name = [.. nmsp.Global, idnode.Value];
                    var r7 = _globalReferenceTable
                        .FirstOrDefault(e => IdentifierComparer.IsEquals(e.Key, name));
                    if (r7.Key != null) return (TypeReference)GetObjectReference(r7.Value);
                }
                
                throw new CompilationException($"Cannot find reference to {idnode:pos}");
            }
            
            default: throw new UnreachableException();
        }
    }
    private BaseNamespaceObject? TryFindNamespace(string[] path)
    {
        var module = _modules.FirstOrDefault(e => e!.Name == path[0], null);

        switch (module)
        {
            case TqModuleObject @tqModule:
            {
                var root = tqModule.Root;
                if (root == null) return null;
                
                LangObject? parent = root;
                for (var i = 1; i < path.Length; i++)
                {
                    parent = parent.SearchChild(path[i], SearchChildMode.OnlyStatic);
                    if (parent == null) return null;
                }

                return parent as BaseNamespaceObject;
            }
            case DotnetModuleObject dotnetModule:
            {
                var stringNamespace = string.Join('.', path[1..]);
                if (dotnetModule.Namespaces.TryGetValue(stringNamespace, out var ns)) return ns;

                List<ITypeDefOrRef> foundTypes = [];
                ITypeDefOrRef? foundType = null;
                
                foreach (var m in dotnetModule.ManifestModules)
                {
                    foreach (var exported in m.ExportedTypes)
                    {
                        if (exported.Namespace == stringNamespace) foundTypes.Add(exported.Resolve()!);
                        else if (exported.Namespace + '.' + exported.Name == stringNamespace) foundType = exported.Resolve();
                    }

                    foreach (var type in m.TopLevelTypes)
                    {
                        if (type.Namespace == stringNamespace) foundTypes.Add(type);
                        else if (type.Namespace + '.' + type.Name == stringNamespace) foundType = type;
                    }
                }
                if (foundTypes.Count == 0 && foundType == null) return null;

                if (foundType != null)
                {
                    var nmsp = new DotnetStaticClassObject(stringNamespace, foundType.Resolve()!);

                    nmsp.Parent = dotnetModule;
                    dotnetModule.Namespaces.Add(stringNamespace, nmsp);
                    return nmsp;
                }
                else
                {
                    var nmsp = new DotnetNamespaceObject(stringNamespace);
                    foreach (var i in foundTypes) nmsp.DotnetTypes.Add(i);

                    nmsp.Parent = dotnetModule;
                    dotnetModule.Namespaces.Add(stringNamespace, nmsp);
                    return nmsp;
                }
            }
            
            default: throw new UnreachableException();
        }
    }
    
    private List<StructObject> TopologicalSort(IEnumerable<StructObject> structs)
    {
        var visited = new HashSet<StructObject>();
        var visiting = new HashSet<StructObject>();
        var ordered = new List<StructObject>();

        foreach (var s in structs) Visit(s);
        return ordered;

        void Visit(StructObject s)
        {
            if (visited.Contains(s)) return;

            if (!visiting.Add(s))
                throw new Exception($"Cyclic dependency detected at struct '{string.Join('.', s.Global)}'");

            var parent = (s.Extends as SolvedStructTypeReference)?.Struct;
            if (parent != null) Visit(parent);

            visiting.Remove(s);
            visited.Add(s);
            ordered.Add(s);
        }
    }
}
