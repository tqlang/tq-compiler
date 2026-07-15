using Tq.Ast;
using Tq.Core.Language;

namespace Tq.Diagnostics;

public class UnexpectedTokenError(SourceLocation location, TokenType found, TokenType expected) : CompilationError(location)
{
    public readonly TokenType Found = found;
    public readonly TokenType Expected = expected;
}
