using System.Linq.Expressions;
using Tqc.Diagnostics;
using Tqc.Syntax.Nodes;
using Tqc.Syntax.Nodes.TypeModifiers;

namespace Tqc.Syntax;

public class Parser(DiagnosticBag diagnostics)
{
    
    private Lexer _lexer = null!;
    private TokenValue? _peeked;
    private List<TriviaNode> _trivia = [];
    private List<AttributeNode> attributeNodes = [];

    #region Trivia Collections
    private readonly TokenKind[] _midDeclarationTrivia = [TokenKind.Comment, TokenKind.LineFeed, TokenKind.Whitespace];
    private readonly TokenKind[] _midStatementTrivia = [TokenKind.Comment, TokenKind.LineFeed, TokenKind.Whitespace];
    private readonly TokenKind[] _midExpressionTrivia = [TokenKind.Comment, TokenKind.LineFeed, TokenKind.Whitespace];
    private readonly TokenKind[] _midExpressionConservativeTrivia = [TokenKind.Comment, TokenKind.Whitespace];
    #endregion

    public SyntaxTree Parse(string Source)
    {
        _lexer = new Lexer(Source);
        _peeked = null;
        return new SyntaxTree(ParseRoot().AsReadOnly());
    }

    public List<SyntaxNode> ParseRoot()
    {
        var declarations = new List<SyntaxNode>();
        while (true)
        {
            AdvanceTrivia(_midDeclarationTrivia);
            if (AtEnd()) break;
            declarations.Add(ParseDeclaration());
        }
        return declarations;
    }

    private SyntaxNode ParseDeclaration()
    {
        while (true)
        {
            if (Check(TokenKind.At))
            {
                var attribute = ParseAttribute();
                attributeNodes.Add(attribute);
                AdvanceTrivia(_midDeclarationTrivia);
                continue;
            }
            break;
        }
        
        if (CheckKeyword("let")) return ParseFieldDeclaration();
        if (CheckKeyword("from")) return parseImportDefinition();
        if (CheckKeyword("func")) return ParseFunctionDeclaration();
        if (CheckKeyword("struct")) return ParseStructDeclaration();
        
        if (CheckKeyword("constructor")) return parseConstructorDeclaration();
        if (CheckKeyword("destructor")) return parseDestructorDeclaration();
        
        var token = Advance();
        Console.WriteLine($"'{token.value}' ({token.kind})");
        throw new NotImplementedException(token.value);
    }

