using Mono.Cecil;

namespace Tqc.Backend.Cecil;

/// <summary>
/// `Tq.Runtime.dll`. What does not depend on the code being compiled is written in C# (the `Tq.Runtime` project) and
/// built with the compiler, which has it next to itself. A build reads a copy of it, adds the helpers of `Tq.Ops` that
/// its code needs, and writes the result to its output directory. The file of the compiler is never changed.
/// </summary>
internal sealed class TqRuntimeAssembly
{
    public const string Name = "Tq.Runtime";
    public const string Namespace = "Tq";

    public AssemblyDefinition Assembly { get; }
    public TypeDefinition Failable { get; }
    public TypeDefinition FailableOf { get; }

    /// <summary>`TqFormatter.Format(object)`.</summary>
    public MethodDefinition FormatterFormatStatic { get; }

    /// <summary>`Tq.Ops`, with the helpers of the operators; by operator and type.</summary>
    public TypeDefinition Ops { get; }
    public Dictionary<string, MethodDefinition> OpsHelpers { get; } = [];

    public TqRuntimeAssembly()
    {
        var path = Path.Combine(AppContext.BaseDirectory, Name + ".dll");
        if (!File.Exists(path))
            throw new BackendError($"'{Name}.dll' was not found next to the compiler (expected at '{path}')");

        // In memory: the file stays free, and each build starts from it as it was compiled.
        Assembly = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });

        Failable = Require("Failable");
        FailableOf = Require("Failable`1");
        Ops = Require("Ops");

        FormatterFormatStatic = Require("TqFormatter").Methods.FirstOrDefault(method =>
                method is { Name: "Format", IsStatic: true, Parameters.Count: 1 })
            ?? throw new BackendError($"The method '{Namespace}.TqFormatter.Format' was not found in '{path}'");

        TypeDefinition Require(string type) =>
            Assembly.MainModule.GetType(Namespace, type)
            ?? throw new BackendError($"The type '{Namespace}.{type}' was not found in '{path}'");
    }
}
