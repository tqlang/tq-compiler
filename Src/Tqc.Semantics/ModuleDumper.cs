using System.Text;
using Tqc.Dotnet.ModuleBuilder;
using Tqc.Dotnet.ProgramMembers;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;

namespace Tqc.Semantics;

/// <summary>
/// The members of the modules that are not written in Tq, as text, for debugging (see <see cref="IrDumper"/> for the
/// ones that are): what the compiler creates for `Std` (the primitive types and `Std.Math.Operator`) and what
/// the .NET assemblies of a module expose. Members are written as the type checker sees them, so a call that is
/// "could not be resolved" can be compared with the signatures it was looked up in. A method has the .NET one as a
/// comment, `// [assembly]Type::name`.
///
///   module System.Runtime (dotnet)
///     namespace System
///       class Console
///         ...
/// </summary>
public sealed class ModuleDumper
{
    private readonly StringBuilder _out = new();
    private int _depth;

    private ModuleDumper() { }

    /// <summary>A module of the compiler or of .NET. Other modules are dumped by <see cref="IrDumper"/>.</summary>
    public static string Dump(IModule module, string name)
    {
        var dumper = new ModuleDumper();
        var kind = module switch
        {
            DotnetModule => "dotnet",
            PrimitiveModule => "primitive, created by the compiler",
            _ => module.GetType().Name,
        };

        dumper.Line($"module {name} ({kind})");
        dumper._depth++;
        dumper.Namespace(module.RootNamespace);
        return dumper._out.ToString();
    }

    private void Line(string text) => _out.Append(' ', _depth * 2).AppendLine(text);

    private void Nested(Action body)
    {
        _depth++;
        body();
        _depth--;
    }

    private void Namespace(INamespace node)
    {
        var keyword = node is DotnetStaticClass ? "static class" : "namespace";
        Line($"{keyword} {node.Name}");

        Nested(() =>
        {
            var members = node.GetAllMembers().OrderBy(member => member.Name, StringComparer.Ordinal).ToArray();

            // Namespaces last, so that the members of a namespace are not lost among its children.
            foreach (var member in members.Where(member => member is not INamespace)) Member(member);
            foreach (var member in members.OfType<INamespace>()) Member(member);
        });
    }

    private void Member(IMember member)
    {
        switch (member)
        {
            case INamespace @namespace:
                Namespace(@namespace);
                break;

            case DotnetDelegate type: Type("delegate", type.Name, type.OwnMembers, null, type); break;
            case DotnetClass type: Type("class", WithParameters(type.Name, type.Parameters), type.OwnMembers, type.BaseType, type, null, type.Implements); break;
            case DotnetStruct type: Type("struct", WithParameters(type.Name, type.Parameters), type.OwnMembers, null, type, null, type.Implements); break;
            case DotnetEnum type: Type("enum", type.Name, type.OwnMembers, null, type); break;
            case DotnetGenericInterface type: Type("interface", WithParameters(type.Name, type.Parameters), type.OwnMembers, null, type, type.ParentInterfaces); break;
            case DotnetInterface type: Type("interface", type.Name, type.OwnMembers, null, type, type.ParentInterfaces); break;

            case PrimitiveStruct primitive:
                var parameters = primitive.Parameters.Count == 0
                    ? ""
                    : "(" + string.Join(", ", primitive.Parameters.Select(p => p.Name)) + ")";
                Line($"primitive struct {primitive.Name}{parameters}");
                break;

            case TqTypedef typedef:
                Line($"typedef {typedef.Name}");
                Nested(() =>
                {
                    foreach (var entry in typedef.Members.Keys.Order(StringComparer.Ordinal)) Line($"case {entry}");
                });
                break;

            case DotnetMethodGroup group:
                foreach (var method in group.Methods) Method(method);
                foreach (var method in group.Extensions) Method(method);
                break;

            case DotnetMethod method: Method(method); break;
            case DotnetProperty property: Property(property); break;
            case DotnetEvent @event: Line($"event {@event.Name}: {Format(@event.Type)}{(@event.IsStatic ? " <static>" : "")}"); break;
            case DotnetField field: Field(field); break;

            default: Line($"{member.GetType().Name} {member.Name}"); break;
        }
    }

    /// <summary>`List(T)`: a generic type is written as it is used in Tq.</summary>
    private static string WithParameters(string name, IReadOnlyList<TqParameter> parameters) =>
        parameters.Count == 0 ? name : $"{name}({string.Join(", ", parameters.Select(parameter => parameter.Name))})";

    private void Type(
        string keyword, string name, IEnumerable<IMember> members, ITypeRef? baseType, IDotnetType type,
        IReadOnlyList<ITypeRef>? parents = null, IReadOnlyList<ITypeRef>? implements = null)
    {
        var header = $"{keyword} {name}";
        if (baseType != null) header += $" extends {Format(baseType)}";
        if (parents is { Count: > 0 }) header += $" extends {string.Join(", ", parents.Select(Format))}";
        if (implements is { Count: > 0 }) header += $" implements {string.Join(", ", implements.Select(Format))}";
        Line($"{header}  // {Cecil(type.TypeReference)}");

        Nested(() =>
        {
            // Overloads of a name stay together, in a stable order.
            foreach (var member in members.OrderBy(member => member.Name, StringComparer.Ordinal)) Member(member);
        });
    }

    private void Method(DotnetMethod method)
    {
        var flags = new List<string>();
        if (method.IsStatic) flags.Add("static");
        if (method.IsExtension) flags.Add("extension");
        if (method.IsAbstract) flags.Add("abstract");
        else if (method.IsVirtual) flags.Add("virtual");

        var parameters = method.TypeParameters.Select(parameter => $"type {parameter.Name}").Concat(
            method.ParameterTypes.Select((type, i) =>
                $"{(i < method.Reference.Parameters.Count ? method.Reference.Parameters[i].Name : "_")}: {Format(type)}"));

        Line($"{(method.IsConstructor ? "constructor" : "func")} {method.Name}({string.Join(", ", parameters)}) -> "
             + $"{(method.ReturnType == null ? "void" : Format(method.ReturnType))}"
             + $"{(flags.Count > 0 ? $" <{string.Join(" ", flags)}>" : "")}"
             + $"  // {Cecil(method.Reference.DeclaringType)}::{method.Reference.Name}");
    }

    private void Property(DotnetProperty property)
    {
        var index = property.IsIndexer ? $"[{string.Join(", ", property.IndexParameterTypes.Select(Format))}]" : "";
        Line($"property {property.Name}{index}: {Format(property.Type)} "
             + $"{{ {(property.Getter != null ? "get; " : "")}{(property.Setter != null ? "set; " : "")}}}");
    }

    private void Field(DotnetField field) => Line(
        $"field {(field.IsStatic ? "static " : "")}{field.Name}: {Format(field.Type)}"
        + (field.IsConstant ? $" = {field.Constant}" : ""));

    private static string Cecil(Mono.Cecil.TypeReference type) => $"[{type.Scope?.Name}]{type.FullName}";

    private static string Format(ITypeRef type) => type switch
    {
        DotnetCharacterTypeRef => "char16",
        UnsupportedTypeRef unsupported => $"UNSUPPORTED({unsupported.Name})",
        _ => TypeFormatter.Format(type),
    };
}