    #region declarations - entities
    private ImportDefinitionNode parseImportDefinition()
    {
        var fromKeyword = ExpectNode("from", "'from' keyword");
        AdvanceTrivia(TokenKind.Whitespace);
        var namespaceIdentifier = ParseCompoundIdentifier();
        AdvanceTrivia(_midDeclarationTrivia);
        var importKeyword = ExpectNode("import", "'import' keyword");
        AdvanceTrivia(TokenKind.Whitespace);
        
        if (!Check(TokenKind.LeftBrace))
            return new ImportDefinitionNode(fromKeyword, namespaceIdentifier, importKeyword, null);

        var leftBrace = ExpectNode(TokenKind.LeftBrace, "open brace ('{')");
        AdvanceTrivia(_midExpressionTrivia);
        var importsList = ParseCommaSeparatedIdentifierCollection(TokenKind.RightBrace);
        AdvanceTrivia(_midExpressionTrivia);
        var rightBrace = ExpectNode(TokenKind.RightBrace, "close brace ('}')");
        var imports = new MembersCollectionNode(leftBrace, [.. importsList], rightBrace);

        return new ImportDefinitionNode(fromKeyword, namespaceIdentifier, importKeyword, imports);
    }
    private FunctionDeclarationNode ParseFunctionDeclaration()
    {
        var funcKeyword = ExpectNode("func", "'func' keyword");
        AdvanceTrivia(TokenKind.Whitespace);
        var nameIdentifier = ParseSingleIdentifier();
        AdvanceTrivia(_midDeclarationTrivia);
        var parameters = ParseParameters();
        AdvanceTrivia(_midDeclarationTrivia);

        ExpressionNode? returnType = null;
        if (PeekKind() != TokenKind.LeftBrace)
        {
            returnType = ParseExpression();
            AdvanceTrivia(_midDeclarationTrivia);
        }

        var body = ParseScope();
        AdvanceTrivia(_midDeclarationTrivia);
        
        return new FunctionDeclarationNode(
            attributeNodes.Dump(),
            funcKeyword,
            nameIdentifier,
            parameters,
            returnType,
            body
        );
    }
    private StructDeclarationNode ParseStructDeclaration()
    {
        var structKeyword = ExpectNode("struct", "'struct' keyword");
        AdvanceTrivia(TokenKind.Whitespace);
        var nameIdentifier = ParseSingleIdentifier();
        AdvanceTrivia(_midDeclarationTrivia);
        
        ParametersCollectionNode? parameters = null;

        (SyntaxNode, ExpressionNode)? extends = null;
        
        if (Check(TokenKind.LeftParenthesis))
        {
            parameters = ParseParameters();
            AdvanceTrivia(_midDeclarationTrivia);
        }

        if (CheckKeyword("extends"))
        {
            var extendsKeyword = AdvanceNode();
            AdvanceTrivia(_midExpressionTrivia);
            var extendsType = ParseExpression();
            extends = (extendsKeyword, extendsType);
            AdvanceTrivia(_midDeclarationTrivia);
        }
        
        var body = ParseStructContent();
        
        return new StructDeclarationNode(
            attributeNodes.Dump(),
            structKeyword,
            nameIdentifier,
            parameters,
            extends,
            body
        );
    }
    private SyntaxNode ParseFieldDeclaration()
    {
        TokenNode letKeyword;
        ExpressionNode? firstExpression = null;
        IdentifierExpressionNode? secondExpression = null;
        
        letKeyword = ExpectNode("let", "'let' keyword");
        AdvanceTrivia(TokenKind.Whitespace);
        
        firstExpression = ParseExpression();
        AdvanceTrivia(_midDeclarationTrivia);

        if (!Check(TokenKind.Equals) && !Check(TokenKind.LineFeed))
        {
            secondExpression = ParseSingleIdentifier();
            AdvanceTrivia(_midDeclarationTrivia);
        }
        
        if (Check(TokenKind.Equals))
        {
            var equals = AdvanceNode();
            AdvanceTrivia(_midDeclarationTrivia);
            var value = ParseExpression();
            
            if (secondExpression != null && firstExpression is not IdentifierExpressionNode)
                throw new NotImplementedException();
            
            if (secondExpression == null) {
                secondExpression = (IdentifierExpressionNode)firstExpression;
                firstExpression  = null!;
            }

            return new FieldDeclarationNode(
                attributeNodes.Dump(),
                letKeyword,
                firstExpression,
                secondExpression,
                equals,
                value
            );
        }
        
        if (Check(TokenKind.LeftBracket))
        {
            throw new NotImplementedException("Properties");
        }
        
        if (secondExpression == null && firstExpression is not IdentifierExpressionNode name)
            throw new NotImplementedException();
            
        if (secondExpression == null) {
            secondExpression = (IdentifierExpressionNode)firstExpression;
            firstExpression  = null!;
        }

        return new FieldDeclarationNode(
            attributeNodes.Dump(),
            letKeyword,
            firstExpression,
            secondExpression,
            null,
            null
        );
        
    }
    
    private ConstructorDeclarationNode parseConstructorDeclaration()
    {
        var ctorKeyword = ExpectNode("constructor", "'constructor' keyword");
        AdvanceTrivia(_midDeclarationTrivia);
        
        var parameters = ParseParameters();
        AdvanceTrivia(_midDeclarationTrivia);

        ExpressionNode? returnType = null;
        if (PeekKind() != TokenKind.LeftBrace)
        {
            returnType = ParseExpression();
            AdvanceTrivia(_midDeclarationTrivia);
        }

        var body = ParseScope();
        AdvanceTrivia(_midDeclarationTrivia);
        
        return new ConstructorDeclarationNode(
            attributeNodes.Dump(),
            ctorKeyword,
            parameters,
            returnType,
            body
        );
    }
    private DestructorDeclarationNode parseDestructorDeclaration()
    {
        var dtorKeyword = ExpectNode("destructor", "'destructor' keyword");
        AdvanceTrivia(_midDeclarationTrivia);
        
        var parameters = ParseParameters();
        AdvanceTrivia(_midDeclarationTrivia);

        ExpressionNode? returnType = null;
        if (PeekKind() != TokenKind.LeftBrace)
        {
            returnType = ParseExpression();
            AdvanceTrivia(_midDeclarationTrivia);
        }

        var body = ParseScope();
        AdvanceTrivia(_midDeclarationTrivia);
        
        return new DestructorDeclarationNode(
            attributeNodes.Dump(),
            dtorKeyword,
            parameters,
            returnType,
            body
        );
    }
    #endregion
    
