using Tq.CodeProcess.Core.EvaluationData.LanguageObjects.Attributes;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.AttributeReferences;

namespace Tq.CodeProcess.Core.EvaluationData.LanguageObjects;

/// <summary>
/// Common contract every program member depends on — Tq-native
/// (StructObject, FunctionObject, FieldObject, ...) and dotnet-backed
/// (DotnetTypeObject, DotnetMethodObject, ...) alike.
///
/// `Name`, `Parent` and `SearchChild` already exist on LangObject today;
/// this interface just formalizes them as a contract the Analyser can
/// depend on directly, instead of switching on concrete types.
///
/// `Encapsulation` is the one genuinely new piece: a generic attribute
/// bag that replaces IStaticModifier, IPublicModifier, IInternalModifier,
/// IAbstractModifier, IVirtualModifier, IOverrideAttribute,
/// IExternModifier and IExportModifier. Static/Public/Abstract/Extern/...
/// all become BuiltinAttributes keys instead of one marker interface each.
///
/// LangObject needs to declare `: IMember` and add one member:
///   public IDictionary&lt;BuiltinAttributes, object?&gt; Encapsulation { get; }
///     = new Dictionary&lt;BuiltinAttributes, object?&gt;();
/// </summary>
public interface IMember
{
    string Name { get; }
    LangObject? Parent { get; }

    LangObject? SearchChild(string name, SearchChildMode mode = SearchChildMode.All);

    IDictionary<BuiltinAttributes, object?> Encapsulation { get; }
}

public static class MemberEncapsulationExtensions
{
    extension(IMember member)
    {
        /// <summary>True only if the attribute was explicitly set to true.
        /// Never set (e.g. "Final" on something that was never a struct) reads as false.</summary>
        public bool HasFlag(BuiltinAttributes attribute)
            => member.Encapsulation.TryGetValue(attribute, out var value) && value is true;
        public void SetFlag(BuiltinAttributes attribute, bool value = true)
            => member.Encapsulation[attribute] = value;
        
        /// <summary>For attributes that carry a value instead of a plain bool
        /// (Extern's (string,string) tuple, Export's string).</summary>
        public T? GetValue<T>(BuiltinAttributes attribute)
            => member.Encapsulation.TryGetValue(attribute, out var value) && value is T typed ? typed : default;
        public void SetValue<T>(BuiltinAttributes attribute, T value)
            => member.Encapsulation[attribute] = value;
    }

}
