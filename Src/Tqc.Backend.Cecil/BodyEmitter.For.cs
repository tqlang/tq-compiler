using Mono.Cecil;
using Mono.Cecil.Cil;
using Tqc.IR.Nodes;

namespace Tqc.Backend.Cecil;

/// <summary>
/// `for` as the `foreach` of .NET. An array or a `Slice<T>` is kept in a variable and read by position, up to its length;
/// anything else is iterated through its enumerator (see `BodyEmitter.Enumeration.cs`).
/// </summary>
internal sealed partial class BodyEmitter
{
    private void EmitFor(IrFor loop)
    {
        var actual = Emit(loop.Collection, null);
        if (IsInlineArray(actual, out var inline)) actual = EmitInlineArrayView(actual!, inline);

        var array = actual as ArrayType;
        GenericInstanceType? slice = null;
        if (array == null && !IsSlice(actual, out slice))
        {
            if (actual != null) EmitEnumeration(loop, actual);
            return;
        }

        var element = array != null ? array.ElementType : slice!.GenericArguments[0];
        var int32 = Core("System.Int32");

        var source = NewTemp(actual!);
        var position = NewTemp(int32);
        _il.Emit(OpCodes.Stloc, source);
        _il.Emit(OpCodes.Ldc_I4_0);
        _il.Emit(OpCodes.Stloc, position);

        var turn = _il.Create(OpCodes.Nop);
        var test = _il.Create(OpCodes.Nop);
        var end = _il.Create(OpCodes.Nop);

        _il.Emit(OpCodes.Br, test);
        _il.Append(turn);

        // `item = source[position]`
        if (ResolveTarget(loop.Item) is not { } item) return;
        item.Prefix();
        if (array != null)
        {
            _il.Emit(OpCodes.Ldloc, source);
            _il.Emit(OpCodes.Ldloc, position);
            _il.Emit(OpCodes.Ldelem_Any, element);
        }
        else
        {
            _il.Emit(OpCodes.Ldloca, source);
            _il.Emit(OpCodes.Ldloc, position);
            _il.Emit(OpCodes.Call, _owner.SliceMember(slice!, "get_Item", 1));
            _il.Emit(OpCodes.Ldobj, element);
        }

        item.Store();

        if (!EmitForIndex(loop, position)) return;

        _breakTargets.Push(end);
        EmitStatement(loop.Body);
        _breakTargets.Pop();

        _il.Emit(OpCodes.Ldloc, position);
        _il.Emit(OpCodes.Ldc_I4_1);
        _il.Emit(OpCodes.Add);
        _il.Emit(OpCodes.Stloc, position);

        _il.Append(test);
        _il.Emit(OpCodes.Ldloc, position);
        if (array != null)
        {
            _il.Emit(OpCodes.Ldloc, source);
            _il.Emit(OpCodes.Ldlen);
            _il.Emit(OpCodes.Conv_I4);
        }
        else
        {
            _il.Emit(OpCodes.Ldloca, source);
            _il.Emit(OpCodes.Ldfld, _owner.SliceLength(slice!));
        }

        _il.Emit(OpCodes.Blt, turn);
        _il.Append(end);
    }

    /// <summary>`index = position`, if the loop has an index.</summary>
    private bool EmitForIndex(IrFor loop, VariableDefinition position)
    {
        if (loop.Index == null) return true;
        if (ResolveTarget(loop.Index) is not { } index) return false;

        index.Prefix();
        _il.Emit(OpCodes.Ldloc, position);
        if (TryNum(index.Type, out var number)) ConvertNumber(new Num(false, true, 32, false), number);
        index.Store();
        return true;
    }
}
