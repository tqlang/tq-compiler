using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

/// <summary>`delete value`: calls the destructor of a value.</summary>
public record DeleteStatementNode(TokenNode DeleteKeyword, ExpressionNode Expression, TriviaNode[] LeadingTrivia) : StatementNode(LeadingTrivia)
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendSyntaxNode(DeleteKeyword)
        .AppendSyntaxNode(Expression)
        .AppendTrivia(LeadingTrivia);

    public override SourceSpan Span => DeleteKeyword.Span + Expression.Span;
}
