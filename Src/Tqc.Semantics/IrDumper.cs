using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Tqc.Core;
using Tqc.IR;
using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;

namespace Tqc.Semantics;

/// <summary>
/// The IR of a module as text, for debugging, in the style of LLVM IR: the type comes before the value, an operation is
/// a call (`add(a, b)`) and what is not a value is a statement. The control flow stays structured, as the IR is a tree.
///
///   namespace Std.Console {
///       %Std.Console.Health = type class extends %Base {    ; file.tq(3,1)
///           field @Std.Console.Health.value: i32            ; public
///           define public void @Std.Console.Health.change(i32 %new) {
///               i32 store(i32 field(%Std.Console.Health %this, @value), i32 add(i32 %new, i32 1))    ; @6:9
///               void call @Std.Console.write(string %x)    ; @7:9
///               ret void
///           }
///       }
///   }
///
/// - Every expression is written `type operation(arguments)`; the type is the one the type checker gave it, `?` when it has none.
/// - Values: `%name` is a local, a parameter or `this`; `@name` is a function, a field, a property or a namespace; `%Name` is a type.
/// - A constant is `type value` (`i32 5`, `bool true`, `string "text"`, `null`).
/// - Operations: `call`, `new`, `convert`, `store`, `field`, `prop`, `member`, `index`, `array`, `range`, `addr`, `is`, `match`,
///   `concat`, `local` (the declaration of a variable), and one per operator (`add`, `add.wrap`, `add.sat`, `div.floor`, `eq`, `lt`,
///   `not`, `neg`, `try`, `unwrap`...).
/// - A call of a group of overloads that the checker did not resolve is `call UNRESOLVED @name(...)`; a name the binder could
///   not bind is `?name`.
/// - A statement ends with `; @line:column`. Comments start with `;`.
/// </summary>
public sealed class IrDumper
{
    private readonly StringBuilder _out = new();
    private readonly TypeTable _types;
    private readonly List<string> _path = [];
    private int _depth;

    private IrDumper(TypeTable types) => _types = types;

    public static string Dump(TqModule module, string name)
    {
        var dumper = new IrDumper(module.Types);
        dumper.Line($"; module {name}");
        dumper.Namespace(module.RootNamespace);
        return dumper._out.ToString();
    }

    // ---- Output

    private void Line(string text = "") => _out.Append(' ', _depth * 4).AppendLine(text);

    private void Block(string header, Action body, string? comment = null)
    {
        Line($"{header} {{{(comment == null ? "" : $"    ; {comment}")}");
        _depth++;
        body();
        _depth--;
        Line("}");
    }

    private static string Where(SourceLocation location) => $"@{location.span.LineStart}:{location.span.ColStart}";

    private static string WhereFile(SourceLocation location) =>
        $"{location.filePath}({location.span.LineStart},{location.span.ColStart})";

    private string TypeText(IrNode node) => _types.Get(node) is { } type ? TypeFormatter.Format(type) : "?";

    /// <summary>The name of a member with the namespaces and the structs it is in: `Std.Console.Health.change`.</summary>
    private string Qualified(string name) => string.Join(".", _path.Append(name));

    private void Inside(string name, Action body)
    {
        var added = !string.IsNullOrEmpty(name);
        if (added) _path.Add(name);
        try { body(); }
        finally { if (added) _path.RemoveAt(_path.Count - 1); }
    }

    // ---- Members

    private void Namespace(TqNamespace node)
    {
        Block($"namespace {node.Name}", () => Inside(node.Name, () =>
        {
            if (!node.FileScopes.IsDefault) foreach (var scope in node.FileScopes) FileScope(scope);
            foreach (var member in Ordered(node.Members.Values)) Member(member);
            foreach (var child in node.Namespaces.OrderBy(pair => pair.Key, StringComparer.Ordinal)) Namespace(child.Value);
        }));
    }

