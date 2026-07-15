using System.Text;

namespace Tq.Ast;

public abstract class SyntaxNode {
    public override string ToString() => AppendStringBuilder(new StringBuilder()).ToString();
    public abstract StringBuilder AppendStringBuilder(StringBuilder sb);
}
