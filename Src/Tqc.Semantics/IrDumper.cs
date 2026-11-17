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
/// The IR of a module as text, for debugging. It is a format of its own (not a language): one node per line, the
/// children indented under it, and every expression ends in `: type`, the type the type checker gave it (`?` when
/// it has none). It holds what the IR holds: members, their attributes and access, the bodies, what each name was
/// bound to, and what the type checker resolved (overloads, constructors, conversions).
///
///   module Name
///     namespace Std.Console
///       struct Health [public ns=Game] @3:1
///         attr @onset
///         field value : i32
///         func change(new: i32) -> void
///           body
///             Assign @6:9 : i32
///               target:
///                 Access @6:9 : i32
///                 ...
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
        dumper.Line($"module {name}");
        dumper._depth++;
        dumper.Namespace(module.RootNamespace);
        return dumper._out.ToString();
    }

    // ---- Output

    private void Line(string text) => _out.Append(' ', _depth * 2).AppendLine(text);

    private void Nested(Action body)
    {
        _depth++;
        body();
        _depth--;
    }

    private void Labeled(string label, Action body)
    {
        Line(label + ":");
        Nested(body);
    }

    private static string Where(SourceLocation location) => $"@{location.span.LineStart}:{location.span.ColStart}";

    private static string WhereFile(SourceLocation location) =>
        $"{location.filePath}({location.span.LineStart},{location.span.ColStart})";

    private string TypeOfNode(IrNode node) => _types.Get(node) is { } type ? TypeFormatter.Format(type) : "?";

    // ---- Members

    private void Namespace(TqNamespace node)
    {
        Line($"namespace {node.Name}");
        Nested(() =>
        {
            if (!node.FileScopes.IsDefault) foreach (var scope in node.FileScopes) FileScope(scope);
            foreach (var member in Ordered(node.Members.Values)) Member(member);
            foreach (var child in node.Namespaces.OrderBy(pair => pair.Key, StringComparer.Ordinal)) Namespace(child.Value);
        });
    }

    private void FileScope(TqFileScope scope)
    {
        Line($"file {scope.FilePath}");
        Nested(() =>
        {
            foreach (var import in scope.Imports)
            {
                var names = import.Imports == null
                    ? ""
                    : " { " + string.Join(", ", import.Imports.Select(pair => pair.Key == pair.Value.Item2
                        ? pair.Key
                        : $"{pair.Key} = {pair.Value.Item2}")) + " }";
                Line($"import {string.Join(".", import.NamespaceReference)}{names} {Where(import.Location)}");
            }

            if (scope.Members.Count > 0)
                Line($"declares {string.Join(", ", scope.Members.Keys.Order(StringComparer.Ordinal))}");
        });
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
            default: Line($"{member.GetType().Name} {member.Name}"); break;
        }
    }

    private static string AccessOf(ITqMember member)
    {
        var access = member.Access;
        var text = $"{access.Visibility.ToString().ToLowerInvariant()} ns={access.Namespace}";
        if (access.Allowed.Length > 0) text += $" allow=[{string.Join(", ", access.Allowed.Select(m => m.Name))}]";
        if (access.Denied.Length > 0) text += $" deny=[{string.Join(", ", access.Denied.Select(m => m.Name))}]";
        return $"[{text}]";
    }

    private void Attributes(IReadOnlyList<TqAttribute> attributes)
    {
        foreach (var attribute in attributes)
        {
            var (name, arguments, kind) = attribute switch
            {
                TqBuiltinAttribute builtin => (builtin.Name, builtin.Arguments, "attr"),
                TqUnknownAttribute unknown => (unknown.Name, unknown.Arguments, "attr?"),
                _ => (attribute.GetType().Name, ImmutableArray<IrExpression>.Empty, "attr"),
            };

            Line($"{kind} @{name}{(arguments.IsDefaultOrEmpty ? "" : $"({arguments.Length})")} {Where(attribute.Location)}");
            if (!arguments.IsDefaultOrEmpty)
                Nested(() => { foreach (var argument in arguments) Node(argument); });
        }
    }

    private void Struct(TqStruct structure)
    {
        var flags = new List<string>();
        if (structure.IsReference) flags.Add("class");
        if (structure.IsInterface) flags.Add("interface");
        else if (structure.IsAbstract) flags.Add("abstract");
        if (structure.IsFinal) flags.Add("final");

        var parameters = structure.Parameters.Count == 0
            ? ""
            : "(" + string.Join(", ", structure.Parameters.Select(p => p.Name)) + ")";

        Line($"struct {structure.Name}{parameters} {AccessOf(structure)}{(flags.Count > 0 ? $" <{string.Join(" ", flags)}>" : "")} {WhereFile(structure.SourceLocation)}");
        Nested(() =>
        {
            Attributes(structure.Attributes);
            Parameters(structure.Parameters, "type parameter");

            if (structure.Extends != null) Labeled("extends", () => Node(structure.Extends));

            foreach (var constructor in structure.Constructors) Function(constructor, "constructor");
            if (structure.Destructor != null) Function(structure.Destructor, "destructor");
            foreach (var member in Ordered(structure.Members.Values)) Member(member);
        });
    }

    private void Typedef(TqTypedef typedef)
    {
        Line($"typedef {typedef.Name} {AccessOf(typedef)} {WhereFile(typedef.SourceLocation)}");
        Nested(() =>
        {
            Attributes(typedef.Attributes);
            if (typedef.UnderlyingType != null) Labeled("underlying", () => Node(typedef.UnderlyingType));

            foreach (var numeric in typedef.NumericCases)
                Line($"case {string.Join(", ", numeric.Values.Select(v => v.BigInteger.ToString(CultureInfo.InvariantCulture)))} {Where(numeric.Location)}");

            foreach (var member in Ordered(typedef.Members.Values)) Member(member);
        });
    }

    private void NamedCase(TqNamedCase named)
    {
        Line($"case {named.Name} {AccessOf(named)} {Where(named.SourceLocation)}");
        Nested(() =>
        {
            Attributes(named.Attributes);
            if (named.Value != null) Labeled("value", () => Node(named.Value));
        });
    }

    private void Field(TqField field)
    {
        Line($"field {field.Name} {AccessOf(field)} {WhereFile(field.SourceLocation)}");
        Nested(() =>
        {
            Attributes(field.Attributes);
            if (field.Type != null) Labeled("type", () => Node(field.Type));
            if (field.Value != null) Labeled("value", () => Node(field.Value));
        });
    }

    private void Property(TqProperty property)
    {
        Line($"property {property.Name} {AccessOf(property)} {WhereFile(property.SourceLocation)}"
             + $" backing={(property.HasBackingField ? "yes" : "no")}{(property.UsesFieldKeyword ? " uses-field" : "")}");
        Nested(() =>
        {
            Attributes(property.Attributes);
            if (property.Type != null) Labeled("type", () => Node(property.Type));

            if (property.GetterFunction != null) Function(property.GetterFunction, "getter");
            if (property.GetterSource != null) Labeled("getter source", () => Node(property.GetterSource));
            if (property.Getter != null) Line($"getter resolved: {Callable(property.Getter)}");

            if (property.SetterFunction != null) Function(property.SetterFunction, "setter");
            if (property.SetterSource != null) Labeled("setter source", () => Node(property.SetterSource));
            if (property.Setter != null) Line($"setter resolved: {Callable(property.Setter)}");
        });
    }

    private void Function(TqFunction function, string keyword)
    {
        var flags = new List<string>();
        if (function.IsStatic) flags.Add("static");
        if (function.IsAbstract) flags.Add("abstract");
        if (function.CanBeOverridden && !function.IsAbstract) flags.Add("overridable");

        Line($"{keyword} {function.Name} {AccessOf(function)}{(flags.Count > 0 ? $" <{string.Join(" ", flags)}>" : "")} {WhereFile(function.SourceLocation)}");
        Nested(() =>
        {
            Attributes(function.Attributes);
            Parameters(function.Parameters ?? [], "param");
            if (function.ReturnType != null) Labeled("returns", () => Node(function.ReturnType));
            if (function.Body != null) Labeled("body", () => Node(function.Body));
        });
    }

    private void Parameters(IEnumerable<TqParameter> parameters, string keyword)
    {
        foreach (var parameter in parameters)
        {
            Line($"{keyword} {parameter.Name}{(parameter.Lazy ? " <lazy>" : "")}");
            Nested(() =>
            {
                Labeled("type", () => Node(parameter.Type));
                if (parameter.DefaultValue != null) Labeled("default", () => Node(parameter.DefaultValue));
            });
        }
    }

    // ---- References

    private static string Callable(IFunction function) =>
        function is TqFunction { Owner: { } owner } ? $"{owner.Name}.{function.Name}" : function.Name;

    private static string Describe(Ref reference) => reference switch
    {
        UnsolvedRef unsolved => $"UNSOLVED '{unsolved.Value}'",
        LocalReference local => $"local {local.Local.name}",
        ParameterReference parameter => $"param {parameter.Parameter.Name}",
        ThisReference self => $"this ({self.Owner.Name})",
        BackingFieldReference backing => $"backing-field ({backing.Property.Name})",
        PropertyReference property => $"property {property.Property.Name}",
        FieldReference field => $"field {field.Field.Name}",
        FunctionTypeReference function => $"func {Callable(function.Function)}",
        FunctionGroupTypeReference group => $"funcgroup {group.FunctionGroup.Name}",
        NamespaceReference @namespace => $"namespace {@namespace.Namespace.Name}",
        ProtocolTypeReference protocol => $"protocol {protocol.Protocol.Name}",
        TypeMetaReference meta => $"type {meta.Type.Name}",
        StructTypeReference structure => $"struct {TypeFormatter.Format(structure)}",
        TypedefTypeReference typedef => $"typedef {typedef.Typedef.Name}",
        SliceTypeRef slice => $"slice {TypeFormatter.Format(slice)}",
        _ => reference.GetType().Name + " " + reference,
    };

    // ---- Statements and expressions

    private void Node(IrNode? node)
    {
        if (node == null)
        {
            Line("<none>");
            return;
        }

        var at = Where(node.location);
        var type = node is IrExpression ? $" : {TypeOfNode(node)}" : "";

        switch (node)
        {
            case IrBody body:
                Line($"Body {at}");
                Nested(() => { foreach (var child in body.Children) Node(child); });
                break;

            case IrReturnStatement @return:
                Line($"Return {at}");
                if (@return.ReturnValue != null) Nested(() => Node(@return.ReturnValue));
                break;

            case IrExpressionStatement statement:
                Line($"ExpressionStatement {at}");
                Nested(() => Node(statement.Expression));
                break;

            case IrIf @if:
                Line($"If {at}");
                Nested(() =>
                {
                    Labeled("condition", () => Node(@if.Condition));
                    Labeled("then", () => Node(@if.Then));
                    if (@if.Else != null) Labeled("else", () => Node(@if.Else));
                });
                break;

            case IrWhile @while:
                Line($"While {at}");
                Nested(() =>
                {
                    if (@while.Declaration != null) Labeled("declaration", () => Node(@while.Declaration));
                    Labeled("condition", () => Node(@while.Condition));
                    if (@while.Step != null) Labeled("step", () => Node(@while.Step));
                    Labeled("body", () => Node(@while.Body));
                });
                break;

            case IrBreak:
                Line($"Break {at}");
                break;

            case IrArgumentsList list:
                Line($"Arguments {at}");
                Nested(() => { foreach (var argument in list.arguments) Node(argument); });
                break;

            case IrAccess access:
                Line($"Access {at}{type}");
                Nested(() =>
                {
                    Labeled("base", () => Node(access.baseExpr));
                    Labeled("member", () => Node(access.access));
                });
                break;

            case IrCall call:
                Line($"Call {at}{type}");
                Nested(() =>
                {
                    Labeled("callee", () => Node(call.callee));
                    Node(call.ArgumentsList);
                });
                break;

            case IrNew creation:
                Line($"New {at}{type} constructor={(creation.Constructor == null ? "-" : Callable(creation.Constructor))}");
                Nested(() =>
                {
                    Labeled("type", () => Node(creation.Type));
                    Node(creation.ArgumentsList);
                });
                break;

            case IrConvert convert:
                Line($"Convert {at}{type}");
                Nested(() =>
                {
                    Labeled("value", () => Node(convert.Expression));
                    Labeled("to", () => Node(convert.toType));
                });
                break;

            case IrAssign assign:
                Line($"Assign {at}{type}");
                Nested(() =>
                {
                    Labeled("target", () => Node(assign.Target));
                    Labeled("source", () => Node(assign.Source));
                });
                break;

            case IrSliceTypeModifier slice:
                Line($"SliceType {at}{type}");
                Nested(() => Node(slice.subtype));
                break;

            case IrBinary binary:
                Line($"Binary {binary.Operator} {at}{type}");
                Nested(() =>
                {
                    Node(binary.Left);
                    Node(binary.Right);
                });
                break;

            case IrUnary unary:
                Line($"Unary {unary.Operator}{(unary.IsPostfix ? " postfix" : "")} {at}{type}");
                Nested(() => Node(unary.Operand));
                break;

            case IrIndex index:
                Line($"Index {at}{type}");
                Nested(() =>
                {
                    Labeled("target", () => Node(index.Target));
                    Labeled("indices", () => { foreach (var i in index.Indices) Node(i); });
                });
                break;

            case IrArray array:
                Line($"Array {at}{type}");
                Nested(() => { foreach (var element in array.Elements) Node(element); });
                break;

            case IrReference reference:
                Line($"Ref {Describe(reference.Reference)} {at}{type}");
                break;

            case IrIdentifier identifier:
                Line($"Identifier '{identifier.identifier}' {at}{type}");
                break;

            case IrComptimeInt integer:
                Line($"Int {integer.BigInteger.ToString(CultureInfo.InvariantCulture)} {at}{type}");
                break;

            case IrComptimeDecimal number:
                Line($"Decimal {number.Value.ToString(CultureInfo.InvariantCulture)} {at}{type}");
                break;

            case IrComptimeBool boolean:
                Line($"Bool {(boolean.Value ? "true" : "false")} {at}{type}");
                break;

            case IrLocal local:
                Line($"Local {local.name} {at}{type}");
                if (local.type != null) Nested(() => Labeled("declared type", () => Node(local.type)));
                break;

            case IrStringConstant text:
                Line($"String \"{Escape(text.Value)}\" {at}{type}");
                break;

            case IrStringConcat concat:
                Line($"StringConcat {at}{type}");
                Nested(() => { foreach (var part in concat.Parts) Node(part); });
                break;

            default:
                Line($"{node.GetType().Name} {at}{type}");
                break;
        }
    }

    private static string Escape(string text) => text
        .Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
}
