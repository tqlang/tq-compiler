using System.Text;

namespace Tq.CodeProcess.Core.Language.SyntaxNodes;

public class StringLiteralNode() : ExpressionNode()
{
    public SyntaxNode[] Content => [.. _children[1..^1]];

    public bool IsSimple => !_children[1..^1].Any(e => e is StringInterpolationNode);
    public string RawContent => BuildStringContent();

    public override string ToString() => $"\"{RawContent}\"";

    private string BuildStringContent()
    {
        var str = new StringBuilder();

        foreach (var i in _children[1..^1])
        {
            if (i is StringSectionNode @sec) str.Append(sec.Value);
            else if (i is CharacterLiteralNode @charr) str.Append(charr.BuildCharacter());
            else throw new InvalidOperationException(i.ToString());
        }

        return str.ToString();
    }
}
