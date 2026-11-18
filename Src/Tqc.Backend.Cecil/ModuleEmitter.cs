using Mono.Cecil;
using Mono.Cecil.Cil;
using Tqc.Core;
using Tqc.Dotnet.ProgramMembers;
using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;
using FieldReference = Mono.Cecil.FieldReference;

namespace Tqc.Backend.Cecil;

/// <summary>
/// Emits one module. The work is done in stages, because each one needs everything of the one before to exist: types,
/// then what they extend, fields, functions, properties and last the bodies.
/// </summary>
internal sealed partial class ModuleEmitter
{
    private readonly CecilBackend _backend;
    private readonly TqModule _tq;
    private readonly string _name;
    private readonly bool _isEntry;
    private readonly AssemblyDefinition _assembly;
    private readonly ModuleDefinition _module;

    /// <summary>The members of a type (or of the static class of a namespace) that are emitted into it.</summary>
    private sealed record Container(TypeDefinition Type, string Namespace, IReadOnlyList<IMember> Members, TqStruct? Struct);

    private readonly List<Container> _containers = [];
    private readonly List<(MethodDefinition Method, Action<ILProcessor> Emit)> _bodies = [];

    public ModuleEmitter(CecilBackend backend, TqModule tq, string name, bool isEntry)
    {
        _backend = backend;
        _tq = tq;
        _name = name;
        _isEntry = isEntry;

        _assembly = AssemblyDefinition.CreateAssembly(
            new AssemblyNameDefinition(name, new Version(1, 0, 0, 0)),
            name + ".dll",
            new ModuleParameters { Kind = isEntry ? ModuleKind.Console : ModuleKind.Dll, Runtime = TargetRuntime.Net_4_0 });
        _module = _assembly.MainModule;
    }

    public AssemblyDefinition Emit()
    {
        // Needed by everything below; fails with a message when the build has no dotnet runtime.
        _ = Core("System.Object");

        DeclareNamespace(_tq.RootNamespace, _name, "", null);
        foreach (var container in _containers) DeclareBases(container);
        foreach (var container in _containers) DeclareFields(container);
        foreach (var container in _containers) DeclareFunctions(container);
        foreach (var container in _containers) DeclareProperties(container);

        foreach (var (method, emit) in _bodies) emit(method.Body.GetILProcessor());

        if (_isEntry) DeclareEntryPoint();
        return _assembly;
    }

    // ---- Helpers

    internal CecilBackend Backend => _backend;
    internal ModuleDefinition Module => _module;
    internal TqModule Tq => _tq;

    internal void Report(string message, SourceLocation? location = null) =>
        _backend.Diagnostics.ReportError(new BackendError(message, location));

    internal TypeReference Core(string fullName) => _backend.Core.Require(_module, fullName);

    internal TypeReference Use(TypeDefinition type) => type.Module == _module ? type : _module.ImportReference(type);
    internal MethodReference Use(MethodDefinition method) => method.Module == _module ? method : _module.ImportReference(method);
    internal FieldReference Use(FieldDefinition field) => field.Module == _module ? field : _module.ImportReference(field);

    private static bool Has(IReadOnlyList<TqAttribute> attributes, BuiltinAttribute kind) =>
        attributes.OfType<TqBuiltinAttribute>().Any(attribute => attribute.Kind == kind);

    private static IEnumerable<IMember> Ordered(IEnumerable<IMember> members) =>
        members.OrderBy(member => member.Name, StringComparer.Ordinal);

    /// <summary>`public` is public; the rest (the namespace, the module) is as much as .NET can say: the assembly.</summary>
    private static bool IsPublic(MemberAccess access) => access.Visibility == Visibility.Public;

    private static SourceLocation? LocationOf(IMember member) => member switch
    {
        TqStruct structure => structure.SourceLocation,
        TqTypedef typedef => typedef.SourceLocation,
        TqNamedCase named => named.SourceLocation,
        TqField field => field.SourceLocation,
        TqProperty property => property.SourceLocation,
        TqFunction function => function.SourceLocation,
        TqFunctionGroup group => group.Functions.FirstOrDefault()?.SourceLocation,
        _ => null,
    };

    // ---- Types

    /// <summary>The .NET type for a type written in the IR. Whatever cannot be one is reported, and `object` is used.</summary>
    internal TypeReference MapExpression(IrExpression expression)
    {
        var raw = TypeExpressions.Raw(expression);
        return raw != null ? MapType(raw, expression.location) : Unsupported("this type expression", expression.location);
    }

    private TypeReference Unsupported(string what, SourceLocation? location)
    {
        Report($"{what} cannot be emitted to .NET yet", location);
        return Core("System.Object");
    }

    internal TypeReference MapType(ITypeRef type, SourceLocation? location)
    {
        switch (type)
        {
            case ComptimeIntegerTypeRef: return Core("System.Int32");
            case ComptimeDecimalTypeRef: return Core("System.Double");
            case SliceTypeRef slice: return new ArrayType(MapType(slice.Element, location));
            case TypedefTypeReference typedef: return Defined(typedef.Typedef, typedef.Typedef.Name, location);
            case ProtocolTypeReference { Protocol: DotnetInterface dotnetInterface }:
                return _module.ImportReference(dotnetInterface.TypeReference);
            case StructTypeReference { Struct: var structure }: return MapStruct(structure, location);
            default: return Unsupported($"The type '{type}'", location);
        }
    }

    private TypeReference Defined(IMember member, string name, SourceLocation? location)
    {
        if (_backend.Definitions.TryGetValue(member, out var definition) && definition is TypeDefinition type) return Use(type);
        return Unsupported($"The type '{name}'", location);
    }

