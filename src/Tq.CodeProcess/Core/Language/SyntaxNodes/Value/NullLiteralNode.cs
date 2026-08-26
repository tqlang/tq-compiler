namespace Tq.CodeProcess.Core.Language.SyntaxNodes;

public class NullLiteralNode(Token t) : ValueNode(t)
{
    public override string ToString() => "null";
}
