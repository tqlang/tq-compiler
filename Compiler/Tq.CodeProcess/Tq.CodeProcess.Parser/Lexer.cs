using Tq.Ast;
using Tq.Core;

namespace Tq.CodeProcess.Parser;

public class Lexer(
    ErrorHandler errorHandler,
    DebugOptions debugOptions = default
) {
    
    private readonly Dictionary<MemoryKey, string> _cache = new ();
    private string GetOrAddCached(ReadOnlyMemory<char> memory)
    {
        var key = new MemoryKey(memory);
        if (_cache.TryGetValue(key, out var existing)) return existing;
        var str = new string(memory.Span);
        _cache[key] = str;
        return str;
    }

    private readonly Dictionary<string, TokenType> _keywordsMap = new()
    {
        { "from", TokenType.FromKeyword },
        { "import", TokenType.ImportKeyword },
        { "let", TokenType.LetKeyword },
        { "const", TokenType.ConstKeyword },
        { "func", TokenType.FuncKeyword },
        { "struct", TokenType.StructKeyword },
        { "typedef", TokenType.TypedefKeyword },
        { "extends", TokenType.ExtendsKeyword },
        { "implements", TokenType.ImplementsKeyword },
        
        { "if", TokenType.IfKeyword },
        { "elif", TokenType.ElifKeyword },
        { "else", TokenType.ElseKeyword },
        { "for", TokenType.ForKeyword },
        { "while", TokenType.WhileKeyword },
        { "match", TokenType.MatchKeyword },
        { "new", TokenType.NewKeyword },
        { "async", TokenType.AsyncKeyword },
        { "await", TokenType.AwaitKeyword },
        { "label", TokenType.LabelKeyword },
        { "goto", TokenType.GotoKeyword },
        { "try", TokenType.TryKeyword },
        { "catch", TokenType.CatchKeyword },
        { "throw", TokenType.ThrowKeyword },
        { "destroy", TokenType.DestroyKeyword },
        { "defer", TokenType.DeferKeyword },
        { "errdefer", TokenType.ErrdeferKeyword },
        { "unsafe", TokenType.UnsafeKeyword },
        { "return", TokenType.ReturnKeyword },
        
        { "as", TokenType.AsKeyword },
        { "in", TokenType.InKeyword },
        { "or", TokenType.OrKeyword },
        { "and", TokenType.AndKeyword },
        
        { "true", TokenType.TrueKeyword },
        { "false", TokenType.FalseKeyword },
        { "null", TokenType.NullKeyword },
    };
    
    private Token Tokenize(TokenType type, (ReadOnlyMemory<char> val, uint line, uint start, uint len) range) => new (
        null,
        type,
        (int)range.line,
        (int)range.start,
        (int)range.len
    );
    private Token TokenizeValue(TokenType type, (ReadOnlyMemory<char> val, uint line, uint start, uint len) range) => new (
            GetOrAddCached(range.val),
            type,
            (int)range.line,
            (int)range.start,
            (int)range.len
        );
    
    public Token[] Lex(string source)
    {
        List<Token> tokens = [];
        SourceWindow src = new (source);
        
        LexRange(src, tokens);
        tokens.Add(Tokenize(TokenType.EofChar, src.GetSlice()));
        
        return [.. tokens];
    }
    public void ClearCache() => _cache.Clear();
    
    private void LexRange(SourceWindow src, List<Token> tokens)
    {

        while (!src.IsEof())
        {
            var c = src.Next();

            switch (c)
            {
                // Check single characters
                case '\n' or '\r': tokens.Add(Tokenize(TokenType.LineFeedChar, src.GetSlice())); break;
                case ' ':          tokens.Add(Tokenize(TokenType.SpaceChar, src.GetSlice())); break;
                case '\t':         tokens.Add(Tokenize(TokenType.TabChar, src.GetSlice())); break;

                case '(': tokens.Add(Tokenize(TokenType.LeftParenthesisChar, src.GetSlice())); break;
                case ')': tokens.Add(Tokenize(TokenType.RightParenthesisChar, src.GetSlice())); break;
                case '{': tokens.Add(Tokenize(TokenType.LeftCurlyBraceChar, src.GetSlice())); break;
                case '}': tokens.Add(Tokenize(TokenType.RightCurlyBraceChar, src.GetSlice())); break;
                case '[': tokens.Add(Tokenize(TokenType.LeftSquareBracketChar, src.GetSlice())); break;
                case ']': tokens.Add(Tokenize(TokenType.RightSquareBracketChar, src.GetSlice())); break;
                case '?': tokens.Add(Tokenize(TokenType.QuestionChar, src.GetSlice())); break;
                case '@': tokens.Add(Tokenize(TokenType.AtSignChar, src.GetSlice())); break;
                case ',': tokens.Add(Tokenize(TokenType.CommaChar, src.GetSlice())); break;
                case '.': tokens.Add(Tokenize(TokenType.DotChar, src.GetSlice())); break;
                case ':': tokens.Add(Tokenize(TokenType.ColonChar, src.GetSlice())); break;
                case ';': tokens.Add(Tokenize(TokenType.SemiColonChar, src.GetSlice())); break;
                case '~': tokens.Add(Tokenize(TokenType.TildeChar, src.GetSlice())); break;
                case '|': tokens.Add(Tokenize(TokenType.PipeChar, src.GetSlice())); break;
                case '&': tokens.Add(Tokenize(TokenType.AmpersandChar, src.GetSlice())); break;

                case '<':
                {
                    if (src.NextIs('='))
                        tokens.Add(Tokenize(TokenType.LessEqualsOperator, src.GetSlice()));
                    else if (src.NextIs('<'))
                        tokens.Add(
                            src.NextIs('=')
                                ? Tokenize(TokenType.BitShiftLeftAssign, src.GetSlice())
                                : Tokenize(TokenType.BitShiftLeftOperator, src.GetSlice())
                        );
                    else
                        tokens.Add(Tokenize(TokenType.LeftAngleChar, src.GetSlice()));
                }

                break;

                case '>':
                {
                    if (src.NextIs('='))
                        tokens.Add(Tokenize(TokenType.GreatEqualsOperator, src.GetSlice()));
                    else if (src.NextIs('>'))
                        tokens.Add(
                            src.NextIs('=')
                                ? Tokenize(TokenType.BitShiftRightAssign, src.GetSlice())
                                : Tokenize(TokenType.BitShiftRightOperator, src.GetSlice())
                        );
                    else
                        tokens.Add(Tokenize(TokenType.RightAngleChar, src.GetSlice()));
                }

                break;

                case '+':
                    tokens.Add(
                        Tokenize(
                            src.NextIs('=')
                                ? TokenType.AddAssign
                                : src.NextIs('+')
                                    ? TokenType.IncrementOperator
                                    : src.NextIs('%')
                                        ? TokenType.AddWrapOperator
                                        : src.NextIs('|')
                                            ? TokenType.SubOnBoundsOperator
                                            : TokenType.CrossChar,
                            src.GetSlice()
                        )
                    );

                break;

                case '-':
                    tokens.Add(
                        Tokenize(
                            src.NextIs('=')
                                ? TokenType.SubAssign
                                : src.NextIs('-')
                                    ? TokenType.DecrementOperator
                                    : src.NextIs('%')
                                        ? TokenType.SubWrapOperator
                                        : src.NextIs('|')
                                            ? TokenType.SubOnBoundsOperator
                                            : TokenType.MinusChar,
                            src.GetSlice()
                        )
                    );

                break;

                case '*':
                    tokens.Add(
                        Tokenize(
                            src.NextIs('=')
                                ? TokenType.MulAssign
                                : src.NextIs('*')
                                    ? TokenType.PowerOperator
                                    : src.NextIs('%')
                                        ? TokenType.MulWrapOperator
                                        : src.NextIs('|')
                                            ? TokenType.MulOnBoundsOperator
                                            : TokenType.StarChar,
                            src.GetSlice()
                        )
                    );

                break;

                case '/':
                    tokens.Add(
                        Tokenize(
                            src.NextIs('=')
                                ? TokenType.DivAssign
                                : src.NextIs('^')
                                    ? TokenType.DivideCeilOperator
                                    : src.NextIs('_')
                                        ? TokenType.DivideFloorOperator
                                        : TokenType.SlashChar,
                            src.GetSlice()
                        )
                    );

                break;

                case '%':
                    tokens.Add(
                        src.NextIs('=')
                            ? Tokenize(TokenType.RestAssign, src.GetSlice())
                            : Tokenize(TokenType.PercentChar, src.GetSlice())
                    );

                break;

                case '=':
                    tokens.Add(
                        src.NextIs('=')
                            ? Tokenize(
                                src.NextIs('=')
                                    ? TokenType.ExactEqualOperator
                                    : TokenType.EqualOperator,
                                src.GetSlice()
                            )
                            : src.NextIs('>')
                                ? Tokenize(TokenType.RightArrowOperator, src.GetSlice())
                                : Tokenize(TokenType.EqualsChar, src.GetSlice())
                    );

                break;

                case '^':
                    tokens.Add(
                        src.NextIs('=')
                            ? Tokenize(TokenType.BitwiseXorAssign, src.GetSlice())
                            : Tokenize(TokenType.CircumflexChar, src.GetSlice())
                    );

                break;

                case '!':
                    tokens.Add(
                        src.NextIs('=')
                            ? Tokenize(
                                src.NextIs('=')
                                    ? TokenType.ExactUnequalOperator
                                    : TokenType.UnequalOperator,
                                src.GetSlice()
                            )
                            : Tokenize(TokenType.BangChar, src.GetSlice())
                    );

                break;
                
                case '#':
                {
                    if (src.NextStringIs("##"))
                    {
                        src.Next();
                        src.Next();
                        while (!src.IsEof() && !src.NextStringIs("###")) src.Next();
                        tokens.Add(TokenizeValue(TokenType.Comment, src.GetSlice()));
                    } else
                    {
                        while (src.Peek() != '\n') src.Next();
                        tokens.Add(TokenizeValue(TokenType.Comment, src.GetSlice()));
                    }
                }
                break;

                default:
                {
                    // Build number token
                    if (char.IsDigit(c))
                    {
                        var numBase    = 10;
                        var isFloating = false;

                        if (c == '0' && !src.IsEof()) // verify different bases
                        {
                            if (char.ToLower(src.Peek()) == 'x')
                            {
                                numBase = 16;
                                src.Next();
                            }

                            if (char.ToLower(src.Peek()) == 'b')
                            {
                                numBase = 2;
                                src.Next();
                            }
                        }

                        while (!src.IsEof())
                        {
                            var cc = src.Peek();
                            if (cc == '.')
                            {
                                if (numBase != 10 || isFloating) break;

                                isFloating = true;
                                _          = src.Next();
                                continue;
                            }

                            if (numBase == 10 && !char.IsDigit(src.Peek())) break;
                            if (numBase == 16 && !char.IsAsciiHexDigit(src.Peek())) break;
                            if (numBase == 2 && src.Peek() is not ('0' or '1')) break;

                            src.Next();
                        }
                        
                        tokens.Add(
                            TokenizeValue(
                                isFloating
                                    ? TokenType.FloatingNumberLiteral
                                    : TokenType.IntegerNumberLiteral,
                                src.GetSlice()
                            )
                        );
                    }

                    // Build identifier token
                    else if (c.IsValidOnIdentifierStarter())
                    {
                        while (src.Peek().IsValidOnIdentifier()) src.Next();
                        var value = src.GetSlice();
                        
                        tokens.Add(_keywordsMap.TryGetValue(value.Item1.ToString(), out var tokenType)
                            ? Tokenize(tokenType, value)
                            : TokenizeValue(TokenType.Identifier, value));
                    }

                    // Build char tokens
                    else if (c == '\'')
                    {
                        tokens.Add(TokenizeValue(TokenType.SingleQuotes, src.GetSlice()));
                        while (!src.IsEof())
                        {
                            if (src.NextIs('\'')) break;
                            if (src.Peek() == '\\') src.Next();
                            src.Next();
                            tokens.Add(TokenizeValue(TokenType.CharacterLiteral, src.GetSlice()));
                        }

                        tokens.Add(TokenizeValue(TokenType.SingleQuotes, src.GetSlice()));
                    }

                    // Build string tokens
                    else if (c == '"')
                    {
                        tokens.Add(TokenizeValue(TokenType.DoubleQuotes, src.GetSlice()));
                        while (!src.IsEof())
                        {
                            // test escape
                            if (src.Peek() == '\\')
                            {
                                if (src.Dirty) tokens.Add(TokenizeValue(TokenType.StringLiteral, src.GetSlice()));
                                src.Next();

                                if (src.IsEof()) break;
                                var escapeCode = src.Next();

                                switch (escapeCode)
                                {
                                    // interpolation
                                    case '{': throw new NotImplementedException();

                                    // hexadecimal
                                    case 'x':
                                        src.Next();
                                        src.Next();
                                    break;

                                    // default: break;
                                }

                                tokens.Add(TokenizeValue(TokenType.CharacterLiteral, src.GetSlice()));
                            }

                            // test end of string
                            else if (src.Peek() == '"')
                            {
                                if (src.Dirty) tokens.Add(TokenizeValue(TokenType.StringLiteral, src.GetSlice()));
                                src.Next();
                                tokens.Add(Tokenize(TokenType.DoubleQuotes, src.GetSlice()));
                                break;
                            } else
                                src.Next();
                        }
                    }

                    // unrecognized character
                    else
                    {
                        throw new NotImplementedException(c.ToString());
                        // FIXME implement error handlers
                        // try { throw new UnrecognizedCharacterException(c, i); }
                        // catch (SyntaxException e) { currentSrc.ThrowError(e); }
                    }

                    break;
                }
            }
        }
    }
    

    private class SourceWindow(string source) {
        private uint _line;
        private uint _startColumn;
        private uint _endColumn;

        private int _indexStart;
        private int _indexEnd;

        public bool Dirty => _indexEnd != _indexStart;

        private char Advance()
        {
            if (IsEof()) return (char)0x1A;

            var c = source[_indexEnd++];
            _endColumn++;

            switch (c)
            {
                case '\n': NextLine(); break;
            }

            return c;
        }

        private void NextLine()
        {
            _line++;
            _startColumn = 0;
            _endColumn   = 0;
            CloseBounds();
        }

        public bool IsEof() => _indexEnd >= source.Length;

        public char Peek()
        {
            if (IsEof()) return (char)0x1A;
            return source[_indexEnd];
        }

        public char Next() => Advance();

        public bool NextIs(char c)
        {
            if (Peek() != c) return false;
            _ = Advance();
            return true;
        }

        public bool NextStringIs(string s)
        {
            if (_indexEnd + s.Length >= source.Length) return false;
            var res = source[_indexEnd .. (_indexEnd + s.Length)] == s;
            if (!res) return false;

            for (var i = 0; i < s.Length; i++) Advance();
            return true;
        }

        public void Discard() => _ = CloseBounds();

        private (uint line, uint start, uint end) CloseBounds()
        {
            var ret = (line: _line, startColumn: _startColumn, endColumn: _endColumn);
            _startColumn = _endColumn;
            _indexStart  = _indexEnd;
            return ret;
        }

        public (ReadOnlyMemory<char>, uint line, uint start, uint length) GetSlice()
        {
            var memory = source.AsMemory(_indexStart, _indexEnd - _indexStart);
            var bounds = CloseBounds();
            return (memory, bounds.line, bounds.start, bounds.end - bounds.start);
        }

        public override string ToString()
            => $"({_line + 1}:{_startColumn}-{_endColumn})\n{source[0.._indexEnd]}{{{{!index here!}}}}{source[_indexEnd..]}";
    }
    public readonly struct MemoryKey(ReadOnlyMemory<char> memory) : IEquatable<MemoryKey> {
        private readonly ReadOnlyMemory<char> _memory = memory;

        public bool Equals(MemoryKey other) => _memory.Span.SequenceEqual(other._memory.Span);
        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (var c in _memory.Span) hash.Add(c);
            return hash.ToHashCode();
        }
    }
}
