using Tqc.Core;

namespace Tqc.Diagnostics;

public abstract class Diagnostic : Exception
{
    protected Diagnostic() { }
    protected Diagnostic(string message) : base(message) { }
}
public abstract class CompilationError(string message, SourceLocation? location) : Diagnostic;
