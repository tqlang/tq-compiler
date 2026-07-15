using Tomlyn;
using Tomlyn.Model;

namespace Tq.Cli.Builder;

public static class ConfigParser
{
    public static BuildSystemConfig Parse(string tomlContent)
    {
        var config = new BuildSystemConfig();
        var table = TomlSerializer.Deserialize<TomlTable>(tomlContent);
        
        if (table.TryGetValue("outPath", out var outPath)) config.OutPath = outPath.ToString()!;
        if (table.TryGetValue("cachePath", out var cachePath)) config.CachePath = cachePath.ToString()!;
        if (table.TryGetValue("verbose", out var verbose)) config.Verbose = (bool)verbose;
        
        if (table.TryGetValue("run", out var runObj) && runObj is TomlTable runTable)
        {
            if (runTable.TryGetValue("path", out var runPath)) 
                config.Run.Path = runPath.ToString()!;
        }
        
        if (table.TryGetValue("module", out var modObj) && modObj is TomlTableArray modArray)
        {
            foreach (TomlTable modTable in modArray)
            {
                var module = new ModuleConfig();

                foreach (var kvp in modTable)
                {
                    switch (kvp.Key)
                    {
                        case "name": module.Name = kvp.Value.ToString()!; break;
                        case "type": module.Type = kvp.Value.ToString(); break;
                        
                        case "references":
                            if (kvp.Value is TomlArray refArray)
                            {
                                foreach (var reference in refArray)
                                {
                                    module.References.Add(reference.ToString()!);
                                }
                            }
                        break;
                        
                        default:
                            module.ExtraFields[kvp.Key] = kvp.Value;
                            break;
                    }
                }
                
                config.Modules.Add(module);
            }
        }

        return config;
    }
}
