namespace Tqc.Syntax;

public enum TokenKind
{
    LineFeed,
    Semicolon,
    Whitespace,
    Comment,
    
    FloatingNumberLiteral,
    IntegerNumberLiteral,
    StringLiteral,
    Identifier,
    
    EscapedLeftBrace, // \{
    EscapedCharacter,
    
    LeftParenthesis, RightParenthesis, // ( )
    LeftBrace, RightBrace,             // { }
    LeftBracket, RightBracket,         // [ ]
    LeftAngle, RightAngle,             // < >
    
    QuestionMark, // ?
    Bang,         // !
    At,           // @
    Comma,        // ,
    Dot,          // .
    DotDot,       // ..
    Colon,        // :
    Tilde,        // ~
    Pipe,         // |
    Ampersand,    // &
    Equals,       // =
    Quote,        // '
    DoubleQuote,  // "
    
    EqualsEquals,       // ==
    ExactEquals,        // ===
    NotEqualsEquals,    // !=
    NotExactEquals,     // !==
    LessEqual,          // <=
    GreaterEqual,       // >=
    
    BitShiftLeft,        // <<
    BitShiftRight,       // >>
    
    BitwiseAnd, // ⋏
    BitwiseOr,  // ⋎
    BitwiseXor, // ⊕
    
    Plus,              // +
    Increment,         // ++
    AddWrap,           // +%
    AddOnBounds,       // +|
    AddAssign,         // +=
    
    Minus,             // -
    Decrement,         // --
    SubWrap,           // -%
    SubOnBounds,       // -|
    SubAssign,         // -=
    
    Power,             // ** (two adjacent stars; the parser joins them, so that `**T` is still a reference to a reference)
    PowerWrap,         // **% (`*` and `*%`, joined by the parser)
    PowerOnBounds,     // **| (`*` and `*|`, joined by the parser)
    Star,              // *
    MulWrap,           // *%
    MulOnBounds,       // *|
    MulAssign,         // *=
    
    Slash,          // /
    DivFloor,       // /_
    DivCeil,        // /^
    DivAssign,      // /=
    
    Rest,       // %
    RestAssign, // %=
    
    Eof
}
