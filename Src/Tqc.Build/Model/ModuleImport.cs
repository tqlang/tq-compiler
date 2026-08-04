namespace Tqc.Build.Model;

public sealed class ModuleImport
{
    public required string ProjectPath { get; init; }
    public required string ModuleName { get; init; }
}
