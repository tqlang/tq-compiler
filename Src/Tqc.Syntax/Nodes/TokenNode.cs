namespace Tqc.Syntax.Nodes;

public record TokenNode(string? value, TokenKind kind) : SyntaxNode;