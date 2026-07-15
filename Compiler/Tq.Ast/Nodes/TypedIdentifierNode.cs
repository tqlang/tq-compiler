using System.Text;

namespace Tq.Ast;

public class TypedIdentifierNode : ExpressionNode {
    public ExpressionNode Type;
    public IdentifierNode Identifier;

    private TypedIdentifierNode(ExpressionNode type, IdentifierNode identifier)
    {
        Type = type;
        Identifier = identifier;
    }

    public static TypedIdentifierNode Build(ExpressionNode type, IdentifierNode identifier) => new (type, identifier);

    public override StringBuilder AppendStringBuilder(StringBuilder sb) => sb.Append(Type).Append(Identifier);
}
