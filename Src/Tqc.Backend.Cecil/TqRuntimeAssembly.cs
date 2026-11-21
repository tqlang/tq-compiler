using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Tqc.Backend.Cecil;

/// <summary>
/// `Tq.Runtime`, the assembly that holds the types the language needs and .NET has no equivalent for. It is created
/// when the first module that uses one is emitted, and written with the others.
///
///   struct Tq.Failable        { object Error }               `!void`
///   struct Tq.Failable&lt;T&gt;     { T Value; object Error }  `!T`
///   class  Tq.TypeInfo       the dynamic type of a struct of a hierarchy: its base, where the base is inside it, and the
///                             function pointers of its virtual functions
///   ref struct Tq.Ref        `*S` of a struct of a hierarchy: `ref byte View` (the part of the struct that `S` is) and its TypeInfo
///   static class Tq.Ops       the operators that have no instruction: `Tq.Ops.AddOnBounds_Byte(a, b)`, `Tq.Ops.Pow_Int32(a, b)`...
///                             (made by the first module that needs each one, so there is one copy for the whole build)
///
/// `Error` stands for `Std.Types.Error`, which does not exist yet: it is null when the call worked.
/// </summary>
internal sealed class TqRuntimeAssembly
{
    public const string Name = "Tq.Runtime";
    public const string Namespace = "Tq";

    public AssemblyDefinition Assembly { get; }
    public TypeDefinition Failable { get; }
    public TypeDefinition FailableOf { get; }

    /// <summary>`Tq.TypeInfo` (see ModuleEmitter.Hierarchy) and `Tq.Ref`, the type of `*S` of a struct of a hierarchy.</summary>
    public TypeDefinition TypeInfo { get; }
    public MethodDefinition TypeInfoConstructor { get; }
    public FieldDefinition TypeInfoTable { get; }
    public TypeDefinition Ref { get; }
    public MethodDefinition RefConstructor { get; }
    public MethodDefinition RefCast { get; }
    public FieldDefinition RefView { get; }
    public FieldDefinition RefType { get; }

    /// <summary>`Tq.Ops`, with the helpers of the operators; by operator and type.</summary>
    public TypeDefinition Ops { get; }
    public Dictionary<string, MethodDefinition> OpsHelpers { get; } = [];

