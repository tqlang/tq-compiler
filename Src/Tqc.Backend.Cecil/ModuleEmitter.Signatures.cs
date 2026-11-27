using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Tqc.Dotnet.ProgramMembers;
using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;

namespace Tqc.Backend.Cecil;

/// <summary>
/// Tq tells functions apart by more than .NET (or C#) does, so some of them change their signature when emitted:
///
/// - Parameters of different types of Tq can be the same type of .NET (`int` and `i32`). One function keeps the name (the
///   one with the sized type, `i32`) and the others get the types of their parameters in it: `f__int`.
/// - Overloads that only differ in what they return cannot be called from C#. Each one that returns a value gives it
///   through a last parameter `out`: `i32 f(x)` and `string f(x)` are `void f(x, out int)` and `void f(x, out string)`.
///   A reference (`ref S`) cannot be given through `out`; it stays what is returned.
/// - What is still the same signature after that (`f() int` and `f() i32`) gets the return type in the name.
///
/// What overrides or implements a function takes the name and the form of that one, since .NET matches them by signature.
/// The code of Tq calls the methods themselves, so only who reads the assembly sees the difference.
/// </summary>
internal sealed partial class ModuleEmitter
{
    /// <summary>A function being given its signature.</summary>
    private sealed class Shaped
    {
        public required TqFunction Function { get; init; }
        public required MethodDefinition Method { get; init; }
        public required FunctionShape Natural { get; init; }

        /// <summary>The name and the form come from what is overridden or implemented, and do not change.</summary>
        public bool Fixed { get; set; }

        public string Name { get; set; } = "";
        public bool Out { get; set; }
    }

    private void ShapeFunctions()
    {
        var done = new HashSet<Container>(ReferenceEqualityComparer.Instance);

        // What is extended or implemented comes first: those below take its signatures.
        void Visit(Container container)
        {
            if (!done.Add(container)) return;

            if (container.Struct is { } structure)
            {
                foreach (var (ancestor, _) in Ancestry.Reachable(structure, throughBases: true))
                {
                    var above = _containers.FirstOrDefault(candidate => ReferenceEquals(candidate.Struct, ancestor));
                    if (above != null) Visit(above);
                }
            }

            ShapeFunctions(container);
        }

        foreach (var container in _containers) Visit(container);
        foreach (var container in _containers) BridgeInterfaces(container);
    }

    private static IEnumerable<TqFunction> FunctionsOf(IEnumerable<IMember> members)
    {
        foreach (var member in Ordered(members))
        {
            switch (member)
            {
                case TqFunctionGroup group:
                    foreach (var function in group.Functions) yield return function;
                    break;

                case TqFunction function:
                    yield return function;
                    break;
            }
        }
    }

    private void ShapeFunctions(Container container)
    {
        var own = new List<Shaped>();
        foreach (var function in FunctionsOf(container.Members))
        {
            // Constructors, destructors and accessors have the signature .NET wants; so does the `main` of the program.
            if (function is TqConstructor or TqDestructor) continue;
            if (!_backend.Definitions.TryGetValue(function, out var definition) || definition is not MethodDefinition { IsSpecialName: false } method) continue;
            if (container.Struct == null && container.Namespace.Length == 0 && function.Name == "main") continue;

            own.Add(new Shaped { Function = function, Method = method, Natural = NaturalShape(function, method), Name = method.Name });
        }

        if (own.Count == 0) return;

        var inherited = InheritedShapes(container.Struct);

        // 1. What overrides or implements.
        foreach (var shaped in own)
        {
            var natural = shaped.Natural;
            var same = inherited.FirstOrDefault(above => above.Name == natural.Name && above.Tokens == natural.Tokens
                                                                                  && above.ReturnToken == natural.ReturnToken);

            // A function of a generic protocol says `T` where the one that implements it says the type.
            if (same == null && Has(shaped.Function.Attributes, BuiltinAttribute.Override))
                same = inherited.FirstOrDefault(above => above.Name == natural.Name && Matches(above, natural));

            if (same == null) continue;

            shaped.Fixed = true;
            shaped.Name = same.EmittedName;
            shaped.Out = same.Out;
        }

        var free = own.Where(shaped => !shaped.Fixed).ToList();

        // 2. Parameters that are different in Tq and the same in .NET: the name tells them apart.
        foreach (var group in free.GroupBy(shaped => shaped.Natural.ParameterKey))
        {
            var above = inherited.FirstOrDefault(candidate => candidate.ParameterKey == group.Key);
            var kept = above?.Tokens ?? group.OrderBy(shaped => shaped.Natural.Weight).First().Natural.Tokens;

            foreach (var shaped in group.Where(shaped => shaped.Natural.Tokens != kept))
                shaped.Name = $"{shaped.Natural.Name}__{shaped.Natural.Tokens}";
        }

        // 3. Overloads by return: the value goes through `out`.
        foreach (var group in free.GroupBy(shaped => (shaped.Name, shaped.Natural.ParameterKey)))
        {
            var alsoAbove = inherited.Any(above => above.EmittedName == group.Key.Name && above.ParameterKey == group.Key.ParameterKey);
            var alsoFixed = own.Any(shaped => shaped.Fixed && shaped.Name == group.Key.Name && shaped.Natural.ParameterKey == group.Key.ParameterKey);
            if (group.Count() == 1 && !alsoAbove && !alsoFixed) continue;

            foreach (var shaped in group)
                shaped.Out = shaped.Method.ReturnType.FullName != "System.Void" && !shaped.Method.ReturnType.IsByReference;
        }

        // 4. What is still the same signature.
        var taken = new HashSet<string>(own.Where(shaped => shaped.Fixed).Select(SignatureKey));
        taken.UnionWith(inherited.Select(above => above.SignatureKey));

        // The sized types keep the name: `i32` before `int`. The sort is stable, so the first declared wins a tie.
        foreach (var shaped in free.OrderBy(shaped => shaped.Natural.ReturnWeight + shaped.Natural.Weight))
        {
            var name = shaped.Name;
            for (var attempt = 1; !taken.Add(SignatureKey(shaped)); attempt++)
                shaped.Name = $"{name}__{shaped.Natural.ReturnToken}{(attempt > 1 ? attempt : "")}";
        }

        foreach (var shaped in own) Apply(shaped);
    }

