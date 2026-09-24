using System.Text;

namespace Tqc.Syntax.Nodes;

public record FunctionDeclarationNode(
    AttributeNode[] attributes,
    TokenNode FuncToken,
    IdentifierExpressionNode Name,
    ParametersCollectionNode Parameters,
    ExpressionNode? ReturnType,
    ScopeNode? Body
) : DeclarationNode(attributes)
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendSyntaxNode(FuncToken)
        .AppendSyntaxNode(Name)
        .AppendSyntaxNode(Parameters)
        .AppendSyntaxNode(ReturnType)
        .AppendSyntaxNode(Body);
}
