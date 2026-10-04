using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

public record DestructorDeclarationNode(
    TokenNode DestructorKeyword,
    ParametersCollectionNode Parameters,
    ExpressionNode? ReturnType,
    ScopeNode? Body,
    TriviaNode[] LeadingTrivia
) : DeclarationNode(LeadingTrivia)
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendSyntaxNode(DestructorKeyword)
        .AppendSyntaxNode(Parameters)
        .AppendSyntaxNode(ReturnType)
        .AppendSyntaxNode(Body)
        .AppendTrivia(LeadingTrivia);
    
    public override SourceSpan Span => DestructorKeyword.Span + SourceSpan.FirstAndLastNotNull(Parameters, ReturnType, Body);
}
