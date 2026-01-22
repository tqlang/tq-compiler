using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;

namespace Abstract.CodeProcess.Core.EvaluationData;

public sealed class ProgramObject(ModuleObject[] modules, NamespaceObject[] nmsps)
{
    public readonly ModuleObject[] Modules = modules;
    public readonly NamespaceObject[] Namespaces = nmsps;
}
