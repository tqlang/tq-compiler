namespace Tq.Ast;

public record Token(
    string? Value,
    TokenType Type,
    int LineStart,
    int ColStart,
    int Length
)
{
    public (int lineStart, int colStart, int length) Range => (LineStart, ColStart, Length);

    public Token(TokenType type, int lineStart, int ColStart, int length)
        : this(null, type, lineStart, ColStart, length) { }

    public override string ToString()
    {
        return Value != null
            ? $"'{Value}' ({Type}) at {LineStart + 1}:{ColStart} ({Length} chars)"
            : $"({Type}) at {LineStart + 1}:{ColStart} ({Length} chars)";
    }

    public string ValueAsString()
    {
        if (Value != null) return Value;
        if (TokenStrings.TryGetValue(Type, out var s)) return s;
        throw new ArgumentOutOfRangeException($"No textual representation for {Type}");
    }

    private static readonly Dictionary<TokenType, string> TokenStrings = new()
    {
        [TokenType.SpaceChar]    = " ",
        [TokenType.LineFeedChar] = "\n",
        [TokenType.TabChar]      = "\t",
        [TokenType.EofChar]      = "[\\EOF]",

        [TokenType.LeftParenthesisChar]    = "(",
        [TokenType.RightParenthesisChar]   = ")",
        [TokenType.LeftSquareBracketChar]  = "[",
        [TokenType.RightSquareBracketChar] = "]",
        [TokenType.LeftAngleChar]          = "<",
        [TokenType.RightAngleChar]         = ">",
        [TokenType.LeftCurlyBraceChar]     = "{",
        [TokenType.RightCurlyBraceChar]    = "}",

        [TokenType.CrossChar]      = "+",
        [TokenType.MinusChar]      = "-",
        [TokenType.StarChar]       = "*",
        [TokenType.SlashChar]      = "/",
        [TokenType.PercentChar]    = "%",
        [TokenType.EqualsChar]     = "=",
        [TokenType.CircumflexChar] = "^",
        [TokenType.TildeChar]      = "~",
        [TokenType.AmpersandChar]  = "&",
        [TokenType.QuestionChar]   = "?",
        [TokenType.BangChar]       = "!",
        [TokenType.PipeChar]       = "|",
        [TokenType.ColonChar]      = ":",
        [TokenType.SemiColonChar]  = ";",

        [TokenType.AddWrapOperator]     = "+%",
        [TokenType.AddOnBoundsOperator] = "+|",
        [TokenType.SubWrapOperator]     = "-%",
        [TokenType.SubOnBoundsOperator] = "-|",
        [TokenType.MulWrapOperator]     = "*%",
        [TokenType.MulOnBoundsOperator] = "*|",

        [TokenType.DivideFloorOperator] = "/_",
        [TokenType.DivideCeilOperator]  = "/^",

        [TokenType.AtSignChar]         = "@",
        [TokenType.RightArrowOperator] = "=>",

        [TokenType.EqualOperator]        = "==",
        [TokenType.UnequalOperator]      = "!=",
        [TokenType.ExactEqualOperator]   = "===",
        [TokenType.ExactUnequalOperator] = "!==",
        [TokenType.LessEqualsOperator]   = "<=",
        [TokenType.GreatEqualsOperator]  = ">=",

        [TokenType.BitShiftLeftOperator]  = "<<",
        [TokenType.BitShiftRightOperator] = ">>",

        [TokenType.PowerOperator] = "**",

        [TokenType.AddAssign]           = "+=",
        [TokenType.SubAssign]           = "-=",
        [TokenType.MulAssign]           = "*=",
        [TokenType.DivAssign]           = "/=",
        [TokenType.RestAssign]          = "%=",
        [TokenType.BitwiseXorAssign]    = "^=",
        [TokenType.BitwiseAndAssign]    = "&=",
        [TokenType.BitwiseOrAssign]     = "|=",
        [TokenType.BitShiftLeftAssign]  = "<<=",
        [TokenType.BitShiftRightAssign] = ">>=",

        [TokenType.IncrementOperator] = "++",
        [TokenType.DecrementOperator] = "--",

        [TokenType.DotDotOperator] = "..",

        [TokenType.SingleQuotes] = "'",
        [TokenType.DoubleQuotes] = "\"",

        [TokenType.CommaChar] = ",",
        [TokenType.DotChar]   = ".",

        // Keywords
        [TokenType.FromKeyword]       = "from",
        [TokenType.ImportKeyword]     = "import",
        [TokenType.LetKeyword]        = "let",
        [TokenType.ConstKeyword]      = "const",
        [TokenType.FuncKeyword]       = "func",
        [TokenType.StructKeyword]     = "struct",
        [TokenType.TypedefKeyword]    = "typedef",
        [TokenType.ExtendsKeyword]    = "extends",
        [TokenType.ImplementsKeyword] = "implements",

        [TokenType.IfKeyword]    = "if",
        [TokenType.ElifKeyword]  = "elif",
        [TokenType.ElseKeyword]  = "else",
        [TokenType.ForKeyword]   = "for",
        [TokenType.WhileKeyword] = "while",
        [TokenType.MatchKeyword] = "match",
        [TokenType.NewKeyword]   = "new",
        [TokenType.AsyncKeyword] = "async",
        [TokenType.AwaitKeyword] = "await",
        [TokenType.LabelKeyword] = "label",
        [TokenType.GotoKeyword] = "goto",
        [TokenType.TryKeyword] = "try",
        [TokenType.CatchKeyword] = "catch",
        [TokenType.ThrowKeyword] = "throw",
        [TokenType.DeferKeyword] = "defer",
        [TokenType.ErrdeferKeyword] = "errdefer",
        [TokenType.DestroyKeyword] = "destroy",
        [TokenType.UnsafeKeyword] = "unsafe",
        [TokenType.ReturnKeyword] = "return",

        [TokenType.AsKeyword]  = "as",
        [TokenType.InKeyword]  = "in",
        [TokenType.OrKeyword]  = "or",
        [TokenType.AndKeyword] = "and",

        [TokenType.TrueKeyword]  = "true",
        [TokenType.FalseKeyword] = "false",
        [TokenType.NullKeyword]  = "null",
    };
}

