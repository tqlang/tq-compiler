namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;

public class StringTypeReference(StringEncoding encoding): BuiltInTypeReference
{
    public readonly StringEncoding Encoding = encoding;
    public override Alignment Length => new (0, 2);
    public override Alignment Alignment => new (0, 1);
    public override string ToString() => Encoding is StringEncoding.Utf8 or StringEncoding.Undefined
        ? "string"
        : $"string({Encoding}";
}

public enum StringEncoding
{
    Undefined,
    Ascii,
    Utf8,
    Utf16,
    Utf32,
}