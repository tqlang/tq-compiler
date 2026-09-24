using System.Text;

namespace Tqc.Syntax.Nodes;

public record ConstructorDeclarationNode(
    AttributeNode[] attributes,
    TokenNode ConstructorToken,
    ParametersCollectionNode Parameters,
    ExpressionNode? ReturnType,
    ScopeNode? Body
) : DeclarationNode(attributes)
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendSyntaxNode(ConstructorToken)
        .AppendSyntaxNode(Parameters)
        .AppendSyntaxNode(ReturnType)
        .AppendSyntaxNode(Body);
}
