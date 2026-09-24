using System.Text;
using Tqc.Diagnostics;
using Tqc.Syntax.Nodes;

namespace Tqc.Syntax;

public record SyntaxTree(IReadOnlyList<SyntaxNode> Root) : SyntaxNode
{
    public DiagnosticBag Diagnostics { get; } = new();
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => throw new NotImplementedException();
}
