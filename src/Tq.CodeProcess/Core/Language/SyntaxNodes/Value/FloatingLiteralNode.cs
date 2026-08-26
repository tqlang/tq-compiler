using System.Globalization;

namespace Tq.CodeProcess.Core.Language.SyntaxNodes;

public class FloatingLiteralNode(Token token) : ValueNode(token)
{

    public double Value => double.Parse(Token.value.ToString(), CultureInfo.InvariantCulture);

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
