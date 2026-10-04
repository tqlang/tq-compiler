using System.Collections.Immutable;
using System.Text;
using Tqc.Core;
using Tqc.Diagnostics;
using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;
using Tqc.Syntax;
using Tqc.Syntax.Nodes;
using Tqc.Syntax.Nodes.TypeModifiers;

namespace Tqc.Semantics;

public static class TreeLower
{
    public static TqNamespace ParseNamespace(SyntaxNamespaceContainer namespaceContainer)
    {
        var ctx = new STContext();

        var namespaces = namespaceContainer.Namespaces.Select(ParseNamespace).ToDictionary(e => e.Name);
        var scopes = namespaceContainer.Trees.Select(e => ParseTreeMembers(ctx, e)).ToArray();

        Dictionary<string, IMember> members = [];
        foreach (var scope in scopes)
        {
            foreach (var (name, member) in scope.Members)
                if (!members.TryAdd(name, member)) throw new NotImplementedException("Duplicate members (error here is not implemented)");
        }
        members.TrimExcess();
        
        return new TqNamespace(namespaceContainer.Name)
        {
            Namespaces = namespaces.ToImmutableDictionary(),
            Members = members.ToImmutableDictionary(),
            FileScopes =  [.. scopes],
        };
    }

    private static TqFileScope ParseTreeMembers(STContext ctx, SyntaxTree tree)
    {
        ctx.SrcFile = tree.FilePath;
        ctx.Diagnostics = tree.Diagnostics;
        
        var (imports, members) = ParseMemberRoot(ctx, tree.Root);
        var fileScope = new TqFileScope(tree.FilePath)
        {
            Imports = imports,
            Members = members,
        };
        ctx.SrcFile = null!;
        
        return fileScope;
    }

    private static (TqImportMember[], Dictionary<string, IMember>) ParseMemberRoot(STContext ctx, SyntaxNode[] root)
    {
        List<TqImportMember> imports = [];
        Dictionary<string, IMember> members = [];
        
        List<AttributeNode> attributes = [];
        TqAttribute[] parsedAttributes = [];

        foreach (var node in root) {
            try {
                switch (node)
                {
                    case AttributeNode attributeNode:
                        attributes.Add(attributeNode);
                        continue;
                    
                    case ImportDirectiveNode @importDefinitionNode:
                    {
                        var import = LowerImportMember(ctx, @importDefinitionNode);
                        if (attributes.Count > 0)
                            throw Error("Attributes cannot be applied to import directive", importDefinitionNode.GetLoc(ctx.SrcFile));
                        imports.Add(import);
                    }
                    break;

                    case FieldDeclarationNode @fieldDeclarationNode:
                        parsedAttributes = LowerAttributes(ctx, attributes.Dump());
                        var field = LowerFieldMember(ctx, parsedAttributes, fieldDeclarationNode);
                        if (!members.TryAdd(field.Name, field)) throw DuplicatedNameError(field.Name, fieldDeclarationNode.Name.GetLoc(ctx.SrcFile));
                    break;

                    case FunctionDeclarationNode @functionDeclarationNode:
                    {
                        parsedAttributes = LowerAttributes(ctx, attributes.Dump());
                        var function = LowerFunctionMember(ctx, parsedAttributes, functionDeclarationNode);

                        if (members.TryGetValue(function.Name, out var member))
                            switch (member)
                            {
                                case TqFunction olfFunc:
                                {
                                    var group = new TqFunctionGroup(function.Name);
                                    members[function.Name] = group;
                                    group.Functions.Add(olfFunc);
                                    group.Functions.Add(function);
                                }
                                break;

                                case TqFunctionGroup functionGroup:
                                    functionGroup.Functions.Add(function);
                                break;
                                
                                default: throw DuplicatedNameError(function.Name, functionDeclarationNode.Name.GetLoc(ctx.SrcFile));
                            }
                        else members.Add(function.Name, function);
                    } break;

                    case StructDeclarationNode structDeclarationNode:
                        parsedAttributes = LowerAttributes(ctx, attributes.Dump());
                        var structM = LowerStructMember(ctx, parsedAttributes, structDeclarationNode);
                        if (!members.TryAdd(structM.Name, structM)) throw DuplicatedNameError(structM.Name, structDeclarationNode.Name.GetLoc(ctx.SrcFile));
                    break;
                    
                    default: throw new InvalidOperationException($"Unknown node type: {node.GetType().FullName}");
                }
            }
            catch (CompilationError e) { ctx.Diagnostics.ReportError(e); }
        }

        members.TrimExcess();
        return ([..imports], members);
    }

    private static TqAttribute[] LowerAttributes(STContext ctx, AttributeNode[] attributeNodes)
    {
        return [];
    }
    
