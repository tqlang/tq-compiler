namespace Tq.CodeProcess.Core.Language.SyntaxNodes;

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
