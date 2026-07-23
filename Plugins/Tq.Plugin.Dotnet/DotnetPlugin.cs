using Tq.Core.Plugin;

namespace Tq.Plugin.Dotnet;

public class DotnetPlugin : PluginDescriptor
{
    public override string Name => "dotnet";
    public override ModuleParser? ModuleParser => new DotnetModuleParser();
}
