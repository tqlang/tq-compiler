namespace Tqc.Diagnostics;

public record Diagnostic(string Message, int Line, int Column);
public class ParseException : Exception { }
