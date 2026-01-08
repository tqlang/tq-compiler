namespace Abstract.CodeProcess.Core.Language.EvaluationData.LanguageObjects.Attributes;

public interface IDotnetImportModifier
{
    public DotnetImportData? DotnetImport { get; set; }
}

public struct DotnetImportData
{
    public string AssemblyName { get; set; }
    public string ClassName { get; set; }
    public string MethodName { get; set; }
}