    private void FileScope(TqFileScope scope)
    {
        Line($"; file {scope.FilePath}");

        foreach (var import in scope.Imports)
        {
            var names = import.Imports == null
                ? ""
                : " { " + string.Join(", ", import.Imports.Select(pair => pair.Key == pair.Value.Item2
                    ? pair.Key
                    : $"{pair.Key} = {pair.Value.Item2}")) + " }";
            Line($"import @{string.Join(".", import.NamespaceReference)}{names}    ; {Where(import.Location)}");
        }

        if (scope.Members.Count > 0)
            Line($"; declares {string.Join(", ", scope.Members.Keys.Order(StringComparer.Ordinal))}");
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
                foreach (var function in group) Function(function, "define");
                break;
            case TqFunction function: Function(function, "define"); break;
            default: Line($"; {member.GetType().Name} {member.Name}"); break;
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
            Line($"!{name}{list}{(unknown ? "    ; UNKNOWN attribute" : "")}");
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
            : "(" + string.Join(", ", structure.Parameters.Select(p => "%" + p.Name)) + ")";
        var extends = structure.Extends != null ? $" extends {TypeExpr(structure.Extends)}" : "";
        if (structure.Implements.Count > 0) extends += " implements " + string.Join(", ", structure.Implements.Select(TypeExpr));
        var (modifier, access) = AccessOf(structure);

        Attributes(structure.Attributes);
        Block($"%{Qualified(structure.Name)}{parameters} = {modifier} type {(flags.Count > 0 ? string.Join(" ", flags) + " " : "")}{extends}".Replace("  ", " ").TrimEnd(), () => Inside(structure.Name, () =>
        {
            foreach (var parameter in structure.Parameters)
                Line($"; type parameter %{parameter.Name}: {TypeExpr(parameter.Type)}");

            foreach (var constructor in structure.Constructors) Function(constructor, "constructor");
            if (structure.Destructor != null) Function(structure.Destructor, "destructor");
            foreach (var member in Ordered(structure.Members.Values)) Member(member);
        }), $"{access} · {WhereFile(structure.SourceLocation)}");
        Line();
    }

    private void Typedef(TqTypedef typedef)
    {
        var (modifier, access) = AccessOf(typedef);
        var underlying = typedef.UnderlyingType != null ? $" {TypeExpr(typedef.UnderlyingType)}" : "";

        Attributes(typedef.Attributes);
        Block($"%{Qualified(typedef.Name)} = {modifier} typedef{underlying}", () => Inside(typedef.Name, () =>
        {
            foreach (var numeric in typedef.NumericCases)
                Line($"case {string.Join(", ", numeric.Values.Select(v => v.BigInteger.ToString(CultureInfo.InvariantCulture)))}    ; {Where(numeric.Location)}");

            foreach (var member in Ordered(typedef.Members.Values)) Member(member);
        }), $"{access} · {WhereFile(typedef.SourceLocation)}");
        Line();
    }

    private void NamedCase(TqNamedCase named)
    {
        Attributes(named.Attributes);
        var value = named.Value != null ? $" = {Expr(named.Value)}" : "";
        Line($"case @{Qualified(named.Name)}{value}    ; {AccessOf(named).Comment} · {Where(named.SourceLocation)}");
    }

    private void Field(TqField field)
    {
        var (modifier, access) = AccessOf(field);
        var type = field.Type != null ? TypeExpr(field.Type) : "?";
        var value = field.Value != null ? $" = {Expr(field.Value)}" : "";

        Attributes(field.Attributes);
        Line($"field {modifier} {type} @{Qualified(field.Name)}{value}    ; {access} · {WhereFile(field.SourceLocation)}");
    }

