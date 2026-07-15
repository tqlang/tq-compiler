using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Tq.Ast;
using Tq.Core;

namespace Tq.CodeProcess.Parser;

public partial class Parser(
    ErrorHandler errorHandler,
    DebugOptions debugOptions = default
)
{
    private TokenStream _tokens = null!;
    private string _sourcePath = null!;

    public SyntaxTree Parse(string sourcePath, Token[] tokens)
    {
        _tokens     = new TokenStream(tokens);
        _sourcePath = sourcePath;
        return new SyntaxTree(sourcePath, ParseRootBody(ParseRootContext.TopLevel));
    }

    private SyntaxNode ParseRoot(ParseRootContext ctx)
    {
        string[] expected = ctx switch
        {
            ParseRootContext.TopLevel => ["attribute", "'let'", "'const", "'func'", "'struct'", "'typedef'"],
            ParseRootContext.Struct   => ["attribute", "'let'", "'const", "'func'", "'struct'", "'typedef'", "'constructor'"],
            ParseRootContext.Typedef  => ["attribute", "'func'"],
            _                         => throw new InvalidEnumArgumentException()
        };

        List<AttributeNode> attributes = [];

        while (true)
        {
            var windowStart = _tokens.Checkpoint();

            try
            {
                _tokens.SkipTrivia(QueryUtils.TopLevelTrivia);
                switch (_tokens.Peek().Type)
                {
                    case TokenType.FromKeyword:
                    {
                        var from = _tokens.Eat();
                        _tokens.SkipTrivia(QueryUtils.AllWhitespaces);
                        var fromNamespace = ParseExpression();
                        var import = _tokens.SkipTrivia(QueryUtils.TopLevelTrivia).Eat();
                        if (import.Token is not { Type: TokenType.ImportKeyword })
                            ThrowUnexpectedTokenError(errorHandler, _sourcePath, ["import"], import.Token);

                        if (_tokens.SkipTrivia(QueryUtils.TopLevelTrivia)
                            .NextIs(out var leftBrace, TokenType.LeftCurlyBraceChar))
                        {
                            var collection = new List<(SpecificImportNode.SpecificImportItemNode, TokenNode?)>();
                            while (!_tokens.IsEof() && !_tokens.SkipTrivia(QueryUtils.InterParametersTrivia).NextIs(TokenType.RightCurlyBraceChar))
                            {
                                try
                                {
                                    SpecificImportNode.SpecificImportItemNode node;

                                    _tokens.SkipTrivia(QueryUtils.InterParametersTrivia);
                                    var identifier = ParseIdentifier();

                                    if (_tokens.SkipTrivia(QueryUtils.InterParametersTrivia).NextIs(out var asToken, TokenType.AsKeyword))
                                    {
                                        _tokens.SkipTrivia(QueryUtils.InterParametersTrivia);
                                        var alias = ParseIdentifier();
                                        node = SpecificImportNode.SpecificImportItemNode.BuildAliased(identifier, asToken, alias);
                                    }
                                    else
                                    {
                                        node = SpecificImportNode.SpecificImportItemNode.BuildSimple(identifier);
                                    }

                                    _tokens.SkipTrivia(QueryUtils.InterParametersTrivia).NextIs(out var comma, TokenType.CommaChar);
                                    collection.Add((node, comma));

                                    if (comma == null!) break;
                                }
                                catch
                                {
                                    // TODO see later
                                    throw;
                                }
                            }

                            var rightBrace = _tokens.SkipTrivia(QueryUtils.InterParametersTrivia).Check(TokenType.RightCurlyBraceChar, out var assertment).Eat();
                            if (!assertment) ThrowUnexpectedTokenError(errorHandler, _sourcePath, TokenType.RightParenthesisChar, rightBrace.Token);

                            return ImportNode.BuildSpecific(from, fromNamespace, import, leftBrace, [..collection], rightBrace);
                        }

                        _tokens.ResetPtr();

                        return ImportNode.BuildSimple(from, fromNamespace, import);
                    }

                    case TokenType.AtSignChar:
                    {
                        var at = _tokens.Eat();
                        var identifier = ParseIdentifier();

                        if (_tokens.SkipTrivia(QueryUtils.ExpressionLevelTrivia).NextIs(TokenType.LeftParenthesisChar))
                        {
                            var (leftParenthesis, expressions, rightParenthesis) = ParseArgumentList();
                            return AttributeNode.BuildComplete(at, identifier, leftParenthesis, expressions, rightParenthesis);
                        }

                        attributes.Add(AttributeNode.BuildSimple(at, identifier));
                        continue;
                    }

                    case TokenType.LetKeyword:
                    case TokenType.ConstKeyword:
                    {
                        // Bruh little hack here. it will be parsed as an expression
                        // first and rebuilt into a FieldNode
                        var exp = (LocalVariableExpressionNode)ParseExpression();

                        if (_tokens
                            .SkipTrivia(QueryUtils.ExpressionLevelTrivia)
                            .NextIs(out var equals, TokenType.EqualsChar))
                        {
                            return FieldDeclarationNode.BuildWithValue(
                                attributes.Dump(),
                                exp.Definition,
                                exp.Type,
                                exp.Identifier,
                                equals,
                                ParseExpression()
                            );
                        }

                        return FieldDeclarationNode.BuildUndefined(
                            attributes.Dump(),
                            exp.Definition,
                            exp.Type,
                            exp.Identifier
                        );
                    }

                    case TokenType.FuncKeyword:
                    {
                        var definition = _tokens.Eat();
                        _tokens.SkipTrivia(QueryUtils.AllWhitespaces);
                        var identifier = ParseIdentifier();

                        _tokens.SkipTrivia(QueryUtils.ExpressionLevelTrivia);
                        var (
                            leftParenthesis,
                            parameters,
                            rightParenthesis) = ParseParameterList();

                        ExpressionNode? type = null;

                        if (!_tokens
                                .SkipTrivia(QueryUtils.TopLevelTrivia)
                                .NextIs(TokenType.LeftCurlyBraceChar))
                        {
                            type = ParseExpression();
                            _tokens.SkipTrivia(QueryUtils.TopLevelTrivia);
                        }

                        BodyNode body = ParseStatementBody();

                        return FunctionDeclarationNode.Build(
                            attributes.Dump(),
                            definition,
                            identifier,
                            leftParenthesis,
                            parameters,
                            rightParenthesis,
                            type,
                            body
                        );
                    }

                    case TokenType.StructKeyword:
                    {
                        var definition = _tokens.Eat();
                        _tokens.SkipTrivia(QueryUtils.AllWhitespaces);
                        var identifier = ParseIdentifier();

                        (TokenNode leftP, (TypedIdentifierNode, TokenNode?)[] generics, TokenNode rightP)? generics = null;
                        (TokenNode token, ExpressionNode type)? extends = null;
                        (TokenNode token, (ExpressionNode, TokenNode?)[] types)? implements = null;
                        BodyNode body = null!;

                        if (_tokens
                            .SkipTrivia(QueryUtils.TopLevelTrivia)
                            .NextIs(TokenType.LeftParenthesisChar))
                        {
                            generics = ParseParameterList();
                        }

                        if (_tokens
                            .SkipTrivia(QueryUtils.TopLevelTrivia)
                            .NextIs(out var extendsKeyword, TokenType.ExtendsKeyword))
                        {
                            _tokens.SkipTrivia(QueryUtils.AllWhitespaces);
                            var type = ParseExpression();
                            extends = (extendsKeyword, type);
                        }

                        if (_tokens
                            .SkipTrivia(QueryUtils.TopLevelTrivia)
                            .NextIs(out var implementsToken, TokenType.ImplementsKeyword))
                        {
                            var impl = new List<(ExpressionNode, TokenNode?)>();

                            do
                            {
                                _tokens.SkipTrivia(QueryUtils.AllWhitespaces);
                                var t = ParseExpression();
                                _tokens.SkipTrivia(QueryUtils.AllWhitespaces);

                                if (_tokens.NextIs(out var comma, TokenType.CommaChar))
                                    impl.Add((t, comma));
                                else
                                {
                                    impl.Add((t, null));
                                    break;
                                }
                            } while (true);

                            implements = (implementsToken, [..impl]);
                        }

                        if (_tokens
                            .SkipTrivia(QueryUtils.TopLevelTrivia)
                            .NextIs(TokenType.LeftCurlyBraceChar))
                        {
                            body = ParseTopLevelBody(ParseRootContext.Struct);
                        }

                        return StructDeclarationNode.Build(
                            attributes.Dump(),
                            definition,
                            identifier,
                            generics?.leftP,
                            generics?.generics,
                            generics?.rightP,
                            extends?.token,
                            extends?.type,
                            implements?.token,
                            implements?.types,
                            body
                        );
                    }

                    case TokenType.TypedefKeyword:
                    {
                        var definition = _tokens.Eat();
                        (TokenNode leftParenthesis, ExpressionNode Type, TokenNode rightParenthesis)? type = null;

                        _tokens.SkipTrivia(QueryUtils.ExpressionLevelTrivia);
                        if (_tokens.NextIs(TokenType.LeftParenthesisChar))
                        {
                            var lp = _tokens.Eat();
                            _tokens.SkipTrivia(QueryUtils.ExpressionLevelTrivia);
                            var t = ParseExpression();
                            _tokens.SkipTrivia(QueryUtils.ExpressionLevelTrivia);
                            var rp = _tokens.Eat();

                            type = (lp, t, rp);
                        }

                        _tokens.SkipTrivia(QueryUtils.ExpressionLevelTrivia);
                        var identifier = ParseIdentifier();

                        _tokens.SkipTrivia(QueryUtils.ExpressionLevelTrivia);
                        var body = ParseTopLevelBody(ParseRootContext.Typedef);

                        return TypedefDeclarationNode.Build(
                            attributes.Dump(),
                            definition,
                            type,
                            identifier,
                            (ExplicitBodyNode)body
                        );
                    }
                }

                if (ctx == ParseRootContext.Struct && _tokens.Peek() is { Type: TokenType.Identifier, Value: "constructor" })
                {
                    var definition = _tokens.Eat();

                    _tokens.SkipTrivia(QueryUtils.ExpressionLevelTrivia);
                    var (
                        leftParenthesis,
                        parameters,
                        rightParenthesis) = ParseParameterList();

                    _tokens.SkipTrivia(QueryUtils.ExpressionLevelTrivia);
                    var body = ParseStatementBody();

                    return ConstructorDeclarationNode.Build(
                        attributes.Dump(),
                        definition,
                        leftParenthesis,
                        parameters,
                        rightParenthesis,
                        body
                    );
                }

                if (ctx == ParseRootContext.Typedef && _tokens.Peek() is { Type: TokenType.Identifier, Value: "case" })
                {
                    var definition = _tokens.Eat();

                    _tokens.SkipTrivia(QueryUtils.ExpressionLevelTrivia);
                    if (_tokens.NextIs(TokenType.Identifier))
                    {
                        var identifier = ParseIdentifier();
                        ExpressionNode? expression = null;

                        if (_tokens
                            .SkipTrivia(QueryUtils.ExpressionLevelTrivia)
                            .NextIs(out var equalsChar, TokenType.EqualsChar)) expression = ParseExpression();

                        return TypedefDeclarationNode.BuildCaseIdentifier(
                            attributes.Dump(),
                            definition,
                            identifier,
                            expression != null ? (equalsChar, expression) : null
                        );
                    }

                    {
                        List<(ExpressionNode values, TokenNode? comma)> values = [];
                        //ExpressionNode? expression = null;

                        while (true)
                        {
                            _tokens.SkipTrivia(QueryUtils.ExpressionLevelTrivia);
                            var val = ParseExpression();

                            if (_tokens.SkipTrivia(QueryUtils.ExpressionLevelTrivia).NextIs(out var c, TokenType.CommaChar))
                                values.Add((val, c));
                            else
                            {
                                values.Add((val, null));
                                break;
                            }
                        }

                        //if (_tokens.NextIs(out var equalsChar, TokenType.EqualsChar)) expression = ParseExpression();

                        return TypedefDeclarationNode.BuildCaseExpression(
                            attributes.Dump(),
                            definition,
                            [..values]
                            //expression != null ? (equalsChar, expression) : null
                        );
                    }
                }

                ThrowUnexpectedTokenError(errorHandler, _sourcePath, expected, _tokens.Eat().Token);
            }
            catch (Exception e)
            {
                #if DEBUG
                Console.WriteLine($"Exception: {e}");
                #endif
                return InvalidTopLevelNode.Build(
                    attributes.Dump(),
                    _tokens.DumpRange(windowStart.bp)
                );
            }
        }
    }

    private SyntaxNode[] ParseRootBody(ParseRootContext ctx)
    {
        List<SyntaxNode> nodes = [];

        while (!_tokens.SkipTrivia(QueryUtils.TopLevelTrivia).IsEof()
            && (ctx == ParseRootContext.TopLevel
             || !_tokens
                    .ResetPtr()
                    .SkipTrivia(QueryUtils.TopLevelTrivia)
                    .NextIs(TokenType.RightCurlyBraceChar)))
        {
            _tokens.ResetPtr();
            try
            {
                nodes.Add(ParseRoot(ctx));
            }
            catch (CompilationError ex)
            {
                _ = _tokens.Eat();
                errorHandler.RegisterError(ex);
            }
        }

        return [..nodes];
    }

    private BodyNode ParseTopLevelBody(ParseRootContext ctx)
    {
        var leftBrace = _tokens.Eat();
        if (leftBrace.Type != TokenType.LeftCurlyBraceChar)
            ThrowUnexpectedTokenError(errorHandler, _sourcePath, TokenType.LeftCurlyBraceChar, leftBrace.Token);

        _tokens.SkipTrivia(QueryUtils.TopLevelTrivia);
        var content = ParseRootBody(ctx);

        var rightBrace = _tokens.SkipTrivia(QueryUtils.TopLevelTrivia).Eat();
        if (rightBrace.Type != TokenType.RightCurlyBraceChar)
            ThrowUnexpectedTokenError(errorHandler, _sourcePath, TokenType.RightCurlyBraceChar, rightBrace.Token);

        return ExplicitBodyNode.Build(leftBrace, [.. content], rightBrace);
    }

    private (TokenNode leftParenthesis, (TypedIdentifierNode, TokenNode?)[] parameters, TokenNode rightParenthesis) ParseParameterList()
    {
        var leftParenthesis = _tokens.Check(TokenType.LeftParenthesisChar, out var assertment).Eat();
        if (!assertment) ThrowUnexpectedTokenError(errorHandler, _sourcePath, TokenType.LeftParenthesisChar, leftParenthesis.Token);

        var collection = new List<(TypedIdentifierNode, TokenNode?)>();
        while (!_tokens.IsEof() && !_tokens.SkipTrivia(QueryUtils.InterParametersTrivia).NextIs(TokenType.RightParenthesisChar))
        {
            try
            {
                var type = ParseExpression();
                _tokens.SkipTrivia(QueryUtils.AllWhitespaces);
                var identifier = ParseIdentifier();
                _tokens.SkipTrivia(QueryUtils.InterParametersTrivia).NextIs(out var comma, TokenType.CommaChar);
                collection.Add((TypedIdentifierNode.Build(type, identifier), comma));

                if (comma == null!) break;
            }
            catch
            {
                // Ignored
            }
        }

        var rightParenthesis = _tokens.SkipTrivia(QueryUtils.InterParametersTrivia).Check(TokenType.RightParenthesisChar, out assertment).Eat();
        if (!assertment) ThrowUnexpectedTokenError(errorHandler, _sourcePath, TokenType.RightParenthesisChar, rightParenthesis.Token);

        return (leftParenthesis, [..collection], rightParenthesis);
    }

    private (TokenNode leftParenthesis, (ExpressionNode, TokenNode?)[] parameters, TokenNode rightParenthesis) ParseArgumentList()
    {
        var leftParenthesis = _tokens.SkipTrivia(QueryUtils.InterParametersTrivia).Check(TokenType.LeftParenthesisChar, out var assertment).Eat();
        if (!assertment) ThrowUnexpectedTokenError(errorHandler, _sourcePath, TokenType.LeftParenthesisChar, leftParenthesis.Token);

        var collection = new List<(ExpressionNode, TokenNode?)>();
        while (!_tokens.IsEof() && !_tokens.SkipTrivia(QueryUtils.InterParametersTrivia).NextIs(TokenType.RightParenthesisChar))
        {
            try
            {
                var expression = ParseExpression();
                _tokens.SkipTrivia(QueryUtils.InterParametersTrivia).NextIs(out var comma, TokenType.CommaChar);
                collection.Add((expression, comma));

                if (comma == null!) break;
            }
            catch
            {
                // Ignored
            }
        }

        var rightParenthesis = _tokens.SkipTrivia(QueryUtils.InterParametersTrivia).Check(TokenType.RightParenthesisChar, out assertment).Eat();
        if (!assertment) ThrowUnexpectedTokenError(errorHandler, _sourcePath, TokenType.RightParenthesisChar, rightParenthesis.Token);

        return (leftParenthesis, [..collection], rightParenthesis);
    }

    private (TokenNode leftBracket, (ExpressionNode, TokenNode?)[] parameters, TokenNode rightBracket) ParseIndexList()
    {
        var leftBracket = _tokens.SkipTrivia(QueryUtils.InterParametersTrivia).Check(TokenType.LeftSquareBracketChar, out var assertment).Eat();
        if (!assertment) ThrowUnexpectedTokenError(errorHandler, _sourcePath, TokenType.LeftSquareBracketChar, leftBracket.Token);

        var collection = new List<(ExpressionNode, TokenNode?)>();
        while (!_tokens.IsEof() && !_tokens.SkipTrivia(QueryUtils.InterParametersTrivia).NextIs(TokenType.RightParenthesisChar))
        {
            try
            {
                var exp = ParseExpression();
                _tokens.SkipTrivia(QueryUtils.InterParametersTrivia).NextIs(out var comma, TokenType.CommaChar);
                collection.Add((exp, comma));

                if (comma == null!) break;
            }
            catch
            {
                // Ignored
            }
        }

        var rightBracket = _tokens.SkipTrivia(QueryUtils.InterParametersTrivia).Check(TokenType.RightSquareBracketChar, out assertment).Eat();
        if (!assertment) ThrowUnexpectedTokenError(errorHandler, _sourcePath, TokenType.RightSquareBracketChar, rightBracket.Token);

        return (leftBracket, [..collection], rightBracket);
    }

    public class TokenStream(Token[] tokens)
    {
        private int _basePointer;
        private int _cursorPointer;

        public Token Peek() => _cursorPointer < tokens.Length ? tokens[_cursorPointer] : tokens[^1];

        public bool NextIs(params TokenType[] types)
        {
            var res = types.Contains(Peek().Type);
            return res;
        }

        public bool NextIs([NotNullWhen(true)] out TokenNode? token, params TokenType[] types)
        {
            var res = types.Contains(Peek().Type);
            token = res ? Eat() : null!;
            return res;
        }

        public TokenNode Eat()
        {
            List<TriviaNode> trivia = [];
            for (var i = _basePointer; i < _cursorPointer; i++) trivia.Add(new TriviaNode(tokens[i]));
            var node = new TokenNode([..trivia], Peek());
            _cursorPointer++;
            _basePointer = _cursorPointer;
            return node;
        }

        public bool IsEof() => _cursorPointer >= tokens.Length || tokens[_cursorPointer].Type == TokenType.EofChar;

        public TokenStream SkipTrivia(params TokenType[]? types)
        {
            if (types == null || types.Length == 0) return this;
            while (types.Contains(Peek().Type)) _cursorPointer++;
            return this;
        }

        public TokenStream Check(TokenType expected, out bool result)
        {
            result = Peek().Type == expected;
            return this;
        }

        public TokenStream Check(TokenType expected, string value, out bool result)
        {
            var c = Peek();
            result = c.Type == expected && c.Value == value;
            return this;
        }

        public TokenStream ResetPtr()
        {
            _cursorPointer = _basePointer;
            return this;
        }

        public (int bp, int cp) Checkpoint() => (_basePointer, _cursorPointer);
        public void LoadCheckpoint((int, int) c) => (_basePointer, _cursorPointer) = c;

        public TokenNode[] DumpRange(int from) => DumpRange(from, _cursorPointer);

        public TokenNode[] DumpRange(int from, int to)
        {
            List<TokenNode> t = [];

            from = Math.Clamp(from, 0, tokens.Length);
            to   = Math.Clamp(to, 0, tokens.Length);
            if (from < to) return [];

            for (var i = from; i < to; i++) t.Add(new TokenNode([], tokens[i]));
            return [..t];
        }


        public override string ToString() => $"{Peek()}; CP={_cursorPointer}; BP={_basePointer}";
    }

    private static class QueryUtils
    {
        public static readonly TokenType[] CommonTrivia = [TokenType.Comment];
        public static readonly TokenType[] AllLineFeeds = [TokenType.LineFeedChar, TokenType.SemiColonChar];
        public static readonly TokenType[] AllWhitespaces = [TokenType.SpaceChar, TokenType.TabChar];

        public static readonly TokenType[] TopLevelTrivia = [..AllWhitespaces, ..AllLineFeeds, ..CommonTrivia];
        public static readonly TokenType[] StatementLevelTrivia = [..AllWhitespaces, ..CommonTrivia];
        public static readonly TokenType[] ExpressionLevelTrivia = [..AllWhitespaces, ..AllLineFeeds, ..CommonTrivia];

        public static readonly TokenType[] InterParametersTrivia = [..AllWhitespaces, TokenType.LineFeedChar, ..CommonTrivia];
    }

    private enum ParseRootContext
    {
        TopLevel,
        Struct,
        Typedef,
    }
}
