using Abstract.CodeProcess.Core.EvaluationData.IntermediateTree;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.CodeObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.Containers;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;

public interface ICallable
{
    public SourceScript Script { get; }
    
    public List<ParameterObject> Parameters { get; }
    public List<LocalVariableObject> Locals { get; }
    public TypeReference ReturnType { get; }
    
    public bool IsStatic { get; }
    public bool IsGeneric { get; }
    
    public IrBlock? Body { get; set; }

    public void AddParameter(params ParameterObject[] parameter);
    public void AddLocal(params LocalVariableObject[] local);
}