    private void Property(TqProperty property)
    {
        var (modifier, access) = AccessOf(property);
        var type = property.Type != null ? TypeExpr(property.Type) : "?";
        var comment = $"{access} · backing field: {(property.HasBackingField ? "yes" : "no")}"
                      + $"{(property.UsesFieldKeyword ? " · uses `field`" : "")}"
                      + $" · getter: {(property.HasGetter ? "yes" : "no")} · setter: {(property.HasSetter ? "yes" : "no")}"
                      + $" · accessors resolved: {(property.AccessorsResolved ? "yes" : "no")} · {WhereFile(property.SourceLocation)}";

        Attributes(property.Attributes);
        Block($"property {modifier} {type} @{Qualified(property.Name)}", () =>
        {
            if (property.GetterFunction != null) Function(property.GetterFunction, "get");
            if (property.GetterSource != null) Line($"get {Expr(property.GetterSource)}");
            if (property.Getter != null) Line($"; get resolved to @{Callable(property.Getter)}");

            if (property.SetterFunction != null) Function(property.SetterFunction, "set");
            if (property.SetterSource != null) Line($"set {Expr(property.SetterSource)}");
            if (property.Setter != null) Line($"; set resolved to @{Callable(property.Setter)}");
        }, comment);
    }

    private void Function(TqFunction function, string keyword)
    {
        var flags = new List<string>();
        if (function.IsStatic) flags.Add("static");
        if (function.IsAbstract) flags.Add("abstract");

        var (modifier, access) = AccessOf(function);
        var returns = function.ReturnType != null ? TypeExpr(function.ReturnType) : "void";
        var parameters = string.Join(", ", (function.Parameters ?? []).Select(Parameter));
        var name = keyword is "constructor" or "destructor" ? keyword : function.Name;

        var header = (keyword is "get" or "set"
            ? $"{keyword} {modifier} {returns}({parameters})"
            : $"define {modifier} {string.Join(" ", flags)} {returns} @{Qualified(name)}({parameters})").Replace("  ", " ");

        var comment = $"{(function.CanBeOverridden && !function.IsAbstract ? "overridable · " : "")}"
                      + $"{(function.Owner != null ? $"owner={function.Owner.Name} · " : "")}{access} · {WhereFile(function.SourceLocation)}";

        Attributes(function.Attributes);

        if (function.Body == null)
        {
            Line($"{(header.StartsWith("define ") ? "declare " + header["define ".Length..] : header)}    ; {comment}");
            return;
        }

        Line($"{header} {{    ; {comment}");
        _depth++;
        Statements(function.Body);
        _depth--;
        Line("}");
    }

    private string Parameter(TqParameter parameter)
    {
        var lazy = parameter.Lazy ? "lazy " : "";
        var value = parameter.DefaultValue != null ? $" = {Expr(parameter.DefaultValue)}" : "";
        return $"{lazy}{TypeExpr(parameter.Type)} %{parameter.Name}{value}";
    }

    // ---- References

    private static string Callable(IFunction function) =>
        function switch
        {
            TqFunction { Owner: { } owner } => $"{owner.Name}.{function.Name}",
            IntrinsicMethod intrinsic => $"{intrinsic.Owner.Name}.{function.Name}",
            _ => function.Name,
        };

