using System.Text;

namespace Tqc.Logging.LoggerScopes;

public abstract class LoggerScope : ILoggerScope
{
    public abstract void Log(string message);
    public abstract void Err(string message);
    public abstract void Warn(string message);
    public abstract void Dbg(string message);

    abstract internal void Dump(StringBuilder stringBuilder);
}
