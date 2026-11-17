using Tqc.Core;
using Tqc.Diagnostics;

namespace Tqc.Backend.Cecil;

/// <summary>Something the IR says that the .NET backend cannot emit (yet), or that the build does not provide.</summary>
public sealed class BackendError(string message, SourceLocation? location = null) : CompilationError(message, location);
