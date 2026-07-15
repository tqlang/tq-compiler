namespace Tq.CodeProcess.Parser;

public partial class Parser
{
    private BodyNode ParseStatementBody()
    {
        var leftBrace = _tokens.Eat();
        if (leftBrace.Type != TokenType.LeftCurlyBraceChar)
            ThrowUnexpectedTokenError(errorHandler, _sourcePath, TokenType.LeftCurlyBraceChar, leftBrace.Token);

        var content = new List<SyntaxNode>();
        while (!_tokens
                .SkipTrivia([..QueryUtils.StatementLevelTrivia, ..QueryUtils.AllLineFeeds])
                .NextIs(TokenType.RightCurlyBraceChar))
        {
            content.Add(ParseStatement());
        }

        var rightBrace = _tokens.SkipTrivia(QueryUtils.TopLevelTrivia).Eat();
        if (rightBrace.Type != TokenType.RightCurlyBraceChar)
            ThrowUnexpectedTokenError(errorHandler, _sourcePath, TokenType.RightCurlyBraceChar, leftBrace.Token);

        return ExplicitBodyNode.Build(leftBrace, [.. content], rightBrace);
    }

    private StatementNode ParseStatement()
    {
        switch (_tokens.Peek().Type)
        {
            case TokenType.LeftCurlyBraceChar: return StatementBodyNode.Build(ParseStatementBody());

            case TokenType.IfKeyword:
            {
                IfStatementNode @if;
                List<ElifStatementNode> elifs = [];
                ElseStatementNode? @else = null;

                {
                    var ifK = _tokens.Eat();
                    _tokens.SkipTrivia(QueryUtils.AllWhitespaces);
                    var condition = ParseExpression();
                    _tokens.SkipTrivia(QueryUtils.ExpressionLevelTrivia);
                    var then = ParseStatement();
                    @if = IfStatementNode.Build(ifK, condition, then);
                }

                while (_tokens.SkipTrivia(QueryUtils.StatementLevelTrivia).NextIs(TokenType.ElifKeyword))
                {
                    var elifK = _tokens.Eat();
                    _tokens.SkipTrivia(QueryUtils.AllWhitespaces);
                    var condition = ParseExpression();
                    _tokens.SkipTrivia(QueryUtils.ExpressionLevelTrivia);
                    var then = ParseStatement();

                    elifs.Add(ElifStatementNode.Build(elifK, condition, then));
                }

                if (_tokens.SkipTrivia(QueryUtils.StatementLevelTrivia).NextIs(TokenType.ElseKeyword))
                {
                    var elseK = _tokens.Eat();
                    _tokens.SkipTrivia(QueryUtils.ExpressionLevelTrivia);
                    var then = ParseStatement();
                    @else = ElseStatementNode.Build(elseK, then);
                }

                if (elifs.Count == 0 && @else == null) return @if;
                return ConditionalChainNode.Build(@if, [..elifs], @else);
            }
            case TokenType.ElifKeyword:
            case TokenType.ElseKeyword:
                ThrowUnexpectedTokenError(errorHandler, _sourcePath, TokenType.IfKeyword, _tokens.Eat().Token);
            break;

            case TokenType.WhileKeyword:
            {
                var whileK = _tokens.Eat();
                TokenNode? leftParenthesis;
                ExpressionNode exp1;
                TokenNode? colon1;
                ExpressionNode? exp2 = null;
                TokenNode? colon2 = null;
                ExpressionNode? exp3 = null;
                TokenNode? rightParenthesis;

                _tokens.SkipTrivia(QueryUtils.ExpressionLevelTrivia);

                if (_tokens.NextIs(out leftParenthesis, TokenType.LeftParenthesisChar)) 
                    _tokens.SkipTrivia(QueryUtils.ExpressionLevelTrivia);
                
                exp1 = ParseExpression(
                    allowAssignment: true);

                if (_tokens.SkipTrivia(QueryUtils.ExpressionLevelTrivia).NextIs(out colon1, TokenType.ColonChar))
                {
                    _tokens.SkipTrivia(QueryUtils.ExpressionLevelTrivia);
                    exp2 = ParseExpression(
                        allowAssignment: true);

                    if (_tokens.SkipTrivia(QueryUtils.ExpressionLevelTrivia).NextIs(out colon2, TokenType.ColonChar))
                    {
                        _tokens.SkipTrivia(QueryUtils.ExpressionLevelTrivia);
                        exp3 = ParseExpression(
                            allowAssignment: true);
                    }
                }

                if (leftParenthesis != null)
                {
                    if (_tokens.NextIs(out rightParenthesis, TokenType.RightParenthesisChar))
                        _tokens.SkipTrivia(QueryUtils.ExpressionLevelTrivia);
                    else ThrowUnexpectedTokenError(errorHandler, _sourcePath, TokenType.RightParenthesisChar, rightParenthesis.Token);
                }
                
                _tokens.SkipTrivia([..QueryUtils.StatementLevelTrivia, TokenType.LineFeedChar]);
                var action = ParseStatement();

                if (exp3 != null)
                    return WhileStatementNode.BuildInitConditionStep(
                        whileK,
                        exp1,
                        colon1,
                        exp2!,
                        colon2!,
                        exp3,
                        action
                    );
                
                if (exp2 != null)
                    return WhileStatementNode.BuildConditionStep(
                        whileK,
                        exp1,
                        colon1,
                        exp2,
                        action
                    );

                return WhileStatementNode.BuildCondition(whileK, exp1, action);
            }
            case TokenType.ForKeyword:
            {
                var forK = _tokens.Eat();

                _tokens.SkipTrivia(QueryUtils.AllWhitespaces);
                var variable = ParseExpression();

                var inK = _tokens
                          .SkipTrivia(QueryUtils.ExpressionLevelTrivia)
                          .Check(TokenType.InKeyword, out var result)
                          .Eat();
                if (!result) ThrowUnexpectedTokenError(errorHandler, _sourcePath, TokenType.InKeyword, inK.Token);

                _tokens.SkipTrivia(QueryUtils.AllWhitespaces);
                var expression = ParseExpression();

                return ForStatementNode.Build(forK, variable, inK, expression);
            }

            case TokenType.ReturnKeyword:
            {
                var token = _tokens.Eat();
                ExpressionNode? exp = null;

                if (!_tokens.SkipTrivia(QueryUtils.AllWhitespaces).NextIs(QueryUtils.AllLineFeeds))
                    exp = ParseExpression();
                
                return ReturnStatementNode.Build(token, exp);
            }
            
            default: return StatementExpressionNode.Build(ParseExpression(true));
        }

        throw new NotImplementedException();
    }
}
