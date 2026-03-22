using System.Text;

namespace Abstract.CodeProcess.Core.Language.SyntaxNodes.Control;

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
