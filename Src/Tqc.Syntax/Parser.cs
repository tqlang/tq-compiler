using Tqc.Diagnostics;

namespace Tqc.Syntax;

public class Parser
{
    private Lexer _lexer = null!;

    public SyntaxTree Parse(string Source, DiagnosticBag diagnostics)
    {
        _lexer = new Lexer(Source);
        return new SyntaxTree();
    }
    
    
}
