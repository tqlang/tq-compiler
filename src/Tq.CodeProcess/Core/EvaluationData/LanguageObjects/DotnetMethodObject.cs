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
    private readonly MethodDefinition _methodDefinition = definition;
    
    public bool IsConstructor => _methodDefinition.IsConstructor;
    
    public SourceScript Script => throw new NotImplementedException();
    public List<ParameterObject> Parameters { get; } = parameters.ToList();
    public List<LocalVariableObject> Locals { get; } = [];
    public TypeReference ReturnType { get; } = returnType ?? new VoidTypeReference();
    
    public bool IsStatic => _methodDefinition.IsStatic;
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

        if (_methodDefinition.IsConstructor)
            sb.Append($"constructor");
        else
            sb.Append($"method {_methodDefinition.Name}");

        sb.Append('(');
        switch (_methodDefinition.GenericParameters.Count)
        {
            case > 0 when _methodDefinition.Parameters.Count > 0:
            {
                for (var i = 0; i < _methodDefinition.GenericParameters.Count; i++)
                {
                    sb.Append($"type {_methodDefinition.GenericParameters[i]}");
                    sb.Append(", ");
                }
                for (var i = 0; i < _methodDefinition.Parameters.Count; i++)
                {
                    sb.Append($"{_methodDefinition.Parameters[i]}");
                    if (i < _methodDefinition.Parameters.Count - 1) sb.Append(", ");
                }

                break;
            }
            case > 0 when _methodDefinition.Parameters.Count == 0:
            {
                for (var i = 0; i < _methodDefinition.GenericParameters.Count; i++)
                {
                    sb.Append($"type {_methodDefinition.GenericParameters[i]}");
                    if (i < _methodDefinition.GenericParameters.Count - 1) sb.Append(", ");
                }

                break;
            }
            default:
            {
                for (var i = 0; i < _methodDefinition.Parameters.Count; i++)
                {
                    sb.Append($"{_methodDefinition.Parameters[i]}");
                    if (i < _methodDefinition.Parameters.Count - 1) sb.Append(", ");
                }

                break;
            }
        }
        sb.Append(')');

        sb.Append($" {_methodDefinition.Signature!.ReturnType}");
        
        return sb.ToString();
    }

    public override string ToSignature() => _methodDefinition.IsConstructor
        ? "constructor" : $"method {_methodDefinition.Name}";
    
}
