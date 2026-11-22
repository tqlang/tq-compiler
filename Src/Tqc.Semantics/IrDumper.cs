using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Tqc.Core;
using Tqc.IR;
using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;

namespace Tqc.Semantics;

/// <summary>
/// The IR of a module as text, for debugging, written like the language: what was written in Tq reads as Tq, and the
/// comments say what the compiler knows about it.
///
///   namespace Std.Console {
///       public struct Health : Base {            // class · file.tq(3,1)
///           public value: i32;
///           public func change(new: i32) -> void {
///               this.value = (new + 1);          // @6:9
///               write(x /* string */)            // @7:9
///               Console.Write(s) /* -> Write(string) : void */
///           }
///       }
///   }
///
/// - `/* T */` after an expression is the type the type checker gave it (`?` when it has none) and is written on
///   what computes something (calls, operators, conversions, indexing); a local says its type where it is declared.
/// - A call says the overload it chose, `/* -> name(types) : result */`; one that is still a group of overloads says
///   `/* UNRESOLVED call */`.
/// - A name the binder could not bind is written `?name`.
/// - A statement ends with `// @line:column`.
/// </summary>
public sealed class IrDumper
{
    private readonly StringBuilder _out = new();
    private readonly TypeTable _types;
    private int _depth;

    private IrDumper(TypeTable types) => _types = types;

    public static string Dump(TqModule module, string name)
    {
        var dumper = new IrDumper(module.Types);
        dumper.Line($"// module {name}");
        dumper.Namespace(module.RootNamespace);
        return dumper._out.ToString();
    }

    // ---- Output

    private void Line(string text = "") => _out.Append(' ', _depth * 4).AppendLine(text);

