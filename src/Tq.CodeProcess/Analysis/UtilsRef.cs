using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.FieldReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.FunctionReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.NamespaceReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypedefReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Tq.CodeProcess.Core.EvaluationData.LanguageObjects;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences;

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
            
            DotnetTypeObject @t => new DotnetTypeReference(t),
            DotnetFieldObject @f => new DotnetFieldReference(f),
            DotnetNamespaceObject @n => new SolvedNamespaceReference(n),
            DotnetMethodGroupObject @mg => new DotnetMethodGroupReference(mg),
            DotnetStaticClassObject @sc => new SolvedNamespaceReference(sc),
            
            _ => throw new NotImplementedException(),
        };
    }
}