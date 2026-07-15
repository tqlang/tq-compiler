namespace Tq.Cli;

using System.Collections.Generic;

public class BuildSystemConfig
{
    public string OutPath { get; set; } = string.Empty;
    public string CachePath { get; set; } = string.Empty;
    public bool Verbose { get; set; }
    
    public RunConfig Run { get; set; } = new();
    public List<ModuleConfig> Modules { get; set; } = new();
}

public class RunConfig
{
    public string Path { get; set; } = string.Empty;
}

public class ModuleConfig
{
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public List<string> References { get; set; } = new();
    
    public Dictionary<string, object> ExtraFields { get; set; } = new();
}