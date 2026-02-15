using System.Diagnostics.CodeAnalysis;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.Dotnet;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.FieldReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.FunctionReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypedefReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;

namespace Abstract.CodeProcess;

public partial class Analyser
{
    
    private static LanguageReference GetObjectReference(LangObject obj)
    {
        return obj switch
        {
            FunctionObject @f => new SolvedCallableReference(f),
            FunctionGroupObject @fg => new SolvedFunctionGroupReference(fg),

            StructObject @s => new SolvedStructTypeReference(s),
            TypedefObject @t => new SolvedTypedefTypeReference(t),
            
            FieldObject @v => new SolvedFieldReference(v),
            TypedefNamedValue @i => new SolvedTypedefNamedValueReference(i),

            TqNamespaceObject @n => new NamespaceReference(n),
            
            DotnetTypeObject @t => new DotnetTypeReference(t),
            DotnetNamespaceObject @n => new DotnetNamespaceReference(n),
            
            _ => throw new NotImplementedException(),
        };
    }
    
    private static bool IsSolved([NotNullWhen(false)] TypeReference? typeRef) => IsSolved(typeRef, out _);
    private static bool IsSolved(TypeReference? typeRef, out UnsolvedTypeReference unsolved)
    {
        if (typeRef == null)
        {
            unsolved = null!;
            return true;
        }
        while (true)
        {
            switch (typeRef)
            {
                case UnsolvedTypeReference @unsolv: unsolved = unsolv; return false;
                case SliceTypeReference @slice: typeRef = slice.InternalType; continue;
                case ReferenceTypeReference @refe: typeRef = refe.InternalType; continue;
                case NullableTypeReference @nullable: typeRef = nullable.InternalType; continue;
                
                default: unsolved = null!; return true;
            }
        }
    }
    
}