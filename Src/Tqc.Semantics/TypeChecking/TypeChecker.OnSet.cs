using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;

namespace Tqc.Semantics;

/// <summary>
/// `@onset` makes a type observable: `a = v`, where the type of `a` has a function marked `@onset` that takes `v`,
/// is `a.f(v)`. The function is the one that decides what the assignment does (usually it stores the value).
/// </summary>
internal sealed partial class TypeChecker
{
    /// <summary>
    /// The call that replaces the assignment; null when the type has no `@onset` function that takes the value
    /// (the assignment is then an ordinary one, e.g. a value of the type itself), or when this is that function.
    /// </summary>
    private (IrExpression, ITypeRef?)? TryOnSet(IrAssign assign, IrExpression target, IrExpression source, ITypeRef? targetType)
    {
        if (targetType == null || TypeOf(source) == null) return null;
        var container = ContainerOf(targetType);
        if (container is not (TqStruct or TqTypedef)) return null;

        var functions = OnSetFunctions(container);
        if (functions.Count == 0) return null;

        // Inside the function itself the assignment must be able to store the value.
        if (_function != null && functions.Contains(_function)) return null;

        IrExpression[] arguments = [source];
        var applicable = functions
            .Select(ToOverload)
            .Where(overload => overload.Accepts(1) && ConversionRules.Classify(
                TypeOf(source), source, overload.ParameterTypes[0], conversions, _accessor).Kind != MatchKind.None)
            .ToList();
        if (applicable.Count == 0) return null;

        var winner = ChooseOverload($"onset of {TypeFormatter.Format(targetType)}", "overload", applicable, arguments, assign.Location);
        if (winner == null) return (assign with { Target = target, Source = source }, targetType);

        var callee = new IrAccess(assign.Location, target, new IrReference(assign.Location, new FunctionTypeReference(winner.Overload.Function)));
        var call = new IrCall(assign.Location, callee, new IrArgumentsList(assign.Location, ConvertArguments(arguments, winner)));
        return (call, ReturnTypeOf(callee));
    }

    /// <summary>The functions of the container, or of what it extends, marked `@onset` that this code can reach.</summary>
    private List<TqFunction> OnSetFunctions(IMemberContainer container)
    {
        List<TqFunction> found = [];
        HashSet<IMemberContainer> visited = [];

        for (var current = container; current != null && visited.Add(current); current = current.Base)
        {
            foreach (var member in current.OwnMembers)
            {
                IEnumerable<TqFunction> functions = member switch
                {
                    TqFunctionGroup group => group,
                    TqFunction function => [function],
                    _ => [],
                };

                found.AddRange(functions.Where(function =>
                    function.Attributes.OfType<TqBuiltinAttribute>().Any(attribute => attribute.Kind == BuiltinAttribute.OnSet)
                    && AccessChecker.CanAccess(function, _accessor)));
            }
        }

        return found;
    }
}
