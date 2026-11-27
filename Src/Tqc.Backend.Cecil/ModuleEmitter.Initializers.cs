using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Tqc.Backend.Cecil;

/// <summary>
/// `let a = b + 1` may be written before `b`. Inside a type initializer, each static field is set after the ones its value
/// reads, directly or through the functions it calls; the others, and the ones that read each other, keep the order they
/// are written in. (Between types, .NET runs each initializer when the type is first used.)
/// </summary>
internal sealed partial class ModuleEmitter
{
    private readonly Dictionary<MethodDefinition, List<(FieldDefinition Field, Instruction First, Instruction Last)>> _initializerParts = [];

    /// <summary>Called once every body is emitted: what a value reads is only known from the code of the functions.</summary>
    private void OrderStaticInitializers()
    {
        foreach (var (initializer, parts) in _initializerParts)
        {
            if (parts.Count < 2) continue;

            var body = initializer.Body.Instructions;
            var code = parts
                .Select(part => body.Skip(body.IndexOf(part.First)).Take(body.IndexOf(part.Last) - body.IndexOf(part.First) + 1).ToList())
                .ToList();
            var fields = parts.Select(part => part.Field).ToList();
            var needs = code.Select(instructions => FieldsRead(instructions, fields)).ToList();

            var order = new List<int>();
            var visited = new HashSet<int>();

            void Visit(int index)
            {
                if (!visited.Add(index)) return;
                foreach (var needed in needs[index]) Visit(needed);
                order.Add(index);
            }

            for (var i = 0; i < parts.Count; i++) Visit(i);

            var end = body[^1];
            body.Clear();
            foreach (var instruction in order.SelectMany(index => code[index])) body.Add(instruction);
            body.Add(end);
        }
    }

    /// <summary>Which of `fields` the code reads, in itself or in the methods of this module it reaches.</summary>
    private List<int> FieldsRead(IEnumerable<Instruction> code, List<FieldDefinition> fields)
    {
        var read = new SortedSet<int>();
        var seen = new HashSet<MethodDefinition>();
        var pending = new Stack<IEnumerable<Instruction>>([code]);

        while (pending.TryPop(out var instructions))
        {
            foreach (var instruction in instructions)
            {
                switch (instruction.Operand)
                {
                    case FieldReference reference when instruction.OpCode.Code is Code.Ldsfld or Code.Ldsflda:
                        var index = fields.FindIndex(field => field.Name == reference.Name
                                                              && field.DeclaringType.FullName == reference.DeclaringType.GetElementType().FullName);
                        if (index >= 0) read.Add(index);
                        break;

                    case MethodReference called:
                        var element = called.GetElementMethod();
                        var owner = element.DeclaringType.GetElementType();
                        var method = element as MethodDefinition
                                     ?? Module.GetType(owner.FullName)?.Methods.FirstOrDefault(candidate => candidate.FullName == element.FullName);
                        if (method is { HasBody: true } && method.Module == Module && seen.Add(method))
                            pending.Push(method.Body.Instructions);
                        break;
                }
            }
        }

        return [..read];
    }
}
