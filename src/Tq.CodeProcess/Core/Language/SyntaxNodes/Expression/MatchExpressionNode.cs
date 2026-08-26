namespace Tq.CodeProcess.Core.Language.SyntaxNodes;

public class MatchExpressionNode : ExpressionNode
{
    public ExpressionNode Expression => (ExpressionNode)Children[1];
    public BlockNode Scope => (BlockNode)Children[2];
}

public abstract class MatchExpressionItemNode : SyntaxNode;

public class MatchExpressionCaseNode : MatchExpressionItemNode
{
    public ExpressionNode Value => (ExpressionNode)Children[1];
    public SyntaxNode Operation => Children[3];

    public override string ToString() => $"case {Value} => {Operation}";
}

public class MatchExpressionDefaultNode : MatchExpressionItemNode
{
    public SyntaxNode Operation => Children[2];

    public override string ToString() => $"default => {Operation}";
}
