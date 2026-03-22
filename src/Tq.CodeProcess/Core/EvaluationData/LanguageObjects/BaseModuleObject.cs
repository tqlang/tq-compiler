namespace Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;

public abstract class BaseModuleObject(string n) : ContainerObject(null!, n)
{
    public override string ToSignature() => $"module {Name}";
}