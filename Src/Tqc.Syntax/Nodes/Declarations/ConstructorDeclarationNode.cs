using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

public record ConstructorDeclarationNode(
    TokenNode ConstructorToken,
    ParametersCollectionNode Parameters,
    ExpressionNode? ReturnType,
    ScopeNode? Body,
    TriviaNode[] LeadingTrivia
) : DeclarationNode(LeadingTrivia)
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendSyntaxNode(ConstructorToken)
        .AppendSyntaxNode(Parameters)
        .AppendSyntaxNode(ReturnType)
        .AppendSyntaxNode(Body)
        .AppendTrivia(LeadingTrivia);
    
    public override SourceSpan Span => ConstructorToken.Span + SourceSpan.FirstAndLastNotNull(Parameters, ReturnType, Body);
}
