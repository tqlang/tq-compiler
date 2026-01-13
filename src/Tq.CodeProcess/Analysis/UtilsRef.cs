using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.FieldReferences;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.FunctionReferences;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypedefReferences;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences.Builtin;

namespace Abstract.CodeProcess;

public partial class Analyzer
{
    
    private static LanguageReference GetObjectReference(LangObject obj)
    {
        return obj switch
        {
            FunctionObject @f => new SolvedFunctionReference(f),
            FunctionGroupObject @fg => new SolvedFunctionGroupReference(fg),

            StructObject @s => new SolvedStructTypeReference(s),
            TypedefObject @t => new SolvedTypedefTypeReference(t),
            
            FieldObject @v => new SolvedFieldReference(v),
            TypedefNamedValue @i => new SolvedTypedefNamedValueReference(i),

            NamespaceObject @n => new NamespaceReference(n),
            
            _ => throw new NotImplementedException(),
        };
    }
    
    private static bool IsSolved(TypeReference typeref) => IsSolved(typeref, out _);
    private static bool IsSolved(TypeReference typeref, out UnsolvedTypeReference unsolved)
    {
        while (true)
        {
            switch (typeref)
            {
                case UnsolvedTypeReference @unsolv: unsolved = unsolv; return false;
                case SliceTypeReference @slice: typeref = slice.InternalType; continue;
                case ReferenceTypeReference @refe: typeref = refe.InternalType; continue;
                case NullableTypeReference @nullable: typeref = nullable.InternalType; continue;
                
                default: unsolved = null!; return true;
            }
        }
    }
    
}