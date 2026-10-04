using System.Collections;

namespace Tqc.Diagnostics;

public class DiagnosticBag : IEnumerable<Diagnostic>
{
    private readonly List<Diagnostic> _diagnostics = [];
    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;
    public int Count => _diagnostics.Count;
    
    public void ReportErrorAndPanic(CompilationError error)
    {
        _diagnostics.Add(error);
        throw new Exception("Panic: " + error.Message, error); 
    }
    public void ReportError(CompilationError error) => _diagnostics.Add(error);
    
    public bool HasErrors => _diagnostics.Count > 0;

    public IEnumerator<Diagnostic> GetEnumerator() => Diagnostics.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
