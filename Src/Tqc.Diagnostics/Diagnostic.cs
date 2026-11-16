using Tqc.Core;

namespace Tqc.Diagnostics;

public abstract class Diagnostic : Exception
{
    protected Diagnostic() { }
    protected Diagnostic(string message) : base(message) { }
}
public abstract class CompilationError : Diagnostic
{
    public SourceLocation? Location { get; }

    protected CompilationError(string message, SourceLocation? location) : base(message)
    {
        Location = location;
    }
}
