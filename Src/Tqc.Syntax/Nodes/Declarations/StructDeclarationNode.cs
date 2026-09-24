using System.Text;

namespace Tqc.Syntax.Nodes;

public record StructDeclarationNode(
    AttributeNode[] attributes,
    TokenNode StructKeyword,
    IdentifierExpressionNode Name,
    ParametersCollectionNode? Parameters,
    (SyntaxNode extends, ExpressionNode type)? extends,
    BlockNode Body
) : DeclarationNode(attributes)
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
        
        sb.AppendSyntaxNode(Body);
        
        return sb;
    } 
}
