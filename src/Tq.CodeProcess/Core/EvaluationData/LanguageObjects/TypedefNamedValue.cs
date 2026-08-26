using Abstract.CodeProcess.Core.EvaluationData.IntermediateTree;
using Tq.CodeProcess.Core.Language.SyntaxNodes;

namespace Tq.CodeProcess.Core.EvaluationData.LanguageObjects;

public class TypedefNamedValue(TypeDefinitionNamedItemNode node, string name) : LangObject(null!, name)
{
    public TypeDefinitionNamedItemNode syntaxNode = node;
    public IrExpression? Value = null!;
    
    public override string ToString() => Value == null ? $"{Name}" : $"{Name} = {Value}";
    public override string ToSignature() => $"{Parent.ToSignature()}.{Name}";
}
