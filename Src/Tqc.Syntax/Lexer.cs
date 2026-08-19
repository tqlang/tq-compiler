namespace Tqc.Syntax;

internal class Lexer(string Source)
{
    private int _cursorPosition = 0;
    private int _lastPosition = 0;
    
    public bool IsEof() => _cursorPosition >= Source.Length;
    
    private TokenValue NextGenericToken()
    {
        var c = NextChar();
        if (c is ' ' or '\t') return GetTokenValue(TokenKind.Whitespace);

        switch (c)
        {
            case '\n' when NextCharIf('\r'):
            case '\n' or '\r':
                return GetTokenValue(TokenKind.LineFeed);
            
            case ';': return GetTokenValue(TokenKind.Semicolon);
            
            case '(': return GetTokenValue(TokenKind.LeftParenthesis);
            case ')': return GetTokenValue(TokenKind.RightParenthesis);
            case '{': return GetTokenValue(TokenKind.LeftBrace);
            case '}': return GetTokenValue(TokenKind.RightBrace);
            case '[': return GetTokenValue(TokenKind.LeftBracket);
            case ']': return GetTokenValue(TokenKind.RightBracket);
            
            case '?': return GetTokenValue(TokenKind.QuestionMark);
            case '@': return GetTokenValue(TokenKind.At);
            case ':': return GetTokenValue(TokenKind.Colon);
            
            case '\'': return GetTokenValue(TokenKind.Quote);
            case '"': return GetTokenValue(TokenKind.DoubleQuote);
            
            case '<':
            {
                if (NextCharIf('=')) return GetTokenValue(TokenKind.LessEqual);
                if (NextCharIf('<'))
                {
                    return GetTokenValue(NextCharIf('=')
                        ? TokenKind.BitShiftLeftAssign
                        : TokenKind.BitShiftLeft);
                }
                return GetTokenValue(TokenKind.LeftAngle);
            }
            case '>':
            {
                if (NextCharIf('=')) return GetTokenValue(TokenKind.GreaterEqual);
                if (NextCharIf('>'))
                {
                    return GetTokenValue(NextCharIf('=')
                        ? TokenKind.BitShiftRightAssign
                        : TokenKind.BitShiftRight);
                }
                return GetTokenValue(TokenKind.RightAngle);
            }

            case '+':
                return GetTokenValue(
                    NextCharIf('=') ? TokenKind.AddAssign 
                        : NextCharIf('+') ? TokenKind.Increment
                        : NextCharIf('%') ? TokenKind.AddWrap
                        : NextCharIf('|') ? TokenKind.AddOnBounds
                        : TokenKind.Plus
                );
            
            case '-':
                return GetTokenValue(
                    NextCharIf('=') ? TokenKind.SubAssign 
                    : NextCharIf('-') ? TokenKind.Decrement
                    : NextCharIf('%') ? TokenKind.SubWrap
                    : NextCharIf('|') ? TokenKind.SubOnBounds
                    : TokenKind.Plus
                );
            
            case '*':
                return GetTokenValue(
                    NextCharIf('=') ? TokenKind.MulAssign
                    : NextCharIf('%') ? TokenKind.MulWrap
                    : NextCharIf('|') ? TokenKind.MulOnBounds
                    : TokenKind.Star
                );
            
            case '/':
                return GetTokenValue(
                    NextCharIf('=') ? TokenKind.DivAssign
                    : NextCharIf('_') ? TokenKind.DivFloor
                    : NextCharIf('^') ? TokenKind.DivCeil
                    : TokenKind.Slash
                );
            
            case '%':
                return GetTokenValue(
                    NextCharIf('=') ? TokenKind.RestAssign
                    : TokenKind.Rest
                );
            
            case '=':
                return NextCharIf('=')
                    ? GetTokenValue(NextCharIf('=')
                        ? TokenKind.ExactEquals : TokenKind.EqualsEquals)
                    : GetTokenValue(TokenKind.Equals);
            
            case '!':
                return NextCharIf('=')
                    ? GetTokenValue(NextCharIf('=')
                        ? TokenKind.NotExactEquals : TokenKind.NotEqualsEquals)
                    : GetTokenValue(TokenKind.Bang);
        }
         
        if (char.IsDigit(c))
        {
            var numBase = 10;
            var isFloating = false;
        
            if (c == '0' && !IsEof()) // verify different bases
            {
                switch (char.ToLower(PeekChar()))
                {
                    case 'x':
                    {
                        numBase = 16;
                        NextChar();
                    } break;
                    
                    // case 'o':
                    // {
                    //     numBase = 8;
                    //     NextChar();
                    // } break;
                    
                    case 'b':
                    {
                        numBase = 2;
                        NextChar();
                    } break;
                }
            }

            while (!IsEof())
            {
                var cc = PeekChar();
                if (cc == '.')
                {
                    if (numBase != 10 || isFloating) break;

                    isFloating = true;
                    _ = NextChar();
                    continue;
                }

                var d = PeekChar();
                if (d != '_')
                {
                    if (numBase == 10 && !char.IsDigit(d)) break;
                    if (numBase == 16 && !char.IsAsciiHexDigit(d)) break;
                    if (numBase == 2 && d is not ('0' or '1')) break;
                }

                NextChar();
            }

            return GetTokenValue(isFloating ? TokenKind.FloatingNumberLiteral : TokenKind.IntegerNumberLiteral);

        }

        // Build identifier token
        if (c.IsValidOnIdentifierStarter())
        {
            while (PeekChar().IsValidOnIdentifier()) NextChar();
            return GetTokenValue(TokenKind.Identifier);
        }
        
        // unrecognized character
        {
            throw new NotImplementedException(c.ToString());
            // FIXME implement error handlers
            // try { throw new UnrecognizedCharacterException(c, i); }
            // catch (SyntaxException e) { currentSrc.ThrowError(e); }
        }
    }
    private TokenValue NextStringToken()
    {
        throw new NotImplementedException();
    }
    
    private bool NextCharIf(char c)
    {
        if (IsEof()) throw new EndOfStreamException();
        if (Source[_cursorPosition] != c) return false;
        _cursorPosition++;
        return true;
    }
    private char NextChar() => IsEof() ? throw new EndOfStreamException() : Source[_cursorPosition++];
    private char PeekChar() => IsEof() ? throw new EndOfStreamException() : Source[_cursorPosition];
    
    private TokenValue GetTokenValue(TokenKind kind)
    {
        var value = Source[_lastPosition .. _cursorPosition];
        _lastPosition = _cursorPosition;
        return new TokenValue(value, kind);
    }
}

internal record struct TokenValue(string value, TokenKind kind);
