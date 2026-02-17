using System.Text;
using Abstract.CodeProcess.Core.EvaluationData.IntermediateTree;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.CodeObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.Containers;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;
using AsmResolver.DotNet;
using TypeReference = Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.TypeReference;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;

public class DotnetMethodObject(
    string name,
    IMethodDescriptor descriptor, MethodDefinition definition,
    TypeReference? returnType,
    ParameterObject[] parameters)
    : LangObject(null!, name),
    ICallable
{
    public DotnetMethodGroupObject MethodGroup = null!;
    public readonly IMethodDescriptor MethodReference = descriptor;
    public readonly MethodDefinition MethodDefinition = definition;
    
    public bool IsConstructor => MethodDefinition.IsConstructor;
    
    public SourceScript Script => throw new NotImplementedException();
    public List<ParameterObject> Parameters { get; } = parameters.ToList();
    public List<LocalVariableObject> Locals { get; } = [];
    public TypeReference ReturnType { get; } = returnType ?? new VoidTypeReference();
    
    public bool IsStatic => MethodDefinition.IsStatic;
    public bool IsGeneric => false;
    public IrBlock? Body { get => null; set {} }
    
    public void AddParameter(params ParameterObject[] parameter)
    {
        throw new NotImplementedException();
    }

    public void AddLocal(params LocalVariableObject[] local)
    {
        throw new NotImplementedException();
    }
    
    public override string ToString()
    {
        var sb = new StringBuilder();

        if (MethodDefinition.IsConstructor)
            sb.Append($"constructor");
        else
            sb.Append($"method {MethodDefinition.Name}");

        sb.Append('(');
        switch (MethodDefinition.GenericParameters.Count)
        {
            case > 0 when MethodDefinition.Parameters.Count > 0:
            {
                for (var i = 0; i < MethodDefinition.GenericParameters.Count; i++)
                {
                    sb.Append($"type {MethodDefinition.GenericParameters[i]}");
                    sb.Append(", ");
                }
                for (var i = 0; i < MethodDefinition.Parameters.Count; i++)
                {
                    sb.Append($"{MethodDefinition.Parameters[i]}");
                    if (i < MethodDefinition.Parameters.Count - 1) sb.Append(", ");
                }

                break;
            }
            case > 0 when MethodDefinition.Parameters.Count == 0:
            {
                for (var i = 0; i < MethodDefinition.GenericParameters.Count; i++)
                {
                    sb.Append($"type {MethodDefinition.GenericParameters[i]}");
                    if (i < MethodDefinition.GenericParameters.Count - 1) sb.Append(", ");
                }

                break;
            }
            default:
            {
                for (var i = 0; i < MethodDefinition.Parameters.Count; i++)
                {
                    sb.Append($"{MethodDefinition.Parameters[i]}");
                    if (i < MethodDefinition.Parameters.Count - 1) sb.Append(", ");
                }

                break;
            }
        }
        sb.Append(')');

        sb.Append($" {MethodDefinition.Signature!.ReturnType}");
        
        return sb.ToString();
    }

    public override string ToSignature() => MethodDefinition.IsConstructor
        ? "constructor" : $"method {MethodDefinition.Name}";
    
}
