using System.Buffers;
using System.Text;

namespace Tqc.Syntax;

internal class Lexer(string Source)
{
    private int _cursorPosition = 0;
    private int _lastPosition = 0;
    
    private int _line = 0;
    private int _column = 0;
    
    public LexerMode Mode = LexerMode.Normal;
    public enum LexerMode { Normal, String }
    
    public bool IsEof() => _cursorPosition >= Source.Length;

    public TokenValue NextToken()
    {
        return Mode switch
        {
            LexerMode.Normal => NextGenericToken(),
            LexerMode.String => NextStringToken(),
            _                => throw new InvalidOperationException()
        };
    }
    
    private TokenValue NextGenericToken()
    {
        var c = NextRune();
        if (c.Value is ' ' or '\t') return GetTokenValue(TokenKind.Whitespace);

        switch (c.Value)
        {
            case '\n' when NextRuneIf('\r'):
            case '\r' when NextRuneIf('\n'):
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
            
            case '.' when NextRuneIf('.'): return GetTokenValue(TokenKind.DotDot);
            case '.': return GetTokenValue(TokenKind.Dot);
            case ',': return GetTokenValue(TokenKind.Comma);
            
            case '<':
            {
                if (NextRuneIf('=')) return GetTokenValue(TokenKind.LessEqual);
                if (NextRuneIf('<')) return GetTokenValue(TokenKind.BitShiftLeft);
                return GetTokenValue(TokenKind.LeftAngle);
            }
            case '>':
            {
                if (NextRuneIf('=')) return GetTokenValue(TokenKind.GreaterEqual);
                if (NextRuneIf('>')) return GetTokenValue(TokenKind.BitShiftRight);
                return GetTokenValue(TokenKind.RightAngle);
            }

            case '+':
                return GetTokenValue(
                    NextRuneIf('=') ? TokenKind.AddAssign 
                        : NextRuneIf('+') ? TokenKind.Increment
                        : NextRuneIf('%') ? TokenKind.AddWrap
                        : NextRuneIf('|') ? TokenKind.AddOnBounds
                        : TokenKind.Plus
                );
            case '-':
                return GetTokenValue(
                    NextRuneIf('=') ? TokenKind.SubAssign 
                    : NextRuneIf('-') ? TokenKind.Decrement
                    : NextRuneIf('%') ? TokenKind.SubWrap
                    : NextRuneIf('|') ? TokenKind.SubOnBounds
                    : TokenKind.Plus
                );
            case '*':
                return GetTokenValue(
                    NextRuneIf('=') ? TokenKind.MulAssign
                    : NextRuneIf('%') ? TokenKind.MulWrap
                    : NextRuneIf('|') ? TokenKind.MulOnBounds
                    : TokenKind.Star
                );
            case '/':
                return GetTokenValue(
                    NextRuneIf('=') ? TokenKind.DivAssign
                    : NextRuneIf('_') ? TokenKind.DivFloor
                    : NextRuneIf('^') ? TokenKind.DivCeil
                    : TokenKind.Slash
                );
            case '%':
                return GetTokenValue(
                    NextRuneIf('=') ? TokenKind.RestAssign
                    : TokenKind.Rest
                );
            
            case '=':
                return NextRuneIf('=')
                    ? GetTokenValue(NextRuneIf('=')
                        ? TokenKind.ExactEquals : TokenKind.EqualsEquals)
                    : GetTokenValue(TokenKind.Equals);
            
            case '!':
                return NextRuneIf('=')
                    ? GetTokenValue(NextRuneIf('=')
                        ? TokenKind.NotExactEquals : TokenKind.NotEqualsEquals)
                    : GetTokenValue(TokenKind.Bang);
            
            case '⋏': return GetTokenValue(TokenKind.BitwiseAnd);
            case '⋎': return GetTokenValue(TokenKind.BitwiseOr);
            case '⊕': return GetTokenValue(TokenKind.BitwiseXor);
        }
         
        if (c.IsDecimalDigit())
        {
            var numBase = 10;
            var isFloating = false;
        
            if (c.Value == '0' && !IsEof()) // verify different bases
            {
                switch (PeekRune().ToLowerInvariant().Value)
                {
                    case 'x':
                    {
                        numBase = 16;
                        NextRune();
                    } break;
                    
                    // case 'o':
                    // {
                    //     numBase = 8;
                    //     NextChar();
                    // } break;
                    
                    case 'b':
                    {
                        numBase = 2;
                        NextRune();
                    } break;
                }
            }

            while (!IsEof())
            {
                var cc = PeekRune().Value;
                if (cc == '.')
                {
                    if (numBase != 10 || isFloating) break;

                    isFloating = true;
                    _ = NextRune();
                    continue;
                }

                var d = PeekRune();
                if (d.Value != '_')
                {
                    if (numBase == 10 && !d.IsDecimalDigit()) break;
                    if (numBase == 16 && !d.IsHexDigit()) break;
                    if (numBase == 2 && !d.IsBinaryDigit()) break;
                }

                NextRune();
            }

            return GetTokenValue(isFloating ? TokenKind.FloatingNumberLiteral : TokenKind.IntegerNumberLiteral);

        }

        // Build identifier token
        if (c.IsIdentifierStart())
        {
            while (PeekRune().IsIdentifierPart()) NextRune();
            return GetTokenValue(TokenKind.Identifier);
        }
        
        // Build comment
        if (c.Value == '#')
        {
            if (!IsEof() && PeekRune().Value == '#')
            {
                NextRune();
                
                while (!IsEof())
                {
                    if (PeekRune().Value == '#')
                    {
                        int savedPos = _cursorPosition;
                        NextRune();
                        
                        if (!IsEof() && PeekRune().Value == '#')
                        {
                            NextRune();
                            if (!IsEof() && PeekRune().Value == '#')
                            {
                                NextRune();
                                break;
                            }
                        }
                    }
                    
                    else NextRune();
                }
            }
            else
            {
                while (!IsEof() && PeekRune().Value != '\n' && PeekRune().Value != '\r') NextRune();
            }

            return GetTokenValue(TokenKind.Comment);
        }
        
        // unrecognized character
        {
            throw new NotImplementedException($"unknown token {c.ToString()} at {_line}:{_column}");
            // FIXME implement error handlers
            // try { throw new UnrecognizedCharacterException(c, i); }
            // catch (SyntaxException e) { currentSrc.ThrowError(e); }
        }
    }
    private TokenValue NextStringToken()
    {
        var c = NextRune();
        switch (c.Value)
        {
            case '\'': return GetTokenValue(TokenKind.Quote);
            case '"': return GetTokenValue(TokenKind.DoubleQuote);
                
            case '\\':
            {
                var c2 = NextRune();
                return GetTokenValue(c2.Value == '{'
                    ? TokenKind.EscapedLeftBracket
                    : TokenKind.EscapedCharacter);
            }

            default:
            {
                while (IsEof())
                {
                    var c2 = PeekRune();
                    switch (c2.Value)
                    {
                        case '\\' or '"' or '\'':
                            goto outLabel;
                    }
                    NextRune();
                }
                outLabel: return GetTokenValue(TokenKind.StringLiteral);
            }
        }
    }
    
