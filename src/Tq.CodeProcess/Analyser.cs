using System.Text;
using Abstract.CodeProcess.Core;
using Abstract.CodeProcess.Core.EvaluationData;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.AttributeReferences;
using Abstract.CodeProcess.Core.Language.Module;
using Abstract.CodeProcess.Dotnet;

namespace Abstract.CodeProcess;

public partial class Analyser(ErrorHandler handler)
{
    private readonly ErrorHandler _errorHandler = handler;

    private readonly List<ModuleObject> _modules = [];
    private readonly List<NamespaceObject> _namespaces = [];
    private readonly Dictionary<string[], LangObject> _globalReferenceTable = new(new IdentifierComparer());
    private readonly Stack<List<AttributeReference>> _onHoldAttributes = [];
    private AssemblyResolver _assemblyResolver = null!;
    
    public ProgramObject Analyze(
        Module[] modules,
        string[] includes,
        bool dumpGlobalTable = false,
        bool dumpEvaluatedData = false)
    {
        // Setting up
        _assemblyResolver = new AssemblyResolver(new Version(10, 0,0 ,0));
        
        // Stage 1
        SearchReferences(modules, includes);
        
        if (dumpEvaluatedData) DumpEvaluatedData();
        if (dumpGlobalTable) DumpGlobalTable();
        
        // Stage 2
        ScanHeadersMetadata();
        
        if (dumpEvaluatedData) DumpEvaluatedData();
        if (dumpGlobalTable) DumpGlobalTable();
        
        // Stage 3
        ScanObjectHeaders();
        ScanObjectBodies();
        
        if (dumpEvaluatedData) DumpEvaluatedData();
        if (dumpGlobalTable) DumpGlobalTable();
        
        // Stage 4
        DoSemanticAnalysis();
        
        // Debug shit
        if (dumpEvaluatedData) DumpEvaluatedData();
        if (dumpGlobalTable) DumpGlobalTable();

        return new ProgramObject(_assemblyResolver, [.. _modules], [.. _namespaces]);
    }
    
    
    private void DumpGlobalTable()
    {
        var sb = new StringBuilder();

        foreach (var i in _globalReferenceTable)
        {
            var kind = i.Value switch
            {
                ModuleObject => "Modl",
                NamespaceObject => "Nmsp",
                FunctionGroupObject => "FnGp",
                FunctionObject => "Func",
                StructObject => "Type",
                TypedefObject => "TDef",
                TypedefNamedValue => "DefN",
                FieldObject @fld => fld.Static ? "SFld" : "LFld",
                _ => throw new NotImplementedException()
            };
            sb.AppendLine($"{kind}\t{string.Join('.', i.Key)}");
        }
        
        File.WriteAllText(".abs-cache/debug/reftable.txt", sb.ToString());
    }

    private void DumpEvaluatedData()
    {
        var sb = new StringBuilder();

        foreach (var i in _modules)
            sb.AppendLine(i.ToString());
        
        File.WriteAllTextAsync(".abs-cache/debug/eval.txt", sb.ToString());
    }
    
    
}
