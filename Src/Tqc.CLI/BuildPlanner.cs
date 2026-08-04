using Tqc.Build.Model;

namespace Tqc.CLI;

public sealed class BuildPlanner
{
    public IReadOnlyList<ModuleDefinition> CreateOrder(Dictionary<string, ModuleDefinition> graph, string rootName)
    {
        var result = new List<ModuleDefinition>();
        var visiting = new HashSet<ModuleDefinition>();
        var visited = new HashSet<ModuleDefinition>();

        foreach (var (_, node) in graph)
            Visit(node, visiting, visited, result);

        return result;
    }

    private static void Visit(
        ModuleDefinition node,
        HashSet<ModuleDefinition> visiting,
        HashSet<ModuleDefinition> visited,
        List<ModuleDefinition> result
    )
    {
        if (!visited.Add(node)) return;

        foreach (var dependency in node.Dependencies)
        {
            if (dependency is not ModuleReference moduleReference) continue;
            Visit(moduleReference.Definition, visiting, visited, result);
        }

        result.Add(node);
    }
}