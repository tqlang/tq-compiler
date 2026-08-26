using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;
using AsmResolver.DotNet.Signatures.Types;
using Tq.CodeProcess.Core.EvaluationData.LanguageObjects;

namespace Tq.CodeProcess.Core.EvaluationData.LanguageReferences;

public class DotnetGenericImplReference(DotnetTypeObject baseRef, GenericInstanceTypeSignature typeSig, ITypeReference[] args) : Reference, ITypeReference
{
    public readonly DotnetTypeObject Reference = baseRef;
    public readonly GenericInstanceTypeSignature Signature = typeSig;
    public readonly ITypeReference[] GenericArguments = args;

    public override ITypeReference Type => new TypeTypeReference(this);
    public bool IsGeneric => false;
    
    public override string ToString() => Reference.Name;
}
