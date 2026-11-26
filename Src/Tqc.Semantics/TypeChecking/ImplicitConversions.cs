using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;
using Tqc.IR.Targets;

namespace Tqc.Semantics;

/// <summary>
/// The conversions registered with `@implicitConvert`: a function that takes a value of one type
/// and returns another. A conversion is only used from code that can reach the function.
/// </summary>
internal sealed class ImplicitConversions
{
    /// <summary>The sizes the target gives to the types that depend on it.</summary>
    public TargetAbi Abi { get; set; } = TargetAbi.Unknown;

    private readonly List<(ITypeRef From, ITypeRef To, TqFunction Function)> _entries = [];

    public static ImplicitConversions Build(IEnumerable<INamespace> roots, TargetAbi abi)
    {
        var conversions = new ImplicitConversions { Abi = abi };
        foreach (var root in roots) conversions.Scan(root);
        return conversions;
    }

    /// <summary>The function that converts `from` into `to` for this code, if there is one.</summary>
    public TqFunction? Find(ITypeRef from, ITypeRef to, AccessContext accessor) =>
        _entries.FirstOrDefault(entry => entry.From.Equals(from)
                                         && entry.To.Equals(to)
                                         && AccessChecker.CanAccess(entry.Function, accessor)).Function;

    // Only Tq code has attributes; the namespaces of .NET modules can be huge and have none.
    private void Scan(INamespace namespaceNode)
    {
        if (namespaceNode is not (TqNamespace or PrimitiveNamespace)) return;

        foreach (var member in namespaceNode.GetAllMembers()) ScanMember(member);
    }

    private void ScanMember(IMember member)
    {
        switch (member)
        {
            case INamespace child:
                Scan(child);
                break;

            case TqFunctionGroup group:
                foreach (var function in group) Add(function);
                break;

            case TqFunction function:
                Add(function);
                break;

            // A conversion can also be declared inside the type it converts from or to.
            case TqStruct structure:
                foreach (var inner in structure.Members.Values) ScanMember(inner);
                break;

            case TqTypedef typedef:
                foreach (var inner in typedef.Members.Values) ScanMember(inner);
                break;
        }
    }

    private void Add(TqFunction function)
    {
        var registered = function.Attributes.OfType<TqBuiltinAttribute>()
            .Any(attribute => attribute.Kind == BuiltinAttribute.ImplicitConvert);

        // A function of the wrong shape was already reported when it was lowered.
        if (!registered || function.Parameters.Length != 1 || function.ReturnType == null) return;

        var from = TypeChecker.TypeFromExpression(function.Parameters[0].Type);
        var to = TypeChecker.TypeFromExpression(function.ReturnType);
        if (from != null && to != null) _entries.Add((from, to, function));
    }
}
