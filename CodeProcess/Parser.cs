using CodeProcess.Lexing;

namespace CodeProcess;

public class Parser
{
    private TokensCollection.TokensCollectionEnumerator _tokens = null!;
    
    public SyntaxTree Parse(TokensCollection tokensCollection)
    {
        _tokens = (TokensCollection.TokensCollectionEnumerator)tokensCollection.GetEnumerator();
        ParseTreeRoot();
        _tokens = null!;

        return new SyntaxTree();
    }

    private void ParseTreeRoot()
    {
        while (!_tokens.Finished)
        {
            Console.WriteLine(_tokens.Advance());
        }
    }
    
}

