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

    /// <summary>`Tq.Slice&lt;T&gt;`: what `[]T` is.</summary>
    public TypeDefinition Slice { get; }

    /// <summary>`Tq.UnsafeOps`: what `Std.Memory.Unsafe` needs more than instructions (`sliceFromPtr`, `alignOf`).</summary>
    public TypeDefinition UnsafeOps { get; }

    /// <summary>`UnsafeOps.At&lt;T&gt;(T*, int, int)`: the `ref T` of an element of memory whose length is known.</summary>
    public MethodDefinition UnsafeAt { get; }
    
    // The methods of `TqFormatter` that give the text of a value of an interpolated string.
    public MethodDefinition FormatterFormatObject { get; }
    public MethodDefinition FormatterFormatBool { get; }
    public MethodDefinition FormatterFormatNullableBool { get; }
    public MethodDefinition FormatterFormatArray { get; }
    public MethodDefinition FormatterFormatValue { get; }

    /// <summary>`Format&lt;T&gt;` of the slices, by the generic type it takes (`System.Memory`1`...).</summary>
    public Dictionary<string, MethodDefinition> FormatterMethods { get; } = [];

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
        Slice = Require("Slice`1");
        Ops = Require("Ops");
        UnsafeOps = Require("UnsafeOps");
        UnsafeAt = UnsafeOps.Methods.FirstOrDefault(method => method.Name == "At" && method.IsStatic && method.Parameters.Count == 3)
                   ?? throw new BackendError($"The method '{Namespace}.UnsafeOps.At' was not found in '{path}'");

        var formatter = Require("TqFormatter");

        FormatterFormatObject = Formatter("Format", "System.Object");
        FormatterFormatBool = Formatter("Format", "System.Boolean");
        FormatterFormatNullableBool = Formatter("Format", "System.Nullable`1<System.Boolean>");
        FormatterFormatArray = Formatter("Format", "T[]");
        FormatterFormatValue = Formatter("FormatValue", "T");

        foreach (var method in formatter.Methods)
        {
            if (method is { Name: "Format", IsStatic: true, Parameters.Count: 1 }
                && method.Parameters[0].ParameterType is GenericInstanceType { HasGenericArguments: true } parameter
                && parameter.GenericArguments[0].IsGenericParameter)
                FormatterMethods[parameter.ElementType.FullName] = method;
        }
        
        return;
        
        MethodDefinition Formatter(string name, string parameter) =>
            formatter.Methods.FirstOrDefault(method => method.Name == name && method.IsStatic && method.Parameters.Count == 1
                                                       && method.Parameters[0].ParameterType.FullName == parameter)
            ?? throw new BackendError($"The method '{Namespace}.TqFormatter.{name}({parameter})' was not found in '{path}'");

        TypeDefinition Require(string type) =>
            Assembly.MainModule.GetType(Namespace, type)
            ?? throw new BackendError($"The type '{Namespace}.{type}' was not found in '{path}'");
    }
}
