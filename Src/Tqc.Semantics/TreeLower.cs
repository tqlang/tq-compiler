using System.Collections.Immutable;
using System.Text;
using Tqc.Core;
using Tqc.Diagnostics;
using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Primitive;
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

    private enum MemberScope { Root, Struct, Typedef }

    private sealed class LoweredMembers
    {
        public List<TqImportMember> Imports = [];
        public Dictionary<string, IMember> Members = [];
        public List<TqConstructor> Constructors = [];
        public TqDestructor? Destructor;
        public List<TqNumericCase> NumericCases = [];
    }

    private static (TqImportMember[], Dictionary<string, IMember>) ParseMemberRoot(STContext ctx, SyntaxNode[] root)
    {
        var lowered = LowerMembers(ctx, root, MemberScope.Root);
        return ([..lowered.Imports], lowered.Members);
    }

    /// <summary>
    /// Lowers what a file, a struct or a typedef holds. Each kind of container accepts some declarations only;
    /// an error in one member is reported and the next one is still lowered.
    /// </summary>
    private static LoweredMembers LowerMembers(STContext ctx, IEnumerable<SyntaxNode> nodes, MemberScope scope)
    {
        var result = new LoweredMembers();
        var members = result.Members;
        List<AttributeNode> attributes = [];

        foreach (var node in nodes)
        {
            try
            {
                if (node is AttributeNode attributeNode)
                {
                    attributes.Add(attributeNode);
                    continue;
                }

                if (!Accepts(scope, node))
                    throw Error($"A {Describe(node)} cannot be declared {Where(scope)}", node.GetLoc(ctx.SrcFile));

                switch (node)
                {
                    case ImportDirectiveNode importNode:
                    {
                        var import = LowerImportMember(ctx, importNode);
                        if (attributes.Count > 0)
                            throw Error("Attributes cannot be applied to import directive", importNode.GetLoc(ctx.SrcFile));
                        result.Imports.Add(import);
                    }
                    break;

                    case FieldDeclarationNode fieldNode:
                    {
                        var field = LowerFieldMember(ctx, LowerAttributes(ctx, attributes.Dump(), AttributeTarget.Field), fieldNode);
                        if (!members.TryAdd(field.Name, field)) throw DuplicatedNameError(field.Name, fieldNode.Name.GetLoc(ctx.SrcFile));
                    }
                    break;

                    case PropertyDeclarationNode propertyNode:
                    {
                        var property = LowerPropertyMember(ctx, LowerAttributes(ctx, attributes.Dump(), AttributeTarget.Field), propertyNode);
                        if (!members.TryAdd(property.Name, property)) throw DuplicatedNameError(property.Name, propertyNode.Name.GetLoc(ctx.SrcFile));
                    }
                    break;

                    case FunctionDeclarationNode functionNode:
                    {
                        var function = LowerFunctionMember(ctx, LowerAttributes(ctx, attributes.Dump(), AttributeTarget.Function), functionNode);
                        AddFunction(members, function, functionNode.Name.GetLoc(ctx.SrcFile));
                    }
                    break;

                    case ConstructorDeclarationNode constructorNode:
                        result.Constructors.Add(LowerConstructor(ctx, LowerAttributes(ctx, attributes.Dump(), AttributeTarget.Function), constructorNode));
                    break;

                    case DestructorDeclarationNode destructorNode:
                    {
                        var destructor = LowerDestructor(ctx, LowerAttributes(ctx, attributes.Dump(), AttributeTarget.Function), destructorNode);
                        if (result.Destructor != null) throw Error("A struct has only one destructor", destructorNode.GetLoc(ctx.SrcFile));
                        result.Destructor = destructor;
                    }
                    break;

                    case StructDeclarationNode structNode:
                    {
                        var structure = LowerStructMember(ctx, LowerAttributes(ctx, attributes.Dump(), AttributeTarget.Struct), structNode);
                        if (!members.TryAdd(structure.Name, structure)) throw DuplicatedNameError(structure.Name, structNode.Name.GetLoc(ctx.SrcFile));
                    }
                    break;

                    case TypedefDeclarationNode typedefNode:
                    {
                        var typedef = LowerTypedefMember(ctx, LowerAttributes(ctx, attributes.Dump(), AttributeTarget.Typedef), typedefNode);
                        if (!members.TryAdd(typedef.Name, typedef)) throw DuplicatedNameError(typedef.Name, typedefNode.Name.GetLoc(ctx.SrcFile));
                    }
                    break;

                    case TypedefNumberedCaseDeclaration numbered:
                    {
                        LowerAttributes(ctx, attributes.Dump(), AttributeTarget.Case);
                        var location = numbered.GetLoc(ctx.SrcFile);
                        result.NumericCases.Add(new TqNumericCase(
                            [..numbered.IntegerLiterals.Select(literal =>
                                (IrComptimeInt)LowerExpression(ctx, literal.value))], location));
                    }
                    break;

                    case TypedefNamedCaseDeclaration named:
                    {
                        var attributesOfCase = LowerAttributes(ctx, attributes.Dump(), AttributeTarget.Case);
                        var value = named is TypedefNamedCaseWithValueDeclaration withValue
                            ? LowerExpression(ctx, withValue.Value)
                            : null;
                        var entry = new TqNamedCase(named.Name.Value, value, named.GetLoc(ctx.SrcFile)) { Attributes = attributesOfCase };
                        if (!members.TryAdd(entry.Name, entry)) throw DuplicatedNameError(entry.Name, named.Name.GetLoc(ctx.SrcFile));
                    }
                    break;

                    default: throw new InvalidOperationException($"Unknown node type: {node.GetType().FullName}");
                }
            }
            catch (CompilationError e)
            {
                attributes.Clear();
                ctx.Diagnostics.ReportError(e);
            }
        }

        if (attributes.Count > 0)
            ctx.Diagnostics.ReportError(Error("Attributes have no declaration to apply to", attributes[0].GetLoc(ctx.SrcFile)));

        members.TrimExcess();
        return result;
    }

    private static bool Accepts(MemberScope scope, SyntaxNode node) => scope switch
    {
        MemberScope.Root => node is ImportDirectiveNode or FieldDeclarationNode or FunctionDeclarationNode
            or StructDeclarationNode or TypedefDeclarationNode,
        MemberScope.Struct => node is FieldDeclarationNode or PropertyDeclarationNode or FunctionDeclarationNode
            or ConstructorDeclarationNode or DestructorDeclarationNode,
        _ => node is FieldDeclarationNode or FunctionDeclarationNode or TypedefCaseDeclaration,
    };

    private static string Describe(SyntaxNode node) => node switch
    {
        ImportDirectiveNode => "import",
        FieldDeclarationNode => "field",
        PropertyDeclarationNode => "property",
        FunctionDeclarationNode => "function",
        ConstructorDeclarationNode => "constructor",
        DestructorDeclarationNode => "destructor",
        StructDeclarationNode => "struct",
        TypedefDeclarationNode => "typedef",
        TypedefCaseDeclaration => "case",
        _ => "declaration",
    };

    private static string Where(MemberScope scope) => scope switch
    {
        MemberScope.Root => "at the top level of a file",
        MemberScope.Struct => "inside a struct",
        _ => "inside a typedef",
    };

    /// <summary>Overloads of the same name are collected in a function group.</summary>
    private static void AddFunction(Dictionary<string, IMember> members, TqFunction function, SourceLocation location)
    {
        if (!members.TryGetValue(function.Name, out var existing))
        {
            members.Add(function.Name, function);
            return;
        }

        switch (existing)
        {
            case TqFunction first:
                var group = new TqFunctionGroup(function.Name);
                group.Functions.Add(first);
                group.Functions.Add(function);
                members[function.Name] = group;
                break;

            case TqFunctionGroup functionGroup:
                functionGroup.Functions.Add(function);
                break;

            default: throw DuplicatedNameError(function.Name, location);
        }
    }

    /// <summary>An attribute with a wrong argument is reported and dropped; the member itself is still lowered.</summary>
    private static TqAttribute[] LowerAttributes(STContext ctx, AttributeNode[] attributeNodes, AttributeTarget target)
    {
        List<TqAttribute> attributes = [];

        foreach (var node in attributeNodes)
        {
            var location = node.GetLoc(ctx.SrcFile);
            IrExpression[] arguments;
            try { arguments = node.Arguments != null ? LowerArgumentsList(ctx, node.Arguments).arguments : []; }
            catch (InvalidOperationException)
            {
                ctx.Diagnostics.ReportError(Error($"Unsupported argument in attribute '@{node.Identifier.Value}'", location));
                continue;
            }

            var (attribute, error) = BuiltinAttributes.Create(node.Identifier.Value, arguments, location);
            if (attribute == null) ctx.Diagnostics.ReportError(Error(error!, location));
            else attributes.Add(attribute);
        }

        foreach (var (message, location) in BuiltinAttributes.Check(attributes, target))
            ctx.Diagnostics.ReportError(Error(message, location));

        return [.. attributes];
    }
    
    private static TqImportMember LowerImportMember(STContext ctx, ImportDirectiveNode importNode)
    {
        var baseNamespace = importNode.NamespaceIdentifier.Value;
        Dictionary<string, (SourceLocation, string)>? importMap = null;

        if (importNode.ImportMembers != null)
        {
            importMap = [];
            foreach (var (i, _) in importNode.ImportMembers.Identifiers)
            {
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
        }
        
        return new TqImportMember(importNode.GetLoc(ctx.SrcFile), baseNamespace, importMap);
    }
    private static TqField LowerFieldMember(STContext ctx, TqAttribute[] attributes, FieldDeclarationNode fieldNode)
    {
        var type = fieldNode.Type != null ? LowerExpression(ctx, fieldNode.Type) : null;
        var name = fieldNode.Name.Value;
        var value = fieldNode.Value != null ? LowerExpression(ctx, fieldNode.Value) : null;

        // `const T x` is `let Const(T) x`
        if (fieldNode.IsConst) type = CompilerStd.Types.ConstOf(fieldNode.GetLoc(ctx.SrcFile), type);

        return new TqField(type, name, value, fieldNode.GetLoc(ctx.SrcFile)) { Attributes = attributes };
    }
    /// <summary>
    /// The accessors become functions of the struct (`get_name`, `set_name`) that the property holds. One that has
    /// no body and no function is automatic: the backend gives the property its storage.
    /// </summary>
    private static TqProperty LowerPropertyMember(STContext ctx, TqAttribute[] attributes, PropertyDeclarationNode node)
    {
        var name = node.Name.Value;
        var location = node.GetLoc(ctx.SrcFile);
        var type = node.Type != null ? LowerExpression(ctx, node.Type) : null;
        var property = new TqProperty(type, name, location) { Attributes = attributes };

        if (node.Accessors.Length == 0)
            throw Error($"The property '{name}' needs a 'get' or a 'set'", location);

        foreach (var accessor in node.Accessors)
        {
            var isGetter = accessor.IsGetter;
            var accessorLocation = accessor.GetLoc(ctx.SrcFile);

            if (isGetter ? property.HasGetter : property.HasSetter)
                throw Error($"The property '{name}' has more than one '{accessor.Keyword.Value}'", accessorLocation);

            if (accessor.Function != null)
            {
                var source = LowerExpression(ctx, accessor.Function);
                if (isGetter) property.GetterSource = source; else property.SetterSource = source;
                continue;
            }

            if (type == null)
                throw Error($"The property '{name}' needs a type: only accessors defined by a function can give it", location);

            var function = new TqFunction(isGetter ? $"get_{name}" : $"set_{name}", accessorLocation)
            {
                Parameters = isGetter ? [] : [new TqParameter(type, "value")],
                ReturnType = isGetter ? type : null,
                Attributes = [],
            };

            if (accessor.Body != null) function.Body = LowerScope(ctx, accessor.Body);

            if (isGetter) property.GetterFunction = function; else property.SetterFunction = function;
        }

        property.Getter = property.GetterFunction;
        property.Setter = property.SetterFunction;
        return property;
    }

    private static TqFunction LowerFunctionMember(STContext ctx, TqAttribute[] attributes, FunctionDeclarationNode functionNode)
    {
        var name = functionNode.Name.Value;
        var type = functionNode.ReturnType != null ? LowerExpression(ctx, functionNode.ReturnType) : null;
        // TODO body
        
        var function = new TqFunction(name, functionNode.GetLoc(ctx.SrcFile))
        {
            Parameters = LowerParameters(ctx, functionNode.Parameters),
            ReturnType = type,
            Attributes = attributes,
        };

        // A conversion takes one value and gives another.
        foreach (var attribute in attributes.OfType<TqBuiltinAttribute>()
                     .Where(attribute => attribute.Kind is BuiltinAttribute.ImplicitConvert or BuiltinAttribute.ExplicitConvert))
        {
            if (function.Parameters.Length != 1 || function.ReturnType == null)
                ctx.Diagnostics.ReportError(Error(
                    $"'@{attribute.Name}' needs a function with exactly one parameter and a return type", attribute.Location));
        }
        if (functionNode.Body != null) function.Body = LowerScope(ctx, functionNode.Body);
        return function;
    }
    private static TqConstructor LowerConstructor(STContext ctx, TqAttribute[] attributes, ConstructorDeclarationNode node)
    {
        var constructor = new TqConstructor(node.GetLoc(ctx.SrcFile))
        {
            Parameters = LowerParameters(ctx, node.Parameters),
            ReturnType = node.ReturnType != null ? LowerExpression(ctx, node.ReturnType) : null,
            Attributes = attributes,
        };
        if (node.Body != null) constructor.Body = LowerScope(ctx, node.Body);
        return constructor;
    }

    private static TqDestructor LowerDestructor(STContext ctx, TqAttribute[] attributes, DestructorDeclarationNode node)
    {
        var destructor = new TqDestructor(node.GetLoc(ctx.SrcFile))
        {
            Parameters = LowerParameters(ctx, node.Parameters),
            ReturnType = node.ReturnType != null ? LowerExpression(ctx, node.ReturnType) : null,
            Attributes = attributes,
        };
        if (node.Body != null) destructor.Body = LowerScope(ctx, node.Body);
        return destructor;
    }

    private static TqStruct LowerStructMember(STContext ctx, TqAttribute[] attributes, StructDeclarationNode structNode)
    {
        var lowered = LowerMembers(ctx, structNode.Body.Children, MemberScope.Struct);

        var structure = new TqStruct(structNode.Name.Value, structNode.GetLoc(ctx.SrcFile))
        {
            Parameters = structNode.Parameters != null ? LowerParameters(ctx, structNode.Parameters) : [],
            Attributes = attributes,
            Extends = structNode.extends != null ? LowerExpression(ctx, structNode.extends.Value.type) : null,
            Members = lowered.Members.ToImmutableDictionary(),
            Constructors = lowered.Constructors,
            Destructor = lowered.Destructor,
        };

        // A function knows the type it belongs to: whether it is abstract depends on it.
        SetOwner(structure, structure.Members.Values);
        foreach (var constructor in structure.Constructors) constructor.Owner = structure;
        if (structure.Destructor != null) structure.Destructor.Owner = structure;

        return structure;
    }

    private static void SetOwner(IObjectType owner, IEnumerable<IMember> members)
    {
        foreach (var member in members)
        {
            switch (member)
            {
                case TqFunctionGroup group:
                    foreach (var overload in group) overload.Owner = owner;
                    break;

                case TqFunction function:
                    function.Owner = owner;
                    break;

                case TqProperty property:
                    property.Owner = owner;
                    if (property.GetterFunction != null) property.GetterFunction.Owner = owner;
                    if (property.SetterFunction != null) property.SetterFunction.Owner = owner;
                    break;
            }
        }
    }

    private static TqTypedef LowerTypedefMember(STContext ctx, TqAttribute[] attributes, TypedefDeclarationNode typedefNode)
    {
        var lowered = LowerMembers(ctx, typedefNode.Body.Children, MemberScope.Typedef);

        var typedef = new TqTypedef(typedefNode.Name.Value, typedefNode.GetLoc(ctx.SrcFile))
        {
            Attributes = attributes,
            UnderlyingType = typedefNode.UnderlyingType != null ? LowerExpression(ctx, typedefNode.UnderlyingType.Value.Type) : null,
            Members = lowered.Members.ToImmutableDictionary(),
            NumericCases = [..lowered.NumericCases],
        };

        foreach (var entry in lowered.Members.Values.OfType<TqNamedCase>()) entry.Typedef = typedef;
        SetOwner(typedef, typedef.Members.Values);
        return typedef;
    }

    private static TqParameter[] LowerParameters(STContext ctx, ParametersCollectionNode nodes) =>
        [.. nodes.Select(node => new TqParameter(LowerExpression(ctx, node.Type), node.Name.Value))];

    private static IrBody LowerScope(STContext ctx, ScopeNode scope)
    {
        var body = new IrBody(scope.GetLoc(ctx.SrcFile));
        body.Children.AddRange(LowerStatements(ctx, scope.Children));
        return body;
    }
    
    private static IEnumerable<IrStatement> LowerStatements(STContext ctx, IEnumerable<StatementNode> statements)
    {
        var nodes = statements.ToList();

        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];

            switch (node)
            {
                case IfStatementNode ifNode:
                {
                    // The `elif` and `else` that follow belong to this `if`.
                    List<(SourceLocation Location, IrExpression Condition, IrStatement Body)> branches =
                        [(ifNode.GetLoc(ctx.SrcFile), LowerExpression(ctx, ifNode.Condition), LowerStatement(ctx, ifNode.Statement))];
                    IrStatement? otherwise = null;

                    while (i + 1 < nodes.Count)
                    {
                        if (nodes[i + 1] is ElifStatementNode elif)
                        {
                            branches.Add((elif.GetLoc(ctx.SrcFile), LowerExpression(ctx, elif.Condition), LowerStatement(ctx, elif.Statement)));
                            i++;
                        }
                        else if (nodes[i + 1] is ElseStatementNode elseNode)
                        {
                            otherwise = LowerStatement(ctx, elseNode.Statement);
                            i++;
                            break;
                        }
                        else break;
                    }

                    var chain = otherwise;
                    for (var b = branches.Count - 1; b >= 0; b--)
                        chain = new IrIf(branches[b].Location, branches[b].Condition, branches[b].Body, chain);

                    yield return chain!;
                }
                break;

                case ElifStatementNode:
                    throw Error("'elif' without a preceding 'if'", node.GetLoc(ctx.SrcFile));

                case ElseStatementNode:
                    throw Error("'else' without a preceding 'if'", node.GetLoc(ctx.SrcFile));

                case ScopeNode scope:
                    yield return LowerScope(ctx, scope);
                break;

                case WhileStatementNode whileNode:
                {
                    // The declaration has to say `let`: `while i = 0 : ...` does not declare anything.
                    var declaration = whileNode.Declaration != null ? LowerExpression(ctx, whileNode.Declaration) : null;
                    if (declaration is not (IrLocal or IrAssign { Target: IrLocal }) && declaration != null)
                        throw Error("The declaration of 'while' must declare a variable with 'let'", declaration.Location);

                    yield return new IrWhile(
                        whileNode.GetLoc(ctx.SrcFile),
                        declaration,
                        LowerExpression(ctx, whileNode.Condition),
                        whileNode.Step != null ? LowerExpression(ctx, whileNode.Step) : null,
                        LowerStatement(ctx, whileNode.Statement)
                    );
                }
                break;

                case BreakStatementNode:
                    yield return new IrBreak(node.GetLoc(ctx.SrcFile));
                break;

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

    /// <summary>The branch of an `if`: a scope, or a single statement.</summary>
    private static IrStatement LowerStatement(STContext ctx, StatementNode node)
    {
        var lowered = LowerStatements(ctx, [node]).ToList();
        if (lowered.Count == 1) return lowered[0];

        var body = new IrBody(node.GetLoc(ctx.SrcFile));
        body.Children.AddRange(lowered);
        return body;
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
            
            case IdentifierExpressionNode { Value: "true" or "false" } boolean:
                return new IrComptimeBool(boolean.GetLoc(ctx.SrcFile), boolean.Value == "true");

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

            case ArrayLiteralExpressionNode array:
                return new IrArray(array.GetLoc(ctx.SrcFile), [..array.Select(element => LowerExpression(ctx, element))]);

            case IntegerLiteralExpressionNode integer:
                return new IrComptimeInt(integer.GetLoc(ctx.SrcFile), (decimal)integer.Value);

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
            {
                var location = assignment.GetLoc(ctx.SrcFile);
                var target = LowerExpression(ctx, assignment.Left);
                var source = LowerExpression(ctx, assignment.Right);

                // `a += b` is `a = a + b`; the target is evaluated twice.
                if (CompoundOperator(assignment.Op.Kind) is { } compound)
                    source = new IrBinary(location, compound, target, source);

                return new IrAssign(location, target, source);
            }

            case BinaryExpressionNode binary:
                return new IrBinary(
                    binary.GetLoc(ctx.SrcFile),
                    BinaryOperatorOf(binary.Op.Kind),
                    LowerExpression(ctx, binary.Left),
                    LowerExpression(ctx, binary.Right));

            case UnaryPrefixExpressionNode prefix:
            {
                var operand = LowerExpression(ctx, prefix.Exp);
                var location = prefix.GetLoc(ctx.SrcFile);

                // A negative literal is one literal, so that it can still take the type of what it is used with.
                if (prefix.Op.Kind == TokenKind.Minus)
                {
                    if (operand is IrComptimeInt integer) return integer with { Location = location, BigInteger = -integer.BigInteger };
                    if (operand is IrComptimeDecimal number) return number with { Location = location, Value = -number.Value };
                }

                return new IrUnary(location, UnaryOperatorOf(prefix.Op.Kind), operand);
            }

            case UnaryPostfixExpressionNode postfix:
                return new IrUnary(
                    postfix.GetLoc(ctx.SrcFile),
                    UnaryOperatorOf(postfix.Op.Kind),
                    LowerExpression(ctx, postfix.Exp),
                    IsPostfix: true);

            case IndexExpressionNode index:
                return new IrIndex(
                    index.GetLoc(ctx.SrcFile),
                    LowerExpression(ctx, index.Callee),
                    [..index.Indices.Arguments.Select(element => LowerExpression(ctx, element.node))]);

            case ParenthesisExpressionNode parenthesis:
                return LowerExpression(ctx, parenthesis.Expression);

            // `*T` is `Ref(T)`. (`!T` and `?T` are unary operators until the binder knows that the operand is a type.)
            case ReferenceTypeModifierNode reference:
                return CompilerStd.Types.Apply(CompilerStd.Types.Ref, reference.GetLoc(ctx.SrcFile), LowerExpression(ctx, reference.ValueType));

            case DecimalLiteralExpressionNode number:
                return new IrComptimeDecimal(number.GetLoc(ctx.SrcFile), number.Value);
            
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
            {
                var localType = local.Type == null ? null : LowerExpression(ctx, local.Type);
                if (local.IsConst) localType = CompilerStd.Types.ConstOf(local.GetLoc(ctx.SrcFile), localType);

                return new IrLocal(local.GetLoc(ctx.SrcFile), localType, local.Name.Value);
            }
            
            default:
                throw new InvalidOperationException($"Unknown node type: {node.GetType().FullName}");
        }
    }
    private static BinaryOperator BinaryOperatorOf(TokenKind kind) => kind switch
    {
        TokenKind.Plus => BinaryOperator.Add,
        TokenKind.AddWrap => BinaryOperator.AddWrap,
        TokenKind.AddOnBounds => BinaryOperator.AddOnBounds,
        TokenKind.Minus => BinaryOperator.Sub,
        TokenKind.SubWrap => BinaryOperator.SubWrap,
        TokenKind.SubOnBounds => BinaryOperator.SubOnBounds,
        TokenKind.Star => BinaryOperator.Mul,
        TokenKind.MulWrap => BinaryOperator.MulWrap,
        TokenKind.MulOnBounds => BinaryOperator.MulOnBounds,
        TokenKind.Slash => BinaryOperator.Div,
        TokenKind.DivFloor => BinaryOperator.DivFloor,
        TokenKind.DivCeil => BinaryOperator.DivCeil,
        TokenKind.Rest => BinaryOperator.Rest,
        TokenKind.EqualsEquals => BinaryOperator.Equal,
        TokenKind.ExactEquals => BinaryOperator.ExactEqual,
        TokenKind.NotEqualsEquals => BinaryOperator.NotEqual,
        TokenKind.NotExactEquals => BinaryOperator.NotExactEqual,
        TokenKind.LeftAngle => BinaryOperator.Less,
        TokenKind.RightAngle => BinaryOperator.Greater,
        TokenKind.LessEqual => BinaryOperator.LessEqual,
        TokenKind.GreaterEqual => BinaryOperator.GreaterEqual,
        _ => throw new InvalidOperationException($"Not a binary operator: {kind}"),
    };

    private static BinaryOperator? CompoundOperator(TokenKind kind) => kind switch
    {
        TokenKind.AddAssign => BinaryOperator.Add,
        TokenKind.SubAssign => BinaryOperator.Sub,
        TokenKind.MulAssign => BinaryOperator.Mul,
        TokenKind.DivAssign => BinaryOperator.Div,
        TokenKind.RestAssign => BinaryOperator.Rest,
        _ => null,
    };

    private static UnaryOperator UnaryOperatorOf(TokenKind kind) => kind switch
    {
        TokenKind.Bang => UnaryOperator.Not,
        TokenKind.QuestionMark => UnaryOperator.Nullable,
        TokenKind.Minus => UnaryOperator.Negate,
        TokenKind.Increment => UnaryOperator.Increment,
        TokenKind.Decrement => UnaryOperator.Decrement,
        _ => throw new InvalidOperationException($"Not a unary operator: {kind}"),
    };

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