    public TqRuntimeAssembly(CoreTypes core)
    {
        Assembly = AssemblyDefinition.CreateAssembly(
            new AssemblyNameDefinition(Name, new Version(1, 0, 0, 0)),
            Name + ".dll",
            new ModuleParameters { Kind = ModuleKind.Dll, Runtime = TargetRuntime.Net_4_0 });

        var module = Assembly.MainModule;
        var @object = core.Require(module, "System.Object");
        var valueType = core.Require(module, "System.ValueType");

        const TypeAttributes attributes = TypeAttributes.Public | TypeAttributes.Sealed
            | TypeAttributes.SequentialLayout | TypeAttributes.BeforeFieldInit;

        Failable = new TypeDefinition(Namespace, "Failable", attributes, valueType);
        Failable.Fields.Add(new FieldDefinition("Error", FieldAttributes.Public, @object));
        module.Types.Add(Failable);

        FailableOf = new TypeDefinition(Namespace, "Failable`1", attributes, valueType);
        var parameter = new GenericParameter("T", FailableOf);
        FailableOf.GenericParameters.Add(parameter);
        FailableOf.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public, parameter));
        FailableOf.Fields.Add(new FieldDefinition("Error", FieldAttributes.Public, @object));
        module.Types.Add(FailableOf);

        (TypeInfo, TypeInfoConstructor, TypeInfoTable, var offsetOf) = BuildTypeInfo(core, module, @object);
        (Ref, RefConstructor, RefCast, RefView, RefType) = BuildRef(core, module, valueType, offsetOf);

        Ops = new TypeDefinition(Namespace, "Ops",
            TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit, @object);
        module.Types.Add(Ops);
    }

    private static MethodReference Constructor(CoreTypes core, ModuleDefinition module, string typeName, int parameters, string? firstParameter = null)
    {
        var type = core.Find(typeName) ?? throw new BackendError($"The type '{typeName}' was not found in 'System.Runtime'");
        var constructor = type.Methods.FirstOrDefault(method => method.IsConstructor && !method.IsStatic
                                                                && method.Parameters.Count == parameters
                                                                && (firstParameter == null || method.Parameters[0].ParameterType.FullName == firstParameter))
                          ?? throw new BackendError($"The constructor of '{typeName}' was not found in 'System.Runtime'");
        return module.ImportReference(constructor);
    }

    /// <summary>
    /// `Tq.TypeInfo`: what a reference to a struct of a hierarchy knows of the struct it really is.
    ///
    ///   string Name; TypeInfo Parent; int ParentOffset; nint[] Table;
    ///   int OffsetOf(TypeInfo ancestor)   bytes from the start of the struct to the part of the ancestor (itself: 0);
    ///                                     -1 if it is not one
    /// </summary>
    private static (TypeDefinition, MethodDefinition, FieldDefinition, MethodDefinition) BuildTypeInfo(
        CoreTypes core, ModuleDefinition module, TypeReference @object)
    {
        var type = new TypeDefinition(Namespace, "TypeInfo",
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit, @object);
        module.Types.Add(type);

        var @string = core.Require(module, "System.String");
        var int32 = core.Require(module, "System.Int32");
        var native = core.Require(module, "System.IntPtr");
        var table = new ArrayType(native);

        var name = new FieldDefinition("Name", FieldAttributes.Public, @string);
        var parent = new FieldDefinition("Parent", FieldAttributes.Public, type);
        var parentOffset = new FieldDefinition("ParentOffset", FieldAttributes.Public, int32);
        var functions = new FieldDefinition("Table", FieldAttributes.Public, table);
        type.Fields.Add(name);
        type.Fields.Add(parent);
        type.Fields.Add(parentOffset);
        type.Fields.Add(functions);

        var constructor = new MethodDefinition(".ctor",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
            core.Require(module, "System.Void"));
        constructor.Parameters.Add(new ParameterDefinition("name", ParameterAttributes.None, @string));
        constructor.Parameters.Add(new ParameterDefinition("parent", ParameterAttributes.None, type));
        constructor.Parameters.Add(new ParameterDefinition("parentOffset", ParameterAttributes.None, int32));
        constructor.Parameters.Add(new ParameterDefinition("table", ParameterAttributes.None, table));
        type.Methods.Add(constructor);

        var il = constructor.Body.GetILProcessor();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, Constructor(core, module, "System.Object", 0));
        for (var i = 0; i < 4; i++)
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg, constructor.Parameters[i]);
            il.Emit(OpCodes.Stfld, i switch { 0 => name, 1 => parent, 2 => parentOffset, _ => functions });
        }
        il.Emit(OpCodes.Ret);

        // int OffsetOf(TypeInfo ancestor): the sum of ParentOffset from this one up to the ancestor.
        var offsetOf = new MethodDefinition("OffsetOf", MethodAttributes.Public | MethodAttributes.HideBySig, int32);
        offsetOf.Parameters.Add(new ParameterDefinition("ancestor", ParameterAttributes.None, type));
        type.Methods.Add(offsetOf);

        var sum = new VariableDefinition(int32);
        var current = new VariableDefinition(type);
        offsetOf.Body.Variables.Add(sum);
        offsetOf.Body.Variables.Add(current);
        il = offsetOf.Body.GetILProcessor();

        var loop = il.Create(OpCodes.Nop);
        var found = il.Create(OpCodes.Ldloc, sum);
        var notFound = il.Create(OpCodes.Ldc_I4_M1);

        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Stloc, sum);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Stloc, current);
        il.Append(loop);
        il.Emit(OpCodes.Ldloc, current);
        il.Emit(OpCodes.Brfalse, notFound);
        il.Emit(OpCodes.Ldloc, current);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Beq, found);
        il.Emit(OpCodes.Ldloc, sum);
        il.Emit(OpCodes.Ldloc, current);
        il.Emit(OpCodes.Ldfld, parentOffset);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Stloc, sum);
        il.Emit(OpCodes.Ldloc, current);
        il.Emit(OpCodes.Ldfld, parent);
        il.Emit(OpCodes.Stloc, current);
        il.Emit(OpCodes.Br, loop);
        il.Append(found);
        il.Emit(OpCodes.Ret);
        il.Append(notFound);
        il.Emit(OpCodes.Ret);

        return (type, constructor, functions, offsetOf);
    }

    /// <summary>
    /// `Tq.Ref`, the type of `*S` of a struct of a hierarchy: a reference to the part of the struct that `S` is, and the
    /// struct that it really is. It lives on the stack only (a `ref struct`), so it is never an allocation.
    ///
    ///   ref byte View; TypeInfo Type;
    ///   static Ref Cast(Ref value, TypeInfo from, TypeInfo to)   `x as *D`: the same struct seen as `to`; fails if it is not one
    /// </summary>
    private (TypeDefinition, MethodDefinition, MethodDefinition, FieldDefinition, FieldDefinition) BuildRef(
        CoreTypes core, ModuleDefinition module, TypeReference valueType, MethodDefinition offsetOf)
    {
        var type = new TypeDefinition(Namespace, "Ref",
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.BeforeFieldInit, valueType);
        module.Types.Add(type);

        var byRefLike = core.Find("System.Runtime.CompilerServices.IsByRefLikeAttribute")
                        ?? throw new BackendError("The type 'System.Runtime.CompilerServices.IsByRefLikeAttribute' was not found in 'System.Runtime'");
        var attribute = byRefLike.Methods.First(method => method.IsConstructor && !method.IsStatic && method.Parameters.Count == 0);
        type.CustomAttributes.Add(new CustomAttribute(module.ImportReference(attribute)));

        var @byte = core.Require(module, "System.Byte");
        var @string = core.Require(module, "System.String");
        var @void = core.Require(module, "System.Void");
        var int32 = core.Require(module, "System.Int32");

        var view = new FieldDefinition("View", FieldAttributes.Public, new ByReferenceType(@byte));
        var dynamic = new FieldDefinition("Type", FieldAttributes.Public, TypeInfo);
        type.Fields.Add(view);
        type.Fields.Add(dynamic);

        var constructor = new MethodDefinition(".ctor",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, @void);
        constructor.Parameters.Add(new ParameterDefinition("view", ParameterAttributes.None, new ByReferenceType(@byte)));
        constructor.Parameters.Add(new ParameterDefinition("type", ParameterAttributes.None, TypeInfo));
        type.Methods.Add(constructor);
        var il = constructor.Body.GetILProcessor();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Stfld, view);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Stfld, dynamic);
        il.Emit(OpCodes.Ret);

        // Cast: View + (OffsetOf(to) - OffsetOf(from)), both inside the struct that it really is.
        var cast = new MethodDefinition("Cast", MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig, type);
        var value = new ParameterDefinition("value", ParameterAttributes.None, type);
        var from = new ParameterDefinition("from", ParameterAttributes.None, TypeInfo);
        var to = new ParameterDefinition("to", ParameterAttributes.None, TypeInfo);
        cast.Parameters.Add(value);
        cast.Parameters.Add(from);
        cast.Parameters.Add(to);
        type.Methods.Add(cast);

        var source = new VariableDefinition(int32);
        var target = new VariableDefinition(int32);
        cast.Body.Variables.Add(source);
        cast.Body.Variables.Add(target);
        il = cast.Body.GetILProcessor();

        il.Emit(OpCodes.Ldarga, value);
        il.Emit(OpCodes.Ldfld, dynamic);
        il.Emit(OpCodes.Ldarg, from);
        il.Emit(OpCodes.Call, offsetOf);
        il.Emit(OpCodes.Stloc, source);
        il.Emit(OpCodes.Ldarga, value);
        il.Emit(OpCodes.Ldfld, dynamic);
        il.Emit(OpCodes.Ldarg, to);
        il.Emit(OpCodes.Call, offsetOf);
        il.Emit(OpCodes.Stloc, target);

        var concat = core.Find("System.String")!.Methods.First(method => method.Name == "Concat" && method.IsStatic
            && method.Parameters.Count == 4 && method.Parameters.All(parameter => parameter.ParameterType.FullName == "System.String"));
        var invalid = Constructor(core, module, "System.InvalidCastException", 1, "System.String");

        var ok = il.Create(OpCodes.Ldarga, value);
        il.Emit(OpCodes.Ldloc, target);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Bge, ok);
        il.Emit(OpCodes.Ldstr, "Tq: a reference to '");
        il.Emit(OpCodes.Ldarga, value);
        il.Emit(OpCodes.Ldfld, dynamic);
        il.Emit(OpCodes.Ldfld, TypeInfoName);
        il.Emit(OpCodes.Ldstr, "' cannot be used as a reference to '");
        il.Emit(OpCodes.Ldarg, to);
        il.Emit(OpCodes.Ldfld, TypeInfoName);
        il.Emit(OpCodes.Call, module.ImportReference(concat));
        il.Emit(OpCodes.Newobj, invalid);
        il.Emit(OpCodes.Throw);

        il.Append(ok);
        il.Emit(OpCodes.Ldfld, view);
        il.Emit(OpCodes.Ldloc, target);
        il.Emit(OpCodes.Ldloc, source);
        il.Emit(OpCodes.Sub);
        il.Emit(OpCodes.Conv_I);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ldarga, value);
        il.Emit(OpCodes.Ldfld, dynamic);
        il.Emit(OpCodes.Newobj, constructor);
        il.Emit(OpCodes.Ret);

        return (type, constructor, cast, view, dynamic);
    }

    private FieldDefinition TypeInfoName => TypeInfo.Fields.First(field => field.Name == "Name");
}
