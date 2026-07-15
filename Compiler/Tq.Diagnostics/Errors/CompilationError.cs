using Tq.Core.Language;

namespace Tq.Diagnostics;

public abstract class CompilationError(SourceLocation location) : Exception
{
    public readonly SourceLocation SourceLocation = location;
}
