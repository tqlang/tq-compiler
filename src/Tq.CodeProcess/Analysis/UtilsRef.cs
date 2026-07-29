using Tq.CodeProcess.Core.EvaluationData.LanguageObjects;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.FieldReferences;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.FunctionReferences;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.NamespaceReferences;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypedefReferences;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

namespace Tq.CodeProcess;

public partial class Analyser
{
    
    private static Reference GetObjectReference(LangObject obj)
    {
        return obj switch
        {
            FunctionObject @f => new CallableReference(f),
            FunctionGroupObject @fg => new FunctionGroupReference(fg),

            StructObject @s => new StructReference(s),
            TypedefObject @t => new TypedefReference(t),
            
            FieldObject @v => new SolvedFieldReference(v),
            TypedefNamedValue @i => new SolvedTypedefNamedValueReference(i),

            TqNamespaceObject @n => new NamespaceReference(n),
            
            _ => throw new NotImplementedException(),
        };
    }
}
