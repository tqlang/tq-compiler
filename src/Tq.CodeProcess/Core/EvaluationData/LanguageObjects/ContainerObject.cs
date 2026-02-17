using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.Containers;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;

public abstract class ContainerObject(SourceScript sourceScript, string name) : LangObject(sourceScript, name)
{
    public abstract override LangObject? SearchChild(string name, SearchChildMode mode);
}