    /// <summary>What a name is: `%x` a local, a parameter or `this`; `@x` a function, a field or a namespace; `%X` a type.</summary>
    private static string Symbol(Ref reference) => reference switch
    {
        UnsolvedRef unsolved => $"?{unsolved.Value}",
        LocalReference local => $"%{local.Local.name}",
        ParameterReference parameter => $"%{parameter.Parameter.Name}",
        ThisReference => "%this",
        BackingFieldReference => "%field",
        PropertyReference property => $"@{property.Property.Name}",
        FieldReference field => $"@{field.Field.Name}",
        FunctionTypeReference function => $"@{Callable(function.Function)}",
        FunctionGroupTypeReference group => $"@{group.FunctionGroup.Name}",
        NamespaceReference @namespace => $"@{@namespace.Namespace.Name}",
        ProtocolTypeReference protocol => $"%{protocol.Protocol.Name}",
        TypeMetaReference meta => $"%{meta.Type.Name}",
        StructTypeReference structure => $"%{TypeFormatter.Format(structure)}",
        TypedefTypeReference typedef => $"%{typedef.Typedef.Name}",
        SliceTypeRef slice => $"%{TypeFormatter.Format(slice)}",
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
        var at = $"    ; {Where(node.location)}";

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
                Line(@return.ReturnValue == null ? $"ret void{at}" : $"ret {Expr(@return.ReturnValue)}{at}");
                break;

            case IrExpressionStatement statement:
                ExpressionStatement(statement.Expression, at);
                break;

            case IrSizeChoiceStatement choice:
                Line($"size_choice {{{at}");
                _depth++;
                foreach (var variant in choice.Variants)
                {
                    Line($"{variant.Sizes.Describe()}:");
                    Nested(variant.Statement);
                }

                _depth--;
                Line("}");
                break;

            case IrIf @if:
                Line($"if ({Expr(@if.Condition)}) {{{at}");
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

                Line($"while ({string.Join("; ", parts)}) {{{at}");
                Nested(@while.Body);
                Line("}");
                break;
            }

            case IrFor @for:
            {
                var variables = @for.Index != null ? $"{@for.Item.name}, {@for.Index.name}" : @for.Item.name;
                Line($"for ({variables} in {Expr(@for.Collection)}) {{{at}");
                Nested(@for.Body);
                Line("}");
                break;
            }

            case IrBreak:
                Line($"break{at}");
                break;

            case IrDelete @delete:
                Line($"delete {Expr(@delete.Operand)}{at}");
                break;

            case IrExpression expression:
                ExpressionStatement(expression, at);
                break;

