using Mono.Cecil;
using Mono.Cecil.Cil;
using Tqc.Core;
using Tqc.Dotnet.ProgramMembers;
using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;

namespace Tqc.Backend.Cecil;

internal sealed partial class ModuleEmitter
{
    /// <summary>The type of each property, found once: it is the type of its storage and of its accessors.</summary>
    private readonly Dictionary<TqProperty, TypeReference?> _propertyTypes = new(ReferenceEqualityComparer.Instance);

    // ---- Fields

    private void DeclareFields(Container container)
    {
        foreach (var member in Ordered(container.Members))
        {
            switch (member)
            {
                case TqField field:
                    DeclareField(container, field);
                    break;

                // The storage of a property whose accessor uses `field` or is automatic. The IR does not declare it.
                case TqProperty { HasBackingField: true } property when PropertyTypeOf(property) is { } type:
                {
                    var backing = new FieldDefinition($"<{property.Name}>k__BackingField", FieldAttributes.Private, type);
                    container.Type.Fields.Add(backing);
                    _backend.BackingFields[property] = backing;
                    break;
                }
            }
        }
    }

    private TypeReference? PropertyTypeOf(TqProperty property)
    {
        if (_propertyTypes.TryGetValue(property, out var known)) return known;

        // Without a declared type it is the one of the accessors: what the getter returns or the setter takes.
        var expression = property.Type
                         ?? property.GetterFunction?.ReturnType
                         ?? property.SetterFunction?.Parameters?.FirstOrDefault()?.Type
                         ?? (property.Getter as TqFunction)?.ReturnType
                         ?? (property.Setter as TqFunction)?.Parameters?.FirstOrDefault()?.Type;

        TypeReference? type = null;
        if (expression == null) Report($"The type of the property '{property.Name}' is not known", property.SourceLocation);
        else type = MapExpression(expression);

        _propertyTypes[property] = type;
        return type;
    }

    internal static bool IsUntyped(IrExpression expression) =>
        expression is IrReference { Reference: TypeMetaReference { Type: var type } } && ReferenceEquals(type, CompilerStd.Types.AnyType);

    private void DeclareField(Container container, TqField field)
    {
        var std = CompilerStd.Types;
        var isGlobal = container.Struct == null;

        if (container.Struct is { IsInterface: true })
        {
            Report($"The interface '{container.Struct.Name}' cannot have the field '{field.Name}'", field.SourceLocation);
            return;
        }

        // `const T x` is `let Const(T) x`; `const x = v` is `Const(AnyType)`, with the type taken from the value.
        var typeExpression = field.Type;
        var isConstant = false;
        if (typeExpression is IrReference { Reference: TypeMetaReference { Type: GenericInstance { Arguments: [var inner] } instance } }
            && ReferenceEquals(instance.Definition, std.Const))
        {
            isConstant = true;
            typeExpression = IsUntyped(inner) ? null : inner;
        }

        TypeReference type;
        if (typeExpression != null)
        {
            type = MapExpression(typeExpression);
        }
        else if (field.Value != null && _tq.Types.Get(field.Value) is { } inferred)
        {
            type = MapType(inferred, field.SourceLocation);
        }
        else
        {
            Report($"The type of the field '{field.Name}' is not known", field.SourceLocation);
            return;
        }

        if (type is ByReferenceType)
        {
            Report($"The field '{field.Name}' cannot hold a reference to a value in .NET; only a parameter or a result can", field.SourceLocation);
            type = Core("System.Object");
        }

        var attributes = IsPublic(field.Access) ? FieldAttributes.Public : FieldAttributes.Assembly;
        if (isGlobal || isConstant || Has(field.Attributes, BuiltinAttribute.Static)) attributes |= FieldAttributes.Static;

        var definition = new FieldDefinition(field.Name, attributes, type);

        if (isConstant)
        {
            if (TryConstant(field.Value, type, out var constant))
            {
                definition.Attributes |= FieldAttributes.Literal | FieldAttributes.HasDefault;
                definition.Constant = constant;
            }
            else
            {
                Report($"The value of the constant '{field.Name}' must be a literal that fits '{type.FullName}' to be emitted", field.SourceLocation);
            }
        }

        // The initial value of any other field takes code (a constructor), which is not emitted yet.
        container.Type.Fields.Add(definition);
        _backend.Definitions[field] = definition;
    }

    // ---- Functions

