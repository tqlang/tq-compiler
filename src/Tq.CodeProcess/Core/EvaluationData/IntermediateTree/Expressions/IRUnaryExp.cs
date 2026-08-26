using Abstract.CodeProcess.Core.EvaluationData.IntermediateTree;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Tq.CodeProcess.Core.Language.SyntaxNodes;

namespace Tq.CodeProcess.Core.EvaluationData.IntermediateTree.Expressions;

public class IRUnaryExp(SyntaxNode origin, IRUnaryExp.UnaryOperation op, IrExpression value) : IrExpression(origin)
{
    public override ITypeReference Type => Value.Type;

    public UnaryOperation Operation = op;
    public IrExpression Value = value;
    
    public enum UnaryOperation
    {
        Plus,
        Minus,
        Not,
        
        BitwiseNot,
        
        PreIncrement, PostIncrement,
        PreDecrement, PostDecrement,
    }

    public override string ToString() => Operation switch
    {
        UnaryOperation.Plus => $"+{Value}",
        UnaryOperation.Minus => $"-{Value}",
        UnaryOperation.Not => $"!{Value}",
        
        UnaryOperation.BitwiseNot => $"~{Value}",
        
        UnaryOperation.PreIncrement => $"++{Value}",
        UnaryOperation.PostIncrement => $"{Value}++",
        
        UnaryOperation.PreDecrement => $"--{Value}",
        UnaryOperation.PostDecrement => $"{Value}--",
        
        _ => throw new ArgumentOutOfRangeException()
    };
}