            default:
                Line($"; {node.GetType().Name} {Where(node.location)}");
                break;
        }
    }

    /// <summary>An expression used as a statement; a `match` is written with a block for each arm.</summary>
    private void ExpressionStatement(IrExpression expression, string at)
    {
        if (expression is not IrMatch match)
        {
            Line($"{Expr(expression)}{at}");
            return;
        }

        Line($"{TypeText(match)} match({Expr(match.Subject)}) {{{at}");
        _depth++;
        foreach (var arm in match.Arms)
        {
            if (arm.Body is IrExpression value)
            {
                Line($"{ArmHead(arm)} => {Expr(value)}");
                continue;
            }

            Line($"{ArmHead(arm)} => {{");
            Nested(arm.Body);
            Line("}");
        }

        _depth--;
        Line("}");
    }

    private string ArmHead(IrMatchArm arm) => arm.IsDefault
        ? "default"
        : "case " + string.Join(", ", arm.Patterns.Select(Expr))
                  + (arm.Binding != null ? " %" + arm.Binding.name : "");

    private void Nested(IrNode? node)
    {
        _depth++;
        Statements(node);
        _depth--;
    }

    // ---- Expressions

    /// <summary>The name of the operation of an operator: `add`, `add.wrap`, `add.sat`, `div.floor`, `eq`, `lt`...</summary>
    private static readonly Dictionary<BinaryOperator, string> Operators = new()
    {
        [BinaryOperator.Add] = "add", [BinaryOperator.AddWrap] = "add.wrap", [BinaryOperator.AddOnBounds] = "add.sat",
        [BinaryOperator.Sub] = "sub", [BinaryOperator.SubWrap] = "sub.wrap", [BinaryOperator.SubOnBounds] = "sub.sat",
        [BinaryOperator.Mul] = "mul", [BinaryOperator.MulWrap] = "mul.wrap", [BinaryOperator.MulOnBounds] = "mul.sat",
        [BinaryOperator.Div] = "div", [BinaryOperator.DivFloor] = "div.floor", [BinaryOperator.DivCeil] = "div.ceil",
        [BinaryOperator.Rest] = "rem", [BinaryOperator.Pow] = "pow",
        [BinaryOperator.PowWrap] = "pow.wrap", [BinaryOperator.PowOnBounds] = "pow.sat",
        [BinaryOperator.Equal] = "eq", [BinaryOperator.ExactEqual] = "eq.exact",
        [BinaryOperator.NotEqual] = "ne", [BinaryOperator.NotExactEqual] = "ne.exact",
        [BinaryOperator.Less] = "lt", [BinaryOperator.Greater] = "gt",
        [BinaryOperator.LessEqual] = "le", [BinaryOperator.GreaterEqual] = "ge",
        [BinaryOperator.Coalesce] = "coalesce",
        [BinaryOperator.BitAnd] = "and", [BinaryOperator.BitOr] = "or", [BinaryOperator.BitXor] = "xor",
        [BinaryOperator.ShiftLeft] = "shl", [BinaryOperator.ShiftRight] = "shr",
        [BinaryOperator.ShiftLeftWrap] = "shl.wrap", [BinaryOperator.ShiftRightWrap] = "shr.wrap",
    };

    /// <summary>`type operation`: the type the checker gave the expression (`?` when it has none), then what it does.</summary>
    private string Typed(IrNode node, string text) => $"{TypeText(node)} {text}";

    /// <summary>A type written as a type: `%Health`, `i32`, `?i32`, `[]u8`, `[4]i32`.</summary>
    private string TypeExpr(IrNode? node)
    {
        switch (node)
        {
            case null: return "?";
            case IrReference { Reference: var reference and not (LocalReference or ParameterReference or UnsolvedRef) }:
                return reference switch
                {
                    TypeMetaReference { Type: IStruct s } => TypeFormatter.Format(new StructTypeReference(s)),
                    TypeMetaReference meta                => meta.Type.ToString() ?? "?",
                    StructTypeReference structure         => TypeFormatter.Format(structure),
                    _                                     => Symbol(reference),
                };
            case IrIdentifier identifier: return $"?{identifier.identifier}";
            case IrAccess access: return $"{TypeExpr(access.baseExpr)}.{Symbol(access.access.Reference).TrimStart('@', '%')}";
            case IrSliceTypeModifier slice: return $"[]{TypeExpr(slice.subtype)}";
            case IrArrayTypeModifier array: return $"[{Expr(array.Length)}]{TypeExpr(array.subtype)}";
            case IrUnary { Operator: UnaryOperator.Nullable } nullable: return $"?{TypeExpr(nullable.Operand)}";
            default: return Expr(node);
        }
    }

    private string Expr(IrNode? node)
    {
        switch (node)
        {
            case null:
                return "<none>";

            case IrReference reference:
                return Typed(reference, Symbol(reference.Reference));

            case IrIdentifier identifier:
                return Typed(identifier, $"?{identifier.identifier}");

            case IrAccess access:
            {
                var operation = access.access.Reference switch
                {
                    FieldReference => "field",
                    PropertyReference => "prop",
                    _ => "member",
                };
                return Typed(access, $"{operation}({Expr(access.baseExpr)}, {Symbol(access.access.Reference)})");
            }

            case IrCall call:
                return Typed(call, $"call {Callee(call)}({Arguments(call.ArgumentsList)})");

            case IrNew creation:
            {
                var constructor = creation.Constructor == null ? " UNRESOLVED" : "";
                return Typed(creation, $"new{constructor} {TypeExpr(creation.Type)}({Arguments(creation.ArgumentsList)})");
            }

            case IrConvert convert:
                return Typed(convert, $"convert({Expr(convert.Expression)}, {TypeExpr(convert.toType)})");

            case IrAssign assign:
                return Typed(assign, $"store({Expr(assign.Target)}, {Expr(assign.Source)})");

            case IrFunctionDelegate function:
                return Typed(function, $"delegate {TypeFormatter.Format(function.DelegateType)} @{Callable(function.Function)}");

            case IrDelegateChain chain:
                return Typed(chain, $"{(chain.Remove ? "remove" : "combine")}({Expr(chain.Current)}, {Expr(chain.Handler)})");

            case IrSliceTypeModifier or IrArrayTypeModifier:
                return Typed(node, $"type {TypeExpr(node)}");

            case IrMatch match:
                return Typed(match, $"match({Expr(match.Subject)}) {{ " + string.Join(" ", match.Arms.Select(arm =>
                    ArmHead(arm) + " => " + (arm.Body is IrExpression value ? Expr(value) : "{ ... }"))) + " }");

            case IrTypeTest test:
                return Typed(test, $"{(test.SameType ? "is.exact" : "is")}({Expr(test.Value)}, {TypeExpr(test.Type)})");

            case IrRange range:
                return Typed(range, $"range({Expr(range.From)}, {Expr(range.To)})");

            case IrAddressOf addressOf:
                return Typed(addressOf, $"addr({Expr(addressOf.Operand)})");

            case IrBinary binary:
                return Typed(binary, $"{Operators.GetValueOrDefault(binary.Operator, binary.Operator.ToString().ToLowerInvariant())}({Expr(binary.Left)}, {Expr(binary.Right)})");

            case IrUnary unary:
            {
                var name = unary.Operator switch
                {
                    UnaryOperator.Not => "not",
                    UnaryOperator.Negate => "neg",
                    UnaryOperator.Increment => unary.IsPostfix ? "inc.post" : "inc",
                    UnaryOperator.Decrement => unary.IsPostfix ? "dec.post" : "dec",
                    UnaryOperator.Nullable => "nullable",
                    UnaryOperator.Try => "try",
                    UnaryOperator.Ignored => "ignored",
                    UnaryOperator.Unwrap => "unwrap",
                    UnaryOperator.BitNot => "bitnot",
                    _ => unary.Operator.ToString().ToLowerInvariant(),
                };
                return Typed(unary, $"{name}({Expr(unary.Operand)})");
            }

            case IrIndex index:
                return Typed(index, $"index({Expr(index.Target)}{string.Concat(index.Indices.Select(i => ", " + Expr(i)))})");

            case IrArray array:
                return Typed(array, $"array({string.Join(", ", array.Elements.Select(Expr))})");

            case IrComptimeInt integer:
                return Typed(integer, integer.BigInteger.ToString(CultureInfo.InvariantCulture));

            case IrComptimeDecimal number:
                return Typed(number, number.Value.ToString(CultureInfo.InvariantCulture));

            case IrNull nothing:
                return Typed(nothing, "null");

            case IrComptimeBool boolean:
                return Typed(boolean, boolean.Value ? "true" : "false");

            case IrLocal local:
                return Typed(local, $"local(%{local.name}{(local.type != null ? ", " + TypeExpr(local.type) : "")})");

            case IrStringConstant text:
                return Typed(text, $"\"{Escape(text.Value)}\"");

            case IrSizeChoice choice:
                return Typed(choice, $"size_choice({string.Join(", ", choice.Variants.Select(variant => $"{variant.Sizes.Describe()}: {Expr(variant.Expression)}"))})");

            case IrStringConcat concat:
                return Typed(concat, $"concat({string.Join(", ", concat.Parts.Select(Expr))})");

            case IrArgumentsList list:
                return Arguments(list);

            default:
                return $"<{node.GetType().Name}>";
        }
    }

    private string Arguments(IrArgumentsList list) => string.Join(", ", list.arguments.Select(Expr));

    /// <summary>What a call calls: the function the checker chose, a group it did not, or an expression.</summary>
    private string Callee(IrCall call) => ReferenceOf(call.callee) switch
    {
        FunctionTypeReference function => $"@{Callable(function.Function)}",
        FunctionGroupTypeReference group => $"UNRESOLVED @{group.FunctionGroup.Name}",
        _ => Expr(call.callee),
    };

    private static string Escape(string text) => text
        .Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
}