    #region declarations - misc

    private AttributeNode ParseAttribute()
    {
        var at = ExpectNode(TokenKind.At, "At ('@')");
        var name = ParseSingleIdentifier();
        ArgumentsCollectionNode? arguments = null;
        if (Check(TokenKind.LeftParenthesis)) arguments = ParseArgumentCollection();
        
        return new AttributeNode(at, name, arguments);
    }
    private ParametersCollectionNode ParseParameters()
    {
        var leftParameters = ExpectNode(TokenKind.LeftParenthesis, "open parenthesis ('(')");
        AdvanceTrivia(_midDeclarationTrivia);
        List<(ParameterNode, TokenNode?)> parameters = [];
        
        while (!AtEnd())
        {
            if (PeekKind() == TokenKind.RightParenthesis) break;

            var type = ParseExpression();
            AdvanceTrivia(_midDeclarationTrivia);
            var name = ParseSingleIdentifier();
            AdvanceTrivia(_midDeclarationTrivia);
            TokenNode? comma = null;
            
            if (PeekKind() == TokenKind.Comma)
            {
                comma = AdvanceNode();
                AdvanceTrivia(_midDeclarationTrivia);
            }
            
            parameters.Add((new ParameterNode(type, name), comma));
        }
        
        var rightParameters = ExpectNode(TokenKind.RightParenthesis, "close parenthesis (')')");

        return new ParametersCollectionNode(leftParameters, [..parameters], rightParameters);
    }
    private BlockNode ParseStructContent()
    {
        var content = new List<SyntaxNode>();
        
        var start = ExpectNode(TokenKind.LeftBrace, "'{'");
        AdvanceTrivia(_midDeclarationTrivia);
        
        while (!AtEnd() && !Check(TokenKind.RightBrace)) content.Add(ParseDeclaration());
        AdvanceTrivia(_midDeclarationTrivia);
        
        var end = ExpectNode(TokenKind.RightBrace, "'}'");
        AdvanceTrivia(_midDeclarationTrivia);
        
        return new BlockNode(start, [..content], end);
    }
    #endregion
    
    private StatementNode ParseStatement()
    {
        StatementNode statement;
        AdvanceTrivia(_midStatementTrivia);
        
        // Conditionals
        if (CheckKeyword("if")) statement = ParseIfStatement();
        else if (CheckKeyword("elif")) statement =  ParseElifStatement();
        else if (CheckKeyword("else")) statement = ParseElseStatement();
        
        // Misc
        else if (CheckKeyword("return")) statement =  ParseReturnStatement();
        else if (Check(TokenKind.LeftBrace)) statement = ParseScope();
        
        else statement = ParseExpressionStatement();

        AdvanceTrivia(TokenKind.Whitespace);
        if (Check(TokenKind.Semicolon) || Check(TokenKind.LineFeed)) AdvanceTrivia();
        else if (!Check(TokenKind.RightBrace)) Expect(TokenKind.LineFeed, "Line feed after statement");

        AdvanceTrivia(_midStatementTrivia);
        return statement;
    }

    #region statements - conditionals
    private IfStatementNode ParseIfStatement()
    {
        var ifKeyword = ExpectNode("if", "'if' keyword");
        AdvanceTrivia(TokenKind.Whitespace);
        var condition = ParseExpression();
        AdvanceTrivia(_midDeclarationTrivia);
        var thenBranch = ParseStatement();
        
        return new IfStatementNode(ifKeyword, condition, thenBranch);
    }
    private ElifStatementNode ParseElifStatement()
    {
        var ifKeyword = ExpectNode("elif", "'elif' keyword");
        AdvanceTrivia(TokenKind.Whitespace);
        var condition = ParseExpression();
        AdvanceTrivia(_midDeclarationTrivia);
        var thenBranch = ParseStatement();
        
        return new ElifStatementNode(ifKeyword, condition, thenBranch);
    }
    private ElseStatementNode ParseElseStatement()
    {
        var ifKeyword = ExpectNode("else", "'else' keyword");
        AdvanceTrivia(_midDeclarationTrivia);
        var thenBranch = ParseStatement();
        
        return new ElseStatementNode(ifKeyword, thenBranch);
    }
    #endregion

