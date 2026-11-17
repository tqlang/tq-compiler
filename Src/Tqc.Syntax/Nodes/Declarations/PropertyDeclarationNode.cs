using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

/// <summary>
/// `let T name { get; set }`: a property. Each accessor is empty (`get`), defined by a function (`get = f`) or
/// implemented where it is written (`get { ... }`). Accessors are separated by line feeds or semicolons.
/// </summary>
public record PropertyDeclarationNode(
    TokenNode LetKeyword,
    ExpressionNode? Type,
    IdentifierExpressionNode Name,
    TokenNode LeftBrace,
    PropertyAccessorNode[] Accessors,
    TokenNode RightBrace,
    TriviaNode[] LeadingTrivia
) : DeclarationNode(LeadingTrivia)
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb)
    {
        sb
            .AppendSyntaxNode(LetKeyword)
            .AppendSyntaxNode(Type)
            .AppendSyntaxNode(Name)
            .AppendSyntaxNode(LeftBrace);

        foreach (var accessor in Accessors) accessor.AppendSyntaxToStringBuilder(sb);

        return sb
            .AppendSyntaxNode(RightBrace)
            .AppendTrivia(LeadingTrivia);
    }

    public override SourceSpan Span => LetKeyword.Span + RightBrace.Span;
}

/// <summary>`get`, `set`, `get = function` or `get { ... }`.</summary>
public record PropertyAccessorNode(
    TokenNode Keyword,
    TokenNode? EqualsToken,
    ExpressionNode? Function,
    ScopeNode? Body,
    TriviaNode[] LeadingTrivia
) : SyntaxNode
{
    public bool IsGetter => Keyword.Value == "get";

    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendSyntaxNode(Keyword)
        .AppendSyntaxNode(EqualsToken)
        .AppendSyntaxNode(Function)
        .AppendSyntaxNode(Body)
        .AppendTrivia(LeadingTrivia);

    public override SourceSpan Span => Keyword.Span + SourceSpan.FirstAndLastNotNull(Function, Body);
}
