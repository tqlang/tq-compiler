using System.Text;

namespace Tq.Ast;

public class LocalVariableExpressionNode: ExpressionNode
{
    public TokenNode Definition;
    public ExpressionNode? Type;
    public IdentifierNode Identifier;

    private LocalVariableExpressionNode(TokenNode definition, ExpressionNode? type, IdentifierNode identifier)
    {
        Definition = definition;
        Type = type;
        Identifier = identifier;
    }

    public static LocalVariableExpressionNode Build(TokenNode definition, ExpressionNode? type, IdentifierNode identifier)
        => new (definition, type, identifier);

    public override StringBuilder AppendStringBuilder(StringBuilder sb) => sb.Append(Definition).Append(Type).Append(Identifier);
}
