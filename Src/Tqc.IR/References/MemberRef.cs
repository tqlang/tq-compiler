using Tqc.IR.ProgramMembers;

namespace Tqc.IR.References;

public abstract record MemberRef(IMember Member) : Ref;
