namespace Tq.Ast;

public enum TokenType : byte
{
    Undefined, // undefined token (default value)

    IntegerNumberLiteral,
    FloatingNumberLiteral,

    StringLiteral,
    CharacterLiteral,
    Identifier,

    LeftParenthesisChar,  // (
    RightParenthesisChar, // )

    LeftCurlyBraceChar,  // {
    RightCurlyBraceChar, // }

    LeftSquareBracketChar,  // [
    RightSquareBracketChar, // ]

    LeftAngleChar,  // <
    RightAngleChar, // >

    EscapedLeftBracket, // \{

    CrossChar,      // +
    MinusChar,      // -
    StarChar,       // *
    SlashChar,      // /
    PercentChar,    // %
    EqualsChar,     // =
    CircumflexChar, // ^
    TildeChar,      // ~
    AmpersandChar,  // &
    QuestionChar,   // ?
    BangChar,       // !
    PipeChar,       // |
    ColonChar,      // :

    AddWrapOperator,     // +%
    AddOnBoundsOperator, // +|
    SubWrapOperator,     // -%
    SubOnBoundsOperator, // -|
    MulWrapOperator,     // *%
    MulOnBoundsOperator, // *|

    DivideFloorOperator, // /_
    DivideCeilOperator,  // /^

    AtSignChar, // @

    RightArrowOperator, // =>

    EqualOperator,        // ==
    UnequalOperator,      // !=
    ExactEqualOperator,   // ===
    ExactUnequalOperator, // !==
    LessEqualsOperator,   // <=
    GreatEqualsOperator,  // >=

    BitShiftLeftOperator,  // <<
    BitShiftRightOperator, // >>

    PowerOperator, // **

    AddAssign,           // +=
    SubAssign,           // -=
    MulAssign,           // *=
    DivAssign,           // /=
    RestAssign,          // %=
    BitwiseXorAssign,    // ^=
    BitwiseAndAssign,    // &=
    BitwiseOrAssign,     // |=
    BitShiftLeftAssign,  // <<=
    BitShiftRightAssign, // >>=

    IncrementOperator, // ++
    DecrementOperator, // --

    DotDotOperator, // ..

    SingleQuotes, // '
    DoubleQuotes, // "

    CommaChar, // ,
    DotChar,   // .

    FromKeyword,
    ImportKeyword,
    LetKeyword,
    ConstKeyword,
    FuncKeyword,
    StructKeyword,
    TypedefKeyword,
    ExtendsKeyword,
    ImplementsKeyword,

    IfKeyword,
    ElifKeyword,
    ElseKeyword,
    ForKeyword,
    WhileKeyword,
    MatchKeyword,
    NewKeyword,
    AsyncKeyword,
    AwaitKeyword,
    LabelKeyword,
    GotoKeyword,
    TryKeyword,
    CatchKeyword,
    ThrowKeyword,
    DeferKeyword,
    ErrdeferKeyword,
    DestroyKeyword,
    UnsafeKeyword,
    ReturnKeyword,

    AsKeyword,
    InKeyword,
    OrKeyword,
    AndKeyword,

    TrueKeyword,
    FalseKeyword,
    NullKeyword,

    // Trivia
    Comment,
    DocComment,
    EofChar,       // \EOF
    LineFeedChar,  // \n
    SemiColonChar, // ;
    SpaceChar,     // \s
    TabChar,       // \t
}
