namespace CodeProcess.SyntaxNode;

public struct SyntaxNodeSpan(uint startLine, uint startColumn, uint endLine, uint endColumn)
{
    public readonly uint StartLine = startLine;
    public readonly uint StartColumn = startColumn;
    public readonly uint EndLine = endLine;
    public readonly uint EndColumn = endColumn;

    public override string ToString() => $"{StartLine}:{StartColumn}:{EndLine}:{EndColumn}";
}
