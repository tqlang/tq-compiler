using Tqc.Core;
using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;

namespace Tqc.Semantics;

internal static partial class ReferenceBinder
{
    private static void BindMember(BindingContext ctx, IMember member)
    {
        switch (member)
        {
            case TqField field:
                BindField(ctx, field);
                break;

            case TqProperty property:
                BindProperty(ctx, property);
                break;

            case TqFunctionGroup functionGroup:
                foreach (var overload in functionGroup) BindFunction(ctx, overload);
                break;

            case TqFunction function:
                BindFunction(ctx, function);
                break;

            case TqStruct structure:
                BindStruct(ctx, structure);
                break;

            case TqTypedef typedef:
                BindTypedef(ctx, typedef);
                break;

            case TqNamedCase { Value: not null } entry:
                entry.Value = BindExpression(ctx, entry.Value);
                break;
        }
    }

    private static void BindField(BindingContext ctx, TqField field)
    {
        if (field.Type != null) field.Type = BindExpression(ctx, field.Type);
        if (field.Value != null) field.Value = BindExpression(ctx, field.Value);
    }

    /// <summary>The type, the functions that define the accessors and the accessors written in the property.</summary>
    private static void BindProperty(BindingContext ctx, TqProperty property)
    {
        if (property.Type != null) property.Type = BindExpression(ctx, property.Type);
        if (property.GetterSource != null) property.GetterSource = BindExpression(ctx, property.GetterSource);
        if (property.SetterSource != null) property.SetterSource = BindExpression(ctx, property.SetterSource);
        if (property.GetterFunction != null) BindFunction(ctx, property.GetterFunction);
        if (property.SetterFunction != null) BindFunction(ctx, property.SetterFunction);
    }

    /// <summary>
    /// The generic parameters, what the struct extends, then its members. Inside it the names are, outermost first:
    /// the parameters, the members of what it extends, its own members and `this`.
    /// The arguments of an instance are not checked here; the backend applies them.
    /// </summary>
    private static void BindStruct(BindingContext ctx, TqStruct structure)
    {
        ctx.PushFrame();
        BindParameters(ctx, structure.Parameters, structure.Name, structure.SourceLocation);

        if (structure.Extends != null)
        {
            // A plain name was already bound with the attributes; anything else (`Base(T)`) may use the parameters.
            if (structure.Extends is not (IrReference or IrAccess))
                structure.Extends = BindExpression(ctx, structure.Extends);
            CheckExtends(ctx, structure);
        }

        // Ancestors from the farthest, so that a member hides the one of the same name in what it extends.
        List<IMemberContainer> ancestors = [];
        for (var current = structure.Base;
             current != null && !ReferenceEquals(current, structure) && !ancestors.Contains(current);
             current = current.Base)
            ancestors.Insert(0, current);

        var pushed = 0;
        foreach (var ancestor in ancestors)
        {
            ctx.PushFrame();
            pushed++;
            foreach (var member in ancestor.OwnMembers) ctx.Add(member);
        }

        ctx.PushFrame();
        pushed++;
        foreach (var member in structure.Members.Values) ctx.Add(member);
        ctx.Add("this", new ThisReference(structure));

        InsideOf(ctx, structure, () =>
        {
            foreach (var member in structure.Members.Values) BindMember(ctx, member);
            foreach (var constructor in structure.Constructors) BindFunction(ctx, constructor);
            if (structure.Destructor != null) BindFunction(ctx, structure.Destructor);
        });

        for (var i = 0; i < pushed; i++) ctx.PopFrame();
        ctx.PopFrame();
    }

    /// <summary>
    /// A struct extends another struct, whatever it is written in (Tq, .NET...), and not itself, directly or not.
    /// A generic struct needs its arguments: `extends Base(i32)`.
    /// </summary>
    private static void CheckExtends(BindingContext ctx, TqStruct structure)
    {
        if (structure.Extends is not IrReference { Reference: var reference } extends || reference is UnsolvedRef) return;

        if (reference is not TypeMetaReference { Type: IStruct baseStruct })
        {
            ctx.Error($"'{structure.Name}' can only extend a struct", extends.Location);
            structure.Extends = null;
            return;
        }

        if (baseStruct.Parameters.Count > 0)
        {
            ctx.Error($"'{baseStruct.Name}' is generic and needs its arguments to be extended", extends.Location);
            structure.Extends = null;
            return;
        }

        HashSet<IMemberContainer> visited = [];
        for (var current = baseStruct as IMemberContainer; current != null; current = current.Base)
        {
            if (ReferenceEquals(current, structure) || !visited.Add(current))
            {
                ctx.Error($"'{structure.Name}' extends itself", extends.Location);
                structure.Extends = null;
                return;
            }
        }
    }

    /// <summary>The underlying type, then the members. Inside, a member is reached by its name and `this` is the value.</summary>
    private static void BindTypedef(BindingContext ctx, TqTypedef typedef)
    {
        if (typedef.UnderlyingType != null) typedef.UnderlyingType = BindExpression(ctx, typedef.UnderlyingType);

        ctx.PushFrame();
        foreach (var member in typedef.Members.Values) ctx.Add(member);
        ctx.Add("this", new ThisReference(typedef));

        InsideOf(ctx, typedef, () =>
        {
            foreach (var member in typedef.Members.Values) BindMember(ctx, member);
        });

        ctx.PopFrame();
    }

    /// <summary>
    /// In order: a parameter's type and default value can use the previous parameters (`type T, T value`).
    /// Leaves the parameters declared in the current frame.
    /// </summary>
    private static void BindParameters(
        BindingContext ctx, IEnumerable<TqParameter> parameters, string ownerName, SourceLocation location)
    {
        foreach (var parameter in parameters)
        {
            parameter.Type = BindExpression(ctx, parameter.Type);
            if (parameter.DefaultValue != null)
                parameter.DefaultValue = BindExpression(ctx, parameter.DefaultValue);

            if (ctx.IsDeclaredInCurrentFrame(parameter.Name))
                ctx.Error($"Parameter '{parameter.Name}' is declared more than once in '{ownerName}'", location);
            else
                ctx.Add(parameter.Name, new ParameterReference(parameter));
        }
    }

    private static void BindFunction(BindingContext ctx, TqFunction function)
    {
        ctx.PushFrame();
        BindParameters(ctx, function.Parameters, function.Name, function.SourceLocation);

        // The return type also sees the parameters: `func alloc(type T, uint length) []T`.
        if (function.ReturnType != null)
            function.ReturnType = BindExpression(ctx, function.ReturnType);

        if (function.Body != null) BindBody(ctx, function.Body);

        ctx.PopFrame();
    }
}
