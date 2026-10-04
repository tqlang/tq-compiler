namespace Tqc.Syntax.Nodes;

public abstract record StatementNode(TriviaNode[] LeadingTrivia) : SyntaxNode;
