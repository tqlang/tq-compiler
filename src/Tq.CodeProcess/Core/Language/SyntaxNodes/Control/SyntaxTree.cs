using System.Text;

namespace Tq.CodeProcess.Core.Language.SyntaxNodes;

public class SyntaxTree(string path): ControlNode
{
    public readonly string Path = path;
    
    public override string ToString()
    {
        var sb = new StringBuilder();

        foreach (var c in  _children)
        {
            sb.AppendLine(c.ToString());
        }
        
        return sb.ToString();
    }
}
