using System.Diagnostics.CodeAnalysis;
using Tq.Ast;
using Tq.Core;
using Tq.Core.Language;
using Tq.Diagnostics;

namespace Tq.CodeProcess.Parser;

public partial class Parser 
{
    [DoesNotReturn]
    private static void ThrowUnexpectedTokenError(ErrorHandler errH, string source, string expected, Token token)
        => ThrowUnexpectedTokenError(errH, source, [expected], token);
    [DoesNotReturn]
    private static void ThrowUnexpectedTokenError(ErrorHandler errH, string source, string[] expected, Token token) => errH.Throw(
        new UnexpectedTokenError(
            new SourceLocation(source, token.LineStart, token.ColStart),
            $"Unexpected token found. Expected {string.Join(", ", expected)}, found {token.ValueAsString()}"
        )
    );
    
}