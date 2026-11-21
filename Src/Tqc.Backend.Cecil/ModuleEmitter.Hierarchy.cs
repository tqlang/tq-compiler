using System.Runtime.CompilerServices;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.ProgramMembers.Tq;

namespace Tqc.Backend.Cecil;

/// <summary>
/// A struct by value can extend another one. .NET has no inheritance for value types, so it is built from what it has:
///
/// - A struct that extends another keeps it in a field, `__base`. By value, the code goes through the fields to reach
///   what it inherits (`d.__base.x`).
/// - `*S` of a struct of a hierarchy is a `Tq.Ref` (a `ref struct`, so it is never allocated): a reference to the part of
///   the struct that `S` is (`View`), and the `Tq.TypeInfo` of the struct that it really is. A reference to a derived struct
///   is a reference to the base one: it is the same struct with the view moved to the part that is the base (`ldflda __base`).
/// - Each struct has a static `__type`, its `Tq.TypeInfo`: the one of its base, the distance to the base inside it (the
///   layout of a struct is up to the runtime, so it is measured when the struct is first used) and a table with the
///   function pointers of its virtual functions.
/// - A function that is `@virtual`, `@abstract` or `@override` has a slot of that table. Called through a `*S` it is
///   `calli` on the pointer of the slot, which goes to a thunk that moves the reference from the part of the struct that
///   declares the function to the one that implements it, and calls it. Called on a value, or through `this`, it is a
///   call to the function itself (a value is exactly what it is; `this` does not know more).
/// - `x as *D` finds the distance from the struct that it really is to `D` and to the struct it was seen as, and moves the view.
/// </summary>
internal sealed partial class ModuleEmitter
{
    private readonly HashSet<TqStruct> _hierarchy = new(ReferenceEqualityComparer.Instance);

    /// <summary>A struct that is by value (not a class, not an interface).</summary>
    private static bool IsValueStruct(TqStruct structure) => !IsClass(structure) && !structure.IsInterface;

    /// <summary>The structs of this module that extend, or are extended by, another struct by value.</summary>
    private void FindHierarchy()
    {
        foreach (var container in _containers)
        {
            if (container.Struct is not { } structure || !IsValueStruct(structure)) continue;
            if (structure.BaseStruct is not TqStruct @base || !IsValueStruct(@base)) continue;

            _hierarchy.Add(structure);
            _hierarchy.Add(@base);
        }
    }

    private bool InHierarchy(TqStruct structure) => _hierarchy.Contains(structure);

    private TqStruct? BaseOf(TqStruct structure) =>
        InHierarchy(structure) && structure.BaseStruct is TqStruct @base && InHierarchy(@base) ? @base : null;

    /// <summary>The structs of the hierarchy are known to be one before any type that refers to `*S` of them is made.</summary>
    private void RegisterHierarchy()
    {
        foreach (var container in _containers)
        {
            if (container.Struct is { } structure && InHierarchy(structure))
                _backend.HierarchyTypes.Add(container.Type.FullName);
        }
    }

    /// <summary>`__base`, the struct that this one extends.</summary>
    private void DeclareBaseField(Container container)
    {
        if (container.Struct is not { } structure || BaseOf(structure) is not { } @base) return;

        var field = new FieldDefinition("__base", FieldAttributes.Assembly, Use((TypeDefinition)_backend.Definitions[@base]));
        container.Type.Fields.Add(field);
        _backend.BaseFields[container.Type.FullName] = field;
    }

    /// <summary>The functions of the struct that are called on an instance: what the interface lists.</summary>
    private IEnumerable<TqFunction> InstanceFunctions(Container container)
    {
        var all = new List<TqFunction>();
        foreach (var member in Ordered(container.Members))
        {
            switch (member)
            {
                case TqFunctionGroup group: all.AddRange(group.Functions); break;
                case TqFunction function: all.Add(function); break;
                case TqProperty property:
                    if (property.GetterFunction != null) all.Add(property.GetterFunction);
                    if (property.SetterFunction != null) all.Add(property.SetterFunction);
                    break;
            }
        }

        return all.Where(function => function is not TqConstructor
                                     && _backend.Definitions.TryGetValue(function, out var definition)
                                     && definition is MethodDefinition { IsStatic: false, HasGenericParameters: false });
    }

