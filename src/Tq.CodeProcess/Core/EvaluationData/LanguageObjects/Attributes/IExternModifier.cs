namespace Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.Attributes;

public interface IExternModifier
{
    public (string nmsp, string name)? Extern { get; set; }
}