    #region statements - misc
    private ReturnStatementNode ParseReturnStatement()
    {
        var start = ExpectNode("return", "'return' keyword");
        AdvanceTrivia(TokenKind.Whitespace);
        var value = Check(TokenKind.LineFeed) ? null : ParseExpression();
        
        return new ReturnStatementNode(start, value);
    }
    private ScopeNode ParseScope()
    {
        var content = new List<StatementNode>();
        
        var start = ExpectNode(TokenKind.LeftBrace, "'{'");
        while (!AtEnd() && !Check(TokenKind.RightBrace)) content.Add(ParseStatement());
        var end = ExpectNode(TokenKind.RightBrace, "'}'");
        
        return new ScopeNode(start, [..content], end);
    }
    private ExpressionStatementNode ParseExpressionStatement()
    {
        return new ExpressionStatementNode(ParseExpression());
    }
    #endregion
    
    private ExpressionNode ParseExpression() => ParseAssignment();
    private ExpressionNode ParseType() => ParseAdditive();

    private ExpressionNode ParseAssignment()
    {
        var left = ParseEquality();
        var cp = SaveCheckpoint();
        AdvanceTrivia(_midExpressionTrivia);
        
        if (Peek() is { } tok && IsAssignOp(tok.kind))
        {
            var op = AdvanceNode();
            AdvanceTrivia(_midExpressionTrivia);
            var right = ParseAssignment();
            return new AssignmentExpressionNode(left, op, right);
        }
        Rollback(cp);
        
        return left;
    }

    private ExpressionNode ParseEquality()
    {
        var left = ParseRelational();
        var cp = SaveCheckpoint();
        AdvanceTrivia(_midExpressionTrivia);
        
        while (PeekKind()
               is TokenKind.EqualsEquals or TokenKind.ExactEquals 
               or TokenKind.NotEqualsEquals or TokenKind.NotExactEquals)
        {
            var op = AdvanceNode();
            AdvanceTrivia(_midExpressionTrivia);
            var right = ParseEquality();
            left = new BinaryExpressionNode(left, op, right);
        }
        
        Rollback(cp);
        return left;
    }

    private ExpressionNode ParseRelational()
    {
        var left = ParseAdditive();
        var cp = SaveCheckpoint();
        AdvanceTrivia(_midExpressionTrivia);
        
        while (PeekKind()
               is TokenKind.LeftAngle or TokenKind.RightAngle
               or TokenKind.LessEqual or TokenKind.GreaterEqual)
        {
            var op = AdvanceNode();
            AdvanceTrivia(_midExpressionTrivia);
            var right = ParseAdditive();
            left = new BinaryExpressionNode(left, op, right);
        }
        
        Rollback(cp);
        return left;
    }

    private ExpressionNode ParseAdditive()
    {
        var left = ParseMultiplicative();
        var cp = SaveCheckpoint();
        AdvanceTrivia(_midExpressionTrivia);
        
        while (PeekKind()
               is TokenKind.Plus or TokenKind.Minus 
               or TokenKind.AddWrap or TokenKind.SubWrap 
               or TokenKind.AddOnBounds or TokenKind.SubOnBounds)
        {
            var op = AdvanceNode();
            AdvanceTrivia(_midExpressionTrivia);
            var right = ParseMultiplicative();
            left = new BinaryExpressionNode(left, op, right);
        }
        
        Rollback(cp);
        return left;
    }

    private ExpressionNode ParseMultiplicative()
    {
        var left = ParsePrefix();
        var cp = SaveCheckpoint();
        AdvanceTrivia(_midExpressionTrivia);
        
        while (PeekKind()
               is TokenKind.Star or TokenKind.Slash or TokenKind.Rest
               or TokenKind.MulWrap or TokenKind.MulOnBounds
               or TokenKind.DivFloor or TokenKind.DivCeil)
        {
            var op = AdvanceNode();
            AdvanceTrivia(_midExpressionTrivia);
            var right = ParsePrefix();
            left = new BinaryExpressionNode(left, op, right);
        }
        
        Rollback(cp);
        return left;
    }