    /// <summary>What .NET tells methods apart by, apart from what they return.</summary>
    private static string SignatureKey(Shaped shaped) =>
        FunctionShape.Key(shaped.Name, shaped.Natural.ParameterKey, shaped.Out ? TypeKey(shaped.Method.ReturnType) : null);

    private void Apply(Shaped shaped)
    {
        var method = shaped.Method;
        var natural = shaped.Natural;
        natural.EmittedName = shaped.Name;
        natural.Out = shaped.Out;
        natural.SignatureKey = SignatureKey(shaped);
        _backend.Shapes[shaped.Function] = natural;

        method.Name = shaped.Name;
        if (!shaped.Out) return;

        var result = method.ReturnType;
        method.Parameters.Add(new ParameterDefinition(
            FreeName(method, "result"), ParameterAttributes.Out, new ByReferenceType(result)));
        method.ReturnType = Core("System.Void");
        _backend.OutResults[method] = result;
    }

    private static string FreeName(MethodDefinition method, string name)
    {
        while (method.Parameters.Any(parameter => parameter.Name == name)) name += "_";
        return name;
    }

    /// <summary>The signature of a function before any change, in the terms of Tq and of .NET.</summary>
    private FunctionShape NaturalShape(TqFunction function, MethodDefinition method)
    {
        var tokens = new List<string>();
        var weight = 0;
        foreach (var parameter in function.Parameters ?? [])
        {
            if (!GenericParameterTypeRef.IsTypeParameter(parameter))
                tokens.Add(TypeToken(TypeExpressions.Raw(parameter.Type), function, ref weight));
        }

        var returnWeight = 0;
        var returnToken = function.ReturnType == null ? "void" : TypeToken(TypeExpressions.Raw(function.ReturnType), function, ref returnWeight);

        return new FunctionShape
        {
            Name = method.Name,
            Tokens = string.Join("_", tokens),
            ReturnToken = returnToken,
            Weight = weight,
            ReturnWeight = returnWeight,
            GenericCount = method.GenericParameters.Count,
            Parameters = [..method.Parameters.Select(parameter => TypeKey(parameter.ParameterType))],
            Return = TypeKey(method.ReturnType),
            EmittedName = method.Name,
        };
    }