    private TypeReference MapStruct(IStruct structure, SourceLocation? location)
    {
        var std = CompilerStd.Types;

        switch (structure)
        {
            case TqStruct tq: return Defined(tq, tq.Name, location);
            case DotnetClass dotnetClass: return _module.ImportReference(dotnetClass.TypeReference);
            case DotnetStruct dotnetStruct: return _module.ImportReference(dotnetStruct.TypeReference);
            case DotnetEnum dotnetEnum: return _module.ImportReference(dotnetEnum.TypeReference);
            case GenericInstance instance: return MapInstance(instance, location);
        }

        if (ReferenceEquals(structure, std.Bool)) return Core("System.Boolean");
        if (ReferenceEquals(structure, std.Char)) return Core("System.Char");
        if (ReferenceEquals(structure, std.String)) return Core("System.String");
        if (ReferenceEquals(structure, std.Void) || ReferenceEquals(structure, std.NoReturn)) return Core("System.Void");

        return Unsupported($"The type '{structure.Name}'", location);
    }

    /// <summary>`i32`, `f64`, `Ref(T)`, `!T`... the pseudotypes and sized numbers of `Std.Types`.</summary>
    private TypeReference MapInstance(GenericInstance instance, SourceLocation? location)
    {
        var std = CompilerStd.Types;
        var definition = instance.Definition;
        var arguments = instance.Arguments;

        if (ReferenceEquals(definition, std.Integer) && arguments is [IrComptimeBool signed, IrComptimeInt bits])
        {
            var name = (signed.Value, (int)bits.BigInteger) switch
            {
                (true, 8) => "System.SByte",
                (false, 8) => "System.Byte",
                (true, 16) => "System.Int16",
                (false, 16) => "System.UInt16",
                (true, 32) => "System.Int32",
                (false, 32) => "System.UInt32",
                (true, 64) => "System.Int64",
                (false, 64) => "System.UInt64",
                (true, 128) => "System.Int128",
                (false, 128) => "System.UInt128",
                _ => null,
            };
            return name != null ? Core(name) : Unsupported($"The type '{instance.Name}'", location);
        }

        // `int` and `uint` are 32 bits and `iptr` and `uptr` the size of a pointer, when compiling to .NET.
        if (ReferenceEquals(definition, std.NaturalInteger) && arguments is [IrComptimeBool natural])
            return Core(natural.Value ? "System.Int32" : "System.UInt32");

        if (ReferenceEquals(definition, std.NativeInteger) && arguments is [IrComptimeBool native])
            return Core(native.Value ? "System.IntPtr" : "System.UIntPtr");

        if (ReferenceEquals(definition, std.Float) && arguments is [IrComptimeInt floatBits])
        {
            return (int)floatBits.BigInteger switch
            {
                16 => Core("System.Half"),
                32 => Core("System.Single"),
                64 => Core("System.Double"),
                _ => Unsupported($"The type '{instance.Name}'", location),
            };
        }

        if (arguments is not [var only]) return Unsupported($"The type '{instance.Name}'", location);

        // `const T` is a `T` that is not assigned.
        if (ReferenceEquals(definition, std.Const)) return MapExpression(only);

        // `*T`: a class is already a reference; a value is a managed pointer, which only parameters and results can be.
        if (ReferenceEquals(definition, std.Ref))
        {
            var referred = MapExpression(only);
            return referred.IsValueType ? new ByReferenceType(referred) : referred;
        }

        // `?T`: a value needs a wrapper; a reference can be null already.
        if (ReferenceEquals(definition, std.Nullable))
        {
            var value = MapExpression(only);
            if (!value.IsValueType) return value;

            var nullable = new GenericInstanceType(Core("System.Nullable`1"));
            nullable.GenericArguments.Add(value);
            return nullable;
        }

        // `!T`: `Tq.Failable<T>`, or `Tq.Failable` when there is no result.
        if (ReferenceEquals(definition, std.Failable))
        {
            var result = MapExpression(only);
            if (result.FullName == "System.Void") return _module.ImportReference(_backend.Runtime.Failable);

            var failable = new GenericInstanceType(_module.ImportReference(_backend.Runtime.FailableOf));
            failable.GenericArguments.Add(result);
            return failable;
        }

        return Unsupported($"The type '{instance.Name}'", location);
    }

    /// <summary>The value of a literal as the constant of a field of this type; false if it is not one.</summary>
    private static bool TryConstant(IrExpression? value, TypeReference type, out object? constant)
    {
        constant = null;

        try
        {
            switch (value)
            {
                case IrComptimeBool boolean when type.FullName == "System.Boolean":
                    constant = boolean.Value;
                    return true;

                case IrStringConstant text when type.FullName == "System.String":
                    constant = text.Value;
                    return true;

                case IrComptimeInt integer:
                    constant = ConvertNumber(integer.BigInteger, type.FullName);
                    return constant != null;

                case IrComptimeDecimal number:
                    constant = ConvertNumber(number.Value, type.FullName);
                    return constant != null && type.FullName is "System.Single" or "System.Double";

                default:
                    return false;
            }
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    private static object? ConvertNumber(decimal value, string typeName) => typeName switch
    {
        "System.SByte" => Convert.ToSByte(value),
        "System.Byte" => Convert.ToByte(value),
        "System.Int16" => Convert.ToInt16(value),
        "System.UInt16" => Convert.ToUInt16(value),
        "System.Int32" => Convert.ToInt32(value),
        "System.UInt32" => Convert.ToUInt32(value),
        "System.Int64" => Convert.ToInt64(value),
        "System.UInt64" => Convert.ToUInt64(value),
        "System.Single" => Convert.ToSingle(value),
        "System.Double" => Convert.ToDouble(value),
        _ => null,
    };
}
