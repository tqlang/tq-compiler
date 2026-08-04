namespace Tqc.Build.Model;

public sealed class ProjectDefinition
{
    public required string Name { get; init; }
    public required string Target { get; init; }
    public required string Root { get; init; }
}