namespace Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;

public class TypedefNamedValue(string name) : LangObject(name)
{
    public override string ToString() => $"{Name}";
    public override string ToSignature() => $"{Parent.ToSignature()}.{Name}";
}
