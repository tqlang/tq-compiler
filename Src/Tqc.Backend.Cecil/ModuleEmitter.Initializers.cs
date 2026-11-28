using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Tqc.Backend.Cecil;

/// <summary>
/// `let a = b + 1` may be written before `b`. Inside a type initializer, each static field is set after the ones its value
/// reads, directly or through the functions it calls; the others keep the order they are written in. A value that reads
/// its own field, directly or through other fields and calls, in one type or across several, is an error. (Between types,
/// .NET runs each initializer when the type is first used.)
/// </summary>
internal sealed partial class ModuleEmitter
{
    private readonly Dictionary<MethodDefinition, List<(FieldDefinition Field, Instruction First, Instruction Last)>> _initializerParts = [];

    /// <summary>Called once every body is emitted: what a value reads is only known from the code of the functions.</summary>
    private void OrderStaticInitializers()
    {
        ReportCircularInitializers();

        foreach (var (initializer, parts) in _initializerParts)
        {
            if (parts.Count < 2) continue;

            var body = initializer.Body.Instructions;
            var code = parts.Select(part => CodeOf(body, part.First, part.Last)).ToList();
            var fields = parts.Select(part => part.Field).ToList();

            // What the value reads when it runs, and what it may read later through a function it only takes the address of.
            var needs = code.Select(instructions => FieldsRead(instructions, fields, false)).ToList();
            var mayNeed = code.Select(instructions => FieldsRead(instructions, fields, true)).ToList();

            var order = new List<int>();
            var visited = new HashSet<int>();

            void Visit(int index)
            {
                if (!visited.Add(index)) return;
                foreach (var needed in needs[index]) Visit(needed);

                // A possible read never goes against a certain one.
                foreach (var needed in mayNeed[index])
                    if (!Reaches(needs, needed, index))
                        Visit(needed);

                order.Add(index);
            }

            for (var i = 0; i < parts.Count; i++) Visit(i);

            var end = body[^1];
            body.Clear();
            foreach (var instruction in order.SelectMany(index => code[index])) body.Add(instruction);
            body.Add(end);
        }
    }

    private static List<Instruction> CodeOf(IList<Instruction> body, Instruction first, Instruction last)
    {
        var start = body.IndexOf(first);
        return body.Skip(start).Take(body.IndexOf(last) - start + 1).ToList();
    }

    private static bool Reaches(List<List<int>> needs, int from, int to)
    {
        var seen = new HashSet<int>();
        var pending = new Stack<int>([from]);

        while (pending.TryPop(out var index))
        {
            if (index == to) return true;
            if (!seen.Add(index)) continue;
            foreach (var needed in needs[index]) pending.Push(needed);
        }

        return false;
    }

    /// <summary>Reports the values that need their own field to be set first: no order can give it to them.</summary>
    private void ReportCircularInitializers()
    {
        var parts = _initializerParts
            .SelectMany(entry => entry.Value.Select(part => (part.Field, Code: CodeOf(entry.Key.Body.Instructions, part.First, part.Last))))
            .ToList();
        var fields = parts.Select(part => part.Field).ToList();
        var needs = parts.Select(part => FieldsRead(part.Code, fields, false)).ToList();
        var sources = _staticInitializers.Values.SelectMany(pending => pending)
            .ToDictionary(item => item.Definition, item => item.Field);

        var done = new HashSet<int>();
        var path = new List<int>();

        void Visit(int index)
        {
            var start = path.IndexOf(index);
            if (start >= 0)
            {
                var cycle = path.Skip(start).Append(index).Select(i => $"'{sources[fields[i]].Name}'");
                Report($"Circular dependency between initial values: {string.Join(" needs ", cycle)}",
                    sources[fields[index]].SourceLocation);
                return;
            }

            if (done.Contains(index)) return;

            path.Add(index);
            foreach (var needed in needs[index]) Visit(needed);
            path.RemoveAt(path.Count - 1);
            done.Add(index);
        }

        for (var i = 0; i < parts.Count; i++) Visit(i);
    }

    /// <summary>
    /// Which of `fields` the code reads, in itself or in the methods of this module it calls and, with
    /// `throughPointers`, in the ones it only takes the address of.
    /// </summary>
    private List<int> FieldsRead(IEnumerable<Instruction> code, List<FieldDefinition> fields, bool throughPointers)
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

                    case MethodReference called when throughPointers
                                                     || instruction.OpCode.Code is Code.Call or Code.Callvirt or Code.Newobj:
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
