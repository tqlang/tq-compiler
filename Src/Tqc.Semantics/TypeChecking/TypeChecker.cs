using Tqc.Core;
using Tqc.Diagnostics;
using Tqc.IR;
using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;
using Tqc.IR.Targets;

namespace Tqc.Semantics;

/// <summary>
/// Types the expressions of a module whose names are already bound, filling `TqModule.Types`,
/// and replaces each call to an overload group with a call to the chosen function.
/// An expression whose type is not known yet (member access on a value, for instance)
/// simply has no entry in the table.
/// </summary>
internal sealed partial class TypeChecker(
    TypeTable types, DiagnosticBag diagnostics, ImplicitConversions conversions, OperatorOverloads operators)
{
    /// <summary>The namespace of the code being checked; what it can reach depends on it.</summary>
    private AccessContext _accessor;

    /// <summary>The function whose body is being checked; `return` is checked against it.</summary>
    private TqFunction? _function;

    public static void Check(TqModule module, IModule[] dependencies, DiagnosticBag diagnostics, TargetAbi abi)
    {
        var conversions = ImplicitConversions.Build(
            [module.RootNamespace, ..dependencies.Select(dependency => dependency.RootNamespace)], abi);

        INamespace[] roots = [module.RootNamespace, ..dependencies.Select(dependency => dependency.RootNamespace)];
        var operators = OperatorOverloads.Build(
            roots, (message, location) => diagnostics.ReportError(new TypeError(message, location)));

        InheritanceChecker.Check(module.RootNamespace, diagnostics);

        new TypeChecker(module.Types, diagnostics, conversions, operators)
            .CheckNamespace(module.RootNamespace, []);

        // After the types of the fields that are declared without one are known.
        RecursiveStructChecker.Check(module.RootNamespace, diagnostics, value => module.Types.Get(value));
    }

    /// <summary>Above zero while something is tried that may fail without it being an error (see Quietly).</summary>
    private int _silenced;

    private void Error(string message, SourceLocation location)
    {
        if (_silenced != 0) return;

        // While the code is tried for one size of `int` at a time, the errors wait: see PerNaturalSize.
        if (_captured != null) _captured.Add((message, location));
        else diagnostics.ReportError(new TypeError(message, location));
    }

    /// <summary>What `action` gives, with the errors it finds not reported: to try one way and then another.</summary>
    private T Quietly<T>(Func<T> action)
    {
        _silenced++;
        try { return action(); }
        finally { _silenced--; }
    }

    private void CheckNamespace(TqNamespace namespaceNode, List<TqNamespace> path)
    {
        path.Add(namespaceNode);

        foreach (var fileScope in namespaceNode.FileScopes)
        foreach (var member in fileScope.Members.Values.OrderBy(member => member is TqField ? 0 : 1))
        {
            _accessor = AccessContext.Of(path);
            CheckMember(member);
        }

        foreach (var child in namespaceNode.Namespaces.Values) CheckNamespace(child, path);

        path.RemoveAt(path.Count - 1);
    }

    private void CheckMember(IMember member)
    {
        if (member is TqField { Type: { } fieldType }) CheckValueType(fieldType);
        if (member is TqProperty { Type: { } propertyType }) CheckValueType(propertyType);

        switch (member)
        {
            case TqField { Value: not null } field:
            {
                var original = field.Value;
                var declared = field.Type != null ? TypeFromExpression(field.Type) : null;

                // `const x = 1` and `let x = value`: the field takes the type of its value.
                var inferring = declared == null || (IsConst(declared, out var argument) && IsInferredArgument(argument));

                field.Value = PerSizeValue(original.Location, () =>
                {
                    var value = Visit(original);
                    return inferring ? value : Coerce(value, ValueTypeOf(declared!, null), WithoutConst(field.Type!), $"the field '{field.Name}'");
                });

                if (inferring && TypeOf(field.Value) is { } inferred) _inferredConstants[field] = DefaultTypeOf(inferred);
                break;
            }

            case TqProperty property:
                EnsureAccessors(property);
                InferPropertyType(property);
                // The getter of a property that infers its type was checked with it.
                if (property.GetterFunction != null && !property.InfersType) CheckFunction(property.GetterFunction);
                if (property.SetterFunction != null) CheckFunction(property.SetterFunction);
                break;

            case TqFunctionGroup functionGroup:
                foreach (var overload in functionGroup) CheckFunction(overload);
                break;

            case TqFunction function:
                CheckFunction(function);
                break;

            case TqStruct structure:
                InsideOf(structure, () =>
                {
                    // The fields first: one without a type has the type of its value, which the functions that use it need.
                    foreach (var inner in structure.Members.Values.OrderBy(inner => inner is TqField ? 0 : 1)) CheckMember(inner);
                    foreach (var constructor in structure.Constructors) CheckFunction(constructor);
                    if (structure.Destructor != null) CheckFunction(structure.Destructor);
                });
                break;

            case TqTypedef typedef:
                InsideOf(typedef, () =>
                {
                    foreach (var inner in typedef.Members.Values) CheckMember(inner);
                });
                break;

            case TqNamedCase { Value: not null } entry:
            {
                var original = entry.Value;
                var underlying = entry.Typedef?.UnderlyingType;
                var underlyingType = underlying != null ? TypeFromExpression(underlying) : null;

                entry.Value = PerSizeValue(original.Location, () =>
                {
                    var value = Visit(original);
                    return underlying != null ? Coerce(value, underlyingType!, underlying, $"the case '{entry.Name}'") : value;
                });
                break;
            }
        }
    }

    /// <summary>Checks the code of a struct or typedef as code that is inside it.</summary>
    private void InsideOf(IMember container, Action action)
    {
        var outer = _accessor;
        _accessor = outer.Inside(container);
        action();
        _accessor = outer;
    }

    private void CheckFunction(TqFunction function)
    {
        foreach (var parameter in function.Parameters) CheckValueType(parameter.Type);
        CheckValueType(function.ReturnType);

        // A default value is of the type of its parameter.
        foreach (var parameter in function.Parameters.Where(parameter => parameter.DefaultValue != null))
        {
            var original = parameter.DefaultValue!;
            var parameterType = TypeFromExpression(parameter.Type);
            parameter.DefaultValue = PerSizeValue(
                original.Location, () => Coerce(Visit(original), parameterType, parameter.Type, $"the default of '{parameter.Name}'"));
        }

        var outer = _function;
        _function = function;
        if (function.Body != null) CheckBody(function.Body);
        _function = outer;
    }

    /// <summary>
    /// `return` gives what the function declares: nothing in a function without a return type, and a value
    /// convertible to it otherwise. `!T` accepts a `T`. A return type that is not known yet (`?T`, a generic) is not checked.
    /// </summary>
    private IrStatement CheckReturn(IrReturnStatement returnStatement)
    {
        // `return f()`: a call is chosen by what it returns, which has to be what the function returns.
        ITypeRef? expectedReturn = null;
        if (returnStatement.ReturnValue is IrCall && _function is { } current && current is not (TqConstructor or TqDestructor))
        {
            expectedReturn = current.ReturnType == null ? new StructTypeReference(CompilerStd.Types.Void) : TypeFromExpression(current.ReturnType);
            if (CompilerStd.Types.IsFailable(expectedReturn, out var inside)) expectedReturn = StructSemantics.ValueTypeOf(inside);
        }

        var value = returnStatement.ReturnValue != null
            ? expectedReturn != null ? VisitExpecting(returnStatement.ReturnValue, Expectation.Of(expectedReturn)) : Visit(returnStatement.ReturnValue)
            : null;
        var function = _function;
        if (function == null) return returnStatement with { ReturnValue = value };

        // Constructors and destructors do not return a value.
        var returnsNothing = function is TqConstructor or TqDestructor || function.ReturnType == null;
        var declared = returnsNothing ? null : TypeFromExpression(function.ReturnType!);

        // `!T` returns a `T` (the error side is raised, not returned): the value is checked against `T`.
        var failable = CompilerStd.Types.IsFailable(declared, out var result);
        if (failable) declared = StructSemantics.ValueTypeOf(result);
        var isVoid = returnsNothing || (declared != null && ConversionRules.IsStruct(declared, CompilerStd.Types.Void));

        if (isVoid)
        {
            var valueType = value != null ? TypeOf(value) : null;
            if (value != null && !(valueType != null && ConversionRules.IsStruct(valueType, CompilerStd.Types.Void)))
                Error($"'{function.Name}' returns no value", value.Location);
            return returnStatement with { ReturnValue = value };
        }

        if (declared == null) return returnStatement with { ReturnValue = value };

        if (value == null)
        {
            Error($"'{function.Name}' must return a value of type '{TypeFormatter.Format(declared)}'", returnStatement.Location);
            return returnStatement;
        }

        return returnStatement with
        {
            ReturnValue = Coerce(value, declared, failable ? TypeExpressionOf(declared, value.Location) : function.ReturnType,
                $"the return value of '{function.Name}'"),
        };
    }

    /// <summary>How many loops the statement being checked is inside of: `break` needs at least one.</summary>
    private int _loopDepth;

    private void CheckBody(IrBody body)
    {
        for (var i = 0; i < body.Children.Count; i++)
            body.Children[i] = CheckNode(body.Children[i]);
    }

    private IrNode CheckNode(IrNode node)
    {
        switch (node)
        {
            case IrReturnStatement returnStatement:
                return CheckPerSize(returnStatement, () => CheckReturn(returnStatement));

            case IrExpressionStatement expressionStatement:
                return expressionStatement with { Expression = VisitPerSize(expressionStatement.Expression, Expectation.Discard) };

            case IrIf ifStatement:
            {
                var condition = VisitPerSize(ifStatement.Condition);
                var conditionType = TypeOf(condition);
                if (conditionType != null && !IsBool(conditionType))
                    Error($"The condition of 'if' must be bool, not '{TypeFormatter.Format(conditionType)}'", condition.Location);

                return ifStatement with
                {
                    Condition = condition,
                    Then = (IrStatement)CheckNode(ifStatement.Then),
                    Else = ifStatement.Else != null ? (IrStatement)CheckNode(ifStatement.Else) : null,
                };
            }

            case IrWhile loop:
            {
                var declaration = loop.Declaration != null ? VisitPerSize(loop.Declaration) : null;

                var condition = VisitPerSize(loop.Condition);
                var conditionType = TypeOf(condition);
                if (conditionType != null && !IsBool(conditionType))
                    Error($"The condition of 'while' must be bool, not '{TypeFormatter.Format(conditionType)}'", condition.Location);

                var step = loop.Step != null ? VisitPerSize(loop.Step) : null;

                _loopDepth++;
                var body = (IrStatement)CheckNode(loop.Body);
                _loopDepth--;

                return loop with { Declaration = declaration, Condition = condition, Step = step, Body = body };
            }

            case IrBreak breakStatement:
                if (_loopDepth == 0) Error("'break' can only be used inside a loop", breakStatement.Location);
                return breakStatement;

            case IrBody nested:
                CheckBody(nested);
                return nested;

            case IrExpression expression:
                return VisitPerSize(expression);

            default:
                return node;
        }
    }
}
