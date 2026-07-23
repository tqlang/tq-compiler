using Tq.Core.Misc;

namespace Tq.Cli;

using System.Collections.Generic;

public class BuildSystemConfig
{
    public string OutPath { get; set; } = string.Empty;
    public string CachePath { get; set; } = string.Empty;
    public bool Verbose { get; set; }
    
    public RunConfig Run { get; set; } = new();
    public List<BuildModuleConfig> Modules { get; set; } = new();
}

public class RunConfig
{
    public string Path { get; set; } = string.Empty;
}
