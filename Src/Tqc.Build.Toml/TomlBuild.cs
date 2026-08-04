using Tomlyn;
using Tomlyn.Model;
using Tqc.Build.Model;

namespace Tqc.Build.Toml;

public static class TomlBuild
{
    public static BuildManifest Load(string manifestPath)
    {
        var manifestContent = File.ReadAllText(manifestPath);
        var document = TomlSerializer.Deserialize<TomlTable>(manifestContent)!;
        return ParseManifest(manifestPath, document);
    }
    
    private static BuildManifest ParseManifest(string directory, TomlTable root)
    {
        var outDir = root["out-dir"].ToString() ?? ".tq-out";
        var cacheDir = root["cache-dir"].ToString() ?? ".tq-cache";
        
        ProjectDefinition project = null!;
        if (root.TryGetValue("project", out var projectTableObj) && projectTableObj is TomlTable projectTable)
            project = ParseProject(projectTable);
        
        var manifest = new BuildManifest(directory) {
            OutDirectory   = outDir,
            CacheDirectory = cacheDir,
            Project        = project,
        };
        
        if (root.TryGetValue("module", out var moduleValue))
        {
            if (moduleValue is not TomlTableArray modules)
                throw new Exception("'module' must be an array of tables.");
            foreach (var value in modules) manifest.Modules.Add(ParseModule(value));
        }
        return manifest;
    }

    private static ProjectDefinition ParseProject(TomlTable projectTable)
    {
        var name = projectTable["name"];
        var target = projectTable["target"];
        var root = projectTable["root"];
        
        return new ProjectDefinition
        {
            Name = name?.ToString()!,
            Target = target?.ToString()!,
            Root = root?.ToString()!,
        };
    }

    private static ModuleDefinition ParseModule(TomlTable table)
    {
        var module = new ModuleDefinition
        {
            Name = GetRequiredString(table, "name"),
            Type = GetOptionalString(table, "type"),
        };

        foreach (var (key, value) in table)
        {
            switch (key)
            {
                case "name":
                case "type":
                break;

                case "dependencies": ParseDependencies(value, module.Dependencies); break;

                default: module.ExtraFields[key] = ConvertValue(value); break;
            }
        }

        return module;
    }
    
    private static void ParseDependencies(object value, List<ModuleDependency> output)
    {
        if (value is not TomlArray array) throw new Exception("'dependencies' must be an array.");
        foreach (var item in array)
        {
            if (item is not string text) throw new Exception("Module dependency must be a string.");
            output.Add(ParseDependency(text));
        }
    }
    private static ModuleDependency ParseDependency(string text)
    {
        var parts = text.Split(':', StringSplitOptions.TrimEntries);

        return parts switch
        {
            ["module", var name] =>
                new ModuleReference(name),

            ["dotnet", var name, var version]when Version.TryParse(version, out var parsedVersion) =>
                new DotnetAssemblyReference(name, parsedVersion),

            _ => throw new Exception($"Invalid module dependency '{text}'.")
        };
    }
    
    private static object? ConvertValue(object? value)
    {
        return value switch
        {
            null => null,

            string => value,
            bool   => value,

            int  => value,
            long => value,

            float  => value,
            double => value,

            TomlArray array =>
                array.Select(ConvertValue).ToArray(),

            TomlTable table =>
                table.ToDictionary(
                    x => x.Key,
                    x => ConvertValue(x.Value)),

            _ => throw new Exception($"Unsupported TOML value: {value.GetType()}")
        };
    }
    private static string GetRequiredString(TomlTable table, string key)
    {
        if (!table.TryGetValue(key, out var value)) throw new Exception($"Missing required property '{key}'.");
        if (value is not string text) throw new Exception($"Property '{key}' must be a string.");
        return text;
    }
    private static string? GetOptionalString(TomlTable table, string key)
    {
        if (!table.TryGetValue(key, out var value)) return null;
        if (value is not string text) throw new Exception($"Property '{key}' must be a string.");
        return text;
    }
}
