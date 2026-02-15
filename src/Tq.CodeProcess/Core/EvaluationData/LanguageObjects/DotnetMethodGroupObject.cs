using System.Text;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;

public class DotnetMethodGroupObject(string n) : LangObject(null!, n)
{
    public List<DotnetMethodObject> Overloads = [];

    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var i in Overloads) sb.Append(i);
        return sb.ToString();
    }

    public override string ToSignature() => Name;
}
