using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Tq.Diagnostics;

namespace Tq.Core;

public sealed class ErrorHandler
{
    private readonly List<CompilationError> _errors = [];
    public int ErrorCount => _errors.Count;
    
    private readonly List<CompilationWarning> _warnings = [];
    public int WarningCount => _warnings.Count;

    [DoesNotReturn]
    public void Throw(CompilationError error)
    {
        RegisterError(error);   
        throw error;
    }
    public void Warn(CompilationWarning warning) => RegisterWarning(warning);
    
    public void RegisterError(CompilationError error) => _errors.Add(error);
    public void RegisterWarning(CompilationWarning warning) => _warnings.Add(warning);
    
    public string DumpToString(bool useColors = false)
    {
        var c0 = useColors ? "\e[0m" : "";
        var cr = useColors ? "\0[31m" : "";
        var cy = useColors ? "\0[32m" : "";
        var cg = useColors ? "\0[90m" : "";
        
        var s = new StringBuilder();

        s.AppendLine($"{cy}/!\\ {WarningCount} warnings:{c0}");
        foreach (var i in _warnings)
        {
            s.AppendLine($"{i.SourceLocation}: {i}");
            #if DEBUG
            foreach (var frame in i.StackTrace.GetFrames())
                s.Append($"\t{cg}{frame}{c0}");
            #endif
        }
        
        s.AppendLine($"{cr}(/) {ErrorCount} errors:{c0}");
        foreach (var i in _errors)
        {
            s.AppendLine($"{i.SourceLocation}: {i}");
            #if DEBUG
            foreach (var frame in new StackTrace(i, 1).GetFrames())
                s.Append($"\t{cg}{frame}{c0}");
            #endif
        }
        
        return s.ToString();
    }
}
