using System.Text;
using Tqc.Logging.LoggerScopes;

namespace Tqc.Logging;

public static class Logger
{
    private static List<LoggerScope> _scopes = [];
    private static StringBuilder _stringBuilderBuffer = new ();
    
    private static void Update()
    {
        _stringBuilderBuffer.Clear();
        _stringBuilderBuffer.Append("\e[?25l\e[H");
        foreach (var i in _scopes) i.Dump(_stringBuilderBuffer);
        _stringBuilderBuffer.Append("\e[J");
        Console.WriteLine(_stringBuilderBuffer.ToString());
    }

    public static ILoggerScope CreateScope(string title)
    {
        var newScope = new OpenLoggerScope(title, Update);
        _scopes.Add(newScope);
        return newScope;
    }

    extension(ILoggerScope scope)
    {
        public void Close(string title)
        {
            if (scope is not OpenLoggerScope openScope) return;
            
            var index = _scopes.IndexOf(openScope);
            _scopes[index] = new ClosedLoggerScope(title);
            Update();
        }
    }
}
