using System.Text;

namespace Tqc.Logging.LoggerScopes;

internal class ClosedLoggerScope(string Title) : LoggerScopes.LoggerScope
{
    public override void Log(string message) {}
    public override void Err(string message) {}
    public override void Warn(string message) {}
    public override void Dbg(string message) {}

    override internal void Dump(StringBuilder sb)
    {
        sb.AppendLine(Title);
    }
}
