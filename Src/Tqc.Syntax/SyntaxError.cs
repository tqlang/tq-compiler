using Tqc.Core;
using Tqc.Diagnostics;

namespace Tqc.Syntax;

/// <summary>The source code cannot be read: found by the lexer or the parser.</summary>
public class SyntaxError(string message, SourceLocation location) : CompilationError(message, location);
