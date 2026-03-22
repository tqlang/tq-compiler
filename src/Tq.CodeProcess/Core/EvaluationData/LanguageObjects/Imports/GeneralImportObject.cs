using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.Imports;

public class GeneralImportObject(SyntaxNode node, string[] path) : ImportObject
{
    public readonly SyntaxNode Node = node;
    public string[] NamespacePath = path;
    public BaseNamespaceObject NamespaceObject = null!;

    public override string ToString() => NamespaceObject != null!
            ? $"from {string.Join('.', NamespaceObject.Global)} import"
            : $"from {string.Join('.', NamespacePath)} import";

    public override LangObject? SearchReference(string reference)
        => NamespaceObject.SearchChild(reference, SearchChildMode.OnlyStatic);
}
