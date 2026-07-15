using System.Diagnostics;
using Tq.Core.Language;

namespace Tq.Diagnostics;

public abstract class CompilationWarning(SourceLocation location)
{
    public readonly SourceLocation SourceLocation = location;
    public readonly StackTrace StackTrace = new();
}
