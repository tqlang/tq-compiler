namespace Tqc.Backend.Cecil;

/// <summary>
/// The signature of an emitted function: how it was declared, and the name and form it has in the assembly
/// (see ModuleEmitter.Signatures).
/// </summary>
internal sealed class FunctionShape
{
    public required string Name { get; init; }

    /// <summary>The types of the parameters and of the result as Tq knows them (`int` is not `i32`).</summary>
    public required string Tokens { get; init; }
    public required string ReturnToken { get; init; }

    public int Weight { get; init; }
    public int ReturnWeight { get; init; }

    /// <summary>The types of the parameters and of the result as .NET knows them, before any `out`.</summary>
    public required int GenericCount { get; init; }
    public required string[] Parameters { get; init; }
    public required string Return { get; init; }

    public required string EmittedName { get; set; }

    /// <summary>The result is given through a last parameter `out`.</summary>
    public bool Out { get; set; }

    /// <summary>A method of .NET, which is only matched by its types.</summary>
    public bool IsDotnet { get; init; }

    public string ParameterKey => $"{Name}`{GenericCount}({string.Join(",", Parameters)})";

    /// <summary>What no other method of the type may have, once emitted.</summary>
    public string SignatureKey { get; set; } = "";

    /// <param name="outResult">The type given through `out`; null when there is none.</param>
    public static string Key(string emittedName, string parameterKey, string? outResult) =>
        $"{emittedName}|{parameterKey[parameterKey.IndexOf('`')..]}|{outResult}";
}
