using System.Collections;

namespace CodeProcess.Lexing;

public class TokensCollection(Token[] tokens) : IEnumerable<Token>
{
    private Token[]  _tokens = tokens;

    public Token this[int index] => _tokens[index];

    public IEnumerator<Token> GetEnumerator() => new TokensCollectionEnumerator(this);
    IEnumerator IEnumerable.GetEnumerator() => new TokensCollectionEnumerator(this);
    
    
    public class TokensCollectionEnumerator(TokensCollection parent) : IEnumerator<Token>
    {
        private readonly TokensCollection parent = parent;
        private int index = -1;
        
        public Token Current => index >= parent._tokens.Length ? parent.Last() : parent[index];
        object IEnumerator.Current => Current;

        public bool Finished => index >= parent._tokens.Length;

        public Token Advance()
        {
            MoveNext();
            return Current;
        }
        public bool AdvanceIf(TokenType tokenType, out Token token)
        {
            if (Current.Type != tokenType)
            {
                token = default;
                return false;
            }

            token = Current;
            MoveNext();
            return true;
        }
        
        
        public bool MoveNext()
        {
            index++;
            return !Finished;
        }
        public void Reset()
        {
            index = -1;
        }

        
        void IDisposable.Dispose() { }
    }
}
