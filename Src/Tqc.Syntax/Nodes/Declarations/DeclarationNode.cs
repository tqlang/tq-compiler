namespace Tqc.Syntax.Nodes;

public abstract record DeclarationNode(TriviaNode[] LeadingTrivia) : SyntaxNode;
