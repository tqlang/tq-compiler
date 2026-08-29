using Tq.CodeProcess.Core;
using Tq.CodeProcess.Core.EvaluationData;
using Tq.CodeProcess.Core.EvaluationData.ProgramMembers;
using Tq.CodeProcess.Core.Language.Module;

namespace Tq.CodeProcess;

public partial class Analyzer(ErrorHandler errorHandler)
{

    public ProgramObject Analyze(
        string programName,
        TempModule[] modules,
        string[] includes)
    {
        List <Module> finalModules = [];
        foreach (var module in modules) finalModules.Add(InspectModule(module));

        return new ProgramObject
        {
            ProgramName = programName,
            Modules     = [.. finalModules],
        };
    }

    public Module InspectModule(TempModule module)
    {
        // var rootNmsp = new Namespace(module.name);
        // foreach (var i in module.Namespaces)
        // {
        //     var nmsp = new Namespace(i.Identifier[^1]);
        //     foreach (var tree in i.Trees)
        //     {
        //         
        //     }
        // }
        throw new NotImplementedException("Not yet implemented");
    }
    
}
