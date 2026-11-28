using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Tqc.Backend.Cecil;

/// <summary>
/// In .NET only the result can be on the stack when a method returns. A `try` in the middle of an expression
/// (`add(x, try f())`) returns while there are values waiting there, so they are popped first. How many there are is
/// read from the code, once all of it is emitted.
/// </summary>
internal sealed partial class BodyEmitter
{
    private void DiscardBeforeEarlyReturns()
    {
        if (_earlyReturns.Count == 0) return;

        var depths = StackDepths();
        foreach (var start in _earlyReturns)
        {
            // Nothing is there for a code that is never reached.
            if (!depths.TryGetValue(start, out var depth)) continue;
            for (var i = 0; i < depth; i++) _il.InsertAfter(start, _il.Create(OpCodes.Pop));
        }
    }

    /// <summary>A `try` of .NET starts with nothing on the stack.</summary>
    private void CheckProtectedBlocks()
    {
        if (_method.Body.ExceptionHandlers.Count == 0) return;

        var depths = StackDepths();
        if (_method.Body.ExceptionHandlers.Any(handler => depths.GetValueOrDefault(handler.TryStart) != 0))
            Report("A 'for' that disposes its enumerator cannot be in the middle of an expression in .NET", _function.SourceLocation);
    }

    /// <summary>The number of values on the stack before each instruction that can be reached.</summary>
    private Dictionary<Instruction, int> StackDepths()
    {
        var depths = new Dictionary<Instruction, int>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<(Instruction Instruction, int Depth)>();
        if (_method.Body.Instructions.Count > 0) pending.Push((_method.Body.Instructions[0], 0));

        // No branch goes to a `finally`: .NET runs it, with nothing on the stack.
        foreach (var handler in _method.Body.ExceptionHandlers) pending.Push((handler.HandlerStart, 0));

        while (pending.TryPop(out var next))
        {
            var (instruction, depth) = next;

            // Every path that reaches an instruction does it with the same stack, so one visit is enough.
            if (!depths.TryAdd(instruction, depth)) continue;

            // `leave` empties the stack, whatever is on it.
            var after = instruction.OpCode.Code is Code.Leave or Code.Leave_S
                ? 0
                : depth - Pops(instruction) + Pushes(instruction);
            var flow = instruction.OpCode.FlowControl;

            if (instruction.Operand is Instruction target) pending.Push((target, after));
            if (instruction.Operand is Instruction[] targets)
                foreach (var one in targets) pending.Push((one, after));

            if (flow is not (FlowControl.Branch or FlowControl.Return or FlowControl.Throw) && instruction.Next != null)
                pending.Push((instruction.Next, after));
        }

        return depths;
    }

    private static int Pops(Instruction instruction) => instruction.OpCode.StackBehaviourPop switch
    {
        StackBehaviour.Pop0 or StackBehaviour.PopAll => 0,
        StackBehaviour.Varpop => instruction.Operand is IMethodSignature called
            // `newobj` makes the instance it calls the constructor of; `ret` ends the walk, whatever it takes.
            ? called.Parameters.Count + (called.HasThis && instruction.OpCode.Code != Code.Newobj ? 1 : 0)
              + (instruction.OpCode.Code == Code.Calli ? 1 : 0)
            : 0,
        // The name says what is taken, one part for each value: `Popref_popi_popi`.
        var behaviour => behaviour.ToString().Split('_').Length,
    };

    private static int Pushes(Instruction instruction) => instruction.OpCode.StackBehaviourPush switch
    {
        StackBehaviour.Push0 => 0,
        StackBehaviour.Push1_push1 => 2,
        StackBehaviour.Varpush => instruction.Operand is IMethodSignature called && called.ReturnType.FullName != "System.Void" ? 1 : 0,
        _ => 1,
    };
}