    private ExpressionNode ParsePrefix()
    {
        switch (PeekKind())
        {
            case TokenKind.Star:
            {
                var starNode = AdvanceNode();
                var type = ParsePrefix();
                return new ReferenceTypeModifierNode(starNode, type);
            }
            
            case TokenKind.LeftBracket:
            {
                var leftBracket = AdvanceNode();
                var rightBracket = ExpectNode(TokenKind.RightBracket, "Right bracket (']')");
                var type = ParsePrefix();
                return new SliceTypeModifierNode(leftBracket, rightBracket, type);
            }

            case TokenKind.Bang
                or TokenKind.Minus
                or TokenKind.Increment
                or TokenKind.Decrement:
            {
                var op = AdvanceNode();
                var operand = ParsePrefix();
                return new UnaryPrefixExpressionNode(op, operand);
            }
        }
        
        return ParsePostfix();
    }

    private ExpressionNode ParsePostfix()
    {
        var expr = ParsePrimary();
        
        while (true)
        {
            var cp = SaveCheckpoint();
            AdvanceTrivia(_midExpressionTrivia);
            
            if (Check(TokenKind.LeftParenthesis))
            {
                var args = ParseArgumentCollection();
                expr = new CallExpressionNode(expr, args);
                
                continue;
            }
            
            if (Check(TokenKind.LeftBracket))
            {
                var leftBracket = ExpectNode(TokenKind.LeftParenthesis, "open bracket ('[')");
                AdvanceTrivia(_midExpressionTrivia);
                var args = ParseCommaSeparatedExpressionCollection();
                AdvanceTrivia(_midExpressionTrivia);
                var rightBracket = ExpectNode(TokenKind.LeftParenthesis, "close bracket (']')");
                expr = new IndexExpressionNode(expr, new IndexCollectionNode(leftBracket, [..args], rightBracket));
                
                continue;
            }
            
            if (Check(TokenKind.Dot))
            {
                var dot = AdvanceNode();
                AdvanceTrivia(_midExpressionTrivia);
                var member = ParseSingleIdentifier();
                expr = new MemberAccessExpressionNode(expr, dot, member);
                
                continue;
            }
            
            if (Peek() is { kind: TokenKind.Increment or TokenKind.Decrement } postOp)
            {
                var op = AdvanceNode();
                expr = new UnaryPostfixExpressionNode(expr, op);
                
                break;
            }
            
            if (CheckKeyword("as"))
            {
                var op = AdvanceNode();
                AdvanceTrivia(_midExpressionTrivia);
                var operand = ParseExpression();
                expr = new ConversionExpressionNode(expr, op, operand);
                
                break;
            }

            Rollback(cp);
            break;
        }
        
        return expr;
    }

    private ExpressionNode ParsePrimary()
    {
        if (CheckKeyword("let"))
        {
            TokenNode letKeyword;
            ExpressionNode? firstExpression;
            IdentifierExpressionNode? secondExpression = null;
        
            letKeyword = ExpectNode("let", "'let' keyword");
            AdvanceTrivia(TokenKind.Whitespace);
        
            firstExpression = ParseType();
            AdvanceTrivia(_midDeclarationTrivia);
            
            if (!Check(TokenKind.Equals) && !Check(TokenKind.LineFeed))
            {
                secondExpression = ParseSingleIdentifier();
                AdvanceTrivia(_midDeclarationTrivia);
            }
            
            if (secondExpression == null && firstExpression is not IdentifierExpressionNode name)
                throw new NotImplementedException();
            
            if (secondExpression == null) {
                secondExpression = (IdentifierExpressionNode)firstExpression;
                firstExpression  = null!;
            }

            return new LocalExpressionNode(letKeyword, firstExpression, secondExpression);
        }

        if (CheckKeyword("new"))
        {
            var keyword = AdvanceNode();
            AdvanceTrivia(_midExpressionConservativeTrivia);
            
            var expr =  ParseExpression();
            var cp = SaveCheckpoint();

            if (Check(TokenKind.LeftParenthesis))
                return new NewExpressionNode(keyword, expr, ParseArgumentCollection());

            if (expr is CallExpressionNode callExpr)
            {
                Rollback(cp);
                return new NewExpressionNode(keyword, callExpr.Callee, callExpr.Arguments);
            }
            
            throw new NotImplementedException();
        }
        
        if (PeekKind() is TokenKind.IntegerNumberLiteral)
            return new IntegerLiteralExpressionNode(AdvanceNode());
        
        if (PeekKind() is TokenKind.FloatingNumberLiteral)
            return new DecimalLiteralExpressionNode(AdvanceNode());
            
        if (Check(TokenKind.Identifier))
            return ParseSingleIdentifier();
        
        if (Check(TokenKind.LeftParenthesis))
        {
            var open = ExpectNode(TokenKind.LeftParenthesis, "open parenthesis ('(')");
            AdvanceTrivia(_midExpressionTrivia);
            var expr = ParseExpression();
            AdvanceTrivia(_midExpressionTrivia);
            var close = ExpectNode(TokenKind.RightParenthesis, "close parenthesis (')')");
            return new ParenthesisExpressionNode(open, expr, close);
        }

        if (Check(TokenKind.DoubleQuote)) return ParseStringExpression();
        
        if (Peek() is { } bad) throw Error(bad, "Invalid expression");
            throw new EndOfStreamException("Expected expression, found End of Stream");
    }

