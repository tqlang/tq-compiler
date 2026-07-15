using System.Text;

namespace Tq.Ast;

public abstract class TopLevelNode : SyntaxNode;

public class InvalidTopLevelNode : TopLevelNode
{
    public AttributeNode[] Attributes;
    public TokenNode[] Nodes;

    private InvalidTopLevelNode(AttributeNode[] attributes, TokenNode[] nodes)
    {
        Attributes = attributes;
        Nodes = nodes;
    }

    public static InvalidTopLevelNode Build(AttributeNode[] attributes, TokenNode[] nodes) => new InvalidTopLevelNode(attributes, nodes);

    public override StringBuilder AppendStringBuilder(StringBuilder sb)
    {
        foreach (var i in Attributes) i.AppendStringBuilder(sb);
        foreach (var i in Nodes) i.AppendStringBuilder(sb);
        
        return sb;
    }
}

public abstract class ImportNode : TopLevelNode
{
    public static SimpleImportNode BuildSimple(TokenNode from, ExpressionNode fromNamespace, TokenNode import) => new(from, fromNamespace, import);

    public static SpecificImportNode BuildSpecific(
        TokenNode from,
        ExpressionNode fromNamespace,
        TokenNode import,
        TokenNode leftBrace,
        (SpecificImportNode.SpecificImportItemNode import, TokenNode? comma)[] items,
        TokenNode rightBrace
    ) => new(from, fromNamespace, import, leftBrace, items, rightBrace);
}

public class SimpleImportNode : ImportNode
{
    public TokenNode From;
    public ExpressionNode Namespace;
    public TokenNode Import;

    internal SimpleImportNode(TokenNode from, ExpressionNode fromNamespace, TokenNode import)
    {
        From      = from;
        Namespace = fromNamespace;
        Import    = import;
    }

    public override StringBuilder AppendStringBuilder(StringBuilder sb) => sb.Append(From).Append(Namespace).Append(Import);
}

public class SpecificImportNode : ImportNode
{
    public TokenNode From;
    public ExpressionNode Namespace;
    public TokenNode Import;
    public TokenNode LeftBraceToken;
    public (SpecificImportItemNode import, TokenNode? comma)[] Items;
    public TokenNode RightBraceToken;

    internal SpecificImportNode(
        TokenNode from,
        ExpressionNode fromNamespace,
        TokenNode import,
        TokenNode leftBraceToken,
        (SpecificImportItemNode import, TokenNode? comma)[] items,
        TokenNode rightBraceToken
    )
    {
        From            = from;
        Namespace       = fromNamespace;
        Import          = import;
        LeftBraceToken  = leftBraceToken;
        Items           = items;
        RightBraceToken = rightBraceToken;
    }

    public override StringBuilder AppendStringBuilder(StringBuilder sb)
    {
        sb.Append(From)
          .Append(Namespace)
          .Append(Import)
          .Append(LeftBraceToken);
        foreach (var i in Items) sb.Append(i.import).Append(i.comma);
        return sb.Append(RightBraceToken);
    }

    public class SpecificImportItemNode : TopLevelNode
    {
        public IdentifierNode Identifier;
        public TokenNode? As;
        public IdentifierNode? Alias;

        private SpecificImportItemNode(
            IdentifierNode identifier,
            TokenNode? asKeyword,
            IdentifierNode? alias
        )
        {
            Identifier = identifier;
            As         = asKeyword;
            Alias      = alias;
        }

        public static SpecificImportItemNode BuildSimple(IdentifierNode identifier) => new(identifier, null, null);

        public static SpecificImportItemNode BuildAliased(IdentifierNode identifier, TokenNode asKeyword, IdentifierNode alias) => new(identifier, asKeyword, alias);

        public override StringBuilder AppendStringBuilder(StringBuilder sb) => sb.Append(Identifier).Append(As).Append(Alias);
    }
}

public class FunctionDeclarationNode : TopLevelNode
{
    public AttributeNode[] Attributes;
    public readonly TokenNode Definition;
    public readonly IdentifierNode Name;
    public readonly TokenNode LeftParenthesisToken;
    public readonly (TypedIdentifierNode parameter, TokenNode? comma)[] Parameters;
    public readonly TokenNode RightParenthesisToken;
    public readonly ExpressionNode? Type;
    public readonly BodyNode Body;

