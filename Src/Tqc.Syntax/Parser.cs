using Tqc.Core;
using Tqc.Diagnostics;
using Tqc.Syntax.Nodes;
using Tqc.Syntax.Nodes.TypeModifiers;

namespace Tqc.Syntax;

public class Parser(DiagnosticBag diagnostics)
{
    private string _path = null!;
    private Lexer _lexer = null!;
    private TokenValue? _peeked;
    private List<TriviaNode> _trivia = [];

    #region Trivia Collections
    private readonly TokenKind[] _midDeclarationTrivia = [TokenKind.Comment, TokenKind.LineFeed, TokenKind.Whitespace];
    private readonly TokenKind[] _midDeclarationConservativeTrivia = [TokenKind.Comment, TokenKind.Whitespace];
    private readonly TokenKind[] _midStatementTrivia = [TokenKind.Comment, TokenKind.LineFeed, TokenKind.Whitespace];
    private readonly TokenKind[] _midExpressionTrivia = [TokenKind.Comment, TokenKind.LineFeed, TokenKind.Whitespace];
    private readonly TokenKind[] _midExpressionConservativeTrivia = [TokenKind.Comment, TokenKind.Whitespace];
    #endregion

    public SyntaxTree Parse(string path, string source)
    {
        _path   = path;
        _lexer  = new Lexer(source);
        _peeked = null;
        return new SyntaxTree(path, ParseRoot());
    }

    public SyntaxNode[] ParseRoot()
    {
        var declarations = new List<SyntaxNode>();
        while (true)
        {
            AdvanceTrivia(_midDeclarationTrivia);
            if (AtEnd()) break;
            declarations.Add(ParseDeclaration(
                ScopeAllowedDeclarations.Import
                | ScopeAllowedDeclarations.Attributes
                | ScopeAllowedDeclarations.Field
                | ScopeAllowedDeclarations.Struct
                | ScopeAllowedDeclarations.Typedef
            ));
        }
        return [.. declarations];
    }

    private SyntaxNode ParseDeclaration(ScopeAllowedDeclarations allowedDeclarations)
    {
        if (allowedDeclarations.HasFlag(ScopeAllowedDeclarations.Attributes) && Check(TokenKind.At)) return ParseAttribute();
        
        if (allowedDeclarations.HasFlag(ScopeAllowedDeclarations.Field) && (CheckKeyword("let") || CheckKeyword("const"))) return ParseFieldDeclaration();
        if (allowedDeclarations.HasFlag(ScopeAllowedDeclarations.Field) && CheckKeyword("const")) return ParseFieldDeclaration();
        
        if (allowedDeclarations.HasFlag(ScopeAllowedDeclarations.Import) && CheckKeyword("from")) return parseImportDefinition();
        if (CheckKeyword("func")) return ParseFunctionDeclaration();
        if (allowedDeclarations.HasFlag(ScopeAllowedDeclarations.Struct) && CheckKeyword("struct")) return ParseStructDeclaration();
        if (allowedDeclarations.HasFlag(ScopeAllowedDeclarations.Typedef) && CheckKeyword("typedef")) return ParseTypedefDeclaration();
        
        if (allowedDeclarations.HasFlag(ScopeAllowedDeclarations.Constructor) && CheckKeyword("constructor")) return ParseConstructorDeclaration();
        if (allowedDeclarations.HasFlag(ScopeAllowedDeclarations.Destructor) && CheckKeyword("destructor")) return ParseDestructorDeclaration();
        
        if (allowedDeclarations.HasFlag(ScopeAllowedDeclarations.Case) && CheckKeyword("case")) return ParseCaseDeclaration();
        
        var token = Advance();
        Console.WriteLine($"'{token.value}' ({token.kind})");
        throw new NotImplementedException(token.value);
    }

