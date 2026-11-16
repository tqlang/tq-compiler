using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

public record TypedefDeclarationNode(
    TokenNode TypedefKeyword,
    (TokenNode Left, ExpressionNode Type, TokenNode Right)? UnderlyingType,
    IdentifierExpressionNode Name,
    BlockNode Body,
    TriviaNode[] LeadingTrivia
) : DeclarationNode(LeadingTrivia) {
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb)
    {
        sb.AppendSyntaxNode(TypedefKeyword);
        if (UnderlyingType != null)
        {
            sb
                .AppendSyntaxNode(UnderlyingType.Value.Left)
                .AppendSyntaxNode(UnderlyingType.Value.Type)
                .AppendSyntaxNode(UnderlyingType.Value.Right);
        }

        return sb
            .AppendSyntaxNode(Name)
            .AppendSyntaxNode(Body)
            .AppendTrivia(LeadingTrivia);
    }

    public override SourceSpan Span => TypedefKeyword.Span + SourceSpan.FirstAndLastNotNull(Name, Body);
}

public abstract record TypedefCaseDeclaration(
    TokenNode CaseKeyword,
    TriviaNode[] LeadingTrivia
) : DeclarationNode(LeadingTrivia);

public record TypedefNumberedCaseDeclaration(
    TokenNode CaseKeyword,
    (IntegerLiteralExpressionNode value, TokenNode? comma)[] IntegerLiterals,
    TriviaNode[] LeadingTrivia
) : TypedefCaseDeclaration(CaseKeyword, LeadingTrivia)
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb)
    {
        sb.AppendSyntaxNode(CaseKeyword);

        foreach (var (v, c) in IntegerLiterals)
            sb.AppendSyntaxNode(v).AppendSyntaxNode(c);
        
        return sb.AppendTrivia(LeadingTrivia);
    }

    public override SourceSpan Span
    {
        get
        {
            var a = CaseKeyword.Span;
            var b = IntegerLiterals.Length > 0 ? IntegerLiterals[^1].value.Span : a;
            return a + b;
        }
    }
}

public record TypedefNamedCaseDeclaration(
    TokenNode CaseKeyword,
    IdentifierExpressionNode Name,
    TriviaNode[] LeadingTrivia
) : TypedefCaseDeclaration(CaseKeyword, LeadingTrivia)
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendSyntaxNode(CaseKeyword)
        .AppendSyntaxNode(Name)
        .AppendTrivia(LeadingTrivia);
    
    public override SourceSpan Span => CaseKeyword.Span + Name.Span;
}

public record TypedefNamedCaseWithValueDeclaration(
    TokenNode CaseKeyword,
    IdentifierExpressionNode Name,
    TokenNode EqualsToken,
    ExpressionNode Value,
    TriviaNode[] LeadingTrivia
) : TypedefNamedCaseDeclaration(CaseKeyword, Name, LeadingTrivia)
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendSyntaxNode(CaseKeyword)
        .AppendSyntaxNode(Name)
        .AppendSyntaxNode(EqualsToken)
        .AppendSyntaxNode(Value)
        .AppendTrivia(LeadingTrivia);
    
    public override SourceSpan Span => CaseKeyword.Span + Value.Span;
    
}
