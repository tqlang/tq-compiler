using Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageObjects.CodeObjects;

namespace Abstract.CodeProcess.Core.Language.EvaluationData;

public class ExecutionContextData(LangObject parent)
{
    private readonly LangObject _parent = parent;
    private readonly List<IRBlock> _blocks = [];
    public IRNode? Last = null;
    
    public LangObject Parent => _parent;
    public ParameterObject[] Parameters => _parent switch
    {
        FunctionObject @func => func.Parameters,
        StructObject @struc => throw new NotImplementedException(),
        FieldObject @field => [],
        _ => []
    };

    public LocalVariableObject[] Locals => _parent switch
    {
        FunctionObject @func => func.Locals,
        StructObject @struc => throw new NotImplementedException(),
        FieldObject @field => [],
        _ => []
    };

    public IRBlock CurrentBlock => _blocks[^1];

    public void PushBlock(IRBlock block) => _blocks.Add(block);
    public void PopBlock() => _blocks.RemoveAt(_blocks.Count - 1);


    public void AppendLocal(params LocalVariableObject[] local)
    {
        switch (_parent)
        {
            case FunctionObject @func: func.AddLocal(local); break;
            default: throw new NotImplementedException();
        }
    }
}
