using Tqc.Core;
using Tqc.Diagnostics;

namespace Tqc.Semantics;

internal sealed class BindingError(string message, SourceLocation? location) : CompilationError(message, location);
