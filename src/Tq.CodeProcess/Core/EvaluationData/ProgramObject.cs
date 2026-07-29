using Tq.CodeProcess.Core.EvaluationData.LanguageObjects;

namespace Tq.CodeProcess.Core.EvaluationData;

public sealed class ProgramObject(
    string name,
    BaseModuleObject[] modules,
    TqNamespaceObject[] nmsps
)
{
    public readonly string Name = name;
    public readonly BaseModuleObject[] Modules = modules;
    public readonly TqNamespaceObject[] Namespaces = nmsps;
}
