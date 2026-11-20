using Mono.Cecil;
using Tqc.Core;
using Tqc.Dotnet.ProgramMembers;
using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;

namespace Tqc.Backend.Cecil;

internal sealed partial class ModuleEmitter
{
    /// <summary>
    /// A namespace that has members is a static class: its functions and fields are the static members of the class, its
    /// structs and typedefs are types nested in it, and its child namespaces are static classes nested in it too. A
    /// namespace with only child namespaces and nothing of its own, at the top, is no more than a .NET namespace in the
    /// name of what it contains (`Std.Console` is the class `Console` in the .NET namespace `Std`, if `Std` has no members).
    /// </summary>
    /// <param name="node">The namespace.</param>
    /// <param name="name">Its name: the module's, for the root namespace.</param>
    /// <param name="path">The .NET namespace of the namespaces above it that are not classes ("" for the root).</param>
    /// <param name="declaring">The class of the namespace above it, if that is a class.</param>
    private void DeclareNamespace(TqNamespace node, string name, string path, TypeDefinition? declaring)
    {
        var isClass = declaring != null ? HasContent(node) : node.Members.Count > 0;

        if (!isClass)
        {
            // Nothing of its own: only the name is passed on.
            if (declaring != null) return;

            var inner = path.Length == 0 ? name : path + "." + name;
            foreach (var child in node.Namespaces.OrderBy(pair => pair.Key, StringComparer.Ordinal))
                DeclareNamespace(child.Value, child.Key, inner, null);
            return;
        }

        var type = new TypeDefinition(declaring != null ? "" : path, name,
            (declaring != null ? TypeAttributes.NestedPublic : TypeAttributes.Public)
            | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
            Core("System.Object"));
        Add(type, declaring);

        var qualified = path.Length == 0 ? name : path + "." + name;
        List<IMember> statics = [];

        foreach (var member in Ordered(node.Members.Values))
        {
            switch (member)
            {
                case TqStruct structure:
                    DeclareStruct(structure, qualified, type);
                    break;

                case TqTypedef typedef:
                    DeclareTypedef(typedef, qualified, type);
                    break;

                case TqFunctionGroup or TqFunction or TqField:
                    statics.Add(member);
                    break;

                default:
                    Report($"'{member.Name}' ({member.GetType().Name}) cannot be a member of a namespace in .NET yet", LocationOf(member));
                    break;
            }
        }

        _containers.Add(new Container(type, qualified, statics, null));

        foreach (var child in node.Namespaces.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            DeclareNamespace(child.Value, child.Key, qualified, type);
    }

    /// <summary>The namespace, or one inside it, has members.</summary>
    private static bool HasContent(TqNamespace node) =>
        node.Members.Count > 0 || node.Namespaces.Values.Any(HasContent);

    private static TypeAttributes TypeVisibility(MemberAccess access, bool nested) => (IsPublic(access), nested) switch
    {
        (true, false) => TypeAttributes.Public,
        (false, false) => TypeAttributes.NotPublic,
        (true, true) => TypeAttributes.NestedPublic,
        (false, true) => TypeAttributes.NestedAssembly,
    };

    private void Add(TypeDefinition type, TypeDefinition? declaring)
    {
        if (declaring != null) declaring.NestedTypes.Add(type);
        else _module.Types.Add(type);
    }

    private void DeclareStruct(TqStruct structure, string dotted, TypeDefinition? declaring)
    {
        if (structure.Parameters.Count > 0)
        {
            Report($"The generic struct '{structure.Name}' cannot be emitted to .NET yet", structure.SourceLocation);
            return;
        }

        var attributes = TypeVisibility(structure.Access, declaring != null) | TypeAttributes.BeforeFieldInit;
        TypeReference? baseType;

        if (structure.IsInterface)
        {
            attributes |= TypeAttributes.Interface | TypeAttributes.Abstract;
            baseType = null;
        }
        else if (IsClass(structure))
        {
            attributes |= TypeAttributes.Class;
            if (structure.IsAbstract) attributes |= TypeAttributes.Abstract;
            if (structure.IsFinal) attributes |= TypeAttributes.Sealed;
            baseType = Core("System.Object");
        }
        else
        {
            // `sequential` is what .NET does for a struct; `@packed`, `@union` and the layout attributes are not emitted yet.
            attributes |= TypeAttributes.Sealed | TypeAttributes.SequentialLayout;
            baseType = Core("System.ValueType");
        }

        var type = new TypeDefinition(declaring != null ? "" : dotted, structure.Name, attributes, baseType);
        Add(type, declaring);
        _backend.Definitions[structure] = type;
        _containers.Add(new Container(type, dotted, [..structure.Members.Values], structure));

        foreach (var member in Ordered(structure.Members.Values))
        {
            switch (member)
            {
                case TqStruct nested: DeclareStruct(nested, dotted, type); break;
                case TqTypedef nested: DeclareTypedef(nested, dotted, type); break;
            }
        }
    }

    /// <summary>
    /// A typedef is an enum: its named cases are the constants, and its type is the integer it is stored as (`i32` if
    /// it says none). The numeric cases (`case 1, 2, 3`) have no name, so they have no constant. A typedef cannot have
    /// functions or fields of its own in the enum.
    /// </summary>
    private void DeclareTypedef(TqTypedef typedef, string dotted, TypeDefinition? declaring)
    {
        var attributes = TypeVisibility(typedef.Access, declaring != null) | TypeAttributes.Sealed;
        var type = new TypeDefinition(declaring != null ? "" : dotted, typedef.Name, attributes, Core("System.Enum"));
        Add(type, declaring);
        _backend.Definitions[typedef] = type;

        var underlying = typedef.UnderlyingType != null ? MapExpression(typedef.UnderlyingType) : Core("System.Int32");
        if (ConvertNumber(0, underlying.FullName) == null)
        {
            Report($"The typedef '{typedef.Name}' is stored as '{underlying.FullName}', which cannot be the type of an enum",
                typedef.SourceLocation);
            underlying = Core("System.Int32");
        }

        type.Fields.Add(new FieldDefinition("value__",
            FieldAttributes.Public | FieldAttributes.SpecialName | FieldAttributes.RTSpecialName, underlying));

        decimal next = 0;
        foreach (var member in typedef.Members.Values.OrderBy(member => LocationOf(member)?.span.LineStart ?? 0)
                     .ThenBy(member => LocationOf(member)?.span.ColStart ?? 0).ThenBy(member => member.Name, StringComparer.Ordinal))
        {
            if (member is not TqNamedCase named)
            {
                Report($"'{member.Name}': a typedef cannot have functions or fields in .NET yet", LocationOf(member));
                continue;
            }

            decimal value = next;
            if (named.Value != null)
            {
                if (TryInteger(named.Value, out var evaluated)) value = evaluated;
                else Report($"The value of the case '{named.Name}' must be a number or a constant of .NET to be emitted", named.SourceLocation);
            }

            var field = new FieldDefinition(named.Name,
                FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.HasDefault, type);

            if (TryConstant(new IrComptimeInt(named.SourceLocation, (System.Numerics.BigInteger)value), underlying, out var constant))
                field.Constant = constant;
            else
                Report($"The value {value} of the case '{named.Name}' does not fit '{underlying.FullName}'", named.SourceLocation);

            type.Fields.Add(field);
            _backend.Definitions[named] = field;
            next = value + 1;
        }
    }

    /// <summary>
    /// What a struct extends: a struct that is an interface is implemented, any other is the base type. Only a
    /// class extends or implements anything.
    /// </summary>
    private void DeclareBases(Container container)
    {
        if (container.Struct is not { Extends: { } extends } structure) return;

        var type = container.Type;
        var raw = TypeExpressions.Raw(extends);
        if (raw == null)
        {
            Report($"'{structure.Name}' extends something that is not a type", extends.location);
            return;
        }

        // A struct by value extends another one of this module by keeping it in a field (see ModuleEmitter.Hierarchy).
        if (!IsClass(structure) && !structure.IsInterface && BaseOf(structure) != null) return;

        if (!IsClass(structure) && !structure.IsInterface)
        {
            Report($"'{structure.Name}' is a struct by value and cannot extend '{structure.BaseStruct?.Name ?? "that"}' in .NET: only a struct by value of the same module can be extended; otherwise mark it '@class'", structure.SourceLocation);
            return;
        }

        var reference = MapExpression(extends);
        var isInterface = raw is ProtocolTypeReference || raw is StructTypeReference { Struct: IObjectType { IsInterface: true } };

        if (isInterface) type.Interfaces.Add(new InterfaceImplementation(reference));
        else if (!structure.IsInterface) type.BaseType = reference;
        else Report($"The interface '{structure.Name}' can only extend interfaces", structure.SourceLocation);
    }
}