    private bool NextRuneIf(char c) => NextRuneIf(new Rune(c));
    private bool NextRuneIf(Rune expected)
    {
        if (IsEof()) return false;

        var position = _cursorPosition;
        var actual = NextRune();
        if (actual == expected) return true;
        
        _cursorPosition = position;
        return false;
    }
    private Rune NextRune()
    {
        if (IsEof()) throw new EndOfStreamException();

        var status = Rune.DecodeFromUtf16(Source.AsSpan(_cursorPosition), out var rune, out var charsConsumed);
        if (status != OperationStatus.Done) throw new InvalidDataException($"Invalid UTF-16 sequence at {_cursorPosition}");

        _cursorPosition += charsConsumed;
        return rune;
    }
    private Rune PeekRune()
    {
        if (IsEof()) throw new EndOfStreamException();

        var status = Rune.DecodeFromUtf16(Source.AsSpan(_cursorPosition), out var rune, out _);
        if (status != OperationStatus.Done) throw new InvalidDataException($"Invalid UTF-16 sequence at {_cursorPosition}");

        return rune;
    }
    
    private TokenValue GetTokenValue(TokenKind kind)
    {
        var value = Source[_lastPosition .. _cursorPosition];
        
        var token = new TokenValue(value, kind, _line, _column);
        _lastPosition = _cursorPosition;
        
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            switch (c)
            {
                case '\r':
                {
                    _line++;
                    _column = 0;
                    if (i + 1 < value.Length && value[i + 1] == '\n') i++;
                    break;
                }
                
                case '\n':
                    _line++;
                    _column = 0;
                break;
                
                default: _column++; break;
            }
        }
    
        return token;
    }
    

    internal readonly record struct State(
        int CursorPosition,
        int LastPosition,
        int Line,
        int Column,
        LexerMode Mode
    );
    
    public LexCheckpoint SaveCheckpoint()
    {
        return new LexCheckpoint(new State(_cursorPosition, _lastPosition, _line, _column, Mode), this);
    }
    internal void Restore(State state)
    {
        _cursorPosition = state.CursorPosition;
        _lastPosition   = state.LastPosition;
        _line           = state.Line;
        _column         = state.Column;
        Mode            = state.Mode;
    }
}

public readonly struct LexCheckpoint
{
    private readonly Lexer _lexer;
    private readonly Lexer.State _state;

    internal LexCheckpoint(Lexer.State state, Lexer lexer)
    {
        _lexer = lexer;
        _state = state;
    }

    public void Restore()
    {
        _lexer.Restore(_state);
    }
}

internal record struct TokenValue(string value, TokenKind kind, int Line, int Column);
