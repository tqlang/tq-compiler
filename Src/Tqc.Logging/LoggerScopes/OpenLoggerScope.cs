using System.Text;

namespace Tqc.Logging.LoggerScopes;

internal class OpenLoggerScope(string Title, Action Update) : LoggerScopes.LoggerScope
{
    private RingBuffer<string> lines = new (10);
    private StringBuilder _stringBuilder = new StringBuilder();
    
    public override void Log(string message) => WriteGeneric(LogLevel.Info, message);
    public override void Err(string message) => WriteGeneric(LogLevel.Error, message);
    public override void Warn(string message) => WriteGeneric(LogLevel.Warn, message);
    public override void Dbg(string message) => WriteGeneric(LogLevel.Debug, message);

    private void WriteGeneric(LogLevel level, string message)
    {
        _stringBuilder.Clear();
        _stringBuilder.Append(
            level switch
            {
                LogLevel.Info  => "[INFO ]",
                LogLevel.Error => "[ERROR]",
                LogLevel.Warn  => "[WARN ]",
                LogLevel.Debug => "[DEBUG]",
                _ => throw new ArgumentOutOfRangeException()
            }
        ).Append(' ').Append(message);
        lines.Add(_stringBuilder.ToString());
        
        Update.Invoke();
    }

    override internal void Dump(StringBuilder sb)
    {
        sb.AppendLine(Title);
        foreach (var line in lines)
            sb.Append('\t').AppendLine(line);
    }
}
