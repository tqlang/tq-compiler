using Tqc.Core;
using Tqc.Diagnostics;

namespace Tqc.Semantics;

internal sealed class TypeError(string message, SourceLocation? location) : CompilationError(message, location);
