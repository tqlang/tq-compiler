namespace Tqc.Build.Model;

public sealed class ModuleDefinition
{
    public required string Name { get; init; }
    public required string? Type { get; init; }

    public Dictionary<string, object?> ExtraFields { get; } = [];
    public List<ModuleDependency> Dependencies { get; } = [];
}