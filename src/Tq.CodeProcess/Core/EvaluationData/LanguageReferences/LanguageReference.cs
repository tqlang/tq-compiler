using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

namespace Tq.CodeProcess.Core.EvaluationData.LanguageReferences;

public abstract class Reference
{
    public abstract ITypeReference Type { get; }
    public virtual bool IsSolved => true;
    
}
