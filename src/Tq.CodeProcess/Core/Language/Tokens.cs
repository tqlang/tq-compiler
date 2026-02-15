namespace Abstract.CodeProcess.Core.Language;

public struct Token
{
    public ReadOnlyMemory<char> value;
    public TokenType type;

    public uint line;
    public uint start;
    public uint end;

    public readonly (uint line, uint start, uint end) Range => (line, start, end);
    public readonly uint Length => end - start;

    public readonly override string ToString() => $"'{ValueString()}' ({type}) at {line+1}:{start}";
    public readonly string ValueString() => type switch
        {
            TokenType.EndOfStatement => "\n",
            TokenType.EofChar => "[\\EOF]",
            _ => value.ToString(),
        };
}
public enum TokenType : byte
{
    Undefined,              // undefined token (default value)
    
    IntegerNumberLiteral,
    FloatingNumberLiteral,
    
    StringLiteral,
    CharacterLiteral,
    Identifier,
    
    FromKeyword,            // from
    ImportKeyword,          // import
    TypeKeyword,
    LetKeyword,             // let
    ConstKeyword,           // const
    FuncKeyword,            // func
    StructKeyword,          // struct
    ExtendsKeyword,         // extends
    PacketKeyword,          // packet
    TypedefKeyword,         // typedef
    CaseKeyword,            // case
    ConstructorKeyword,     // constructor
    DestructorKeyword,      // destructor
    
    MatchKeyword,           // match

    IfKeyword,              // if
    ElifKeyword,            // elif
    ElseKeyword,            // else
    WhileKeyword,           // while
    ForKeyword,             // for
    DoKeyword,              // do
    InKeyword,              // in
    BreakKeyword,           // break
    UnreachableKeyword,     // unreachable

    AsKeyword,              // as
    NewKeyword,             // new

    ReturnKeyword,          // return

    NullKeyword,            // null
    TrueKeyword,            // true
    FalseKeyword,           // false
    
    BitwiseAndKeyword,      // AND
    BitwiseOrKeyword,       // OR
    BitwiseXorKeyword,      // XOR

    LeftParenthesisChar,    // (
    RightParenthesisChar,   // )

    LeftBracketChar,        // {
    RightBracketChar,       // }

    LeftSquareBracketChar,  // [
    RightSquareBracketChar, // ]

    LeftAngleChar,          // <
    RightAngleChar,         // >

    EscapedLeftBracket,     // \{

    CrossChar,              // +
    MinusChar,              // -
    StarChar,               // *
    SlashChar,              // /
    PercentChar,            // %
    EqualsChar,             // =
    CircumflexChar,         // ^
    TildeChar,              // ~
    AmpersandChar,          // &
    QuestionChar,           // ?
    BangChar,               // !
    PipeChar,               // |
    ColonChar,              // :
    
    AddWarpOperator,        // +%
    AddOnBoundsOperator,    // +|
    SubWarpOperator,        // -%
    SubOnBoundsOperator,    // -|
    
    DivideFloorOperator,    // /_
    DivideCeilOperator,     // /^

    AtSiginChar,            // @

    RightArrowOperator,     // =>

    EqualOperator,          // ==
    UnequalOperator,        // !=
    ExactEqualOperator,     // ===
    ExactUnequalOperator,   // !==
    LessEqualsOperator,     // <=
    GreatEqualsOperator,    // >=

    AndOperator,            // and
    OrOperator,             // or
    
    BitShiftLeftOperator,   // <<
    BitShiftRightOperator,  // >>

    PowerOperator,          // **

    AddAssign,              // +=
    SubAssign,              // -=
    MulAssign,              // *=
    DivAssign,              // /=
    RestAssign,             // %=
    BitwiseXorAssign,       // ^=
    BitwiseAndAssign,       // &=
    BitwiseOrAssign,        // |=
    BitShiftLeftAssign,     // <<=
    BitShiftRightAssign,    // >>=

    IncrementOperator,      // ++
    DecrementOperator,      // --

    DotDotOperator,         // ..

    SingleQuotes,           // '
    DoubleQuotes,           // "

    CommaChar,              // ,
    DotChar,                // .

    EofChar,                // \EOF
    EndOfStatement,         // \n or ;
    SpaceChar,             //  
}
