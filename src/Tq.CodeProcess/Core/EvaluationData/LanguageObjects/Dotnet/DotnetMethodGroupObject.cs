using System.Text;

namespace Tq.CodeProcess.Core.EvaluationData.LanguageObjects;

public class DotnetMethodGroupObject(string n) : DotnetMemberObject(null!, n)
{
    public readonly List<DotnetMethodObject> Overloads = [];

    public override LangObject? SearchChild(string name, SearchChildMode mode = SearchChildMode.All) => null;
    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var i in Overloads) sb.Append(i);
        return sb.ToString();
    }

    public override string ToSignature() => Name;
}
