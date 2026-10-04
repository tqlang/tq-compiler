using System.Text;
using Tqc.Core;
using Tqc.Diagnostics;
using Tqc.Syntax.Nodes;

namespace Tqc.Syntax;

public record SyntaxTree(string FilePath, SyntaxNode[] Root) : SyntaxNode
{
    public DiagnosticBag Diagnostics { get; } = new();
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => throw new NotImplementedException();

    public override SourceSpan Span => default;
}
