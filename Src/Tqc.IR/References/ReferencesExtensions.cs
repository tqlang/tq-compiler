using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.ProgramMembers;

namespace Tqc.IR.References;

public static class ReferencesExtensions
{
    extension(IMember member)
    {
        public Ref GetReference() => member switch
        {
            IType type                   => new TypeMetaReference(type),
            INamespace @namespace        => new NamespaceReference(@namespace),
            IFunction function           => new FunctionTypeReference(function),
            IFunctionGroup functionGroup => new FunctionGroupTypeReference(functionGroup),
            TqProperty property          => new PropertyReference(property),
            IField field                 => new FieldReference(field),
            IProtocol protocol           => new ProtocolTypeReference(protocol),
            _ => throw new NotSupportedException($"Member '{member.Name}' ({member.GetType().Name}) cannot be referenced."),
        };
    }
}
