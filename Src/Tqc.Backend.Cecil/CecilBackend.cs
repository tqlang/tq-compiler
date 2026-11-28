using Mono.Cecil;
using Tqc.Diagnostics;
using Tqc.Dotnet.ProgramMembers;
using Tqc.IR.ProgramMembers;
using Tqc.IR.Targets;
using Tqc.IR.ProgramMembers.Tq;

namespace Tqc.Backend.Cecil;

/// <summary>
/// Compiles each Tq module to an assembly of its own, with Mono.Cecil. The modules are emitted in dependency order
/// with one backend, because the assembly of a module refers to the definitions of the ones before it.
///
/// What a module becomes: a namespace with members is a static class (child namespaces and the types in it are nested in
/// it, its functions and fields are its static members); a struct is a class (`@class`) or a value type; a `typedef` is
/// an enum.
/// What is emitted is the structure: types, fields, properties and the signatures of the functions. The code of a
/// function is not translated yet: its body throws `NotImplementedException` (automatic properties and default
/// constructors are real).
///
/// Usage: <c>AddDotnetModule</c> for each dotnet module, <c>Emit</c> for each Tq module, then <c>Write</c>.
/// Whatever cannot be emitted is reported to the diagnostics.
/// </summary>
public sealed class CecilBackend(DiagnosticBag diagnostics)
{
    /// <summary>What this backend answers about the sizes of the types that depend on the target: `int` is System.Int32, `iptr` is 32 or 64 bits.</summary>
    public static TargetAbi Abi { get; } = new(8, [32], [32, 64]);

    private readonly List<AssemblyDefinition> _dotnetAssemblies = [];
    private readonly List<(AssemblyDefinition Assembly, string Name, bool IsEntry)> _emitted = [];
    private CoreTypes? _core;
    private TqRuntimeAssembly? _runtime;

    internal DiagnosticBag Diagnostics => diagnostics;
    internal CoreTypes Core => _core ??= new CoreTypes(_dotnetAssemblies);
    internal TqRuntimeAssembly Runtime => _runtime ??= new TqRuntimeAssembly();

    /// <summary>What was emitted, by the member of the IR it came from; later modules refer to it.</summary>
    internal Dictionary<object, IMemberDefinition> Definitions { get; } = new(ReferenceEqualityComparer.Instance);

    /// <summary>The constructor a class without constructors gets, and the storage of automatic properties.</summary>
    internal Dictionary<TqStruct, MethodDefinition> DefaultConstructors { get; } = new(ReferenceEqualityComparer.Instance);
    internal Dictionary<TqProperty, FieldDefinition> BackingFields { get; } = new(ReferenceEqualityComparer.Instance);

    // The hierarchy of structs by value (see ModuleEmitter.Hierarchy), found by the full name of the value type.
    /// <summary>The nested interface `Ref` of each struct of a hierarchy, and the names of them all.</summary>
    internal Dictionary<string, TypeDefinition> RefInterfaces { get; } = [];
    internal HashSet<string> RefInterfaceNames { get; } = [];

    /// <summary>`S.Box`, the class that has the value of each struct of a hierarchy.</summary>
    internal Dictionary<string, TypeDefinition> RefBoxes { get; } = [];

    /// <summary>`__base`: the field of a struct that holds the one it extends.</summary>
    internal Dictionary<string, FieldDefinition> BaseFields { get; } = [];

    /// <summary>The `Dispose` of each struct that has a destructor (the one it declares, or the one that destroys its fields).</summary>
    internal Dictionary<TqStruct, MethodDefinition> DisposeMethods { get; } = [];

    /// <summary>property `ref S Value` (`get_Value`) of the interface of the struct.</summary>
    internal Dictionary<string, MethodDefinition> RefGetters { get; } = [];

    /// <summary>The method of the interface of a struct for each of its functions.</summary>
    internal Dictionary<TqFunction, MethodDefinition> RefMethods { get; } = new(ReferenceEqualityComparer.Instance);

    /// <summary>The assemblies of a dotnet module are searched for the base types before `System.Runtime`, which is implicit.</summary>
    public void AddDotnetModule(IModule module)
    {
        if (module is DotnetModule dotnet) _dotnetAssemblies.AddRange(dotnet.Assemblies);
    }

    /// <summary>Emits a module. `isEntry` makes it a program: its `main` is the entry point.</summary>
    public void Emit(IModule module, string name, bool isEntry)
    {
        if (module is not TqModule tqModule) throw new ArgumentException("module is not TqModule", nameof(module));

        try
        {
            var assembly = new ModuleEmitter(this, tqModule, name, isEntry).Emit();
            _emitted.Add((assembly, name, isEntry));
        }
        catch (BackendError error)
        {
            diagnostics.ReportError(error);
        }
    }

    /// <summary>Writes `Name.dll` of each module (and `Tq.Runtime.dll` if it is used) to the directory.</summary>
    public void Write(string directory)
    {
        Directory.CreateDirectory(directory);

        foreach (var (assembly, name, isEntry) in _emitted)
        {
            assembly.Write(Path.Combine(directory, name + ".dll"));
            if (isEntry) File.WriteAllText(Path.Combine(directory, name + ".runtimeconfig.json"), RuntimeConfig);
        }

        _runtime?.Assembly.Write(Path.Combine(directory, TqRuntimeAssembly.Name + ".dll"));
    }

    /// <summary>What `dotnet Name.dll` needs to know to start the program.</summary>
    private const string RuntimeConfig = """
        {
          "runtimeOptions": {
            "tfm": "net10.0",
            "framework": {
              "name": "Microsoft.NETCore.App",
              "version": "11.0.0-rc.3.26522.148"
            }
          }
        }
        """;
}
