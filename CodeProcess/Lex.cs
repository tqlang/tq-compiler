using CodeProcess.Lexing;

namespace CodeProcess;

public ref struct Lex
{
    
    private ReadOnlySpan<char> _source;
    
    private int _span_start;
    private int _span_end;

    private int _currentLine_begin;
    private int _currentColumn_beguin;
    private int _currentLine_end;
    private int _currentColumn_end;
    
    private List<Token> _tokens;
    
    private const char EOF = (char)0x1A;
    private static Dictionary<string, TokenType> _keyword2TokenMap = new()
    {
        // keywords
        { "from", TokenType.FromKeyword },
        { "import", TokenType.ImportKeyword },

        { "let", TokenType.LetKeyword },
        { "const", TokenType.ConstKeyword },
        { "func", TokenType.FuncKeyword },
        { "struct", TokenType.StructKeyword },
        { "extends", TokenType.ExtendsKeyword },
        { "packet", TokenType.PacketKeyword },
        { "typedef", TokenType.TypedefKeyword },

        { "switch", TokenType.SwitchKeyword },
        { "match", TokenType.MatchKeyword },
        { "if", TokenType.IfKeyword },
        { "elif", TokenType.ElifKeyword },
        { "else", TokenType.ElseKeyword },

        { "while", TokenType.WhileKeyword },
        { "for", TokenType.ForKeyword },
        { "do", TokenType.DoKeyword },
        { "in", TokenType.InKeyword },
        { "break", TokenType.BreakKeyword },

        { "return", TokenType.ReturnKeyword },

        { "as", TokenType.AsKeyword },
        { "new", TokenType.NewKeyword },

        // values
        { "null", TokenType.NullKeyword },
        { "true", TokenType.TrueKeyword },
        { "false", TokenType.FalseKeyword },

        // operators
        { "and", TokenType.AndOperator },
        { "or", TokenType.OrOperator },

        // types
        { "void", TokenType.TypeKeyword },
        { "noreturn", TokenType.TypeKeyword },
        { "type", TokenType.TypeKeyword },

        { "byte", TokenType.TypeKeyword },
        { "f32", TokenType.TypeKeyword },   { "float", TokenType.TypeKeyword },
        { "f64", TokenType.TypeKeyword },   { "double", TokenType.TypeKeyword },

        { "bool", TokenType.TypeKeyword },
        { "char", TokenType.TypeKeyword },
        { "string", TokenType.TypeKeyword },

    };
    
    public List<Token> Parse(ReadOnlySpan<char> source)
    {
        _source = source; 
        _currentLine_begin = _currentColumn_beguin = _currentLine_end = _currentColumn_end = 0;
        
        _tokens = [];

        while (!IsEof())
        {
            var c = Advance();

            switch (c)
            {
                case '\t' or ' ':
                    while (NextIs(' ', '\t') && !IsEof()) ;
                    CreateToken(TokenType.WhitespaceTrivia);
                    continue;
                
                case '\n' or '\r':
                    while (NextIs('\n', '\r') && !IsEof()) ;
                    CreateToken(TokenType.LineFeedTrivia);
                    continue;
                
                case '(': CreateToken(TokenType.LeftPerenthesisChar); break;
                case ')': CreateToken(TokenType.RightParenthesisChar); break;
                case '{': CreateToken(TokenType.LeftBracketChar); break;
                case '}': CreateToken(TokenType.RightBracketChar); break;
                case '[': CreateToken(TokenType.LeftSquareBracketChar); break;
                case ']': CreateToken(TokenType.RightSquareBracketChar); break;
                case '?': CreateToken(TokenType.QuestionChar); break;
                case '!': CreateToken(TokenType.BangChar); break;
                case '@': CreateToken(TokenType.AtSiginChar); break;
                case ',': CreateToken(TokenType.CommaChar); break;
                case '.': CreateToken(TokenType.DotChar); break;
                case ':': CreateToken(TokenType.ColonChar); break;
                
                case '<':
                {
                    if (NextIs('=')) CreateToken(TokenType.LessEqualsOperator);
                    else if (NextIs('<')) CreateToken(NextIs('=')
                        ? TokenType.BitShiftLeftAssign
                        : TokenType.BitShiftLeftOperator);
                    else CreateToken(TokenType.LeftAngleChar);
                } break;
                
                case '>':
                {
                    if (NextIs('=')) CreateToken(TokenType.GreatEqualsOperator);
                    else if (NextIs('>')) CreateToken(NextIs('=')
                        ? TokenType.BitShiftRightAssign
                        : TokenType.BitShiftLeftOperator);
                    else CreateToken(TokenType.RightAngleChar);
                } break;
                
                case '+': CreateToken(
                          NextIs('=') ? TokenType.AddAssigin
                        : NextIs('+') ? TokenType.IncrementOperator
                        : NextIs('%') ? TokenType.AddWarpOperator
                        : NextIs('|') ? TokenType.SubOnBoundsOperator
                        : TokenType.CrossChar); break;
                
                case '-': CreateToken(
                          NextIs('=') ? TokenType.SubAssigin
                        : NextIs('-') ? TokenType.DecrementOperator
                        : NextIs('%') ? TokenType.SubWarpOperator
                        : NextIs('|') ? TokenType.SubOnBoundsOperator
                        : TokenType.MinusChar); break;
                
                case '*': CreateToken(NextIs('=')
                    ? TokenType.MulAssigin
                    : TokenType.StarChar); break;
                
                case '/': CreateToken(
                          NextIs('=') ? TokenType.DivAssigin
                        : NextIs('^') ? TokenType.DivideCeilOperator
                        : NextIs('_') ? TokenType.DivideFloorOperator
                        : TokenType.SlashChar); break;
                
                case '%': CreateToken(NextIs('=')
                    ? TokenType.RestAssigin
                    : TokenType.PercentChar); break;
                
                case '=': CreateToken(NextIs('=')
                    ? TokenType.EqualOperator
                    : NextIs('>') 
                        ? TokenType.RightArrowOperator
                        : TokenType.EqualsChar); break;
                
                case '^': CreateToken(NextIs('=')
                    ? TokenType.BitwiseXorAssign
                    : TokenType.CircumflexChar); break;
                
                case '|': CreateToken(NextIs('=')
                    ? TokenType.BitwiseOrAssign
                    : TokenType.PipeChar); break;
                
                case '&': CreateToken(NextIs('=')
                    ? TokenType.BitwiseAndAssign
                    : TokenType.AmpersandChar); break;
                
                case '#':
                {
                    if (NextStringIs("##"))
                    {
                        while (!NextStringIs("###")) Advance();
                        CreateToken(TokenType.CommentTrivia);
                    }
                    else
                    {
                        while (Peek() != '\n') Advance();
                        CreateToken(TokenType.CommentTrivia);
                    }
                } break;
                
                default:
                {
                    // Build number token
                    if (char.IsDigit(c))
                    {
                        var numBase = 10;
                        var isFloating = false;
                    
                        if (c == '0' && !IsEof()) // verify different bases
                        {
                            if (char.ToLower(Peek()) == 'x')
                            {
                                numBase = 16;
                                Advance();
                            }
                            if (char.ToLower(Peek()) == 'b')
                            {
                                numBase = 2;
                                Advance();
                            }
                        }

                        while (!IsEof())
                        {
                            char cc = Peek();
                            if (cc == '.')
                            {
                                if (numBase != 10 || isFloating) break;

                                isFloating = true;
                                Advance();
                                continue;
                            }

                            if (numBase == 10 && !char.IsDigit(Peek())) break;
                            if (numBase == 16 && !char.IsAsciiHexDigit(Peek())) break;
                            if (numBase == 2  && Peek() is not ('0' or '1')) break;
                            
                            Advance();
                        }

                        CreateToken(isFloating
                                ? TokenType.FloatingNumberLiteral
                                : TokenType.IntegerNumberLiteral);

                    }

                    // Build identifier token
                    else if (IsValidOnIdentifierStarter(c))
                    {
                        while (IsValidOnIdentifier(Peek())) Advance();
                        CreateToken(_keyword2TokenMap.GetValueOrDefault(GetSlice(), TokenType.Identifier));
                    }

                    // Build string tokens
                    else if (c == '"')
                    {
                        CreateToken(TokenType.DoubleQuotes);

                        while (!IsEof())
                        {
                            // test escape
                            if (Peek() == '\\')
                            {
                                if (IsDirty()) CreateToken(TokenType.StringLiteral);
                            
                                if (IsEof()) break;
                                char escapeCode = Advance();
                            
                                // interpolation
                                if (escapeCode == '{') throw new NotImplementedException();
                            
                                // hexadecimal
                                else if (escapeCode == 'x')
                                {
                                    while (Peek() != '\\') Advance();
                                    CreateToken(TokenType.CharacterLiteral);
                                }
                                else
                                {
                                    Advance();
                                    CreateToken(TokenType.CharacterLiteral);
                                }
                            }

                            // test end of string
                            else if (Peek() == '"')
                            {
                                if (IsDirty()) CreateToken(TokenType.StringLiteral);
                                Advance();
                                CreateToken(TokenType.DoubleQuotes);
;                               break;
                            }
                            else Advance();
                        }
                    }
                    else
                    {
                        throw new NotImplementedException("Unhandled character: " + c);
                    }
                } break;
            }
        }
        
        return _tokens;
    }


    private bool CreateToken(TokenType type)
    {
        _tokens.Add(new Token(
            type,
            (uint)_span_start,
            (uint)(_span_end - _span_start), 
            (uint)_currentLine_begin,
            (uint)_currentColumn_beguin));
        Discard();
        return true;
    }

    private void Discard()
    {
        _currentLine_end = _currentLine_begin;
        _currentColumn_beguin = _currentColumn_end;
        _span_start = _span_end;
    }

    private bool IsDirty() => _span_start != _span_end;

    
    private char Advance()
    {
        if (IsEof()) return (char)0x1A;
            
        var c = _source[_span_end++];
        _currentColumn_end++;

        switch (c)
        {
            case '\n': NextLine(); break;
        }
            
        return c;
    }
    private char Peek()
    {
        if (IsEof()) return (char)0x1A;
        return _source[_span_end];
    }
    private string GetSlice() => _source[_span_start .. _span_end].ToString();
    
    private bool NextIs(params char[] c)
    {
        var c2 = Peek();
        if (!c.Contains(c2)) return false;
        Advance();
        return true;
    }

    private bool NextStringIs(string s)
    {
        if (_source.Length > _span_end + s.Length) return false;
        var res = s == _source.Slice(_span_end, s.Length);
        if (res) _span_end += s.Length;
        return res;
    }
    
    private void NextLine()
    {
        _currentLine_end++;
        _currentColumn_end = 0;
    }
    private bool IsEof() => _span_end >= _source.Length;

    private bool IsValidOnIdentifierStarter(char c) => char.IsAsciiLetter(c) || c == '_';
    private bool IsValidOnIdentifier(char c) => char.IsAsciiLetterOrDigit(c) || c == '_';
}
