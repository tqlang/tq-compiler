using Abstract.CodeProcess.Core.EvaluationData.IntermediateTree;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.CodeObjects;

namespace Abstract.CodeProcess.Core.EvaluationData;

public class ExecutionContextData(LangObject parent)
{
    private readonly LangObject _parent = parent;
    private readonly Stack<(int stacklen, IrBlock block)> _stack = [];
    public IrNode? Last = null;
    
    public LangObject Parent => _parent;
    public ParameterObject[] Parameters => _parent switch
    {
        FunctionObject @func => func.Parameters.ToArray(),
        StructObject @struc => throw new NotImplementedException(),
        FieldObject @field => [],
        _ => []
    };
    public List<LocalVariableObject> Locals = [];

    public IrBlock CurrentBlock => _stack.Peek().block;

    public void PushBlock(IrBlock block) => _stack.Push((Locals.Count, block));

    public void PopBlock()
    {
        var stackSize = _stack.Pop().stacklen;
        Locals.RemoveRange(Locals.Count - stackSize, stackSize);
    }


    public void AppendLocal(params LocalVariableObject[] local)
    {
        switch (_parent)
        {
            case FunctionObject @func: func.AddLocal(local); break;
            default: throw new NotImplementedException();
        }
    }
}
