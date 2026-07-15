using System.Diagnostics;
using StringContent = Tq.CodeProcess.Core.Language.AbstractSyntaxTree.StringContent;

namespace Tq.CodeProcess.Parser;

public partial class Parser
{
    private ExpressionNode ParseExpression(bool allowAssignment = false)
    {
        _tokens.SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia);
        if (allowAssignment) return ParseAssignmentExpression();
        return ParseBooleanOperationExpression();
    }

    private ExpressionNode ParseAssignmentExpression()
    {
        var node = ParseTernaryExpression();

        if (!_tokens
             .SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia)
             .NextIs(
                 TokenType.EqualsChar, // =
                 TokenType.AddAssign,  // +=
                 TokenType.SubAssign,  // -=
                 TokenType.MulAssign,  // *=
                 TokenType.DivAssign,  // /=
                 TokenType.RestAssign  // %=
             ))
        {
            _tokens.ResetPtr();
            return node;
        }

        var op = _tokens.Eat();
        _tokens.SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia);
        var right = ParseAssignmentExpression();

        return BinaryExpressionNode.Build(node, op, right);
    }

    private ExpressionNode ParseTernaryExpression()
    {
        var a = ParseBooleanOperationExpression();
        if (!_tokens.NextIs(out var n1, TokenType.QuestionChar)) return a;

        var b = ParseExpression();

        if (!_tokens.NextIs(out var n2, TokenType.QuestionChar))
            ThrowUnexpectedTokenError(errorHandler, _sourcePath, TokenType.ColonChar, n2.Token);

        var c = ParseExpression();

        return TernaryOperatorExpressionNode.Build(a, n1, b, n2, c);
    }
    
    private ExpressionNode ParseBooleanOperationExpression()
    {
        var node = ParseAdditiveExpression();

        if (!_tokens
             .SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia)
             .NextIs(
                 TokenType.OrKeyword,
                 TokenType.AndKeyword
             )
           )
            return node;

        var op = _tokens.Eat();
        _tokens.SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia);
        var right = ParseBooleanOperationExpression();

        return BinaryExpressionNode.Build(node, op, right);
    }

    private ExpressionNode ParseAdditiveExpression()
    {
        var node = ParseMultiplicativeExpression();

        if (!_tokens
             .SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia)
             .NextIs(
                 TokenType.CrossChar,           // +
                 TokenType.AddOnBoundsOperator, // +|
                 TokenType.AddWrapOperator,     // +%
                 TokenType.MinusChar,           // -
                 TokenType.SubOnBoundsOperator, // -|
                 TokenType.SubWrapOperator      // -%
             )) return node;

        var op = _tokens.Eat();
        _tokens.SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia);
        var right = ParseAdditiveExpression();

        return BinaryExpressionNode.Build(node, op, right);
    }

    private ExpressionNode ParseMultiplicativeExpression()
    {
        var node = ParseComparisonExpression();

        if (!_tokens
             .SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia)
             .NextIs(
                 TokenType.StarChar,            // *
                 TokenType.SlashChar,           // /
                 TokenType.DivideCeilOperator,  // /^
                 TokenType.DivideFloorOperator, // /_
                 TokenType.PercentChar,         // %
                 TokenType.PowerOperator        // **
             )) return node;

        var op = _tokens.Eat();
        _tokens.SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia);
        var right = ParseMultiplicativeExpression();

        return BinaryExpressionNode.Build(node, op, right);
    }

    private ExpressionNode ParseComparisonExpression()
    {
        var node = ParseBitwiseOperationExpression();

        if (!_tokens
             .SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia)
             .NextIs(
                 TokenType.EqualOperator,        // ==
                 TokenType.UnequalOperator,      // !=
                 TokenType.ExactEqualOperator,   // ===
                 TokenType.ExactUnequalOperator, // !==
                 TokenType.LeftAngleChar,        // <
                 TokenType.RightAngleChar,       // >
                 TokenType.LessEqualsOperator,   // <=
                 TokenType.GreatEqualsOperator   // >=
             )) return node;

        var op = _tokens.Eat();
        _tokens.SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia);
        var right = ParseComparisonExpression();

        return BinaryExpressionNode.Build(node, op, right);
    }

    private ExpressionNode ParseBitwiseOperationExpression()
    {
        var node = ParseCastingExpression();

        if (!_tokens
             .SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia)
             .NextIs(
                 TokenType.PipeChar,             // |
                 TokenType.AmpersandChar,        // &
                 TokenType.CircumflexChar,       // ^
                 TokenType.BitShiftLeftOperator, // <<
                 TokenType.BitShiftRightOperator // >>
             )) return node;

        var op = _tokens.Eat();
        _tokens.SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia);
        var right = ParseMultiplicativeExpression();

        return BinaryExpressionNode.Build(node, op, right);
    }

    private ExpressionNode ParseCastingExpression()
    {
        var node = ParseUnaryExpression();

        if (!_tokens
             .SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia)
             .NextIs(TokenType.AsKeyword)
           ) return node;

        var op = _tokens.Eat();
        _tokens.SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia);
        var right = ParseExpression();

        return BinaryExpressionNode.Build(node, op, right);
    }

    private ExpressionNode ParseUnaryExpression()
    {
        ExpressionNode node;

        // pre-op
        if (_tokens
            .SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia)
            .NextIs(
                TokenType.CrossChar,         // +
                TokenType.MinusChar,         // -
                TokenType.StarChar,          // *
                TokenType.PowerOperator,     // **
                TokenType.AmpersandChar,     // &
                TokenType.TildeChar,         // ~
                TokenType.BangChar,          // !
                TokenType.QuestionChar,      // ?
                TokenType.IncrementOperator, // ++
                TokenType.DecrementOperator  // --
            ))
        {
            var op = _tokens.Eat();
            _tokens.SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia);
            var expression = ParseUnaryExpression();

            node = UnaryExpressionNode.BuildPrefix(op, expression);
        }
        else
        {
            _tokens.SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia);
            node = ParseValue();
        }

        // post-op
        if (_tokens
            .SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia)
            .NextIs(
                TokenType.IncrementOperator, // ++
                TokenType.DecrementOperator, // --
                TokenType.BangChar,          // !
                TokenType.QuestionChar       // ?
            ))
        {
            var op = _tokens.Eat();
            node = UnaryExpressionNode.BuildPostfix(node, op);
        }

        return node;
    }

    private ExpressionNode ParseValue()
    {
        ExpressionNode node;

        switch (_tokens.SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia).Peek().Type)
        {
            // Partial identifiers
            case TokenType.DotChar:
            {
                var dot = _tokens.Eat();
                _tokens.SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia);
                var identifier = ParseIdentifier();
                node = AccessExpressionNode.BuildImplicit(dot, identifier);
            }
            break;

            // Complete identifiers
            case TokenType.Identifier:
                node = ParseIdentifier();
            break;

            // Local variables declaration
            case TokenType.LetKeyword or
                TokenType.ConstKeyword:
            {
                var def = _tokens.Eat();
                ExpressionNode? type = null;
                IdentifierNode identifier;

                _tokens.SkipTrivia(Parser.QueryUtils.AllWhitespaces);
                var first = ParseExpression();

                if (_tokens
                    .SkipTrivia(Parser.QueryUtils.AllWhitespaces)
                    .NextIs(TokenType.Identifier))
                {
                    type       = first;
                    identifier = ParseIdentifier();
                }
                else
                {
                    identifier = (IdentifierNode)first;
                }

                node = LocalVariableExpressionNode.Build(def, type, identifier);
            }
            break;

            case TokenType.MatchKeyword:
            {
                var matchK = _tokens.Eat();

                _tokens.SkipTrivia(Parser.QueryUtils.AllWhitespaces);
                var expression = ParseExpression();

                var leftCurlyBrace = _tokens
                                     .SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia)
                                     .Check(TokenType.LeftCurlyBraceChar, out var result)
                                     .Eat();
                if (!result) ThrowUnexpectedTokenError(errorHandler, _sourcePath, TokenType.LeftCurlyBraceChar, leftCurlyBrace.Token);

                var cases = new List<MatchBaseCaseNode>();

                while (!_tokens.NextIs(TokenType.EofChar, TokenType.RightCurlyBraceChar))
                {
                    _tokens.SkipTrivia([.. Parser.QueryUtils.StatementLevelTrivia, ..Parser.QueryUtils.AllLineFeeds]);
                    switch (_tokens.Peek())
                    {
                        case { Type: TokenType.Identifier, Value: "case" }:
                        {
                            var caseToken = _tokens.Eat();

                            _tokens.SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia);
                            var caseOptions = new List<(ExpressionNode expression, TokenNode? comma)>();

                            do
                            {
                                var exp = ParseExpression();
                                _tokens.SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia).NextIs(out var comma2, TokenType.CommaChar);

                                caseOptions.Add((exp, comma2));

                                if (comma2 == null!) break;
                            } while (true);


                            var rightArrow = _tokens
                                             .SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia)
                                             .Check(TokenType.RightArrowOperator, out result)
                                             .Eat();
                            if (!result) ThrowUnexpectedTokenError(errorHandler, _sourcePath, TokenType.RightArrowOperator, rightArrow.Token);

                            _tokens.SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia);
                            var statement = ParseStatement();

                            var option = MatchExpressionNode.BuildValueCase(caseToken, [.. caseOptions], rightArrow, statement);

                            cases.Add(option);
                        }
                        break;

                        case { Type: TokenType.Identifier, Value: "default" }:
                        {
                            var defaultToken = _tokens.Eat();

                            var rightArrow = _tokens
                                             .SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia)
                                             .Check(TokenType.RightArrowOperator, out result)
                                             .Eat();
                            if (!result) ThrowUnexpectedTokenError(errorHandler, _sourcePath, TokenType.RightArrowOperator, rightArrow.Token);

                            _tokens.SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia);
                            var statement = ParseStatement();

                            var option = MatchExpressionNode.BuildDefaultCase(defaultToken, rightArrow, statement);
                            cases.Add(option);
                        }
                        break;
                    }
                }

                var rightCurlyBrace = _tokens
                                      .SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia)
                                      .Check(TokenType.RightCurlyBraceChar, out result)
                                      .Eat();
                if (!result) ThrowUnexpectedTokenError(errorHandler, _sourcePath, TokenType.LeftCurlyBraceChar, leftCurlyBrace.Token);

                node = MatchExpressionNode.Build(
                    matchK,
                    expression,
                    leftCurlyBrace,
                    [.. cases],
                    rightCurlyBrace
                );
            }
            break;

            case TokenType.ThrowKeyword:
            {
                var throwK = _tokens.Eat();
                _tokens.SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia);
                var expression = ParseExpression();
                node = ThrowExpressionNode.Build(throwK, expression);
            }
            break;

            // Constructor invoke
            case TokenType.NewKeyword:
            {
                // Possible constructor syntax
                //  new <Type>(args...)
                //  new <Type(generic...)>(args...)

                var newKeyword = _tokens.Eat();

                // ParseExpression will parse it as a generic.
                // After it the parser must verify
                // if there is a following argument block.
                // If not, it must dissect the returned
                // type expression.

                _tokens.SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia);
                var type = ParseExpression();

                TokenNode leftParenthesisToken;
                TokenNode rightParenthesisToken;
                (ExpressionNode expression, TokenNode? comma)[] arguments;
                ExplicitBodyNode? initializers = null;

                if (_tokens.SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia).NextIs(TokenType.LeftParenthesisChar))
                    (leftParenthesisToken, arguments, rightParenthesisToken) = ParseArgumentList();

                else if (type is CallExpressionNode @call)
                {
                    type                  = call.Expression;
                    leftParenthesisToken  = call.LeftParenthesisToken;
                    rightParenthesisToken = call.RightParenthesisToken;
                    arguments             = call.Arguments;
                }

                else throw new UnreachableException();

                if (_tokens
                    .SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia)
                    .NextIs(out var leftBrace, TokenType.LeftCurlyBraceChar))
                {
                    var content = new List<SyntaxNode>();

                    while (!_tokens.IsEof() && !_tokens.NextIs(TokenType.RightCurlyBraceChar))
                    {
                        var expression = ParseExpression();
                        var equals = _tokens.Check(TokenType.EqualsChar, out var r1).Eat();
                        if (!r1) ThrowUnexpectedTokenError(errorHandler, _sourcePath, TokenType.EqualsChar, equals.Token);
                        var value = ParseExpression();

                        content.Add(AssignmentExpressionNode.Build(expression, equals, value));
                    }

                    var rightBrace = _tokens.Check(TokenType.RightCurlyBraceChar, out var r2).Eat();
                    if (!r2) ThrowUnexpectedTokenError(errorHandler, _sourcePath, TokenType.RightCurlyBraceChar, rightBrace.Token);

                    initializers = ExplicitBodyNode.Build(leftBrace, [..content], rightBrace);
                }

                node = NewObjectNode.Build(newKeyword, type, leftParenthesisToken, arguments, rightParenthesisToken, initializers);
            }
            break;

            // Parenthesis enclosed expression
            case TokenType.LeftParenthesisChar:
            {
                var leftP = _tokens.Eat();

                _tokens.SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia);
                var expression = ParseExpression();

                _tokens.SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia);
                var rightP = _tokens.Check(TokenType.RightParenthesisChar, out var r).Eat();

                if (!r) ThrowUnexpectedTokenError(errorHandler, _sourcePath, TokenType.RightParenthesisChar, rightP.Token);

                node = ParenthesisExpressionNode.Build(leftP, expression, rightP);
            }
            break;

            // Numbers
            case TokenType.IntegerNumberLiteral:  node = IntegerLiteralNode.Build(_tokens.Eat()); break;
            case TokenType.FloatingNumberLiteral: node = FloatingLiteralNode.Build(_tokens.Eat()); break;

            // Booleans
            case TokenType.TrueKeyword:
            case TokenType.FalseKeyword: node = BooleanLiteralNode.Build(_tokens.Eat()); break;

            // String
            case TokenType.DoubleQuotes:
            {
                List<StringContent> content = [];

                var leftQuote = _tokens.Eat();
                while (!_tokens.IsEof())
                {
                    if (_tokens.NextIs(TokenType.StringLiteral))
                        content.Add(StringLiteralNode.BuildTextContent(_tokens.Eat()));

                    else if (_tokens.NextIs(TokenType.EscapedLeftBracket))
                    {
                        var start = _tokens.Eat();

                        _tokens.SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia);
                        var exp = ParseExpression();

                        _tokens.SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia);
                        var end = _tokens.Check(TokenType.EscapedLeftBracket, out var result).Eat();

                        if (!result) ThrowUnexpectedTokenError(errorHandler, _sourcePath, TokenType.RightCurlyBraceChar, end.Token);

                        content.Add(StringLiteralNode.BuildInterpolationContent(start, exp, end));
                    }

                    else if (_tokens.NextIs(TokenType.CharacterLiteral))
                        content.Add(StringLiteralNode.BuildCharContent(_tokens.Eat()));

                    else if (_tokens.NextIs(TokenType.DoubleQuotes)) break;

                    else ThrowUnexpectedTokenError(errorHandler, _sourcePath, ["string content"], _tokens.Eat().Token);
                }

                var rightQuote = _tokens.Check(TokenType.DoubleQuotes, out var r2).Eat();
                if (!r2) ThrowUnexpectedTokenError(errorHandler, _sourcePath, ["string content"], _tokens.Eat().Token);

                node = StringLiteralNode.Build(leftQuote, [.. content], rightQuote);
            }
            break;

            // Char
            case TokenType.SingleQuotes:
            {
                var leftTick = _tokens.Eat();
                var character = _tokens.Eat();
                var rightTick = _tokens.Check(TokenType.SingleQuotes, out var expected).Eat();
                if (!expected) ThrowUnexpectedTokenError(errorHandler, _sourcePath, TokenType.SingleQuotes, rightTick.Token);

                node = CharacterLiteralNode.Build(leftTick, character, rightTick);
            }
            break;

            // array types
            case TokenType.LeftSquareBracketChar:
            {
                var leftSquareBracket = _tokens.Eat();

                if (_tokens.SkipTrivia(Parser.QueryUtils.AllWhitespaces)
                           .NextIs(out var rightSquareBracket, TokenType.RightSquareBracketChar))
                {
                    _tokens.SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia);
                    var expression = ParseExpression();

                    node = SliceModifierNode.Build(leftSquareBracket, rightSquareBracket, expression);
                    break;
                }

                throw new NotImplementedException();
            }

            // null
            case TokenType.NullKeyword:
                node = NullLiteralNode.Build(_tokens.Eat());
            break;

            default:
            {
                var t = _tokens.Eat().Token;
                throw new UnexpectedTokenError($"Unexpected token {t}", new Location(_sourcePath, t.LineStart, t.ColStart));
            }
        }

        retryRecursiveTests:
        {
            // Test for access
            if (_tokens
                .SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia)
                .NextIs(out var dot, TokenType.DotChar))
            {
                _tokens.SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia);
                var access = ParseIdentifier();

                node = AccessExpressionNode.Build(node, dot, access);
                goto retryRecursiveTests;
            }

            // Test for invoke
            if (_tokens.SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia).NextIs(TokenType.LeftParenthesisChar))
            {
                var (leftParenthesisToken, content, rightParenthesisToken) = ParseArgumentList();
                node                                                       = CallExpressionNode.Build(node, leftParenthesisToken, content, rightParenthesisToken);
                goto retryRecursiveTests;
            }

            // Test for indexing
            if (_tokens.SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia).NextIs(TokenType.LeftSquareBracketChar))
            {
                var (leftBracket, content, rightBracket) = ParseIndexList();
                node                                     = IndexExpressionNode.Build(node, leftBracket, content, rightBracket);
                goto retryRecursiveTests;
            }
        }
        _tokens.ResetPtr();

        return node;
    }

    private GenericCollectionNode ParseGenericCollection()
    {
        var leftCurlyBrace = _tokens.Eat();
        var elements = new List<(ExpressionNode expression, TokenNode? comma)>();

        if (!_tokens.IsEof() && !_tokens
             .SkipTrivia(Parser.QueryUtils.ExpressionLevelTrivia)
             .NextIs(TokenType.RightCurlyBraceChar))
            while (!_tokens.IsEof())
            {
                try
                {
                    var value = ParseExpression();
                    _tokens.SkipTrivia(Parser.QueryUtils.InterParametersTrivia).NextIs(out var comma, TokenType.CommaChar);
                    elements.Add((value, comma));

                    if (comma == null!) break;
                }
                catch
                {
                    // Ignored
                }
            }

        var rightSquareBracket = _tokens.Check(TokenType.RightCurlyBraceChar, out var result).Eat();
        if (!result) ThrowUnexpectedTokenError(errorHandler, _sourcePath, TokenType.RightCurlyBraceChar, rightSquareBracket.Token);

        return GenericCollectionNode.Build(leftCurlyBrace, [.. elements], rightSquareBracket);
    }

    public IdentifierNode ParseIdentifier()
    {
        var token = _tokens.Check(TokenType.Identifier, out var result).Eat();
        if (!result) ThrowUnexpectedTokenError(errorHandler, _sourcePath, TokenType.Identifier, token.Token);
        return IdentifierNode.Build(token);
    }
}
