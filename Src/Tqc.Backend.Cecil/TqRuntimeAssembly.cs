using Mono.Cecil;

namespace Tqc.Backend.Cecil;

/// <summary>
/// `Tq.Runtime`, the assembly that holds the types the language needs and .NET has no equivalent for. It is created
/// when the first module that uses one is emitted, and written with the others.
///
///   struct Tq.Failable        { object Error }               `!void`
///   struct Tq.Failable&lt;T&gt;     { T Value; object Error }  `!T`
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
    }
}
