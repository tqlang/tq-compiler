namespace Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.Containers;

public interface IDotnetCtorDtorContainer
{
    public List<DotnetMethodObject> Constructors { get; }
    public DotnetTypeObject? Destructor { get; set; }
}
