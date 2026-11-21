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
/// - `*S` of a struct of the hierarchy is a reference to an object of the class `S.Ref`, nested in the struct. The classes
///   extend one another the way the structs do (`D.Ref` extends `B.Ref`), so a reference to the derived struct is a
///   reference to the base one, and `x as *D` is a cast of .NET. A reference is a real reference: it can be a field, be
///   returned, be kept. What it refers to is a struct, kept by `S.Box`, the concrete class that has it.
/// - Each `X.Ref` has `ref X get_Value_X()` (abstract: the `Box` of the struct has it), which is how the fields are read
///   and written with no copy; the one of the class above is overridden to give the `__base` of it. And each `X.Ref` has a
///   method for each function of X, which calls it on the value; the `@override` of a struct is the override of the
///   method of the class, so a call through a reference reaches the most derived function.
/// - A struct by value where `*S` is expected is put in a new `Box` (a copy).
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

    /// <summary>
    /// The class `Ref` of each struct of the hierarchy (the type of `*S`), and `Box`, the class that has the value. The one
    /// of a struct extends the one of its base.
    /// </summary>
    private void DeclareRefInterfaces()
    {
        var ordered = _containers
            .Where(container => container.Struct != null && InHierarchy(container.Struct))
            .OrderBy(container => Depth(container.Struct!));

        foreach (var container in ordered)
        {
            var structure = container.Struct!;
            var type = container.Type;
            var access = IsPublic(structure.Access) ? TypeAttributes.NestedPublic : TypeAttributes.NestedAssembly;

            TypeReference parent = Core("System.Object");
            if (BaseOf(structure) is { } above) parent = Use(_backend.RefInterfaces[((TypeDefinition)_backend.Definitions[above]).FullName]);

            var reference = new TypeDefinition("", "Ref", access | TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit, parent);
            type.NestedTypes.Add(reference);
            reference.Methods.Add(NewConstructor(reference, parent));

            var box = new TypeDefinition("", "Box", access | TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit, Use(reference));
            type.NestedTypes.Add(box);

            _backend.RefInterfaces[type.FullName] = reference;
            _backend.RefInterfaceNames.Add(reference.FullName);
            _backend.RefBoxes[type.FullName] = box;
        }
    }

    private int Depth(TqStruct structure)
    {
        var depth = 0;
        for (var current = BaseOf(structure); current != null; current = BaseOf(current)) depth++;
        return depth;
    }

    /// <summary>A constructor with no parameters that calls the one of the parent.</summary>
    private MethodDefinition NewConstructor(TypeDefinition type, TypeReference parent)
    {
        var constructor = new MethodDefinition(".ctor",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
            Core("System.Void"));

        MethodReference baseConstructor;
        if (parent is TypeDefinition definition)
            baseConstructor = definition.Methods.First(method => method.IsConstructor && !method.IsStatic && !method.HasParameters);
        else
            baseConstructor = _module.ImportReference(_backend.Core.Find("System.Object")!.Methods
                .First(method => method.IsConstructor && !method.IsStatic && !method.HasParameters));

        var il = constructor.Body.GetILProcessor();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, baseConstructor);
        il.Emit(OpCodes.Ret);
        return constructor;
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
    /// The members of the classes: the value of each struct (`get_Value_X`), a method for each function of the struct, and the
    /// storage of the `Box`.
    /// </summary>
    private void DeclareRefMembers()
    {
        var containers = _containers
            .Where(container => container.Struct != null && InHierarchy(container.Struct))
            .OrderBy(container => Depth(container.Struct!))
            .ToList();

        foreach (var container in containers)
        {
            var structure = container.Struct!;
            var type = container.Type;
            var reference = _backend.RefInterfaces[type.FullName];
            var box = _backend.RefBoxes[type.FullName];

            // The value: a reference to the struct inside the object. The `Box` has it; the classes above it ask this one.
            var valueName = $"get_Value_{type.Name}";
            var byReference = new ByReferenceType(Use(type));
            var getter = new MethodDefinition(valueName,
                MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.HideBySig,
                byReference);
            reference.Methods.Add(getter);
            _backend.RefGetters[type.FullName] = getter;

            // The struct that is extended is the `__base` of this one.
            if (BaseOf(structure) is { } above)
            {
                var aboveType = (TypeDefinition)_backend.Definitions[above];
                var override_ = new MethodDefinition($"get_Value_{aboveType.Name}",
                    MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig, new ByReferenceType(Use(aboveType)));
                var il = override_.Body.GetILProcessor();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Callvirt, getter);
                il.Emit(OpCodes.Ldflda, _backend.BaseFields[type.FullName]);
                il.Emit(OpCodes.Ret);
                reference.Methods.Add(override_);
            }

            // A method for each function: it runs the function of the struct on the value. The `@override` of a struct
            // overrides the method of the one above, so what runs through a reference is the most derived one.
            foreach (var function in InstanceFunctions(container))
            {
                var method = (MethodDefinition)_backend.Definitions[function];
                var overridden = Has(function.Attributes, BuiltinAttribute.Override) ? FindOverridden(function, structure) : null;

                var attributes = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig;
                if (overridden == null) attributes |= MethodAttributes.NewSlot;
                if (method.IsSpecialName) attributes |= MethodAttributes.SpecialName;

                var declared = new MethodDefinition(method.Name, attributes, method.ReturnType);
                foreach (var parameter in method.Parameters)
                    declared.Parameters.Add(new ParameterDefinition(parameter.Name, ParameterAttributes.None, parameter.ParameterType));

                var il = declared.Body.GetILProcessor();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Callvirt, getter);
                for (var i = 0; i < declared.Parameters.Count; i++) il.Emit(OpCodes.Ldarg, declared.Parameters[i]);
                il.Emit(OpCodes.Call, Use(method));
                il.Emit(OpCodes.Ret);

                reference.Methods.Add(declared);
                _backend.RefMethods[function] = declared;
            }

            // The `Box`: the struct, and the access to it.
            var value = new FieldDefinition("Value", FieldAttributes.Public, Use(type));
            box.Fields.Add(value);

            var constructor = new MethodDefinition(".ctor",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                Core("System.Void"));
            constructor.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, Use(type)));
            var body = constructor.Body.GetILProcessor();
            body.Emit(OpCodes.Ldarg_0);
            body.Emit(OpCodes.Call, reference.Methods.First(candidate => candidate.IsConstructor && !candidate.HasParameters));
            body.Emit(OpCodes.Ldarg_0);
            body.Emit(OpCodes.Ldarg_1);
            body.Emit(OpCodes.Stfld, value);
            body.Emit(OpCodes.Ret);
            box.Methods.Add(constructor);

            var access = new MethodDefinition(valueName,
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig, byReference);
            body = access.Body.GetILProcessor();
            body.Emit(OpCodes.Ldarg_0);
            body.Emit(OpCodes.Ldflda, value);
            body.Emit(OpCodes.Ret);
            box.Methods.Add(access);
        }
    }

    /// <summary>The function of a struct above, with the same name and parameters, that has a method in its class.</summary>
    private TqFunction? FindOverridden(TqFunction function, TqStruct structure)
    {
        var method = (MethodDefinition)_backend.Definitions[function];

        for (var above = BaseOf(structure); above != null; above = BaseOf(above))
        {
            var container = _containers.First(candidate => ReferenceEquals(candidate.Struct, above));
            foreach (var candidate in InstanceFunctions(container))
            {
                if (candidate.Name != function.Name || !_backend.RefMethods.ContainsKey(candidate)) continue;

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
