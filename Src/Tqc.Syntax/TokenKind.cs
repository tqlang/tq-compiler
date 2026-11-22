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
    Coalesce,     // ?? (two adjacent question marks, joined by the parser)
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
    FatArrow,     // =>
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
    
    FatRightArrow,       // =>
    
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
    
    Power,             // **
    PowerWrap,         // **%
    PowerOnBounds,     // **|
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
    
    Eof,
}