    private void DeclareFunctions(Container container)
    {
        foreach (var member in Ordered(container.Members))
        {
            switch (member)
            {
                case TqFunctionGroup group:
                    foreach (var function in group.Functions) DeclareFunction(container, function, null);
                    break;

                case TqFunction function:
                    DeclareFunction(container, function, null);
                    break;

                case TqProperty property when PropertyTypeOf(property) != null:
                    if (property.GetterFunction != null) DeclareFunction(container, property.GetterFunction, property);
                    if (property.SetterFunction != null) DeclareFunction(container, property.SetterFunction, property);
                    break;
            }
        }

        if (container.Struct is not { } structure) return;

        if (structure.IsInterface)
        {
            if (structure.Constructors.Count > 0 || structure.Destructor != null)
                Report($"The interface '{structure.Name}' cannot have a constructor or a destructor", structure.SourceLocation);
            return;
        }

        foreach (var constructor in structure.Constructors) DeclareFunction(container, constructor, null);

        // A class that declares no constructor can be created without arguments; so can a struct by value whose fields
        // have initial values, because those take code.
        if (structure.Constructors.Count == 0 && (IsClass(structure) || InstanceInitializers(structure).Count > 0))
            DeclareDefaultConstructor(container, structure);
        if (structure.Destructor != null) DeclareDestructor(container, structure, structure.Destructor);
    }

