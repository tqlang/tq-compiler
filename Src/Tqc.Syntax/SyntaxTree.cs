using Tqc.Diagnostics;

namespace Tqc.Syntax;

public class SyntaxTree
{
    public DiagnosticBag Diagnostics { get; } = new();
}
