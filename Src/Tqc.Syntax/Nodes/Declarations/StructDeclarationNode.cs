using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

public record StructDeclarationNode(
    TokenNode StructKeyword,
    IdentifierExpressionNode Name,
    ParametersCollectionNode? Parameters,
    (SyntaxNode extends, ExpressionNode type)? extends,
    ImplementsClauseNode? Implements,
    BlockNode Body,
    TriviaNode[] LeadingTrivia
) : DeclarationNode(LeadingTrivia)
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb)
    {
        sb
            .AppendSyntaxNode(StructKeyword)
            .AppendSyntaxNode(Name)
            .AppendSyntaxNode(Parameters);
        
        if (extends != null)
        {
            sb
                .AppendSyntaxNode(extends.Value.extends)
                .AppendSyntaxNode(extends.Value.type);
        }
        
        return sb
            .AppendSyntaxNode(Implements)
            .AppendSyntaxNode(Body)
            .AppendTrivia(LeadingTrivia);
    }

    /// <summary>Declared with `protocol` instead of `struct`.</summary>
    public bool IsProtocol => StructKeyword.Value == "protocol";

    public override SourceSpan Span => StructKeyword.Span + Body.Span;
}
