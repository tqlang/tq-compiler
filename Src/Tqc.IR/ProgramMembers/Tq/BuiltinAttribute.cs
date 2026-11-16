namespace Tqc.IR.ProgramMembers.Tq;

public enum BuiltinAttribute
{
    // Access
    Public, Internal, Private, AllowAccessTo, DenyAccessTo, Static, DefineGlobal,

    // Object orientation
    Class, Final, Abstract, Interface, ValueOnly, ReferenceOnly, Virtual, Override,

    // Functions
    Inline, NoInline, Comptime, Runtime, CallConv,

    // Overloading
    Getter, Setter, IndexerGet, IndexerSet, ImplicitConvert, ExplicitConvert, OverrideOperator, DefineAttribute,

    // Layout
    Packed, Off, Lay, Pad, Union,
}
