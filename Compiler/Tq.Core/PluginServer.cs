using System.Reflection;
using System.Runtime.Loader;
using Tq.Core.Plugin;

namespace Tq.Core;

public static class PluginServer
{
    public static PluginDescriptor[] Plugins { get; private set; }

    public static void Install()
    {
        List<PluginDescriptor> plugins = [];

        string baseDir = Path.Join(AppContext.BaseDirectory, "plugins");
        string[] assemblyFiles = Directory.GetFiles(baseDir, "Tq.Plugin*.dll");

        foreach (var file in assemblyFiles)
        {   
            var alc = new PluginLoadContext(baseDir);
            Assembly assembly = alc.LoadFromAssemblyPath(file);

            foreach (var i in assembly.DefinedTypes)
            {
                if (!i.IsAssignableTo(typeof(PluginDescriptor))) continue;

                plugins.Add((PluginDescriptor)Activator.CreateInstance(i)!);
                break;
            }

        }

        Console.WriteLine($"Plugins installed: {string.Join(", ", plugins.Select(e => e.Name))}");
        Plugins = [..plugins];
    }
    
    private class PluginLoadContext(string pluginFolder) : AssemblyLoadContext(isCollectible: true)
    {
        override protected Assembly? Load(AssemblyName assemblyName)
        {
            if (assemblyName.Name == typeof(PluginDescriptor).Assembly.GetName().Name) return null;
            
            var assemblyPath = Path.Combine(pluginFolder, $"{assemblyName.Name}.dll");
            if (File.Exists(assemblyPath)) return LoadFromAssemblyPath(assemblyPath);
            
            return null;
        }
    }
}
