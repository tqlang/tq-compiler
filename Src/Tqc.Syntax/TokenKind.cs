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
    
    EscapedLeftBracket, // \{
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
