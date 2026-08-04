namespace Tqc.Build.ResolutionNodes;

public record NamespaceNode
{
    private List<NamespaceNode> Namespaces = [];
    private List<ScriptNode> Scripts = [];
}
