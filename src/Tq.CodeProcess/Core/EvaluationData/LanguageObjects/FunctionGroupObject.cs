using System.Text;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;

public class FunctionGroupObject(string n): LangObject(n)
{
    public readonly List<FunctionObject> Overloads = [];
    
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"; funcgroup {Name}");
        foreach (var i in Overloads) sb.AppendLine(i.ToString());
        return sb.ToString();
    }

    public override string ToSignature() => $"funcgroup {Name}";
}
