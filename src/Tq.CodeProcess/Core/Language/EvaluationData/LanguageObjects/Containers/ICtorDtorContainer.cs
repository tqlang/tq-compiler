namespace Abstract.CodeProcess.Core.Language.EvaluationData.LanguageObjects.Containers;

public interface ICtorDtorContainer
{
    public List<ConstructorObject> Constructors { get; }
    public List<DestructorObject> Destructors { get; }
}
