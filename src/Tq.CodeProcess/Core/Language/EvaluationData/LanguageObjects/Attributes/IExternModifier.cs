namespace Abstract.CodeProcess.Core.Language.EvaluationData.LanguageObjects.Attributes;

public interface IExternModifier
{
    public (string nmsp, string name)? Extern { get; set; }
}
