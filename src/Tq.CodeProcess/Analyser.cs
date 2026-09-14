using System.Text;
using Abstract.CodeProcess.Core;
using Abstract.CodeProcess.Core.EvaluationData;
using Abstract.CodeProcess.Dotnet;
using AsmResolver.DotNet;
using Tq.CodeProcess.Core.EvaluationData.LanguageObjects;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.AttributeReferences;
using Tq.CodeProcess.Core.Language.Module;

namespace Tq.CodeProcess;

public partial class Analyser(ErrorHandler handler)
{
    private readonly ErrorHandler _errorHandler = handler;
    
    private readonly List<BaseModuleObject> _modules = [];
    private readonly List<TqNamespaceObject> _namespaces = [];
    private readonly Stack<List<AttributeReference>> _onHoldAttributes = [];
    
    private AssemblyResolver _assemblyResolver = null!;
    private readonly List<AssemblyDefinition> _assemblies = [];
    
    public ProgramObject? Analyze(
        string programName,
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

        if (_errorHandler.ErrorCount > 0) return null!;
        
        // Stage 2
        ScanHeadersMetadata();
        
        if (dumpEvaluatedData) DumpEvaluatedData();
        if (dumpGlobalTable) DumpGlobalTable();
        
        if (_errorHandler.ErrorCount > 0) return null!;
        
        // Stage 3
        ScanObjectHeaders();
        ScanObjectBodies();
        
        if (dumpEvaluatedData) DumpEvaluatedData();
        if (dumpGlobalTable) DumpGlobalTable();
        
        if (_errorHandler.ErrorCount > 0) return null!;
        
        // Stage 4
        DoSemanticAnalysis();
        
        if (dumpEvaluatedData) DumpEvaluatedData();
        if (dumpGlobalTable) DumpGlobalTable();

        // Stage 5

        FixMess();
        if (dumpEvaluatedData) DumpEvaluatedData();
        if (dumpGlobalTable) DumpGlobalTable();

        
        if (_errorHandler.ErrorCount > 0) return null!;
        return new ProgramObject(
            programName,
            _assemblyResolver,
            [.. _modules],
            [.. _namespaces]
        );
    }
    
    
    private void DumpGlobalTable()
    {
        var sb = new StringBuilder();

        // Walks the tree from the module roots instead of iterating a
        // flat `_globalReferenceTable`. `.GetType().Name` stands in for
        // the old switch, so this needs no case for Dotnet* types (or
        // any future member kind) to stay generic.
        foreach (var member in _modules.SelectMany(WalkMembers))
        {
            var kind = member.GetType().Name;
            if (member is FieldObject && member.HasFlag(BuiltinAttributes.Static)) kind += "(static)";

            List<string> path = [member.Name];
            for (var p = member.Parent; p != null; p = p.Parent) path.Add(p.Name);
            path.Reverse();

            sb.AppendLine($"{kind}\t{string.Join('.', path)}");
        }
        
        File.WriteAllText(".tq-cache/debug/reftable.txt", sb.ToString());
    }

    private void DumpEvaluatedData()
    {
        var sb = new StringBuilder();

        foreach (var i in _modules)
            sb.AppendLine(i.ToString());
        
        File.WriteAllTextAsync(".abs-cache/debug/eval.txt", sb.ToString());
    }
    
    
}
