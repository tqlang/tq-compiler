using System.Text;

namespace Tqc.Syntax.Nodes;

public record DestructorDeclarationNode(
    AttributeNode[] attributes,
    TokenNode DestructorKeyword,
    ParametersCollectionNode Parameters,
    ExpressionNode? ReturnType,
    ScopeNode? Body
) : DeclarationNode(attributes)
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendSyntaxNode(DestructorKeyword)
        .AppendSyntaxNode(Parameters)
        .AppendSyntaxNode(ReturnType)
        .AppendSyntaxNode(Body);
}