    private FunctionDeclarationNode(
        AttributeNode[] attributes,
        TokenNode definition,
        IdentifierNode name,
        TokenNode leftParenthesisToken,
        (TypedIdentifierNode parameter, TokenNode? comma)[] parameters,
        TokenNode rightParenthesisToken,
        ExpressionNode? type,
        BodyNode body
    )
    {
        Attributes            = attributes;
        Definition            = definition;
        Name                  = name;
        LeftParenthesisToken  = leftParenthesisToken;
        Parameters            = parameters;
        RightParenthesisToken = rightParenthesisToken;
        Type                  = type;
        Body                  = body;
    }

    public static FunctionDeclarationNode Build(
        AttributeNode[] attributes,
        TokenNode definition,
        IdentifierNode name,
        TokenNode leftParenthesisToken,
        (TypedIdentifierNode parameter, TokenNode? comma)[] parameters,
        TokenNode rightParenthesisToken,
        ExpressionNode? type,
        BodyNode body
    ) => new(attributes, definition, name, leftParenthesisToken, parameters, rightParenthesisToken, type, body);

    public override StringBuilder AppendStringBuilder(StringBuilder sb)
    {
        foreach (var i in Attributes) sb.Append(i);

        sb.Append(Definition)
          .Append(Name)
          .Append(LeftParenthesisToken);

        foreach (var (parameter, comma) in Parameters) sb.Append(parameter).Append(comma);

        sb.Append(RightParenthesisToken)
          .Append(Type)
          .Append(Body);

        return sb;
    }
}

public class ConstructorDeclarationNode : TopLevelNode
{
    public AttributeNode[] Attributes;
    public readonly TokenNode Definition;
    public readonly TokenNode LeftParenthesisToken;
    public readonly (TypedIdentifierNode parameter, TokenNode? comma)[] Parameters;
    public readonly TokenNode RightParenthesisToken;
    public readonly BodyNode Body;

    private ConstructorDeclarationNode(
        AttributeNode[] attributes,
        TokenNode definition,
        TokenNode leftParenthesisToken,
        (TypedIdentifierNode parameter, TokenNode? comma)[] parameters,
        TokenNode rightParenthesisToken,
        BodyNode body
    )
    {
        Attributes            = attributes;
        Definition            = definition;
        LeftParenthesisToken  = leftParenthesisToken;
        Parameters            = parameters;
        RightParenthesisToken = rightParenthesisToken;
        Body                  = body;
    }

    public static ConstructorDeclarationNode Build(
        AttributeNode[] attributes,
        TokenNode definition,
        TokenNode leftParenthesisToken,
        (TypedIdentifierNode parameter, TokenNode? comma)[] parameters,
        TokenNode rightParenthesisToken,
        BodyNode body
    ) => new(attributes, definition, leftParenthesisToken, parameters, rightParenthesisToken, body);

    public override StringBuilder AppendStringBuilder(StringBuilder sb)
    {
        foreach (var i in Attributes) sb.Append(i);

        sb.Append(Definition)
          .Append(LeftParenthesisToken);

        foreach (var (parameter, comma) in Parameters) sb.Append(parameter).Append(comma);

        sb.Append(RightParenthesisToken)
          .Append(Body);

        return sb;
    }
}

public class StructDeclarationNode : TopLevelNode
{
    public AttributeNode[] Attributes;
    public readonly TokenNode Definition;
    public readonly IdentifierNode Name;

    public readonly TokenNode? LeftParenthesisToken;
    public readonly (TypedIdentifierNode parameter, TokenNode? comma)[]? GenericParameters;
    public readonly TokenNode? RightParenthesisToken;

    public readonly TokenNode? ExtendsToken;
    public readonly ExpressionNode? ExtendsType;

    public readonly TokenNode? ImplementsToken;
    public readonly (ExpressionNode Type, TokenNode? Comma)[]? ImplementsTypes;

    public readonly BodyNode Body;

