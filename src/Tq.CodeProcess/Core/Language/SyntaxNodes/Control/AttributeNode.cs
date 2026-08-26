using System.Text;

namespace Tq.CodeProcess.Core.Language.SyntaxNodes;

public class AttributeNode : ControlNode
{
    public override string ToString()
    {
        var sb = new StringBuilder();
        
        sb.Append('@');
        sb.Append(Children[1]);
        sb.Append('(');
        if (Children.Length > 2)
        {
            var args = (Children[2] as ArgumentCollectionNode)!.Arguments;
            sb.Append(string.Join(", ", args.Select(arg => arg.ToString())));
        }
        sb.Append(')');

        return sb.ToString();
    }
}
