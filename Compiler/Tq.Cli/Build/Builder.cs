using System.Collections.Concurrent;
using Tq.Core;
using Tq.Core.Language.Members;
using Tq.Core.Misc;

namespace Tq.Cli.Build;

public static partial class Builder
{
    private delegate BaseModule ParseModuleCall(BuildModuleConfig moduleConfig);
    
    public static void Build(BuildSystemConfig config)
    {
        Dictionary<string, ParseModuleCall> ParseModuleCallTable = [];
        
        foreach (var i in PluginServer.Plugins)
            ParseModuleCallTable.Add(i.Name, i.ModuleParser!.Parse);

        ConcurrentDictionary<string, BaseModule> Modules = [];

        Parallel.ForEach(
            config.Modules,
            m =>
            {
                if (m.Type == null)
                {
                    var module = new TqModule(m.Name);
                    ParseTqModule(module, m);
                    Modules.TryAdd(m.Name, module);
                }
                else if (ParseModuleCallTable.TryGetValue(m.Type, out var parseModuleCall))
                {
                    var module = parseModuleCall(m);
                    Modules.TryAdd(m.Name, module);
                }
                else throw new Exception($"Unknown module type: {m.Type}");
            }
        );
    }
    
}
