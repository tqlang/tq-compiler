using Tqc.Core;
using Tqc.Syntax;

namespace Tqc.IR.ProgramMembers.Tq;

public record TqImportMember(
    SourceLocation Location,
    string[] NamespaceReference,
    Dictionary<string, (SourceLocation, string)>? Imports
);
