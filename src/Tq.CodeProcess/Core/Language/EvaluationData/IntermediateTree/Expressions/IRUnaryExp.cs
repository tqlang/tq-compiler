using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Expressions;

public class IRUnaryExp(SyntaxNode origin, IRUnaryExp.UnaryOperation op, IrExpression value) : IrExpression(origin)
{
    public override TypeReference Type => Value.Type;

    public UnaryOperation Operation = op;
    public IrExpression Value = value;
    
    public enum UnaryOperation
    {
        Plus,
        Minus,
        Not,
        
        Reference,
        
        BitwiseNot,
        
        PreIncrement, PostIncrement,
        PreDecrement, PostDecrement,
    }

    public override string ToString() => Operation switch
    {
        UnaryOperation.Plus => $"+{Value}",
        UnaryOperation.Minus => $"-{Value}",
        UnaryOperation.Not => $"!{Value}",
        
        UnaryOperation.Reference => $"&{Value}",
        UnaryOperation.BitwiseNot => $"~{Value}",
        
        UnaryOperation.PreIncrement => $"++{Value}",
        UnaryOperation.PostIncrement => $"{Value}++",
        
        UnaryOperation.PreDecrement => $"--{Value}",
        UnaryOperation.PostDecrement => $"{Value}--",
        
        _ => throw new ArgumentOutOfRangeException()
    };
}