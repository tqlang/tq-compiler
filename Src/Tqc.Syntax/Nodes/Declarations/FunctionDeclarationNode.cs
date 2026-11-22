using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

/// <summary>`func f(...) T { ... }`, or `func f(...) T => value`: the body is the value that is returned (or, with no `T`, run).</summary>
public record FunctionDeclarationNode(
    TokenNode FuncToken,
    IdentifierExpressionNode Name,
    ParametersCollectionNode Parameters,
    ExpressionNode? ReturnType,
    ScopeNode? Body,
    TokenNode? Arrow,
    ExpressionNode? Expression,
    TriviaNode[] LeadingTrivia
) : DeclarationNode(LeadingTrivia)
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendSyntaxNode(FuncToken)
        .AppendSyntaxNode(Name)
        .AppendSyntaxNode(Parameters)
        .AppendSyntaxNode(ReturnType)
        .AppendSyntaxNode(Body)
        .AppendSyntaxNode(Arrow)
        .AppendSyntaxNode(Expression)
        .AppendTrivia(LeadingTrivia);
    
    public override SourceSpan Span => FuncToken.Span + SourceSpan.FirstAndLastNotNull(Parameters, ReturnType, Body, Expression);
}
