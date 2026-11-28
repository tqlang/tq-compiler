using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;

namespace Tqc.Semantics;

/// <summary>
/// A function that returns a value cannot reach its end: every path has to `return` (or loop for ever). A function
/// without a return type, a `!void` one and a `noreturn` one can. A call of a `noreturn` function ends the path.
/// </summary>
internal sealed partial class TypeChecker
{
    private void CheckEveryPathReturns(TqFunction function, IrBody body)
    {
        if (function is TqConstructor or TqDestructor || function.ReturnType == null) return;

        // A type that is not known has its error already.
        if (TypeFromExpression(function.ReturnType) is not { } declared) return;

        if (CompilerStd.Types.IsFailable(declared, out var result)) declared = StructSemantics.ValueTypeOf(result);
        if (declared != null && (ConversionRules.IsStruct(declared, CompilerStd.Types.Void) || IsNoReturn(declared))) return;

        if (CanReachEnd(body, null))
            Error($"Not every path of '{function.Name}' returns a value: its end can be reached without a 'return'",
                function.SourceLocation);
    }

    /// <summary>
    /// Whether what comes after `node` can run once it did. `breaks` receives the `break`s that can run and that leave
    /// the loop around `node`.
    /// </summary>
    /// <remarks>
    /// Conditions are not evaluated: both sides of an `if` can run. Only `while true` is known never to end by itself.
    /// </remarks>
    private bool CanReachEnd(IrNode node, List<IrBreak>? breaks)
    {
        if (node is IrExpression expression && TypeOf(expression) is { } type && IsNoReturn(type)) return false;

        switch (node)
        {
            case IrReturnStatement or IrSizeChoiceStatement:
                return false;

            case IrBreak breakStatement:
                breaks?.Add(breakStatement);
                return false;

            // What is after a statement that does not end never runs.
            case IrBody body:
                return body.Children.All(child => CanReachEnd(child, breaks));

            case IrIf ifStatement:
            {
                var then = CanReachEnd(ifStatement.Then, breaks);
                var otherwise = ifStatement.Else == null || CanReachEnd(ifStatement.Else, breaks);
                return then || otherwise;
            }

            case IrWhile loop:
            {
                List<IrBreak> own = [];
                CanReachEnd(loop.Body, own);
                return loop.Condition is not IrComptimeBool { Value: true } || own.Count > 0;
            }

            case IrExpressionStatement statement:
                return CanReachEnd(statement.Expression, breaks);

            // The arms cover every value (see VisitMatch), so one of them runs. All are walked, for their `break`s.
            case IrMatch match:
                return match.Arms.Count(arm => CanReachEnd(arm.Body, breaks)) > 0 || match.Arms.Length == 0;

            case IrSizeChoice choice:
                return choice.Variants.Count(variant => CanReachEnd(variant.Expression, breaks)) > 0;

            default:
                return true;
        }
    }

    private static bool IsNoReturn(ITypeRef type) => ConversionRules.IsStruct(type, CompilerStd.Types.NoReturn);
}
