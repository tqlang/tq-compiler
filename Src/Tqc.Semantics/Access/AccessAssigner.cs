using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Tq;

namespace Tqc.Semantics;

/// <summary>
/// Gives each member of a module its visibility, now that the namespace of each one is known.
/// A member with no access attribute is private. The members named by `@allowAcessTo` and `@denyAcessTo`
/// are only known once names are bound, so the reference binder adds them.
/// Members of a struct or typedef belong to the namespace of the type.
/// </summary>
internal static class AccessAssigner
{
    public static void Assign(TqNamespace root) => AssignNamespace(root, root.Name);

    private static void AssignNamespace(TqNamespace namespaceNode, string path)
    {
        foreach (var member in namespaceNode.Members.Values) AssignMember(member, path);

        foreach (var (name, child) in namespaceNode.Namespaces) AssignNamespace(child, $"{path}.{name}");
    }

    private static void AssignMember(IMember member, string path)
    {
        switch (member)
        {
            case TqFunctionGroup group:
                foreach (var function in group) AssignOne(function, path);
                break;

            case TqStruct structure:
                AssignOne(structure, path);
                foreach (var inner in structure.Members.Values) AssignMember(inner, path);
                foreach (var constructor in structure.Constructors) AssignOne(constructor, path);
                if (structure.Destructor != null) AssignOne(structure.Destructor, path);
                break;

            case TqTypedef typedef:
                AssignOne(typedef, path);
                foreach (var inner in typedef.Members.Values) AssignMember(inner, path);
                break;

            case ITqMember tqMember:
                AssignOne(tqMember, path);
                break;
        }
    }

    private static void AssignOne(ITqMember member, string path)
    {
        var visibility = Visibility.Private;

        foreach (var attribute in member.Attributes.OfType<TqBuiltinAttribute>())
        {
            switch (attribute.Kind)
            {
                case BuiltinAttribute.Public: visibility = Visibility.Public; break;
                case BuiltinAttribute.Internal: visibility = Visibility.Internal; break;
                case BuiltinAttribute.Private: visibility = Visibility.Private; break;
            }
        }

        member.Access = new MemberAccess(visibility, [], [], path);
    }
}
