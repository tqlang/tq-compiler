using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Dotnet;

namespace Abstract.CodeProcess.Core.EvaluationData;

public sealed class ProgramObject(AssemblyResolver asmResolver, ModuleObject[] modules, NamespaceObject[] nmsps)
{
    public readonly AssemblyResolver AssemblyResolver = asmResolver;
    public readonly ModuleObject[] Modules = modules;
    public readonly NamespaceObject[] Namespaces = nmsps;
}
