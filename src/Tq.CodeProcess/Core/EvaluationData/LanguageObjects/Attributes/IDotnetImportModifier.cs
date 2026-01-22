namespace Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.Attributes;

public interface IDotnetImportMethodModifier
{
    public DotnetImportMethodData? DotnetImport { get; set; }
}
public interface IDotnetImportTypeModifier
{
    public DotnetImportTypeData? DotnetImport { get; set; }
}


public struct DotnetImportMethodData
{
    public string? AssemblyName { get; set; }
    public string? ClassName { get; set; }
    public string MethodName { get; set; }
}
public struct DotnetImportTypeData
{
    public string AssemblyName { get; set; }
    public string ClassName { get; set; }
}
