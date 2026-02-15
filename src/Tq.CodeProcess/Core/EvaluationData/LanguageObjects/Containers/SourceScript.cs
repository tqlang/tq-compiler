using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.Imports;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.Containers;

public class SourceScript(string path)
{
    public List<ImportObject> Imports { get; } = [];
    
    public readonly string Path = path;
    public override string ToString() => throw new NotImplementedException();
}
