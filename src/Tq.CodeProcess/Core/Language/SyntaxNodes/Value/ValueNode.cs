using Abstract.CodeProcess.Core.Language.SyntaxNodes.Expression;

namespace Abstract.CodeProcess.Core.Language.SyntaxNodes.Value;

public abstract class ValueNode : ExpressionNode
{
    public ValueNode(Token token)
    {
        this.Token = token;
        _children = null!;
    }

    public Token Token;

    public override (uint line_start, uint line_end, uint start, uint end) Range
        => (Token.Range.line, Token.Range.line, Token.Range.start, Token.Range.end);


    public abstract override string ToString();

    public override string ToTree() => ToString();
}