    /// <summary>
    /// The slots of the virtual functions, the thunks, and the static constructor of each struct, which makes its TypeInfo.
    /// It needs the functions of every struct of the hierarchy to exist.
    /// </summary>
    private void DeclareHierarchy()
    {
        var containers = _containers
            .Where(container => container.Struct != null && InHierarchy(container.Struct))
            .ToDictionary(container => container.Struct!, container => container, ReferenceComparer<TqStruct>.Instance);
        if (containers.Count == 0) return;

        var runtime = _backend.Runtime;
        var ordered = containers.Values.OrderBy(container => ChainOf(container.Struct!).Count).ToList();

        // The slots: the ones of the base come first; an override has the slot of what it overrides.
        var slotCounts = new Dictionary<TqStruct, int>(ReferenceComparer<TqStruct>.Instance);
        foreach (var container in ordered)
        {
            var structure = container.Struct!;
            var count = BaseOf(structure) is { } above ? slotCounts[above] : 0;

            foreach (var function in InstanceFunctions(container))
            {
                if (Has(function.Attributes, BuiltinAttribute.Override))
                {
                    var overridden = FindOverridden(function, structure, containers);
                    if (overridden == null)
                    {
                        Report($"'{function.Name}' is marked '@override', but no struct that '{structure.Name}' extends has a virtual function with the same name and parameters", function.SourceLocation);
                        continue;
                    }

                    _backend.Slots[function] = _backend.Slots[overridden];
                    _backend.SlotOwners[function] = _backend.SlotOwners[overridden];
                }
                else if (Has(function.Attributes, BuiltinAttribute.Virtual) || function.IsAbstract)
                {
                    _backend.Slots[function] = count++;
                    _backend.SlotOwners[function] = structure;
                }
            }

            slotCounts[structure] = count;
        }

        // `__type` and the distances to the structs above, in every struct, before anything refers to them.
        var offsets = new Dictionary<TqStruct, FieldDefinition[]>(ReferenceComparer<TqStruct>.Instance);
        foreach (var container in ordered)
        {
            var chain = ChainOf(container.Struct!);

            var typeInfo = new FieldDefinition("__type", FieldAttributes.Assembly | FieldAttributes.Static | FieldAttributes.InitOnly,
                _module.ImportReference(runtime.TypeInfo));
            container.Type.Fields.Add(typeInfo);
            _backend.TypeInfoFields[container.Type.FullName] = typeInfo;

            var fields = new FieldDefinition[chain.Count];
            for (var hops = 1; hops < chain.Count; hops++)
            {
                fields[hops] = new FieldDefinition($"__offset{hops}", FieldAttributes.Assembly | FieldAttributes.Static, Core("System.Int32"));
                container.Type.Fields.Add(fields[hops]);
            }

            offsets[container.Struct!] = fields;
        }

        // A thunk per function that has a slot: it takes the part of the struct that declares the function (as a `ref byte`)
        // and calls the function on the struct that implements it.
        var thunks = new Dictionary<TqFunction, MethodDefinition>(ReferenceEqualityComparer.Instance);
        foreach (var container in ordered)
        {
            var structure = container.Struct!;
            var chain = ChainOf(structure);

            foreach (var function in InstanceFunctions(container))
            {
                if (!_backend.Slots.ContainsKey(function)) continue;

                var method = (MethodDefinition)_backend.Definitions[function];
                var thunk = new MethodDefinition(method.Name + "$thunk",
                    MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig, method.ReturnType);
                thunk.Parameters.Add(new ParameterDefinition("this", ParameterAttributes.None, new ByReferenceType(Core("System.Byte"))));
                foreach (var parameter in method.Parameters)
                    thunk.Parameters.Add(new ParameterDefinition(parameter.Name, ParameterAttributes.None, parameter.ParameterType));
                container.Type.Methods.Add(thunk);

                var hops = chain.FindIndex(candidate => ReferenceEquals(candidate, _backend.SlotOwners[function]));
                var il = thunk.Body.GetILProcessor();
                il.Emit(OpCodes.Ldarg_0);
                if (hops > 0)
                {
                    // The part that declares the function is `hops` structs inside the one that implements it.
                    il.Emit(OpCodes.Ldsfld, offsets[structure][hops]);
                    il.Emit(OpCodes.Conv_I);
                    il.Emit(OpCodes.Sub);
                }

                for (var i = 1; i < thunk.Parameters.Count; i++) il.Emit(OpCodes.Ldarg, thunk.Parameters[i]);
                il.Emit(OpCodes.Call, Use(method));
                il.Emit(OpCodes.Ret);

                thunks[function] = thunk;
            }
        }

        foreach (var container in ordered)
        {
            var structure = container.Struct!;
            var chain = ChainOf(structure);
            var cctor = new MethodDefinition(".cctor",
                MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig
                | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, Core("System.Void"));
            container.Type.Methods.Add(cctor);

            var scratch = new VariableDefinition(Use(container.Type));
            cctor.Body.Variables.Add(scratch);
            var il = cctor.Body.GetILProcessor();

            // The distance from the start of the struct to each struct above it: the difference of the addresses.
            for (var hops = 1; hops < chain.Count; hops++)
            {
                il.Emit(OpCodes.Ldloca, scratch);
                for (var step = 0; step < hops; step++)
                    il.Emit(OpCodes.Ldflda, _backend.BaseFields[containers[chain[step]].Type.FullName]);
                il.Emit(OpCodes.Conv_U);
                il.Emit(OpCodes.Ldloca, scratch);
                il.Emit(OpCodes.Conv_U);
                il.Emit(OpCodes.Sub);
                il.Emit(OpCodes.Conv_I4);
                il.Emit(OpCodes.Stsfld, offsets[structure][hops]);
            }

            il.Emit(OpCodes.Ldstr, structure.Name);
            if (chain.Count > 1) il.Emit(OpCodes.Ldsfld, Use(_backend.TypeInfoFields[containers[chain[1]].Type.FullName]));
            else il.Emit(OpCodes.Ldnull);
            if (chain.Count > 1) il.Emit(OpCodes.Ldsfld, offsets[structure][1]);
            else il.Emit(OpCodes.Ldc_I4_0);

            // What runs for each slot: the most derived function of the chain that has it.
            il.Emit(OpCodes.Ldc_I4, slotCounts[structure]);
            il.Emit(OpCodes.Newarr, Core("System.IntPtr"));
            for (var slot = 0; slot < slotCounts[structure]; slot++)
            {
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Ldc_I4, slot);

                var implementation = chain
                    .SelectMany(level => InstanceFunctions(containers[level]))
                    .FirstOrDefault(function => _backend.Slots.TryGetValue(function, out var found) && found == slot);

                if (implementation != null && thunks.TryGetValue(implementation, out var thunk)) il.Emit(OpCodes.Ldftn, thunk);
                else
                {
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Conv_I);
                }

                il.Emit(OpCodes.Stelem_I);
            }

