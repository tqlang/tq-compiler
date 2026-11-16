using Tqc.Core;

namespace Tqc.IR.ProgramMembers.Tq;

/// <summary>`constructor(...) { }` of a struct. It has the shape of a function.</summary>
public sealed class TqConstructor(SourceLocation location) : TqFunction("constructor", location);

/// <summary>`destructor() { }` of a struct.</summary>
public sealed class TqDestructor(SourceLocation location) : TqFunction("destructor", location);
