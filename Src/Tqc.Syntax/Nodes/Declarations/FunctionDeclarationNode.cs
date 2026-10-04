using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

public record FunctionDeclarationNode(
    TokenNode FuncToken,
    IdentifierExpressionNode Name,
    ParametersCollectionNode Parameters,
    ExpressionNode? ReturnType,
    ScopeNode? Body,
    TriviaNode[] LeadingTrivia
) : DeclarationNode(LeadingTrivia)
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendSyntaxNode(FuncToken)
        .AppendSyntaxNode(Name)
        .AppendSyntaxNode(Parameters)
        .AppendSyntaxNode(ReturnType)
        .AppendSyntaxNode(Body)
        .AppendTrivia(LeadingTrivia);
    
    public override SourceSpan Span => FuncToken.Span + SourceSpan.FirstAndLastNotNull(Parameters, ReturnType, Body);
}