    private void Block(string header, Action body, string? comment = null)
    {
        Line($"{header} {{{(comment == null ? "" : $"    // {comment}")}");
        _depth++;
        body();
        _depth--;
        Line("}");
    }

    private static string Where(SourceLocation location) => $"@{location.span.LineStart}:{location.span.ColStart}";

    private static string WhereFile(SourceLocation location) =>
        $"{location.filePath}({location.span.LineStart},{location.span.ColStart})";

    private string TypeText(IrNode node) => _types.Get(node) is { } type ? TypeFormatter.Format(type) : "?";

    // ---- Members

    private void Namespace(TqNamespace node)
    {
        Block($"namespace {node.Name}", () =>
        {
            if (!node.FileScopes.IsDefault) foreach (var scope in node.FileScopes) FileScope(scope);
            foreach (var member in Ordered(node.Members.Values)) Member(member);
            foreach (var child in node.Namespaces.OrderBy(pair => pair.Key, StringComparer.Ordinal)) Namespace(child.Value);
        });
    }

    private void FileScope(TqFileScope scope)
    {
        Line($"// file {scope.FilePath}");

        foreach (var import in scope.Imports)
        {
            var names = import.Imports == null
                ? ""
                : " { " + string.Join(", ", import.Imports.Select(pair => pair.Key == pair.Value.Item2
                    ? pair.Key
                    : $"{pair.Key} = {pair.Value.Item2}")) + " }";
            Line($"import {string.Join(".", import.NamespaceReference)}{names};    // {Where(import.Location)}");
        }

        if (scope.Members.Count > 0)
            Line($"// declares {string.Join(", ", scope.Members.Keys.Order(StringComparer.Ordinal))}");
        Line();
    }

    /// <summary>Members in the order they were written, so that the dump does not depend on hashing.</summary>
    private static IEnumerable<IMember> Ordered(IEnumerable<IMember> members) => members
        .OrderBy(member => LocationOf(member) is { } location ? location.filePath : "", StringComparer.Ordinal)
        .ThenBy(member => LocationOf(member)?.span.LineStart ?? 0)
        .ThenBy(member => LocationOf(member)?.span.ColStart ?? 0)
        .ThenBy(member => member.Name, StringComparer.Ordinal);

    private static SourceLocation? LocationOf(IMember member) => member switch
    {
        TqStruct structure => structure.SourceLocation,
        TqTypedef typedef => typedef.SourceLocation,
        TqNamedCase named => named.SourceLocation,
        TqField field => field.SourceLocation,
        TqProperty property => property.SourceLocation,
        TqFunction function => function.SourceLocation,
        TqFunctionGroup group => group.Functions.FirstOrDefault()?.SourceLocation,
        _ => null,
    };

    private void Member(IMember member)
    {
        switch (member)
        {
            case TqStruct structure: Struct(structure); break;
            case TqTypedef typedef: Typedef(typedef); break;
            case TqNamedCase named: NamedCase(named); break;
            case TqField field: Field(field); break;
            case TqProperty property: Property(property); break;
            case TqFunctionGroup group:
                foreach (var function in group) Function(function, "func");
                break;
            case TqFunction function: Function(function, "func"); break;
            default: Line($"// {member.GetType().Name} {member.Name}"); break;
        }
    }

    /// <summary>`public`, `private`...; the namespace and the allowed and denied members go in the comment.</summary>
    private static (string Modifier, string Comment) AccessOf(ITqMember member)
    {
        var access = member.Access;
        var comment = $"ns={access.Namespace}";
        if (access.Allowed.Length > 0) comment += $" allow=[{string.Join(", ", access.Allowed.Select(m => m.Name))}]";
        if (access.Denied.Length > 0) comment += $" deny=[{string.Join(", ", access.Denied.Select(m => m.Name))}]";
        return (access.Visibility.ToString().ToLowerInvariant(), comment);
    }

    private void Attributes(IReadOnlyList<TqAttribute> attributes)
    {
        foreach (var attribute in attributes)
        {
            var (name, arguments, unknown) = attribute switch
            {
                TqBuiltinAttribute builtin => (builtin.Name, builtin.Arguments, false),
                TqUnknownAttribute other => (other.Name, other.Arguments, true),
                _ => (attribute.GetType().Name, ImmutableArray<IrExpression>.Empty, false),
            };

            var list = arguments.IsDefaultOrEmpty ? "" : $"({string.Join(", ", arguments.Select(Expr))})";
            Line($"@{name}{list}{(unknown ? "    // UNKNOWN attribute" : "")}");
        }
    }

    private void Struct(TqStruct structure)
    {
        var flags = new List<string>();
        if (structure.IsReference) flags.Add("class");
        if (structure.IsInterface) flags.Add("protocol");
        else if (structure.IsAbstract) flags.Add("abstract");
        if (structure.IsFinal) flags.Add("final");

        var parameters = structure.Parameters.Count == 0
            ? ""
            : "(" + string.Join(", ", structure.Parameters.Select(p => p.Name)) + ")";
        var extends = structure.Extends != null ? $" : {Expr(structure.Extends)}" : "";
        var (modifier, access) = AccessOf(structure);

        Attributes(structure.Attributes);
        Block($"{modifier} struct {structure.Name}{parameters}{extends}", () =>
        {
            foreach (var parameter in structure.Parameters)
                Line($"// type parameter {parameter.Name}: {Expr(parameter.Type)}");

            foreach (var constructor in structure.Constructors) Function(constructor, "constructor");
            if (structure.Destructor != null) Function(structure.Destructor, "destructor");
            foreach (var member in Ordered(structure.Members.Values)) Member(member);
        }, $"{string.Join(" ", flags)}{(flags.Count > 0 ? " · " : "")}{access} · {WhereFile(structure.SourceLocation)}");
        Line();
    }

    private void Typedef(TqTypedef typedef)
    {
        var (modifier, access) = AccessOf(typedef);
        var underlying = typedef.UnderlyingType != null ? $" : {Expr(typedef.UnderlyingType)}" : "";

        Attributes(typedef.Attributes);
        Block($"{modifier} typedef {typedef.Name}{underlying}", () =>
        {
            foreach (var numeric in typedef.NumericCases)
                Line($"case {string.Join(", ", numeric.Values.Select(v => v.BigInteger.ToString(CultureInfo.InvariantCulture)))};    // {Where(numeric.Location)}");

            foreach (var member in Ordered(typedef.Members.Values)) Member(member);
        }, $"{access} · {WhereFile(typedef.SourceLocation)}");
        Line();
    }

    private void NamedCase(TqNamedCase named)
    {
        Attributes(named.Attributes);
        var value = named.Value != null ? $" = {Expr(named.Value)}" : "";
        Line($"case {named.Name}{value};    // {AccessOf(named).Comment} · {Where(named.SourceLocation)}");
    }

    private void Field(TqField field)
    {
        var (modifier, access) = AccessOf(field);
        var type = field.Type != null ? $": {Expr(field.Type)}" : "";
        var value = field.Value != null ? $" = {Expr(field.Value)}" : "";

        Attributes(field.Attributes);
        Line($"{modifier} {field.Name}{type}{value};    // {access} · {WhereFile(field.SourceLocation)}");
    }

    private void Property(TqProperty property)
    {
        var (modifier, access) = AccessOf(property);
        var type = property.Type != null ? $": {Expr(property.Type)}" : "";
        var comment = $"{access} · backing field: {(property.HasBackingField ? "yes" : "no")}"
                      + $"{(property.UsesFieldKeyword ? " · uses `field`" : "")}"
                      + $" · getter: {(property.HasGetter ? "yes" : "no")} · setter: {(property.HasSetter ? "yes" : "no")}"
                      + $" · accessors resolved: {(property.AccessorsResolved ? "yes" : "no")} · {WhereFile(property.SourceLocation)}";

        Attributes(property.Attributes);
        Block($"{modifier} property {property.Name}{type}", () =>
        {
            if (property.GetterFunction != null) Function(property.GetterFunction, "get");
            if (property.GetterSource != null) Line($"get => {Expr(property.GetterSource)};");
            if (property.Getter != null) Line($"// get resolved to {Callable(property.Getter)}");

            if (property.SetterFunction != null) Function(property.SetterFunction, "set");
            if (property.SetterSource != null) Line($"set => {Expr(property.SetterSource)};");
            if (property.Setter != null) Line($"// set resolved to {Callable(property.Setter)}");
        }, comment);
    }

    private void Function(TqFunction function, string keyword)
    {
        var flags = new List<string>();
        if (function.IsStatic) flags.Add("static");
        if (function.IsAbstract) flags.Add("abstract");
        if (function.CanBeOverridden && !function.IsAbstract) flags.Add("overridable");

        var (modifier, access) = AccessOf(function);
        var name = keyword is "get" or "set" or "constructor" or "destructor" ? "" : $" {function.Name}";
        var parameters = string.Join(", ", (function.Parameters ?? []).Select(Parameter));
        var returns = function.ReturnType != null ? $" -> {Expr(function.ReturnType)}" : "";
        var header = $"{modifier} {string.Join(" ", flags.Where(flag => flag != "overridable"))} {keyword}{name}({parameters}){returns}"
            .Replace("  ", " ").Trim();
        var comment = $"{(function.CanBeOverridden && !function.IsAbstract ? "overridable · " : "")}"
                      + $"{(function.Owner != null ? $"owner={function.Owner.Name} · " : "")}{access} · {WhereFile(function.SourceLocation)}";

        Attributes(function.Attributes);

        if (function.Body == null)
        {
            Line($"{header};    // {comment}");
            return;
        }

        Line($"{header} {{    // {comment}");
        _depth++;
        Statements(function.Body);
        _depth--;
        Line("}");
    }

    private string Parameter(TqParameter parameter)
    {
        var lazy = parameter.Lazy ? "lazy " : "";
        var value = parameter.DefaultValue != null ? $" = {Expr(parameter.DefaultValue)}" : "";
        return $"{lazy}{parameter.Name}: {Expr(parameter.Type)}{value}";
    }

    // ---- References

    private static string Callable(IFunction function) =>
        function is TqFunction { Owner: { } owner } ? $"{owner.Name}.{function.Name}" : function.Name;

    /// <summary>The overload a call chose: `Write(string)`, written the way the checker saw its parameters.</summary>
    private static string Signature(IFunction function)
    {
        var signature = FunctionSignature.Of(function);
        var parameters = signature == null
            ? "?"
            : string.Join(", ", signature.Parameters.Select(type => type == null ? "?" : TypeFormatter.Format(type)));
        return $"{Callable(function)}({parameters})";
    }

    /// <summary>A name as it is written; the binder's answer for it is in the comments of the call that uses it.</summary>
    private static string Name(Ref reference) => reference switch
    {
        UnsolvedRef unsolved => $"?{unsolved.Value}",
        LocalReference local => local.Local.name,
        ParameterReference parameter => parameter.Parameter.Name,
        ThisReference => "this",
        BackingFieldReference => "field",
        PropertyReference property => property.Property.Name,
        FieldReference field => field.Field.Name,
        FunctionTypeReference function => function.Function.Name,
        FunctionGroupTypeReference group => group.FunctionGroup.Name,
        NamespaceReference @namespace => @namespace.Namespace.Name,
        ProtocolTypeReference protocol => protocol.Protocol.Name,
        TypeMetaReference meta => meta.Type.Name,
        StructTypeReference structure => TypeFormatter.Format(structure),
        TypedefTypeReference typedef => typedef.Typedef.Name,
        SliceTypeRef slice => TypeFormatter.Format(slice),
        _ => reference.GetType().Name + " " + reference,
    };

    private static Ref? ReferenceOf(IrExpression expression) => expression switch
    {
        IrReference reference => reference.Reference,
        IrAccess access => access.access.Reference,
        _ => null,
    };

    // ---- Statements

    private void Statements(IrNode? node)
    {
        switch (node)
        {
            case null:
                break;

            case IrBody body:
                foreach (var child in body.Children) Statement(child);
                break;

            default:
                Statement(node);
                break;
        }
    }

    private void Statement(IrNode node)
    {
        var at = $"    // {Where(node.location)}";

        switch (node)
        {
            case IrBody body:
                Line($"{{{at}");
                _depth++;
                Statements(body);
                _depth--;
                Line("}");
                break;

            case IrReturnStatement @return:
                Line(@return.ReturnValue == null ? $"return;{at}" : $"return {Expr(@return.ReturnValue)};{at}");
                break;

            case IrExpressionStatement statement:
                Line($"{Expr(statement.Expression)};{at}");
                break;

            case IrIf @if:
                Line($"if {Expr(@if.Condition)} {{{at}");
                Nested(@if.Then);
                if (@if.Else == null)
                {
                    Line("}");
                    break;
                }

                Line("} else {");
                Nested(@if.Else);
                Line("}");
                break;

            case IrWhile @while:
            {
                var parts = new List<string>();
                if (@while.Declaration != null) parts.Add(Expr(@while.Declaration));
                parts.Add(Expr(@while.Condition));
                if (@while.Step != null) parts.Add(Expr(@while.Step));

                Line($"while ({string.Join(" : ", parts)}) {{{at}");
                Nested(@while.Body);
                Line("}");
                break;
            }

            case IrBreak:
                Line($"break;{at}");
                break;

            case IrExpression expression:
                Line($"{Expr(expression)};{at}");
                break;

            default:
                Line($"// {node.GetType().Name} {Where(node.location)}");
                break;
        }
    }

    private void Nested(IrNode? node)
    {
        _depth++;
        Statements(node);
        _depth--;
    }

    // ---- Expressions

    private static readonly Dictionary<BinaryOperator, string> Operators = new()
    {
        [BinaryOperator.Add] = "+", [BinaryOperator.AddWrap] = "+%", [BinaryOperator.AddOnBounds] = "+|",
        [BinaryOperator.Sub] = "-", [BinaryOperator.SubWrap] = "-%", [BinaryOperator.SubOnBounds] = "-|",
        [BinaryOperator.Mul] = "*", [BinaryOperator.MulWrap] = "*%", [BinaryOperator.MulOnBounds] = "*|",
        [BinaryOperator.Div] = "/", [BinaryOperator.DivFloor] = "/_", [BinaryOperator.DivCeil] = "/^",
        [BinaryOperator.Rest] = "%", [BinaryOperator.Pow] = "**",
        [BinaryOperator.PowWrap] = "**%", [BinaryOperator.PowOnBounds] = "**|",
        [BinaryOperator.Equal] = "==", [BinaryOperator.ExactEqual] = "===",
        [BinaryOperator.NotEqual] = "!=", [BinaryOperator.NotExactEqual] = "!==",
        [BinaryOperator.Less] = "<", [BinaryOperator.Greater] = ">",
        [BinaryOperator.LessEqual] = "<=", [BinaryOperator.GreaterEqual] = ">=",
    };

    /// <summary>
    /// `/* what: T */`: what the expression is for the compiler (what a name was bound to, the overload a call chose, the
    /// kind of a literal) and the type the checker gave it; `?` when it has none. Every expression has one.
    /// </summary>
    private string Typed(string text, IrNode node, string? what = null) =>
        $"{text} /* {(what == null ? "" : what + ": ")}{TypeText(node)} */";

    /// <summary>What a name was bound to: `local`, `param`, `field`, `func Owner.name(types)`, `UNSOLVED`...</summary>
    private static string Kind(Ref reference) => reference switch
    {
        UnsolvedRef => "UNSOLVED",
        LocalReference => "local",
        ParameterReference => "param",
        ThisReference self => $"this of {self.Owner.Name}",
        BackingFieldReference backing => $"backing field of {backing.Property.Name}",
        PropertyReference => "property",
        FieldReference => "field",
        FunctionTypeReference function => $"func {Signature(function.Function)}",
        FunctionGroupTypeReference => "group of overloads",
        NamespaceReference => "namespace",
        ProtocolTypeReference => "protocol",
        TypeMetaReference => "type",
        StructTypeReference => "struct type",
        TypedefTypeReference => "typedef",
        SliceTypeRef => "slice type",
        ArrayTypeRef => "array type",
        _ => reference.GetType().Name,
    };

    /// <summary>An operand of an operator: parenthesized when it is one itself.</summary>
    private string Operand(IrExpression expression) => expression is IrBinary or IrConvert or IrAssign or IrUnary
        ? $"({Expr(expression)})"
        : Expr(expression);

    private string Expr(IrNode? node)
    {
        switch (node)
        {
            case null:
                return "<none>";

            case IrReference reference:
                return Typed(Name(reference.Reference), reference, Kind(reference.Reference));

            case IrIdentifier identifier:
                return Typed($"?{identifier.identifier}", identifier, "UNBOUND identifier");

            case IrAccess access:
                return Typed($"{Operand(access.baseExpr)}.{Name(access.access.Reference)}", access, Kind(access.access.Reference));

            case IrCall call:
                return Typed($"{Operand(call.callee)}({Arguments(call.ArgumentsList)})", call, Resolution(call));

            case IrNew creation:
            {
                var constructor = creation.Constructor == null
                    ? "new, UNRESOLVED constructor"
                    : $"new -> {Signature(creation.Constructor)}";
                return Typed($"new {Expr(creation.Type)}({Arguments(creation.ArgumentsList)})", creation, constructor);
            }

            case IrConvert convert:
                return Typed($"{Operand(convert.Expression)} as {Expr(convert.toType)}", convert, "convert");

            case IrAssign assign:
                return Typed($"{Expr(assign.Target)} = {Expr(assign.Source)}", assign, "assign");

            case IrSliceTypeModifier slice:
                return Typed($"[]{Expr(slice.subtype)}", slice, "slice type");

            case IrArrayTypeModifier arrayType:
                return Typed($"[{Expr(arrayType.Length)}]{Expr(arrayType.subtype)}", arrayType, "array type");

            case IrRange range:
                return Typed($"{Expr(range.From)} .. {Expr(range.To)}", range, "range");

            case IrAddressOf addressOf:
                return Typed($"&{Operand(addressOf.Operand)}", addressOf, "address of");

            case IrBinary binary:
                return Typed($"{Operand(binary.Left)} {Operators.GetValueOrDefault(binary.Operator, binary.Operator.ToString())} {Operand(binary.Right)}", binary, $"binary {binary.Operator}");

            case IrUnary unary:
            {
                var symbol = unary.Operator switch
                {
                    UnaryOperator.Not => "!",
                    UnaryOperator.Negate => "-",
                    UnaryOperator.Increment => "++",
                    UnaryOperator.Decrement => "--",
                    UnaryOperator.Nullable => "?",
                    _ => unary.Operator.ToString(),
                };
                var operand = Operand(unary.Operand);
                return Typed(unary.IsPostfix ? $"{operand}{symbol}" : $"{symbol}{operand}", unary,
                    $"unary {unary.Operator}{(unary.IsPostfix ? " postfix" : "")}");
            }

            case IrIndex index:
                return Typed($"{Operand(index.Target)}[{string.Join(", ", index.Indices.Select(Expr))}]", index, "index");

            case IrArray array:
                return Typed($"[{string.Join(", ", array.Elements.Select(Expr))}]", array, "array");

            case IrComptimeInt integer:
                return Typed(integer.BigInteger.ToString(CultureInfo.InvariantCulture), integer, "int constant");

            case IrComptimeDecimal number:
                return Typed(number.Value.ToString(CultureInfo.InvariantCulture), number, "decimal constant");

            case IrNull nothing:
                return Typed("null", nothing, "null constant");

            case IrComptimeBool boolean:
                return Typed(boolean.Value ? "true" : "false", boolean, "bool constant");

            case IrLocal local:
            {
                var declared = local.type != null ? $": {Expr(local.type)}" : "";
                return Typed($"let {local.name}{declared}", local, "local declaration");
            }

            case IrStringConstant text:
                return Typed($"\"{Escape(text.Value)}\"", text, "string constant");

            case IrStringConcat concat:
                return Typed($"concat({string.Join(", ", concat.Parts.Select(Expr))})", concat, "string concat");

            case IrArgumentsList list:
                return Arguments(list);

            default:
                return $"<{node.GetType().Name}>";
        }
    }

    private string Arguments(IrArgumentsList list) => string.Join(", ", list.arguments.Select(Expr));

    /// <summary>The overload the checker chose for a call, or that it did not.</summary>
    private string Resolution(IrCall call) => ReferenceOf(call.callee) switch
    {
        FunctionTypeReference function => $"call -> {Signature(function.Function)}",
        FunctionGroupTypeReference group => $"call, UNRESOLVED: {group.FunctionGroup.Name} is still a group of overloads",
        { } other => $"call of {Kind(other)}",
        _ => "call",
    };

    private static string Escape(string text) => text
        .Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
}