    #region declarations - entities
    private ImportDirectiveNode parseImportDefinition()
    {
        var fromKeyword = ExpectNode("from", "'from' keyword");
        AdvanceTrivia(TokenKind.Whitespace);
        var namespaceIdentifier = ParseCompoundIdentifier();
        AdvanceTrivia(_midDeclarationTrivia);
        var importKeyword = ExpectNode("import", "'import' keyword");
        AdvanceTrivia(TokenKind.Whitespace);

        if (!Check(TokenKind.LeftBrace))
        {
            return new ImportDirectiveNode(
                fromKeyword,
                namespaceIdentifier,
                importKeyword,
                null,
                ConsumeDeclarationsLeadingTrivia()
            );
        }

        var leftBrace = ExpectNode(TokenKind.LeftBrace, "open brace ('{')");
        AdvanceTrivia(_midExpressionTrivia);

        List<(MembersCollectionItemNode node, TokenNode? comma)> items = [];
        while (!AtEnd())
        {
            if (PeekKind() == TokenKind.LeftParenthesis) break;
            var firstIdentifier = ParseSingleIdentifier();
            AdvanceTrivia(_midExpressionTrivia);

            MembersCollectionItemNode item;

            if (CheckKeyword("as"))
            {
                var asKeyword = AdvanceNode();
                AdvanceTrivia(_midExpressionTrivia);
                var identifier = ParseSingleIdentifier();
                item = new MembersCollectionAliasedItemNode(firstIdentifier, asKeyword, identifier);
            }
            else
            {
                item = new MembersCollectionPureItemNode(firstIdentifier);
            }

            AdvanceTrivia(_midExpressionTrivia);

            TokenNode? comma = null;

            if (Check(TokenKind.Comma))
            {
                comma = AdvanceNode();
                AdvanceTrivia(_midExpressionTrivia);
            }

            items.Add((item, comma));

            if (Check(TokenKind.RightBrace)) break;
        }
    
        AdvanceTrivia(_midExpressionTrivia);
        var rightBrace = ExpectNode(TokenKind.RightBrace, "close brace ('}')");
        var imports = new MembersCollectionNode(leftBrace, [.. items], rightBrace);
        
        return new ImportDirectiveNode(
            fromKeyword,
            namespaceIdentifier,
            importKeyword,
            imports, 
            ConsumeDeclarationsLeadingTrivia()
        );
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
            funcKeyword,
            nameIdentifier,
            parameters,
            returnType,
            body,
            ConsumeDeclarationsLeadingTrivia()
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
            structKeyword,
            nameIdentifier,
            parameters,
            extends,
            body,
            ConsumeDeclarationsLeadingTrivia()
        );
    }
    private TypedefDeclarationNode ParseTypedefDeclaration()
    {
        var typedefKeyword = ExpectNode("typedef", "'typedef' keyword");

        // `typedef(u32) Name`: the type the entries are stored as.
        (TokenNode, ExpressionNode, TokenNode)? underlyingType = null;
        if (Check(TokenKind.LeftParenthesis))
        {
            var left = AdvanceNode();
            AdvanceTrivia(_midExpressionTrivia);
            var type = ParseExpression();
            AdvanceTrivia(_midExpressionTrivia);
            var right = ExpectNode(TokenKind.RightParenthesis, "close parenthesis (')')");
            underlyingType = (left, type, right);
        }

        AdvanceTrivia(TokenKind.Whitespace);
        var nameIdentifier = ParseSingleIdentifier();
        AdvanceTrivia(_midDeclarationTrivia);
        
        var body = ParseTypedefContent();
        
        return new TypedefDeclarationNode(
            typedefKeyword,
            underlyingType,
            nameIdentifier,
            body,
            ConsumeDeclarationsLeadingTrivia()
        );
    }
    private DeclarationNode ParseFieldDeclaration()
    {
        TokenNode letKeyword;
        ExpressionNode? firstExpression;
        IdentifierExpressionNode? secondExpression = null;
        
        letKeyword = CheckKeyword("const") ? ExpectNode("const", "'const' keyword") : ExpectNode("let", "'let' keyword");
        AdvanceTrivia(TokenKind.Whitespace);
        
        firstExpression = ParseExpression();
        AdvanceTrivia(_midDeclarationTrivia);

        if (!Check(TokenKind.Equals) && !Check(TokenKind.LeftBrace) && !Check(TokenKind.LineFeed))
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
                letKeyword,
                firstExpression,
                secondExpression,
                equals,
                value,
                ConsumeDeclarationsLeadingTrivia()
            );
        }
        
        if (Check(TokenKind.LeftBrace))
        {
            // `let name { ... }` has no type; `let T name { ... }` does.
            if (secondExpression == null && firstExpression is not IdentifierExpressionNode)
                throw Error(Peek()!.Value, "Expected the name of the property");
            if (secondExpression == null) {
                secondExpression = (IdentifierExpressionNode)firstExpression;
                firstExpression  = null!;
            }

            return ParsePropertyBody(letKeyword, firstExpression, secondExpression);
        }
        
        if (secondExpression == null && firstExpression is not IdentifierExpressionNode name)
            throw new NotImplementedException();
            
        if (secondExpression == null) {
            secondExpression = (IdentifierExpressionNode)firstExpression;
            firstExpression  = null!;
        }

        return new FieldDeclarationNode(
            letKeyword,
            firstExpression,
            secondExpression,
            null,
            null,
            ConsumeDeclarationsLeadingTrivia()
        );
        
    }
    
    /// <summary>`{ get; set }`, one accessor per line or separated by semicolons.</summary>
    private PropertyDeclarationNode ParsePropertyBody(TokenNode letKeyword, ExpressionNode? type, IdentifierExpressionNode name)
    {
        if (letKeyword.Value == "const")
            throw Error(Peek()!.Value, "A property cannot be 'const'");

        var left = ExpectNode(TokenKind.LeftBrace, "'{'");
        List<PropertyAccessorNode> accessors = [];

        AdvanceTrivia(_midDeclarationTrivia);
        while (true)
        {
            while (Check(TokenKind.Semicolon))
            {
                AdvanceTrivia();
                AdvanceTrivia(_midDeclarationTrivia);
            }

            if (AtEnd() || Check(TokenKind.RightBrace)) break;
            accessors.Add(ParsePropertyAccessor());
            AdvanceTrivia(_midDeclarationTrivia);
        }

        var right = ExpectNode(TokenKind.RightBrace, "'}'");
        return new PropertyDeclarationNode(letKeyword, type, name, left, [..accessors], right, ConsumeDeclarationsLeadingTrivia());
    }

    private PropertyAccessorNode ParsePropertyAccessor()
    {
        if (!CheckKeyword("get") && !CheckKeyword("set"))
            throw Error(Peek()!.Value, "Expected 'get' or 'set'");

        var keyword = AdvanceNode();
        AdvanceTrivia(_midDeclarationConservativeTrivia);

        TokenNode? equals = null;
        ExpressionNode? function = null;
        ScopeNode? body = null;

        if (Check(TokenKind.Equals))
        {
            equals = AdvanceNode();
            AdvanceTrivia(_midDeclarationConservativeTrivia);
            function = ParseExpression();
        }
        else if (Check(TokenKind.LeftBrace))
        {
            body = ParseScope();
        }

        return new PropertyAccessorNode(keyword, equals, function, body, _trivia.Dump());
    }

    private ConstructorDeclarationNode ParseConstructorDeclaration()
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
            ctorKeyword,
            parameters,
            returnType,
            body,
            ConsumeDeclarationsLeadingTrivia()
        );
    }
    private DestructorDeclarationNode ParseDestructorDeclaration()
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
            dtorKeyword,
            parameters,
            returnType,
            body,
            ConsumeDeclarationsLeadingTrivia()
        );
    }

    private TypedefCaseDeclaration ParseCaseDeclaration()
    {
        var caseKeyword = ExpectNode("case", "'case' keyword");
        AdvanceTrivia(_midDeclarationConservativeTrivia);

        if (PeekKind() is TokenKind.IntegerNumberLiteral)
        {
            List<(IntegerLiteralExpressionNode, TokenNode?)> items = [];
            while (!AtEnd())
            {
                if (!Check(TokenKind.IntegerNumberLiteral)) throw Error(Peek()!.Value, "Expected integer number literal");
                var integer = new IntegerLiteralExpressionNode(AdvanceNode());
                AdvanceTrivia(_midExpressionTrivia);
                
                TokenNode? comma = null;
                if (Check(TokenKind.Comma))
                {
                    comma = AdvanceNode();
                    AdvanceTrivia(_midExpressionTrivia);
                }
                
                items.Add((integer, comma));
                if (comma == null) break;
            }

            return new TypedefNumberedCaseDeclaration(
                caseKeyword,
                [.. items],
                ConsumeDeclarationsLeadingTrivia()
            );
        }

        // `case name` or `case name = value`
        var identifier = ParseSingleIdentifier();
        AdvanceTrivia(_midDeclarationTrivia);

        if (Check(TokenKind.Equals))
        {
            var equals = AdvanceNode();
            AdvanceTrivia(_midExpressionTrivia);
            var value = ParseExpression();
            AdvanceTrivia(_midDeclarationTrivia);

            return new TypedefNamedCaseWithValueDeclaration(
                caseKeyword,
                identifier,
                equals,
                value,
                ConsumeDeclarationsLeadingTrivia()
            );
        }

        return new TypedefNamedCaseDeclaration(
            caseKeyword,
            identifier,
            ConsumeDeclarationsLeadingTrivia()
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

        AdvanceTrivia(_midDeclarationTrivia);
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

        while (!AtEnd() && !Check(TokenKind.RightBrace))
        {
            content.Add(ParseDeclaration(
                ScopeAllowedDeclarations.Attributes
                | ScopeAllowedDeclarations.Field
                | ScopeAllowedDeclarations.Constructor
                | ScopeAllowedDeclarations.Destructor
            ));
        }
        AdvanceTrivia(_midDeclarationTrivia);
        
        var end = ExpectNode(TokenKind.RightBrace, "'}'");
        AdvanceTrivia(_midDeclarationTrivia);
        
        return new BlockNode(start, [..content], end);
    }
    private BlockNode ParseTypedefContent()
    {
        var content = new List<SyntaxNode>();
        
        var start = ExpectNode(TokenKind.LeftBrace, "'{'");
        AdvanceTrivia(_midDeclarationTrivia);
        
        while (!AtEnd() && !Check(TokenKind.RightBrace))
        {
            content.Add(ParseDeclaration(
                ScopeAllowedDeclarations.Attributes
                | ScopeAllowedDeclarations.Field
                | ScopeAllowedDeclarations.Case
            ));
        }
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
        
        // Loops
        else if (CheckKeyword("while")) statement = ParseWhileStatement();
        else if (CheckKeyword("break")) statement = ParseBreakStatement();
        
        // Misc
        else if (CheckKeyword("return")) statement =  ParseReturnStatement();
        else if (Check(TokenKind.LeftBrace)) statement = ParseScope();
        
        else statement = ParseExpressionStatement();
        
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
        
        return new IfStatementNode(ifKeyword, condition, thenBranch, ConsumeStatementLeadingTrivia());
    }
    private ElifStatementNode ParseElifStatement()
    {
        var ifKeyword = ExpectNode("elif", "'elif' keyword");
        AdvanceTrivia(TokenKind.Whitespace);
        var condition = ParseExpression();
        AdvanceTrivia(_midDeclarationTrivia);
        var thenBranch = ParseStatement();
        
        return new ElifStatementNode(ifKeyword, condition, thenBranch, ConsumeStatementLeadingTrivia());
    }
    private ElseStatementNode ParseElseStatement()
    {
        var ifKeyword = ExpectNode("else", "'else' keyword");
        AdvanceTrivia(_midDeclarationTrivia);
        var thenBranch = ParseStatement();
        
        return new ElseStatementNode(ifKeyword, thenBranch, ConsumeStatementLeadingTrivia());
    }
    #endregion

    #region statements - loops
    /// <summary>
    /// `while c s`, `while c : step s`, `while decl : c : step s`: the colons say which parts there are.
    /// A `{` after the last part is the body, as in `if`.
    /// </summary>
    private WhileStatementNode ParseWhileStatement()
    {
        var whileKeyword = ExpectNode("while", "'while' keyword");
        AdvanceTrivia(TokenKind.Whitespace);

        List<ExpressionNode> parts = [ParseExpression()];
        List<TokenNode> colons = [];

        while (parts.Count < 3)
        {
            var cp = SaveCheckpoint();
            AdvanceTrivia(TokenKind.Whitespace);
            if (!Check(TokenKind.Colon))
            {
                Rollback(cp);
                break;
            }

            colons.Add(AdvanceNode());
            AdvanceTrivia(TokenKind.Whitespace);
            parts.Add(ParseExpression());
        }

        AdvanceTrivia(_midDeclarationTrivia);
        var body = ParseStatement();

        return parts.Count switch
        {
            1 => new WhileStatementNode(whileKeyword, null, null, parts[0], null, null, body, ConsumeStatementLeadingTrivia()),
            2 => new WhileStatementNode(whileKeyword, null, null, parts[0], colons[0], parts[1], body, ConsumeStatementLeadingTrivia()),
            _ => new WhileStatementNode(whileKeyword, parts[0], colons[0], parts[1], colons[1], parts[2], body, ConsumeStatementLeadingTrivia()),
        };
    }
    private BreakStatementNode ParseBreakStatement()
    {
        var keyword = ExpectNode("break", "'break' keyword");
        return new BreakStatementNode(keyword, ConsumeStatementLeadingTrivia());
    }
    #endregion

    #region statements - misc
    private ReturnStatementNode ParseReturnStatement()
    {
        var start = ExpectNode("return", "'return' keyword");
        AdvanceTrivia(TokenKind.Whitespace);
        var value = Check(TokenKind.LineFeed) ? null : ParseExpression();
        
        return new ReturnStatementNode(start, value, ConsumeStatementLeadingTrivia());
    }
    private ScopeNode ParseScope()
    {
        var content = new List<StatementNode>();
        
        var start = ExpectNode(TokenKind.LeftBrace, "'{'");
        while (!AtEnd() && !Check(TokenKind.RightBrace)) content.Add(ParseStatement());
        var end = ExpectNode(TokenKind.RightBrace, "'}'");
        
        return new ScopeNode(start, [..content], end, ConsumeStatementLeadingTrivia());
    }
    private ExpressionStatementNode ParseExpressionStatement()
    {
        return new ExpressionStatementNode(ParseExpression(), ConsumeStatementLeadingTrivia());
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
                AdvanceTrivia(_midExpressionTrivia);

                // `[a, b]` is an array literal; `[]T` is a slice type.
                if (!Check(TokenKind.RightBracket))
                {
                    var elements = ParseCommaSeparatedExpressionCollection(TokenKind.RightBracket);
                    AdvanceTrivia(_midExpressionTrivia);
                    var close = ExpectNode(TokenKind.RightBracket, "close bracket (']')");
                    return new ArrayLiteralExpressionNode(leftBracket, [..elements], close);
                }

                var rightBracket = ExpectNode(TokenKind.RightBracket, "Right bracket (']')");
                var type = ParsePrefix();
                return new SliceTypeModifierNode(leftBracket, rightBracket, type);
            }

            // `!` and `?` are also the types `Failable(T)` and `Nullable(T)`: the binder tells them apart by the operand.
            case TokenKind.Bang
                or TokenKind.QuestionMark
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
                var leftBracket = ExpectNode(TokenKind.LeftBracket, "open bracket ('[')");
                AdvanceTrivia(_midExpressionTrivia);
                var args = ParseCommaSeparatedExpressionCollection(TokenKind.RightBracket);
                AdvanceTrivia(_midExpressionTrivia);
                var rightBracket = ExpectNode(TokenKind.RightBracket, "close bracket (']')");
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
        if (CheckKeyword("let") || CheckKeyword("const"))
        {
            TokenNode letKeyword;
            ExpressionNode? firstExpression;
            IdentifierExpressionNode? secondExpression = null;
        
            letKeyword = CheckKeyword("const") ? ExpectNode("const", "'const' keyword") : ExpectNode("let", "'let' keyword");
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
            if (PeekKind() == TokenKind.RightParenthesis || alsoBreaksAt.Contains(PeekKind())) break;
            
            var exp = ParseExpression();
            AdvanceTrivia(_midExpressionTrivia);
            TokenNode? comma = null;
            
            if (PeekKind() == TokenKind.Comma)
            {
                comma = AdvanceNode();
                AdvanceTrivia(_midExpressionTrivia);
            }
            
            arguments.Add((exp, comma));
            if (alsoBreaksAt.Contains(PeekKind())) break;
        }

        return arguments;
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

    private TriviaNode[] ConsumeStatementLeadingTrivia()
    {
        AdvanceTrivia(TokenKind.Whitespace);
        if (Check(TokenKind.Semicolon) || Check(TokenKind.LineFeed)) AdvanceTrivia();
        else if (!Check(TokenKind.RightBrace)) Expect(TokenKind.LineFeed, "Line feed after statement");
        
        return _trivia.Dump();
    }
    private TriviaNode[] ConsumeDeclarationsLeadingTrivia()
    {
        AdvanceTrivia(TokenKind.Whitespace);
        if (Check(TokenKind.Semicolon) || Check(TokenKind.LineFeed)) AdvanceTrivia();
        
        return _trivia.Dump();
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
    
    private ParsingError Error(TokenValue tok, string message) => new (message,
        new SourceLocation(_path, tok.Line, tok.Column, tok.Line, tok.Column + tok.value.Length));
    
    #endregion
    
    [Flags]
    private enum ScopeAllowedDeclarations
    {
        Import = 1 << 0,
        Struct = 1 << 1,
        Typedef = 1 << 2,
        Field = 1 << 3,
        Case = 1 << 4,
        Attributes = 1 << 5,
        Constructor = 1 << 6,
        Destructor = 1 << 7,
    }

    private class ParsingError(string message, SourceLocation location) : CompilationError(message, location);
}
