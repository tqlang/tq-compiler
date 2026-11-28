using Tqc.IR.Nodes;
using Tqc.IR.References;

namespace Tqc.Semantics;

internal static partial class ReferenceBinder
{
    /// <summary>The body has its own scope: a local may shadow a parameter, but not redeclare another local.</summary>
    private static void BindBody(BindingContext ctx, IrBody body)
    {
        ctx.PushFrame();

        for (var i = 0; i < body.Children.Count; i++)
            body.Children[i] = BindNode(ctx, body.Children[i]);

        ctx.PopFrame();
    }

    private static IrNode BindNode(BindingContext ctx, IrNode node)
    {
        switch (node)
        {
            case IrReturnStatement { ReturnValue: not null } returnStatement:
                return returnStatement with { ReturnValue = BindExpression(ctx, returnStatement.ReturnValue) };

            case IrDelete deleteStatement:
                return deleteStatement with { Operand = BindExpression(ctx, deleteStatement.Operand) };

            case IrExpressionStatement expressionStatement:
                return expressionStatement with { Expression = BindExpression(ctx, expressionStatement.Expression) };

            case IrIf ifStatement:
                return ifStatement with
                {
                    Condition = BindExpression(ctx, ifStatement.Condition),
                    Then = BindBranch(ctx, ifStatement.Then),
                    Else = ifStatement.Else != null ? BindBranch(ctx, ifStatement.Else) : null,
                };

            case IrWhile loop:
            {
                // What the loop declares is seen by the condition, the step and the body, and by nothing after it.
                ctx.PushFrame();
                var declaration = loop.Declaration != null ? BindExpression(ctx, loop.Declaration) : null;
                var condition = BindExpression(ctx, loop.Condition);
                var step = loop.Step != null ? BindExpression(ctx, loop.Step) : null;
                var body = BindBranch(ctx, loop.Body);
                ctx.PopFrame();

                return loop with { Declaration = declaration, Condition = condition, Step = step, Body = body };
            }

            case IrFor loop:
            {
                // The collection is bound first: it does not see the variables the loop declares.
                var collection = BindExpression(ctx, loop.Collection);

                ctx.PushFrame();
                var item = (IrLocal)BindLocal(ctx, loop.Item);
                var index = loop.Index != null ? (IrLocal)BindLocal(ctx, loop.Index) : null;
                var body = BindBranch(ctx, loop.Body);
                ctx.PopFrame();

                return loop with { Collection = collection, Item = item, Index = index, Body = body };
            }

            case IrBody nested:
                BindBody(ctx, nested);
                return nested;

            case IrExpression expression:
                return BindExpression(ctx, expression);

            default:
                return node;
        }
    }

    /// <summary>A branch has its own scope, also when it is a single statement.</summary>
    private static IrStatement BindBranch(BindingContext ctx, IrStatement branch)
    {
        ctx.PushFrame();
        var bound = (IrStatement)BindNode(ctx, branch);
        ctx.PopFrame();
        return bound;
    }

    private static IrExpression BindLocal(BindingContext ctx, IrLocal local)
    {
        var bound = local.type == null ? local : local with { type = BindExpression(ctx, local.type) };

        if (ctx.IsDeclaredInCurrentFrame(bound.name))
            ctx.Error($"A variable named '{bound.name}' is already declared in this scope", bound.Location);
        else
            ctx.Add(bound.name, new LocalReference(bound));

        return bound;
    }

    private static IrExpression BindAssign(BindingContext ctx, IrAssign assign)
    {
        // `let a = a + 1`: the right-hand `a` is the outer one, so bind the source before declaring.
        if (assign.Target is IrLocal)
        {
            var source = BindExpression(ctx, assign.Source);
            var target = BindExpression(ctx, assign.Target);
            return assign with { Target = target, Source = source };
        }

        return assign with
        {
            Target = BindExpression(ctx, assign.Target),
            Source = BindExpression(ctx, assign.Source),
        };
    }
}
