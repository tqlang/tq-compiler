using System.Diagnostics.CodeAnalysis;
using Tq.Ast;
using Tq.Core;
using Tq.Core.Language;
using Tq.Diagnostics;

namespace Tq.CodeProcess.Parser;

public partial class Parser 
{
    [DoesNotReturn]
    private static void ThrowUnexpectedTokenError(ErrorHandler errH, string source, string[] expected, Token token) => errH.Throw(
        new UnexpectedTokenError(
            $"Expected {string.Join(", ", expected)}. Found '{token.ValueAsString()}'",
            new SourceLocation(source, token.LineStart, token.ColStart, token.LineStart, token.ColStart + token.Length)
        )
    );

    [DoesNotReturn]
    private static void ThrowUnexpectedTokenError(ErrorHandler errH, string source, TokenType expected, Token token) => errH.Throw(
        new UnexpectedTokenError(
            $"Expected ({expected}). Found '{token.ValueAsString()}'",
            new SourceLocation(source, token.LineStart, token.ColStart, token.LineStart, token.ColStart + token.Length)
        )
    );
}