namespace Tq.Core.Plugin;

public abstract class PluginDescriptor
{
    public abstract string Name { get; }
    
    public abstract ModuleParser? ModuleParser { get; }
}
