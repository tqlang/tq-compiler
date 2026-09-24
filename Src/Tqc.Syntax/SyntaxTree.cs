using Tqc.Diagnostics;
using Tqc.Syntax.Nodes;

namespace Tqc.Syntax;

public record SyntaxTree(IReadOnlyList<SyntaxNode> Root)
{
    public DiagnosticBag Diagnostics { get; } = new();
}