            il.Emit(OpCodes.Newobj, _module.ImportReference(runtime.TypeInfoConstructor));
            il.Emit(OpCodes.Stsfld, _backend.TypeInfoFields[container.Type.FullName]);
            il.Emit(OpCodes.Ret);
        }
    }

    /// <summary>The struct and the ones it extends, from the nearest to the farthest.</summary>
    private List<TqStruct> ChainOf(TqStruct structure)
    {
        var chain = new List<TqStruct>();
        for (var current = structure; current != null; current = BaseOf(current)) chain.Add(current);
        return chain;
    }

    /// <summary>The function of a struct above that has a slot and the same name and parameters as this one.</summary>
    private TqFunction? FindOverridden(TqFunction function, TqStruct structure, Dictionary<TqStruct, Container> containers)
    {
        var method = (MethodDefinition)_backend.Definitions[function];

        for (var above = BaseOf(structure); above != null; above = BaseOf(above))
        {
            foreach (var candidate in InstanceFunctions(containers[above]))
            {
                if (!_backend.Slots.ContainsKey(candidate) || candidate.Name != function.Name) continue;

                var other = (MethodDefinition)_backend.Definitions[candidate];
                if (other.Parameters.Select(parameter => parameter.ParameterType.FullName)
                    .SequenceEqual(method.Parameters.Select(parameter => parameter.ParameterType.FullName)))
                    return candidate;
            }
        }

        return null;
    }

    private sealed class ReferenceComparer<T> : IEqualityComparer<T> where T : class
    {
        public static readonly ReferenceComparer<T> Instance = new();

        public bool Equals(T? x, T? y) => ReferenceEquals(x, y);
        public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