    /// <summary>
    /// A type of Tq as a part of a name. `weight` counts what .NET does not see (`int`, which is `i32` there; `?` of a
    /// reference): the function with the least of it keeps the name.
    /// </summary>
    private static string TypeToken(ITypeRef? type, TqFunction function, ref int weight)
    {
        var std = CompilerStd.Types;

        switch (type)
        {
            case SliceTypeRef slice: return "slice_" + TypeToken(slice.Element, function, ref weight);
            case ArrayTypeRef array: return $"array{array.Length}_" + TypeToken(array.Element, function, ref weight);
            case GenericParameterTypeRef generic:
                var position = Array.IndexOf(function.Parameters ?? [], generic.Parameter);
                return position >= 0 ? $"T{position}" : Identifier(generic.Parameter.Name);
            case TypedefTypeReference typedef: return Identifier(typedef.Typedef.Name);
            case ProtocolTypeReference { Protocol: var protocol }: return Identifier(protocol.ToString());
            case StructTypeReference { Struct: GenericInstance instance }:
            {
                var definition = instance.Definition;
                var arguments = instance.Arguments;

                if (ReferenceEquals(definition, std.Integer) && arguments is [IrComptimeBool signed, IrComptimeInt bits])
                    return $"{(signed.Value ? "i" : "u")}{bits.BigInteger}";
                if (ReferenceEquals(definition, std.NaturalInteger) && arguments is [IrComptimeBool natural])
                {
                    weight++;
                    return natural.Value ? "int" : "uint";
                }
                if (ReferenceEquals(definition, std.NativeInteger) && arguments is [IrComptimeBool native])
                    return native.Value ? "iptr" : "uptr";
                if (ReferenceEquals(definition, std.Float) && arguments is [IrComptimeInt floatBits])
                    return $"f{floatBits.BigInteger}";

                var prefix = ReferenceEquals(definition, std.Const) ? "const"
                    : ReferenceEquals(definition, std.Ref) ? "ref"
                    : ReferenceEquals(definition, std.Nullable) ? "nullable"
                    : ReferenceEquals(definition, std.Failable) ? "failable"
                    : Identifier(definition.Name);
                if (prefix is "const" or "nullable") weight++;

                var parts = new List<string> { prefix };
                foreach (var argument in arguments)
                {
                    parts.Add(argument is IrComptimeInt number
                        ? number.BigInteger.ToString()
                        : TypeToken(TypeExpressions.Raw(argument), function, ref weight));
                }
                return string.Join("_", parts);
            }
            // The ones that are keywords are named as they are written.
            case StructTypeReference { Struct: var structure } when ReferenceEquals(structure, std.String): return "string";
            case StructTypeReference { Struct: var structure } when ReferenceEquals(structure, std.Bool): return "bool";
            case StructTypeReference { Struct: var structure } when ReferenceEquals(structure, std.Char): return "char";
            case StructTypeReference { Struct: var structure }: return Identifier(structure.Name);
            default: return "unknown";
        }
    }

    private static string Identifier(string? text)
    {
        var result = new StringBuilder();
        foreach (var character in text ?? "unknown")
            result.Append(char.IsLetterOrDigit(character) ? character : '_');
        return result.ToString();
    }

    /// <summary>A type of .NET as text to compare signatures: a generic parameter is told by its position.</summary>
    private static string TypeKey(TypeReference type) => type switch
    {
        GenericParameter generic => (generic.Type == GenericParameterType.Method ? "!!" : "!") + generic.Position,
        GenericInstanceType instance => $"{instance.ElementType.FullName}<{string.Join(",", instance.GenericArguments.Select(TypeKey))}>",
        ByReferenceType reference => TypeKey(reference.ElementType) + "&",
        PointerType pointer => TypeKey(pointer.ElementType) + "*",
        ArrayType array => TypeKey(array.ElementType) + "[]",
        _ => type.FullName,
    };

    /// <summary>The same parameters and result, where what is generic above matches anything.</summary>
    private static bool Matches(FunctionShape above, FunctionShape below) =>
        above.Parameters.Length == below.Parameters.Length
        && above.Parameters.Zip(below.Parameters).All(pair => pair.First.Contains('!') || pair.First == pair.Second)
        && (above.Return.Contains('!') || above.Return == below.Return);

    /// <summary>The functions of everything the struct extends or implements, of Tq and of .NET, nearest first.</summary>
    private List<FunctionShape> InheritedShapes(TqStruct? structure)
    {
        var found = new List<FunctionShape>();
        if (structure == null) return found;

        var visited = new HashSet<string>();
        foreach (var (ancestor, _) in Ancestry.Reachable(structure, throughBases: true))
        {
            switch (ancestor)
            {
                case TqStruct tq:
                    foreach (var function in FunctionsOf(tq.OwnMembers))
                    {
                        if (_backend.Shapes.TryGetValue(function, out var shape)
                            && _backend.Definitions.TryGetValue(function, out var definition) && definition is MethodDefinition { IsStatic: false })
                            found.Add(shape);
                    }
                    break;

                case IDotnetType dotnet:
                    AddDotnetShapes(dotnet.TypeReference, visited, found);
                    break;
            }
        }

        // Every struct has what `System.Object` has.
        if (_backend.Core.Find("System.Object") is { } @object) AddDotnetShapes(@object, visited, found);
        return found;
    }