    private static TqImportMember LowerImportMember(STContext ctx, ImportDirectiveNode importNode)
    {
        var baseNamespace = importNode.NamespaceIdentifier.Value;
        Dictionary<string, (SourceLocation, string)>? importMap = null;

        if (importNode.ImportMembers != null) foreach (var (i, _) in importNode.ImportMembers.Identifiers)
        {
            importMap = [];
            switch (i)
            {
                case MembersCollectionPureItemNode pureItem:
                {
                    var name = pureItem.value.Value;
                    importMap.Add(name, (pureItem.GetLoc(ctx.SrcFile), name));
                } break;
                
                case MembersCollectionAliasedItemNode aliasedItem:
                {
                    var alias = aliasedItem.alias.Value;
                    var name = aliasedItem.value.Value;
                    importMap.Add(alias, (aliasedItem.GetLoc(ctx.SrcFile), name));
                } break;
                
                default: throw new NotImplementedException();
            }
        }
        
        return new TqImportMember(importNode.GetLoc(ctx.SrcFile), baseNamespace, importMap);
    }
    private static TqField LowerFieldMember(STContext ctx, TqAttribute[] attributes, FieldDeclarationNode fieldNode)
    {
        var type = fieldNode.Type != null ? LowerExpression(ctx, fieldNode.Type) : null;
        var name = fieldNode.Name.Value;
        var value = fieldNode.Value != null ? LowerExpression(ctx, fieldNode.Value) : null;
        
        return new TqField(type, name, value, fieldNode.GetLoc(ctx.SrcFile));
    }
    private static TqFunction LowerFunctionMember(STContext ctx, TqAttribute[] attributes, FunctionDeclarationNode functionNode)
    {
        var name = functionNode.Name.Value;
        var type = functionNode.ReturnType != null ? LowerExpression(ctx, functionNode.ReturnType) : null;
        // TODO body
        
        List<TqParameter> parameters = [];
        foreach (var i in functionNode.Parameters)
        {
            var paramType = LowerExpression(ctx, i.Type);
            var paramName = i.Name.Value;
            parameters.Add(new TqParameter(paramType, paramName));
        }

        var function = new TqFunction(name, [.. parameters], type, functionNode.GetLoc(ctx.SrcFile));
        if (functionNode.Body != null) function.Body = LowerScope(ctx, functionNode.Body);
        return function;
    }
    private static TqStruct LowerStructMember(STContext ctx, TqAttribute[] attributes, StructDeclarationNode structNode)
    {
        var name = structNode.Name.Value;
        // TODO extends
        // TODO generics
        
        return new TqStruct(name, structNode.GetLoc(ctx.SrcFile));
    }

    private static IrBody LowerScope(STContext ctx, ScopeNode scope)
    {
        var body = new IrBody(scope.GetLoc(ctx.SrcFile));
        body.Children.AddRange(LowerStatements(ctx, scope.Children));
        return body;
    }
    
    private static IEnumerable<IrStatement> LowerStatements(STContext ctx, IEnumerable<StatementNode> statements)
    {
        var nodes = statements.ToList();

        foreach (var node in nodes)
        {
            switch (node)
            {
                case IfStatementNode ifStatementNode:
                {
                    // var branches = new List<IrIfBranch>
                    // {
                    //     LowerIfBranch(ctx, ifNode)
                    // };
                    //
                    // while (i + 1 < nodes.Count)
                    // {
                    //     var next = nodes[i + 1];
                    //
                    //     if (next is ElseIfStatementNode elifNode)
                    //     {
                    //         branches.Add(LowerElseIfBranch(ctx, elifNode));
                    //         i++;
                    //     }
                    //     else if (next is ElseStatementNode elseNode)
                    //     {
                    //         // O else não possui condição.
                    //         branches.Add(LowerElseBranch(ctx, elseNode));
                    //         i++;
                    //         break;
                    //     }
                    //     else break;
                    // }
                    //
                    // yield return new IrIfStatement(ifNode.GetLoc(ctx.SrcFile), branches);
                    //
                    // continue;
                    
                    throw new NotImplementedException();
                }
                
                case ElifStatementNode:
                case ElseStatementNode:
                    throw new InvalidOperationException($"Unexpected conditional branch outside of chain: {node.GetType().Name}");
                
                case ReturnStatementNode returnStatement:
                    yield return new IrReturnStatement(
                        returnStatement.GetLoc(ctx.SrcFile),
                        returnStatement.Expression != null ? LowerExpression(ctx, returnStatement.Expression) : null
                    );
                break;
            
                case ExpressionStatementNode exprNode:
                    yield return new IrExpressionStatement(node.GetLoc(ctx.SrcFile), LowerExpression(ctx, exprNode.Expression));
                break;
                
                default: throw new InvalidOperationException($"Unknown node type: {node.GetType().FullName}");
            }
        }
    }
    
