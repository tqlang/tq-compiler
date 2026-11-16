using Tqc.Core;
using Tqc.Diagnostics;
using Tqc.IR;
using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.ProgramMembers.Tq;

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

    public static void Check(TqModule module, IModule[] dependencies, DiagnosticBag diagnostics)
    {
        var conversions = ImplicitConversions.Build(
            [module.RootNamespace, ..dependencies.Select(dependency => dependency.RootNamespace)]);

        INamespace[] roots = [module.RootNamespace, ..dependencies.Select(dependency => dependency.RootNamespace)];
        var operators = OperatorOverloads.Build(
            roots, (message, location) => diagnostics.ReportError(new TypeError(message, location)));

        InheritanceChecker.Check(module.RootNamespace, diagnostics);

        new TypeChecker(module.Types, diagnostics, conversions, operators).CheckNamespace(module.RootNamespace, []);
    }

    private void Error(string message, SourceLocation location) =>
        diagnostics.ReportError(new TypeError(message, location));

    private void CheckNamespace(TqNamespace namespaceNode, List<TqNamespace> path)
    {
        path.Add(namespaceNode);

        foreach (var fileScope in namespaceNode.FileScopes)
        foreach (var member in fileScope.Members.Values)
        {
            _accessor = AccessContext.Of(path);
            CheckMember(member);
        }

        foreach (var child in namespaceNode.Namespaces.Values) CheckNamespace(child, path);

        path.RemoveAt(path.Count - 1);
    }

    private void CheckMember(IMember member)
    {
        switch (member)
        {
            case TqField { Value: not null } field:
                field.Value = Visit(field.Value);
                if (field.Type != null)
                    field.Value = Coerce(field.Value, TypeFromExpression(field.Type), field.Type, $"the field '{field.Name}'");
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
                    foreach (var inner in structure.Members.Values) CheckMember(inner);
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
                entry.Value = Visit(entry.Value);
                if (entry.Typedef?.UnderlyingType is { } underlying)
                    entry.Value = Coerce(entry.Value, TypeFromExpression(underlying), underlying, $"the case '{entry.Name}'");
                break;
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
        // A default value is of the type of its parameter.
        foreach (var parameter in function.Parameters.Where(parameter => parameter.DefaultValue != null))
        {
            var value = Visit(parameter.DefaultValue!);
            parameter.DefaultValue = Coerce(
                value, TypeFromExpression(parameter.Type), parameter.Type, $"the default of '{parameter.Name}'");
        }

        var outer = _function;
        _function = function;
        if (function.Body != null) CheckBody(function.Body);
        _function = outer;
    }

    /// <summary>
    /// `return` gives what the function declares: nothing in a function without a return type, and a value
    /// convertible to it otherwise. A return type that is not known yet (`!T`, `?T`, a generic) is not checked.
    /// </summary>
    private IrStatement CheckReturn(IrReturnStatement returnStatement)
    {
        var value = returnStatement.ReturnValue != null ? Visit(returnStatement.ReturnValue) : null;
        var function = _function;
        if (function == null) return returnStatement with { ReturnValue = value };

        // Constructors and destructors do not return a value.
        var returnsNothing = function is TqConstructor or TqDestructor || function.ReturnType == null;
        var declared = returnsNothing ? null : TypeFromExpression(function.ReturnType!);
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
            ReturnValue = Coerce(value, declared, function.ReturnType, $"the return value of '{function.Name}'"),
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
                return CheckReturn(returnStatement);

            case IrExpressionStatement expressionStatement:
                return expressionStatement with { Expression = Visit(expressionStatement.Expression) };

            case IrIf ifStatement:
            {
                var condition = Visit(ifStatement.Condition);
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
                var declaration = loop.Declaration != null ? Visit(loop.Declaration) : null;

                var condition = Visit(loop.Condition);
                var conditionType = TypeOf(condition);
                if (conditionType != null && !IsBool(conditionType))
                    Error($"The condition of 'while' must be bool, not '{TypeFormatter.Format(conditionType)}'", condition.Location);

                var step = loop.Step != null ? Visit(loop.Step) : null;

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
                return Visit(expression);

            default:
                return node;
        }
    }
}
