using Mono.Cecil;

namespace Tqc.Dotnet.ModuleBuilder;

internal static class TypeClassifier
{
    public static TypeKind Classify(TypeDefinition type) => type switch
    {
        { IsInterface: true } => TypeKind.Interface,
        { IsEnum: true }      => TypeKind.Enum,
        { IsValueType: true } => TypeKind.Struct,

        { BaseType.FullName: "System.MulticastDelegate" } => TypeKind.Delegate,

        { IsAbstract: true, IsSealed: true } => TypeKind.StaticClass,

        _ => TypeKind.Class,
    };
    
    public static bool IsImplicitBase(string fullName) => fullName is
        "System.Object" or "System.Enum" or "System.ValueType" or "System.EventArgs";
    
    public static string GetTypeName(TypeReference type)
    {
        var name = type.Name;
        var tick = name.IndexOf('`');
        return tick >= 0 ? name[..tick] : name;
    }
}