    private void DeclareFunction(Container container, TqFunction function, TqProperty? accessorOf)
    {
        var structure = container.Struct;
        var isConstructor = function is TqConstructor;
        var isStatic = !isConstructor && (structure == null || function.IsStatic);
        var isInterface = structure is { IsInterface: true };
        var isAbstract = !isStatic && !isConstructor && function.IsAbstract;

        // A static member of a generic type would be called through the type with its arguments, which a call of the namespace form does not give.
        if (isStatic && structure != null && container.Type.HasGenericParameters)
        {
            Report($"The function '{function.Name}' is static in the generic struct '{structure.Name}', which cannot be emitted to .NET yet", function.SourceLocation);
            return;
        }

        var attributes = MethodAttributes.HideBySig | (IsPublic(function.Access) || isInterface ? MethodAttributes.Public : MethodAttributes.Assembly);
        if (isConstructor) attributes |= MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
        if (accessorOf != null) attributes |= MethodAttributes.SpecialName;

        if (isStatic)
        {
            attributes |= MethodAttributes.Static;
        }
        else if (!isConstructor && (IsClass(structure) || structure.IsInterface))
        {
            if (isAbstract) attributes |= MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot;
            else if (Has(function.Attributes, BuiltinAttribute.Virtual) || isInterface) attributes |= MethodAttributes.Virtual | MethodAttributes.NewSlot;
            else if (Has(function.Attributes, BuiltinAttribute.Override)) attributes |= MethodAttributes.Virtual;
        }

        var name = isConstructor ? ".ctor" : function.Name;
        var isSetter = accessorOf != null && ReferenceEquals(function, accessorOf.SetterFunction);

        // The accessors of a property have the signature of the property: `() -> T` and `(T) -> void`.
        var method = new MethodDefinition(name, attributes, Core("System.Void"));

        // The parameters of the type `type` are the generic parameters of the method; the rest of the signature may use them.
        foreach (var parameter in function.Parameters ?? [])
        {
            if (accessorOf != null || !GenericParameterTypeRef.IsTypeParameter(parameter)) continue;

            var generic = new GenericParameter(parameter.Name, method);
            method.GenericParameters.Add(generic);
            GenericParameters[parameter] = generic;
        }

        TypeReference returnType;
        if (accessorOf != null) returnType = isSetter ? Core("System.Void") : PropertyTypeOf(accessorOf)!;
        else if (isConstructor || function.ReturnType == null) returnType = Core("System.Void");
        else returnType = MapExpression(function.ReturnType);
        method.ReturnType = returnType;

        if (accessorOf != null)
        {
            if (isSetter) method.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, PropertyTypeOf(accessorOf)!));
        }
        else
        {
            foreach (var parameter in function.Parameters ?? [])
            {
                if (GenericParameterTypeRef.IsTypeParameter(parameter)) continue;
                method.Parameters.Add(new ParameterDefinition(parameter.Name, ParameterAttributes.None, MapExpression(parameter.Type)));
            }
        }

        container.Type.Methods.Add(method);
        _backend.Definitions[function] = method;
        if (isAbstract) return;

        var owner = $"{container.Type.Name}.{name}";
        if (accessorOf != null && function.Body == null && accessorOf.HasBackingField)
            _bodies.Add((method, il => EmitAutomaticAccessor(il, accessorOf!, isSetter)));
        else if (function.Body != null || isConstructor)
            _bodies.Add((method, il => new BodyEmitter(this, function, method, il).Emit()));
        else
            _bodies.Add((method, il => EmitStub(il, $"Tq: '{owner}' has no body")));
    }

    private void DeclareDefaultConstructor(Container container, TqStruct structure)
    {
        var method = new MethodDefinition(".ctor",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
            Core("System.Void"));
        container.Type.Methods.Add(method);
        _backend.DefaultConstructors[structure] = method;

        // A struct by value has nothing to call first: it is only the initial values.
        if (!IsClass(structure))
        {
            var constructor = new TqConstructor(structure.SourceLocation) { Parameters = [], Owner = structure };
            _bodies.Add((method, il => new BodyEmitter(this, constructor, method, il).Emit()));
            return;
        }

        _bodies.Add((method, il =>
        {
            var baseConstructor = FindBaseConstructor(structure);
            if (baseConstructor == null)
            {
                EmitStub(il, $"Tq: '{structure.Name}' extends something that has no constructor without arguments");
                return;
            }

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, baseConstructor);
            new BodyEmitter(this, new TqConstructor(structure.SourceLocation) { Parameters = [], Owner = structure }, method, il).EmitInitializers();
            il.Emit(OpCodes.Ret);
        }));
    }

    /// <summary>The instance fields that have an initial value (`let _list = new List(T)()`), in the order they are written.</summary>
    internal List<(TqField Field, FieldDefinition Definition)> InstanceInitializers(TqStruct structure)
    {
        List<(TqField, FieldDefinition)> result = [];
        foreach (var member in structure.Members.Values.OrderBy(member => LocationOf(member)?.span.LineStart ?? 0)
                     .ThenBy(member => LocationOf(member)?.span.ColStart ?? 0))
        {
            if (member is TqField { Value: not null } field
                && _backend.Definitions.TryGetValue(field, out var definition)
                && definition is FieldDefinition { IsStatic: false, HasConstant: false } fieldDefinition)
                result.Add((field, fieldDefinition));
        }

        return result;
    }

    /// <summary>A destructor is the finalizer of a class. Its body is not translated yet, so it does nothing.</summary>
    private void DeclareDestructor(Container container, TqStruct structure, TqFunction destructor)
    {
        if (!IsClass(structure))
        {
            Report($"The struct '{structure.Name}' is by value and cannot have a destructor in .NET; mark it '@class'", destructor.SourceLocation);
            return;
        }

        var method = new MethodDefinition("Finalize",
            MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.Virtual, Core("System.Void"));
        container.Type.Methods.Add(method);
        _backend.Definitions[destructor] = method;
        _bodies.Add((method, il => il.Emit(OpCodes.Ret)));
    }

    /// <summary>The constructor without arguments of what the struct extends; null if there is none to call.</summary>
    internal MethodReference? FindBaseConstructor(TqStruct structure)
    {
        switch (structure.BaseStruct)
        {
            case null:
            case TqStruct { IsInterface: true }:
                var @object = _backend.Core.Find("System.Object");
                var objectConstructor = @object?.Methods.FirstOrDefault(c1 => c1.IsConstructor && !c1.IsStatic && !c1.HasParameters);
                return objectConstructor != null ? _module.ImportReference(objectConstructor) : null;

            case TqStruct tq:
                if (_backend.DefaultConstructors.TryGetValue(tq, out var generated)) return Use(generated);

                var declared = tq.Constructors.FirstOrDefault(c2 => (c2.Parameters?.Length ?? 0) == 0);
                return declared != null && _backend.Definitions.TryGetValue(declared, out var definition) && definition is MethodDefinition method
                    ? Use(method)
                    : null;

            case DotnetClass dotnet:
                TypeDefinition? resolved;
                try { resolved = dotnet.TypeReference.Resolve(); }
                catch (AssemblyResolutionException) { return null; }

                var constructor = resolved?.Methods.FirstOrDefault(c3 => c3.IsConstructor && !c3.IsStatic
                    && !c3.HasParameters && (c3.IsPublic || c3.IsFamily || c3.IsFamilyOrAssembly));
                return constructor != null ? _module.ImportReference(constructor) : null;

            default:
                return null;
        }
    }

    // ---- Properties

    private void DeclareProperties(Container container)
    {
        foreach (var member in Ordered(container.Members))
        {
            if (member is not TqProperty property || PropertyTypeOf(property) is not { } type) continue;

            var definition = new PropertyDefinition(property.Name, PropertyAttributes.None, type) { HasThis = true };
            definition.GetMethod = AccessorOf(property, property.GetterFunction ?? property.Getter, container.Type, "getter");
            definition.SetMethod = AccessorOf(property, property.SetterFunction ?? property.Setter, container.Type, "setter");

            container.Type.Properties.Add(definition);
            _backend.Definitions[property] = definition;
        }
    }

    /// <summary>The method that reads or writes the property: its own accessor, or the function that `get = f` names.</summary>
    private MethodDefinition? AccessorOf(TqProperty property, IFunction? function, TypeDefinition owner, string role)
    {
        if (function == null) return null;

        if (function is TqFunction tq && _backend.Definitions.TryGetValue(tq, out var definition)
            && definition is MethodDefinition method && method.DeclaringType == owner)
            return method;

        Report($"The {role} of '{property.Name}' is not a function of the same type, which .NET does not allow for a property",
            property.SourceLocation);
        return null;
    }

    // ---- Bodies

    /// <summary>The body of a function is not translated yet: it throws, so that running it is not mistaken for working.</summary>
    internal void EmitStub(ILProcessor il, string message)
    {
        var exception = _backend.Core.Find("System.NotImplementedException")
                        ?? throw new BackendError("The type 'System.NotImplementedException' was not found");
        var constructor = exception.Methods.First(method => method.IsConstructor && !method.IsStatic
            && method.Parameters is [{ ParameterType.FullName: "System.String" }]);

        il.Emit(OpCodes.Ldstr, message);
        il.Emit(OpCodes.Newobj, _module.ImportReference(constructor));
        il.Emit(OpCodes.Throw);
    }

    /// <summary>`get;` and `set;`: reads and writes the storage of the property.</summary>
    private void EmitAutomaticAccessor(ILProcessor il, TqProperty property, bool isSetter)
    {
        var storage = Use(_backend.BackingFields[property]);

        il.Emit(OpCodes.Ldarg_0);
        if (isSetter)
        {
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, storage);
        }
        else
        {
            il.Emit(OpCodes.Ldfld, storage);
        }

        il.Emit(OpCodes.Ret);
    }

    // ---- Entry point

    /// <summary>
    /// `main` of the root module is the entry point. It is called by `EntryPoint.Main(string[])` (a class of its own at the top), which the runtime
    /// starts; what `main` returns (a `!void` is a `Tq.Failable`) is dropped until errors are handled.
    /// </summary>
    private void DeclareEntryPoint()
    {
        IEnumerable<TqFunction> candidates = [];
        if (_tq.RootNamespace.Members.TryGetValue("main", out var found))
        {
            candidates = found switch
            {
                TqFunctionGroup group => group.Functions,
                TqFunction function => [function],
                _ => [],
            };
        }

        var main = candidates.FirstOrDefault(function => function.Parameters?.Length == 1)
                   ?? candidates.FirstOrDefault(function => (function.Parameters?.Length ?? 0) == 0);
        if (main == null)
        {
            Report($"The module '{_name}' is the root of the project and needs a function 'main', with no parameters or with '[]string'");
            return;
        }

        if (!_backend.Definitions.TryGetValue(main, out var definition) || definition is not MethodDefinition method) return;

        var takesArguments = main.Parameters?.Length == 1;
        if (takesArguments && method.Parameters[0].ParameterType is not ArrayType { ElementType.FullName: "System.String" })
        {
            Report("The parameter of 'main' must be '[]string'", main.SourceLocation);
            return;
        }

        var entry = new MethodDefinition("Main", MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
            Core("System.Void"));
        entry.Parameters.Add(new ParameterDefinition("args", ParameterAttributes.None, new ArrayType(Core("System.String"))));

        var il = entry.Body.GetILProcessor();
        if (takesArguments) il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, method);
        if (method.ReturnType.FullName != "System.Void") il.Emit(OpCodes.Pop);
        il.Emit(OpCodes.Ret);

        var type = new TypeDefinition("", "EntryPoint",
            TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
            Core("System.Object"));
        type.Methods.Add(entry);
        _module.Types.Add(type);
        _assembly.EntryPoint = entry;
    }
}
