namespace Tqc.IR.ProgramMembers.Tq;

public enum Visibility
{
    /// <summary>Only the code of the same namespace (any file of it). The default.</summary>
    Private,

    /// <summary>Only the code of the same module.</summary>
    Internal,

    /// <summary>Any code.</summary>
    Public,
}
