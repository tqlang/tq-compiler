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
    /// A namespace is a .NET namespace with the name of its path. The types in it are types; the functions and fields
    /// (they belong to no type in Tq) are static members of a class `Globals` created for it.
    /// </summary>
    private void DeclareNamespace(TqNamespace node, string dotted)
    {
        List<IMember> globals = [];

        foreach (var member in Ordered(node.Members.Values))
        {
            switch (member)
            {
                case TqStruct structure:
                    DeclareStruct(structure, dotted, null);
                    break;

                case TqTypedef typedef:
                    DeclareTypedef(typedef, dotted, null);
                    break;

                case TqFunctionGroup or TqFunction or TqField:
                    globals.Add(member);
                    break;

                default:
                    Report($"'{member.Name}' ({member.GetType().Name}) cannot be a member of a namespace in .NET yet", LocationOf(member));
                    break;
            }
        }

        if (globals.Count > 0)
        {
            var type = new TypeDefinition(dotted, "Globals",
                TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                Core("System.Object"));
            _module.Types.Add(type);
            _globals[dotted] = type;
            _containers.Add(new Container(type, dotted, globals, null));
        }

        foreach (var child in node.Namespaces.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            DeclareNamespace(child.Value, dotted + "." + child.Key);
    }

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
        else if (structure.IsReference)
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
                if (named.Value is IrComptimeInt literal) value = literal.BigInteger;
                else Report($"The value of the case '{named.Name}' must be a literal to be emitted", named.SourceLocation);
            }

            var field = new FieldDefinition(named.Name,
                FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.HasDefault, type);

            if (TryConstant(new IrComptimeInt(named.SourceLocation, value), underlying, out var constant))
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

        if (!structure.IsReference && !structure.IsInterface)
        {
            Report($"'{structure.Name}' is a struct by value and cannot extend anything in .NET; mark it '@class'", structure.SourceLocation);
            return;
        }

        var reference = MapExpression(extends);
        var isInterface = raw is ProtocolTypeReference || raw is StructTypeReference { Struct: IObjectType { IsInterface: true } };

        if (isInterface) type.Interfaces.Add(new InterfaceImplementation(reference));
        else if (!structure.IsInterface) type.BaseType = reference;
        else Report($"The interface '{structure.Name}' can only extend interfaces", structure.SourceLocation);
    }
}
