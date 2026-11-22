using Tqc.IR.Nodes;
using Tqc.IR.References;

namespace Tqc.IR.ProgramMembers.Tq;

/// <summary>What a type extends and implements, found the same way by the checks, the conversions and the backend.</summary>
public static class Ancestry
{
    /// <summary>The protocol (of Tq or an interface of .NET) a bound expression names; null if it names none that is known.</summary>
    public static IMemberContainer? ProtocolOf(IrExpression expression) => expression switch
    {
        IrReference { Reference: TypeMetaReference { Type: IMemberContainer container } } => container,

        // `Protocol(i32)`: its members are the ones of the generic protocol.
        IrReference { Reference: TypeMetaReference { Type: GenericInstance { Definition: IMemberContainer container } } } => container,
        IrReference { Reference: ProtocolTypeReference { Protocol: IMemberContainer container } } => container,
        _ => null,
    };

    /// <summary>
    /// What `derived` extends and implements, nearest first, with the steps it takes to get there. The struct a struct by value
    /// extends is not what it is: it keeps it in a field, so its protocols are not the ones of the struct (`throughBases` is false).
    /// A protocol always goes on to the protocols it extends.
    /// </summary>
    public static List<(IMemberContainer Container, int Steps)> Reachable(IMemberContainer derived, bool throughBases)
    {
        List<(IMemberContainer, int)> found = [];
        HashSet<IMemberContainer> visited = [derived];
        Queue<(IMemberContainer, int)> pending = [];
        pending.Enqueue((derived, 0));

        while (pending.Count > 0)
        {
            var (current, steps) = pending.Dequeue();

            List<IMemberContainer> next = [];
            if (current.Base is { } @base && (throughBases || current is IObjectType { IsInterface: true }))
                next.Add(@base);
            if (current is TqStruct structure) next.AddRange(structure.Protocols);

            foreach (var child in next)
            {
                if (!visited.Add(child)) continue;
                found.Add((child, steps + 1));
                pending.Enqueue((child, steps + 1));
            }
        }

        return found;
    }
}