    private StringExpressionNode ParseStringExpression()
    {
        var oldMode = _lexer.Mode;
        _lexer.Mode = Lexer.LexerMode.String;
        
        List<StringContentNode> content = [];
        
        var open = ExpectNode(TokenKind.DoubleQuote, "double quotes ('\"')");
        while (!AtEnd() && !Check(TokenKind.DoubleQuote))
        {
            if (Check(TokenKind.StringLiteral)) content.Add(new StringLiteralNode(AdvanceNode()));
            if (Check(TokenKind.EscapedCharacter)) content.Add(new StringCharacterLiteralNode(AdvanceNode()));
        }
        var close = ExpectNode(TokenKind.DoubleQuote, "double quotes ('\"')");

        _lexer.Mode = oldMode;
        return new StringExpressionNode(open, [.. content], close);
    }
    
    private CompoundIdentifierExpressionNode ParseCompoundIdentifier()
    {
        List<(TokenNode, TokenNode?)> arguments = [];
        while (!AtEnd())
        {
            var identifier = ExpectNode(TokenKind.Identifier, "identifier");
            AdvanceTrivia(_midExpressionTrivia);
            TokenNode? dot = null;
            if (Check(TokenKind.Dot)) dot = AdvanceNode();
            
            arguments.Add((identifier, dot));
            if (dot == null) break;
        }
        return new CompoundIdentifierExpressionNode([.. arguments]);
    }
    private IdentifierExpressionNode ParseSingleIdentifier()
    {
        var tok = Expect(TokenKind.Identifier, "identifier");
        return new IdentifierExpressionNode(new TokenNode(_trivia.Dump(), tok));
    }

    private List<(ExpressionNode, TokenNode?)> ParseCommaSeparatedExpressionCollection(params TokenKind[] alsoBreaksAt)
    {
        List<(ExpressionNode, TokenNode?)> arguments = [];
        
        while (!AtEnd())
        {
            if (PeekKind() == TokenKind.RightParenthesis) break;
            
            var exp = ParseExpression();
            AdvanceTrivia(_midDeclarationTrivia);
            TokenNode? comma = null;
            
            if (PeekKind() == TokenKind.Comma)
            {
                comma = AdvanceNode();
                AdvanceTrivia(_midDeclarationTrivia);
            }
            
            arguments.Add((exp, comma));
            if (alsoBreaksAt.Contains(PeekKind())) break;
        }

        return arguments;
    }
    private List<(IdentifierExpressionNode, TokenNode?)> ParseCommaSeparatedIdentifierCollection(params TokenKind[] alsoBreaksAt)
    {
        List<(IdentifierExpressionNode, TokenNode?)> identifiers = [];
        
        while (!AtEnd())
        {
            if (PeekKind() == TokenKind.LeftParenthesis) break;
            
            var identifier = ParseSingleIdentifier();
            AdvanceTrivia(_midDeclarationTrivia);
            TokenNode? comma = null;
            
            if (Check(TokenKind.Comma))
            {
                comma = AdvanceNode();
                AdvanceTrivia(_midExpressionTrivia);
            }
            
            identifiers.Add((identifier, comma));
            if (alsoBreaksAt.Contains(PeekKind())) break;
        }

        return identifiers;
    }
    private ArgumentsCollectionNode ParseArgumentCollection()
    {
        var leftParenthesis = ExpectNode(TokenKind.LeftParenthesis, "open parenthesis ('(')");
        AdvanceTrivia(_midExpressionTrivia);
        var args = ParseCommaSeparatedExpressionCollection();
        AdvanceTrivia(_midExpressionTrivia);
        var rightParenthesis = ExpectNode(TokenKind.RightParenthesis, "close parenthesis (')')");
        return new ArgumentsCollectionNode(leftParenthesis, [..args], rightParenthesis);
    }