    private StructDeclarationNode(
        AttributeNode[] attributes,
        TokenNode definition,
        IdentifierNode name,
        TokenNode? leftParenthesisToken,
        (TypedIdentifierNode parameter, TokenNode? comma)[]? genericParameters,
        TokenNode? rightParenthesisToken,
        TokenNode? extendsToken,
        ExpressionNode? extendsType,
        TokenNode? implementsToken,
        (ExpressionNode, TokenNode?)[]? implementsTypes,
        BodyNode body
    )
    {
        Attributes = attributes;
        Definition = definition;
        Name       = name;

        LeftParenthesisToken  = leftParenthesisToken;
        GenericParameters     = genericParameters;
        RightParenthesisToken = rightParenthesisToken;

        ExtendsToken = extendsToken;
        ExtendsType  = extendsType;

        ImplementsToken = implementsToken;
        ImplementsTypes = implementsTypes;

        Body = body;
    }

    public static StructDeclarationNode Build(
        AttributeNode[] attributes,
        TokenNode definition,
        IdentifierNode name,
        TokenNode? leftParenthesisToken,
        (TypedIdentifierNode parameter, TokenNode? comma)[]? genericParameters,
        TokenNode? rightParenthesisToken,
        TokenNode? extendsToken,
        ExpressionNode? extendsType,
        TokenNode? implementsToken,
        (ExpressionNode, TokenNode?)[]? implementsTypes,
        BodyNode body
    ) => new(
        attributes,
        definition,
        name,
        leftParenthesisToken,
        genericParameters,
        rightParenthesisToken,
        extendsToken,
        extendsType,
        implementsToken,
        implementsTypes,
        body
    );

    public override StringBuilder AppendStringBuilder(StringBuilder sb)
    {
        foreach (var i in Attributes) i.AppendStringBuilder(sb);

        Definition.AppendStringBuilder(sb);
        Name.AppendStringBuilder(sb);

        LeftParenthesisToken?.AppendStringBuilder(sb);
        if (GenericParameters is not null)
        {
            foreach (var (parameter, comma) in GenericParameters)
                sb.Append(parameter).Append(comma);
        }

        RightParenthesisToken?.AppendStringBuilder(sb);

        ExtendsToken?.AppendStringBuilder(sb);
        ExtendsType?.AppendStringBuilder(sb);

        ImplementsToken?.AppendStringBuilder(sb);
        if (ImplementsTypes is not null)
        {
            foreach (var (type, comma) in ImplementsTypes)
            {
                type.AppendStringBuilder(sb);
                comma?.AppendStringBuilder(sb);
            }
        }

        Body.AppendStringBuilder(sb);

        return sb;
    }
}

public class FieldDeclarationNode : TopLevelNode
{
    public AttributeNode[] Attributes;
    public TokenNode Definition;
    public ExpressionNode? Type;
    public IdentifierNode Identifier;

    public TokenNode? Equals;
    public ExpressionNode? Value;

    private FieldDeclarationNode(
        AttributeNode[] attributes,
        TokenNode definition,
        ExpressionNode? type,
        IdentifierNode identifier,
        TokenNode? equals,
        ExpressionNode? value
    )
    {
        Attributes = attributes;
        Definition = definition;
        Type       = type;
        Identifier = identifier;
        Equals     = equals;
        Value      = value;
    }

    public static FieldDeclarationNode BuildUndefined(
        AttributeNode[] attributes,
        TokenNode definition,
        ExpressionNode? type,
        IdentifierNode identifier
    ) => new(attributes, definition, type, identifier, null, null);

    public static FieldDeclarationNode BuildWithValue(
        AttributeNode[] attributes,
        TokenNode definition,
        ExpressionNode? type,
        IdentifierNode identifier,
        TokenNode? equals,
        ExpressionNode? value
    ) => new(attributes, definition, type, identifier, equals, value);

    public override StringBuilder AppendStringBuilder(StringBuilder sb)
    {
        foreach (var i in Attributes) i.AppendStringBuilder(sb);

        Definition.AppendStringBuilder(sb);
        Type?.AppendStringBuilder(sb);
        Identifier.AppendStringBuilder(sb);
        Equals?.AppendStringBuilder(sb);
        Value?.AppendStringBuilder(sb);

        return sb;
    }
}

