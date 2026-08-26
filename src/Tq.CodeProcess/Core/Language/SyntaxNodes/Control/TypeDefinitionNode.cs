using System.Text;

namespace Tq.CodeProcess.Core.Language.SyntaxNodes;

public class TypeDefinitionNode : ControlNode
{
    public IdentifierNode Identifier => (IdentifierNode)_children[_children.Count == 4 ? 2 : 1];
    public ArgumentCollectionNode? BackType => _children.Count == 4 ? (ArgumentCollectionNode)_children[1] : null;
    public BlockNode Body => (BlockNode)_children[_children.Count == 4 ? 3 : 2];

    public override string ToString()
    {
        var sb = new StringBuilder();
        
        sb.Append($"typedef{BackType} {Identifier} ");
        if (!Body.Content.Any())
        {
            sb.Append("{}");
            return sb.ToString();
        }
        sb.AppendLine("{");

        var blockList = Body.Content.ToArray();
        foreach (var b in blockList) sb.AppendLine($"\tcase {b}");
        
        sb.Append('}');

        return sb.ToString();
    }
}
