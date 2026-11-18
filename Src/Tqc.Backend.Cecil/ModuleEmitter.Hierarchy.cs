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
/// - Each struct of the hierarchy has a nested interface, `Ref`, which is the type of a reference to it (`*S`). The
///   interface of a struct extends the one of its base, so a reference to the derived one is a reference to the base.
///   The struct implements it, and a reference is the struct boxed.
/// - The interface gives the value (`ref S get_Value()`: the fields are read and written through it, with no copy) and
///   a method for each function of the struct, so that a call through a reference reaches what the derived struct
///   overrides. The derived struct implements what it inherits by passing it on to its `__base`.
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

    /// <summary>The interface of each struct of the hierarchy, and the structs that implement the ones above them.</summary>
    private void DeclareRefInterfaces()
    {
        foreach (var container in _containers)
        {
            if (container.Struct is not { } structure || !InHierarchy(structure)) continue;

            var type = container.Type;
            var attributes = (IsPublic(structure.Access) ? TypeAttributes.NestedPublic : TypeAttributes.NestedAssembly)
                             | TypeAttributes.Interface | TypeAttributes.Abstract;
            var reference = new TypeDefinition("", "Ref", attributes);
            type.NestedTypes.Add(reference);

            _backend.RefInterfaces[type.FullName] = reference;
            _backend.RefInterfaceNames.Add(reference.FullName);
        }

        foreach (var container in _containers)
        {
            if (container.Struct is not { } structure || !InHierarchy(structure)) continue;

            var reference = _backend.RefInterfaces[container.Type.FullName];

            // The interface extends the one of the base; the struct implements its own and all the ones above.
            container.Type.Interfaces.Add(new InterfaceImplementation(reference));

            for (var @base = BaseOf(structure); @base != null; @base = BaseOf(@base))
            {
                var above = _backend.RefInterfaces[((TypeDefinition)_backend.Definitions[@base]).FullName];
                if (ReferenceEquals(@base, BaseOf(structure))) reference.Interfaces.Add(new InterfaceImplementation(above));
                container.Type.Interfaces.Add(new InterfaceImplementation(above));
            }
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

    /// <summary>The members of the interfaces: the value, and a method for each function of the struct.</summary>
    private void DeclareRefMembers()
    {
        foreach (var container in _containers)
        {
            if (container.Struct is not { } structure || !InHierarchy(structure)) continue;

            var reference = _backend.RefInterfaces[container.Type.FullName];
            const MethodAttributes open = MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual
                                          | MethodAttributes.NewSlot | MethodAttributes.HideBySig;

            // The value: a reference to the struct inside the one that is referred to.
            var byReference = new ByReferenceType(Use(container.Type));
            var getter = new MethodDefinition("get_Value", open | MethodAttributes.SpecialName, byReference);
            reference.Methods.Add(getter);
            reference.Properties.Add(new PropertyDefinition("Value", PropertyAttributes.None, byReference) { GetMethod = getter });
            _backend.RefGetters[container.Type.FullName] = getter;

            foreach (var function in InstanceFunctions(container))
            {
                var method = (MethodDefinition)_backend.Definitions[function];
                var declared = new MethodDefinition(method.Name, open | (method.IsSpecialName ? MethodAttributes.SpecialName : 0), method.ReturnType);
                foreach (var parameter in method.Parameters)
                    declared.Parameters.Add(new ParameterDefinition(parameter.Name, ParameterAttributes.None, parameter.ParameterType));

                reference.Methods.Add(declared);
                _backend.RefMethods[function] = declared;
            }
        }
    }

    /// <summary>
    /// What the struct does for each interface it implements: the value is its `__base` (or itself), and a function goes
    /// to the most derived one that overrides it, or to the one that declares it.
    /// </summary>
    private void DeclareRefBridges()
    {
        var containers = _containers.Where(container => container.Struct != null && InHierarchy(container.Struct))
            .ToDictionary(container => container.Struct!, ReferenceEqualityComparer.Instance);

        foreach (var container in containers.Values)
        {
            // From the struct up to the farthest base.
            var chain = new List<TqStruct>();
            for (var current = container.Struct; current != null; current = BaseOf(current)) chain.Add(current);

            for (var level = 0; level < chain.Count; level++)
            {
                var implemented = containers[chain[level]];
                var getter = _backend.RefGetters[implemented.Type.FullName];

                var bridge = NewBridge($"Ref${implemented.Type.Name}$get_Value", getter.ReturnType, getter);
                container.Type.Methods.Add(bridge);
                var il = bridge.Body.GetILProcessor();
                il.Emit(OpCodes.Ldarg_0);
                for (var hop = 0; hop < level; hop++) il.Emit(OpCodes.Ldflda, _backend.BaseFields[containers[chain[hop]].Type.FullName]);
                il.Emit(OpCodes.Ret);

                foreach (var function in InstanceFunctions(implemented))
                {
                    var declared = _backend.RefMethods[function];
                    var (target, targetLevel) = MostDerived(function, containers, chain, level);

                    var forward = NewBridge($"Ref${implemented.Type.Name}${declared.Name}", declared.ReturnType, declared);
                    foreach (var parameter in declared.Parameters)
                        forward.Parameters.Add(new ParameterDefinition(parameter.Name, ParameterAttributes.None, parameter.ParameterType));
                    container.Type.Methods.Add(forward);

                    il = forward.Body.GetILProcessor();
                    il.Emit(OpCodes.Ldarg_0);
                    for (var hop = 0; hop < targetLevel; hop++) il.Emit(OpCodes.Ldflda, _backend.BaseFields[containers[chain[hop]].Type.FullName]);
                    for (var i = 0; i < declared.Parameters.Count; i++) il.Emit(OpCodes.Ldarg, forward.Parameters[i]);
                    il.Emit(OpCodes.Call, Use((MethodDefinition)_backend.Definitions[target]));
                    il.Emit(OpCodes.Ret);
                }
            }
        }
    }

    /// <summary>A method that implements one of the interface, and is not callable but through it.</summary>
    private MethodDefinition NewBridge(string name, TypeReference returnType, MethodDefinition implemented)
    {
        var bridge = new MethodDefinition(name,
            MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.HideBySig,
            returnType);
        bridge.Overrides.Add(Use(implemented));
        return bridge;
    }

    /// <summary>
    /// The function that runs when `function` (declared by the struct at `level` of the chain) is called on the struct at
    /// the start of the chain: a function with the same name and parameters, marked `@override`, of a struct below it;
    /// else the function itself.
    /// </summary>
    private (TqFunction Function, int Level) MostDerived(
        TqFunction function, Dictionary<TqStruct, Container> containers, List<TqStruct> chain, int level)
    {
        var method = (MethodDefinition)_backend.Definitions[function];

        for (var candidateLevel = 0; candidateLevel < level; candidateLevel++)
        {
            foreach (var candidate in InstanceFunctions(containers[chain[candidateLevel]]))
            {
                if (!Has(candidate.Attributes, BuiltinAttribute.Override) || candidate.Name != function.Name) continue;

                var other = (MethodDefinition)_backend.Definitions[candidate];
                if (other.Parameters.Select(parameter => parameter.ParameterType.FullName)
                    .SequenceEqual(method.Parameters.Select(parameter => parameter.ParameterType.FullName)))
                    return (candidate, candidateLevel);
            }
        }

        return (function, level);
    }
}
