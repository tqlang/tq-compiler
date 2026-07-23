using Tq.Ast;
using Tq.Core.Language;

namespace Tq.Diagnostics;

public class UnexpectedTokenError(SourceLocation location, string message) : CompilationError(location)
{
    public readonly string Message = message;
}