    private static bool IsAssignOp(TokenKind kind) => kind is TokenKind.Equals
        or TokenKind.AddAssign or TokenKind.SubAssign or TokenKind.MulAssign
        or TokenKind.DivAssign or TokenKind.RestAssign;

    #region Helpers

    private TokenValue? Peek()
    {
        try
        {
            return _peeked ??= _lexer.NextToken();
        }
        catch (EndOfStreamException _)
        {
            _peeked = null;
            return null;
        }
    }
    private TokenKind PeekKind()
    {
        _peeked ??= _lexer.NextToken();
        return _peeked?.kind ?? TokenKind.Eof;
    }

    private bool AtEnd() => Peek() is null;

    private TokenValue Advance()
    {
        var tok = Peek() ?? throw new EndOfStreamException("Reached End Of File");
        _peeked = null;
        return tok;
    }
    private TokenNode AdvanceNode()
    {
        var tok = Peek() ?? throw new EndOfStreamException("Reached End Of File");
        _peeked = null;
        return new TokenNode(_trivia.Dump(), tok);
    }
    private void AdvanceTrivia()
    {
        var tok = Peek() ;
        if (!tok.HasValue) return;
        _peeked = null;
        _trivia.Add(new TriviaNode(tok.Value));
    }
    private void AdvanceTrivia(params TokenKind[] kinds)
    {
        while (!AtEnd())
        {
            var node = PeekKind();
            if (!kinds.Contains(node)) break;
            AdvanceTrivia();
        }
    }
    
    private bool Check(TokenKind kind) => Peek() is { } t && t.kind == kind;

    private bool CheckKeyword(string word) => Peek() is { kind: TokenKind.Identifier } t && t.value == word;
    

    private TokenValue Expect(TokenKind kind, string expected)
    {
        if (Check(kind)) return Advance();
        if (Peek() is { } t) throw Error(t, $"Expected '{expected}', found '{t.value}'");
        throw new EndOfStreamException($"Expected '{expected}', found End Of File");
    }
    private TokenNode ExpectNode(string keyword, string expected)
    {
        if (CheckKeyword(keyword)) return new TokenNode(_trivia.Dump(), Advance());
        if (Peek() is { } t) throw Error(t, $"Expected '{expected}', found '{t.value}'");
        throw new EndOfStreamException($"Expected '{expected}', found End Of File");
    }
    private TokenNode ExpectNode(TokenKind kind, string expected)
    {
        if (Check(kind)) return new TokenNode(_trivia.Dump(), Advance());
        if (Peek() is { } t) throw Error(t, $"Expected '{expected}', found '{t.value}'");
        throw new EndOfStreamException($"Expected '{expected}', found End Of File");
    }

    private (int, TokenValue?, LexCheckpoint) SaveCheckpoint() => (_trivia.Count, _peeked, _lexer.SaveCheckpoint());
    private void Rollback((int, TokenValue?, LexCheckpoint) cp)
    {
        _trivia.RemoveRange(cp.Item1, _trivia.Count - cp.Item1);
        _peeked = cp.Item2;
        cp.Item3.Restore();
    }
    
    private Exception Error(TokenValue tok, string message) => new ParseException(message, tok.Line, tok.Column);
    
    #endregion
}

public class ParseException(string message, int line, int column) : Exception($"[{line}:{column}] {message}")
{
    public int Line { get; } = line;
    public int Column { get; } = column;
}
