namespace Tqc.Build.Model;

public sealed class BuildManifest(string directory)
{
    public readonly string Directory = directory;
    
    public string? OutDirectory { get; init; }
    public string? CacheDirectory { get; init; }

    public ProjectDefinition? Project { get; init; }

    public List<ModuleDefinition> Modules { get; } = [];
}