    private static IrExpression LowerExpression(STContext ctx, ExpressionNode node)
    {
        switch (node)
        {
            case CompoundIdentifierExpressionNode compoundIdentifier:
            {
                var identifiers = compoundIdentifier.Identifier;
                if (identifiers.IsEmpty) throw new InvalidOperationException("Compound identifier cannot be empty.");

                var identifiers_0 = identifiers[0].token;
                IrExpression result = new IrReference(identifiers_0.GetLoc(ctx.SrcFile), new UnsolvedRef(identifiers_0.Value));

                for (var i = 1; i < identifiers.Length; i++)
                {
                    var token = identifiers[i].token;
                    result = new IrAccess(
                        compoundIdentifier.GetLoc(ctx.SrcFile), result,
                        new IrReference(token.GetLoc(ctx.SrcFile), new UnsolvedRef(token.Value)));
                }

                return result;
            }
            
            case IdentifierExpressionNode identifier:
                return new IrReference(
                    identifier.GetLoc(ctx.SrcFile),
                    new UnsolvedRef(identifier.Value)
                );

            case StringExpressionNode stringExp:
            {
                var parts = new List<IrExpression>();
                var literal = new StringBuilder();

                foreach (var content in stringExp.Contents)
                {
                    switch (content)
                    {
                        case StringCharacterLiteralNode character:
                            literal.Append(character.Value);
                        break;

                        case StringLiteralNode text:
                            literal.Append(text.Value);
                        break;

                        // case InterpolatedContentNode interpolation:
                        // {
                        //     if (literal.Length > 0)
                        //     {
                        //         parts.Add(new IrStringConstant(
                        //             stringExp.GetLoc(ctx.SrcFile),
                        //             literal.ToString()));
                        //         literal.Clear();
                        //     }
                        //
                        //     parts.Add(LowerExpression(ctx, interpolation.Expression));
                        // } break;

                        default:
                            throw new NotSupportedException($"Unsupported string content: {content.GetType().Name}");
                    }
                }

                if (literal.Length > 0)
                    parts.Add(new IrStringConstant(stringExp.GetLoc(ctx.SrcFile), literal.ToString()));

                return parts.Count switch
                {
                    0 => new IrStringConstant(stringExp.GetLoc(ctx.SrcFile), string.Empty),
                    1 => new IrStringConstant(stringExp.GetLoc(ctx.SrcFile), ((IrStringConstant)parts[0]).Value),
                    _ => new IrStringConcat(stringExp.GetLoc(ctx.SrcFile), parts.ToArray()),
                };
            }

            case SliceTypeModifierNode sliceTypeModifier:
                return new IrSliceTypeModifier(
                    sliceTypeModifier.GetLoc(ctx.SrcFile),
                    LowerExpression(ctx, sliceTypeModifier.ElementType)
                );

            case MemberAccessExpressionNode access:
                return new IrAccess(
                    access.GetLoc(ctx.SrcFile),
                    LowerExpression(ctx, access.Left),
                    new IrReference(
                        access.Right.GetLoc(ctx.SrcFile),
                        new UnsolvedRef(access.Right.Value)
                    )
                );
            
            case AssignmentExpressionNode assignment:
                return new IrAssign(
                    assignment.GetLoc(ctx.SrcFile),
                    LowerExpression(ctx, assignment.Left),
                    LowerExpression(ctx, assignment.Right)
                );
            
            case CallExpressionNode call:
                return new IrCall(
                    call.GetLoc(ctx.SrcFile),
                    LowerExpression(ctx, call.Callee),
                    LowerArgumentsList(ctx, call.Arguments)
                );
            
            case NewExpressionNode newExpr:
                return new IrNew(
                    newExpr.GetLoc(ctx.SrcFile),
                    LowerExpression(ctx, newExpr.Type),
                    LowerArgumentsList(ctx, newExpr.Arguments)
                );
            
            case ConversionExpressionNode conversion:
                return new IrConvert(
                    conversion.GetLoc(ctx.SrcFile),
                    LowerExpression(ctx, conversion.Expression),
                    LowerExpression(ctx, conversion.Type)
                );
            
            case LocalExpressionNode local:
                return new IrLocal(
                    local.GetLoc(ctx.SrcFile),
                    local.Type == null ? null : LowerExpression(ctx, local.Type),
                    local.Name.Value
                );
            
            default:
                throw new InvalidOperationException($"Unknown node type: {node.GetType().FullName}");
        }
    }
    private static IrArgumentsList LowerArgumentsList(STContext ctx, ArgumentsCollectionNode args)
    {
        List<IrExpression> argExpressions = [];
        foreach (var i in args) argExpressions.Add(LowerExpression(ctx, i));
        return new IrArgumentsList(args.GetLoc(ctx.SrcFile), [..argExpressions]);
    }
    
    private static LoweringError Error(string message, SourceLocation loc) => new (message, loc);
    private static LoweringError DuplicatedNameError(string name, SourceLocation loc) => new ($"Member with name '{name}' already previously defined", loc);
    
    private class STContext
    {
        public string SrcFile = null!;
        public DiagnosticBag Diagnostics = null!;
    }
    
    private class LoweringError(string message, SourceLocation location) : CompilationError(message, location);
}
