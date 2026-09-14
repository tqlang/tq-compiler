using System.Text;
using Tq.CodeProcess.Core.EvaluationData.LanguageObjects.Containers;

namespace Tq.CodeProcess.Core.EvaluationData.LanguageObjects;

public class FunctionGroupObject(SourceScript sourceScript, string n): LangObject(sourceScript, n)
{
    public readonly List<FunctionObject> Overloads = [];

    public override LangObject? SearchChild(string name, SearchChildMode mode = SearchChildMode.All) => null;
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"; funcgroup {Name}");
        foreach (var i in Overloads) sb.AppendLine(i.ToString());
        return sb.ToString();
    }

    public override string ToSignature() => $"funcgroup {Name}";
}
