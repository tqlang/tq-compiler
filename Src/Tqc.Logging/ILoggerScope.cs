namespace Tqc.Logging;

public interface ILoggerScope
{
    void Log(string message);
    void Err(string message);
    void Warn(string message);
    void Dbg(string message);
}
