using Abstract.CodeProcess.Dotnet;
using Tq.CodeProcess.Core.EvaluationData.LanguageObjects;

namespace Abstract.CodeProcess.Core.EvaluationData;

public sealed class ProgramObject(string name, AssemblyResolver asmResolver, BaseModuleObject[] modules, TqNamespaceObject[] nmsps)
{
    public readonly string Name = name;
    
    public readonly AssemblyResolver AssemblyResolver = asmResolver;
    public readonly BaseModuleObject[] Modules = modules;
    public readonly TqNamespaceObject[] Namespaces = nmsps;
}
