using System.Collections;

namespace Tqc.Diagnostics;

public class DiagnosticBag : IEnumerable<Diagnostic>
{
    private readonly List<Diagnostic> _diagnostics = new();
    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;
    public int Count => _diagnostics.Count;
    
    public void ReportErrorAndPanic(string message, int line, int column)
    {
        _diagnostics.Add(new Diagnostic(message, line, column));
        throw new ParseException(); 
    }
    
    public void ReportError(string message, int line, int column)
    {
        _diagnostics.Add(new Diagnostic(message, line, column));
    }
    
    public bool HasErrors => _diagnostics.Count > 0;

    public IEnumerator<Diagnostic> GetEnumerator() => Diagnostics.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
