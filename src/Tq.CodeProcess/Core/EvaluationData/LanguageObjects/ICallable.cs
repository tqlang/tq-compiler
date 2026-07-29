using Tq.CodeProcess.Core.EvaluationData.IntermediateTree;
using Tq.CodeProcess.Core.EvaluationData.LanguageObjects.CodeObjects;
using Tq.CodeProcess.Core.EvaluationData.LanguageObjects.Containers;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences;

namespace Tq.CodeProcess.Core.EvaluationData.LanguageObjects;

public interface ICallable
{
    public SourceScript Script { get; }
    
    public List<ParameterObject> Parameters { get; }
    public List<LocalVariableObject> Locals { get; }
    public Reference ReturnType { get; }
    
    public bool IsStatic { get; }
    public bool IsGeneric { get; }
    
    public IrBlock? Body { get; set; }

    public void AddParameter(params ParameterObject[] parameter);
    public void AddLocal(params LocalVariableObject[] local);
}