public class TypedefDeclarationNode : TopLevelNode
{
    public AttributeNode[] Attributes;
    public TokenNode Definition;
    public (TokenNode leftParenthesis, ExpressionNode Type, TokenNode rightParenthesis)? Type;
    public IdentifierNode Name;
    public ExplicitBodyNode Body;

    private TypedefDeclarationNode(
        AttributeNode[] attributes,
        TokenNode definition,
        (TokenNode leftParenthesis, ExpressionNode Type, TokenNode rightParenthesis)? type,
        IdentifierNode name,
        ExplicitBodyNode body
    )
    {
        Attributes = attributes;
        Definition = definition;
        Type       = type;
        Name       = name;
        Body       = body;
    }

    public static TypedefDeclarationNode Build(
        AttributeNode[] attributes,
        TokenNode definition,
        (TokenNode leftParenthesis, ExpressionNode Type, TokenNode rightParenthesis)? type,
        IdentifierNode name,
        ExplicitBodyNode body
    ) => new(attributes, definition, type, name, body);

    public static TypedefExpressionCaseDeclarationNode BuildCaseExpression(
        AttributeNode[] attributes,
        TokenNode definition,
        (ExpressionNode values, TokenNode? comma)[] values
    ) => new(attributes, definition, values);

    public static TypedefIdentifierCaseDeclarationNode BuildCaseIdentifier(
        AttributeNode[] attributes,
        TokenNode definition,
        IdentifierNode identifier,
        (TokenNode equals, ExpressionNode value)? value
    ) => new(attributes, definition, identifier, value);

    public override StringBuilder AppendStringBuilder(StringBuilder sb)
    {
        foreach (var i in Attributes) i.AppendStringBuilder(sb);

        Definition.AppendStringBuilder(sb);
        if (Type != null)
        {
            Type.Value.leftParenthesis.AppendStringBuilder(sb);
            Type.Value.Type.AppendStringBuilder(sb);
            Type.Value.rightParenthesis.AppendStringBuilder(sb);
        }

        Name.AppendStringBuilder(sb);
        Body.AppendStringBuilder(sb);

        return sb;
    }
}

public abstract class TypedefCaseDeclarationNode : SyntaxNode;

public class TypedefExpressionCaseDeclarationNode : TypedefCaseDeclarationNode
{
    public AttributeNode[] Attributes;
    public TokenNode Definition;
    public (ExpressionNode values, TokenNode? comma)[] Values;

    internal TypedefExpressionCaseDeclarationNode(
        AttributeNode[] attributes,
        TokenNode definition,
        (ExpressionNode values, TokenNode? comma)[] values
    )
    {
        Attributes = attributes;
        Definition = definition;
        Values     = values;
    }

    public override StringBuilder AppendStringBuilder(StringBuilder sb)
    {
        foreach (var i in Attributes) i.AppendStringBuilder(sb);

        Definition.AppendStringBuilder(sb);

        foreach (var (value, comma) in Values)
        {
            value.AppendStringBuilder(sb);
            comma?.AppendStringBuilder(sb);
        }

        return sb;
    }
}

public class TypedefIdentifierCaseDeclarationNode : TypedefCaseDeclarationNode
{
    public AttributeNode[] Attributes;
    public TokenNode Definition;
    public IdentifierNode Identifier;
    public (TokenNode equals, ExpressionNode value)? Value;

    internal TypedefIdentifierCaseDeclarationNode(
        AttributeNode[] attributes,
        TokenNode definition,
        IdentifierNode identifier,
        (TokenNode equals, ExpressionNode value)? value
    )
    {
        Attributes = attributes;
        Definition = definition;
        Identifier = identifier;
        Value      = value;
    }

    public override StringBuilder AppendStringBuilder(StringBuilder sb)
    {
        foreach (var i in Attributes) i.AppendStringBuilder(sb);

        Definition.AppendStringBuilder(sb);
        Identifier.AppendStringBuilder(sb);

        Value?.equals.AppendStringBuilder(sb);
        Value?.value.AppendStringBuilder(sb);

        return sb;
    }
}