    private static void AddDotnetShapes(TypeReference reference, HashSet<string> visited, List<FunctionShape> found)
    {
        if (!visited.Add(reference.FullName)) return;

        TypeDefinition? type;
        try { type = reference.Resolve(); }
        catch (AssemblyResolutionException) { return; }
        if (type == null) return;

        foreach (var method in type.Methods)
        {
            if (method.IsStatic || method.IsConstructor || !(method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly)) continue;

            var parameters = method.Parameters.Select(parameter => TypeKey(parameter.ParameterType)).ToArray();
            var shape = new FunctionShape
            {
                Name = method.Name,
                // Not a signature of Tq: only the types of .NET match it.
                Tokens = "?",
                ReturnToken = "?",
                GenericCount = method.GenericParameters.Count,
                Parameters = parameters,
                Return = TypeKey(method.ReturnType),
                EmittedName = method.Name,
                IsDotnet = true,
            };
            shape.SignatureKey = FunctionShape.Key(shape.Name, shape.ParameterKey, null);
            found.Add(shape);
        }

        if (type.BaseType != null) AddDotnetShapes(type.BaseType, visited, found);
        foreach (var implemented in type.Interfaces) AddDotnetShapes(implemented.InterfaceType, visited, found);
    }

    /// <summary>
    /// A method implements the one of an interface by having its name and signature. When it took another form (it already
    /// implements or overrides a different one, or comes from the struct above), a private method in the form of the
    /// interface calls it.
    /// </summary>
    private void BridgeInterfaces(Container container)
    {
        if (container.Struct is not { IsInterface: false } structure) return;

        var below = new List<IMemberContainer> { structure };
        below.AddRange(Ancestry.Reachable(structure, throughBases: IsClass(structure)).Select(ancestor => ancestor.Container));
        var implementations = below.OfType<TqStruct>()
            .Where(candidate => !candidate.IsInterface)
            .SelectMany(candidate => FunctionsOf(candidate.OwnMembers))
            .ToList();

        foreach (var protocol in below.OfType<TqStruct>().Where(candidate => candidate.IsInterface))
        {
            foreach (var function in FunctionsOf(protocol.OwnMembers))
            {
                if (!_backend.Shapes.TryGetValue(function, out var wanted)
                    || !_backend.Definitions.TryGetValue(function, out var definition)
                    || definition is not MethodDefinition { IsStatic: false, HasGenericParameters: false, DeclaringType.HasGenericParameters: false } declared)
                    continue;

                var found = implementations.FirstOrDefault(candidate => _backend.Shapes.TryGetValue(candidate, out var shape)
                    && shape.Name == wanted.Name && shape.Tokens == wanted.Tokens && shape.ReturnToken == wanted.ReturnToken);
                if (found == null) continue;

                var shapeFound = _backend.Shapes[found];
                if (shapeFound.EmittedName == wanted.EmittedName && shapeFound.Out == wanted.Out) continue;

                var target = (MethodDefinition)_backend.Definitions[found];
                if (target.IsStatic || target.DeclaringType.HasGenericParameters) continue;

                container.Type.Methods.Add(Bridge(declared, target));
            }
        }
    }

    private MethodDefinition Bridge(MethodDefinition declared, MethodDefinition target)
    {
        var bridge = new MethodDefinition($"{declared.DeclaringType.Name}.{declared.Name}__{target.Name}",
            MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.HideBySig,
            declared.ReturnType);
        foreach (var parameter in declared.Parameters)
            bridge.Parameters.Add(new ParameterDefinition(parameter.Name, parameter.Attributes, parameter.ParameterType));
        bridge.Overrides.Add(Use(declared));

        var wantsOut = _backend.OutResults.TryGetValue(declared, out var wantedResult);
        var givesOut = _backend.OutResults.TryGetValue(target, out var givenResult);
        if (wantsOut) _backend.OutResults[bridge] = wantedResult!;

        var il = bridge.Body.GetILProcessor();
        var values = bridge.Parameters.Count - (wantsOut ? 1 : 0);

        // The value of the target goes where the interface wants it: to its `out`, or to the result.
        if (wantsOut && !givesOut) il.Emit(OpCodes.Ldarg, bridge.Parameters[^1]);

        il.Emit(OpCodes.Ldarg_0);
        for (var i = 0; i < values; i++) il.Emit(OpCodes.Ldarg, bridge.Parameters[i]);

        VariableDefinition? temp = null;
        if (givesOut)
        {
            if (wantsOut)
            {
                il.Emit(OpCodes.Ldarg, bridge.Parameters[^1]);
            }
            else
            {
                temp = new VariableDefinition(givenResult);
                bridge.Body.Variables.Add(temp);
                bridge.Body.InitLocals = true;
                il.Emit(OpCodes.Ldloca, temp);
            }
        }

        il.Emit(target.DeclaringType.IsValueType ? OpCodes.Call : OpCodes.Callvirt, Use(target));
        if (temp != null) il.Emit(OpCodes.Ldloc, temp);
        if (wantsOut && !givesOut) il.Emit(OpCodes.Stobj, wantedResult);
        il.Emit(OpCodes.Ret);
        return bridge;
    }
}
