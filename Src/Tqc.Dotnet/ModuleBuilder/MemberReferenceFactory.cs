using Mono.Cecil;
using Tqc.Dotnet.ModuleBuilder;
using Tqc.Dotnet.ProgramMembers;
using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.ProgramMembers.Tq;
using GenericParameterTypeRef = Tqc.IR.References.GenericParameterTypeRef;
using ITypeRef = Tqc.IR.References.ITypeRef;
using TypeMetaReference = Tqc.IR.References.TypeMetaReference;

namespace Tqc.Dotnet;

/// <summary>Regra única de visibilidade: public, protected e protected internal.</summary>
internal static class MemberVisibility
{
    public static bool IsVisible(FieldDefinition field) => field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly;
    public static bool IsVisible(MethodDefinition method) => method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly;

    /// <summary>Uma propriedade é visível se algum acessor é visível.</summary>
    public static bool IsVisible(PropertyDefinition property) =>
        property.GetMethod is { } get && IsVisible(get) || property.SetMethod is { } set && IsVisible(set);
}

/// <summary>
/// Cria os membros. As referências do Cecil são relativas ao <c>declaring</c> (a Reference do tipo dono,
/// já no escopo do assembly que exporta) e usam tipos remapeados; os tipos Tq saem do tradutor.
/// </summary>
sealed internal class MemberReferenceFactory(SignatureRemapper remapper, TqTypeTranslator translator)
{
    public DotnetField Field(FieldDefinition field, TypeReference declaring)
    {
        var type = remapper.Map(field.FieldType, declaring, null);
        var reference = new FieldReference(field.Name, type, declaring);

        return new DotnetField(
            field.Name,
            translator.Translate(type),
            reference,
            isStatic: field.IsStatic,
            isConstant: field.IsLiteral,
            constant: field.HasConstant ? field.Constant : null);
    }

    public DotnetMethod Method(MethodDefinition method, TypeReference declaring)
    {
        var reference = MethodReference(method, declaring);

        // `T` of a generic method is a parameter of the type `type`, given first in the call.
        var typeParameters = reference.GenericParameters
            .Select(parameter => new TqParameter(
                new IrReference(default, new TypeMetaReference(CompilerStd.Types.Type)), parameter.Name))
            .ToArray();

        ITypeRef? Generic(GenericParameter parameter) =>
            parameter.Type == GenericParameterType.Method && parameter.Position < typeParameters.Length
                ? new GenericParameterTypeRef(typeParameters[parameter.Position])
                : null;

        return new DotnetMethod(
            method.Name,
            translator.TranslateReturn(reference.ReturnType, Generic),
            reference.Parameters.Select(p => translator.Translate(p.ParameterType, Generic)).ToArray(),
            reference,
            typeParameters);
    }

    public DotnetProperty Property(PropertyDefinition property, TypeReference declaring) => new(
        property.Name,
        translator.Translate(remapper.Map(property.PropertyType, declaring, null)),
        property.Parameters.Select(p => translator.Translate(remapper.Map(p.ParameterType, declaring, null))).ToArray(),
        property.GetMethod is { } get && MemberVisibility.IsVisible(get) ? MethodReference(get, declaring) : null,
        property.SetMethod is { } set && MemberVisibility.IsVisible(set) ? MethodReference(set, declaring) : null,
        property.GetMethod is { } getDefinition && MemberVisibility.IsVisible(getDefinition) ? Method(getDefinition, declaring) : null,
        property.SetMethod is { } setDefinition && MemberVisibility.IsVisible(setDefinition) ? Method(setDefinition, declaring) : null);

    public MethodReference MethodReference(MethodDefinition method, TypeReference declaring)
    {
        // O retorno é só um placeholder: precisa existir antes para que os generics de método sejam copiados
        // e os tipos da assinatura possam apontar para eles.
        var reference = new MethodReference(method.Name, method.ReturnType, declaring)
        {
            HasThis = method.HasThis,
            ExplicitThis = method.ExplicitThis,
            CallingConvention = method.CallingConvention,
        };

        foreach (var parameter in method.GenericParameters)
            reference.GenericParameters.Add(new GenericParameter(parameter.Name, reference));

        reference.ReturnType = remapper.Map(method.ReturnType, declaring, reference);

        foreach (var parameter in method.Parameters)
        {
            reference.Parameters.Add(new ParameterDefinition(
                parameter.Name,
                parameter.Attributes,
                remapper.Map(parameter.ParameterType, declaring, reference)));
        }

        return reference;
    }
}