namespace Tq.CodeProcess.Core.EvaluationData.LanguageObjects.Containers;

public interface ICtorDtorContainer
{
    public List<ConstructorObject> Constructors { get; }
    public List<DestructorObject> Destructors { get; }
}
