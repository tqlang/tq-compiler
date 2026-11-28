using Mono.Cecil;
using Mono.Cecil.Cil;
using Tqc.IR.Nodes;

namespace Tqc.Backend.Cecil;

/// <summary>
/// `for` over what is `IEnumerable`: `GetEnumerator`, then `MoveNext` and `Current` while there is one. As in C#, the
/// public `GetEnumerator` of the type is the one used if it has one (the one of `List(T)` allocates nothing), and the one
/// of the interface otherwise; an enumerator that is disposable is disposed in a `finally`.
///
/// Inside that `finally` .NET has no `ret` and no branch to outside: `return` and `break` use `leave`, and a `return`
/// keeps its value in a variable until the end of the method.
/// </summary>
internal sealed partial class BodyEmitter
{
    /// <summary>The ends of the loops that `break` has to `leave` to.</summary>
    private readonly HashSet<Instruction> _leftByBreak = [];

    /// <summary>How many `try` blocks the code being emitted is in.</summary>
    private int _protected;

    /// <summary>Where the returns made inside a `try` go, and the value they give.</summary>
    private Instruction? _exit;

    private VariableDefinition? _exitValue;

    private readonly record struct Member(MethodReference Method, TypeReference Returns);

    /// <summary>With the collection on the stack.</summary>
    private void EmitEnumeration(IrFor loop, TypeReference collection)
    {
        if (ResolveTarget(loop.Item) is not { } item) return;

        var getEnumerator = PublicMember(collection, "GetEnumerator");
        var current = getEnumerator is { } found ? PublicMember(found.Returns, "get_Current") : null;

        // The enumerator of the type may be of a more general element than the interface the type implements.
        var usable = current is { } read && PublicMember(getEnumerator!.Value.Returns, "MoveNext") != null
                     && (read.Returns.FullName != "System.Object" || item.Type.FullName == "System.Object");

        var receiver = collection;
        if (!usable)
        {
            var untyped = item.Type.FullName == "System.Object";
            var definition = _owner.Backend.Core.Find(untyped
                ? "System.Collections.IEnumerable"
                : "System.Collections.Generic.IEnumerable`1");
            if (definition == null)
            {
                Report("'IEnumerable' was not found in 'System.Runtime'", loop.Collection.Location);
                return;
            }

            receiver = _target.ImportReference(definition);
            if (!untyped)
            {
                var applied = new GenericInstanceType(receiver);
                applied.GenericArguments.Add(item.Type);
                receiver = applied;
            }

            if (collection.IsValueType) _il.Emit(OpCodes.Box, collection);
            _il.Emit(OpCodes.Castclass, receiver);
            getEnumerator = PublicMember(receiver, "GetEnumerator");
        }

        if (getEnumerator is not { } get
            || PublicMember(get.Returns, "get_Current") is not { } element
            || PublicMember(get.Returns, "MoveNext") is not { } moveNext)
        {
            Report("'for' cannot iterate this value in .NET", loop.Collection.Location);
            return;
        }

        if (receiver.IsValueType)
        {
            var held = NewTemp(receiver);
            _il.Emit(OpCodes.Stloc, held);
            _il.Emit(OpCodes.Ldloca, held);
            _il.Emit(OpCodes.Call, get.Method);
        }
        else _il.Emit(OpCodes.Callvirt, get.Method);

        var enumerator = NewTemp(get.Returns);
        _il.Emit(OpCodes.Stloc, enumerator);

        var position = NewTemp(Core("System.Int32"));
        _il.Emit(OpCodes.Ldc_I4_0);
        _il.Emit(OpCodes.Stloc, position);

        var disposal = DisposalOf(enumerator);

        var turn = _il.Create(OpCodes.Nop);
        var test = _il.Create(OpCodes.Nop);
        var end = _il.Create(OpCodes.Nop);

        var start = _il.Create(OpCodes.Br, test);
        _il.Append(start);
        _il.Append(turn);

        // `item = enumerator.Current`
        item.Prefix();
        CallOn(enumerator, element.Method);
        var given = element.Returns;
        while (given is IModifierType modified) given = modified.ElementType;
        if (given is ByReferenceType reference)
        {
            given = reference.ElementType;
            _il.Emit(OpCodes.Ldobj, given);
        }

        Coerce(given, item.Type);
        item.Store();

        if (!EmitForIndex(loop, position)) return;

        _breakTargets.Push(end);
        if (disposal != null)
        {
            _leftByBreak.Add(end);
            _protected++;
        }

        EmitStatement(loop.Body);

        _breakTargets.Pop();

        _il.Emit(OpCodes.Ldloc, position);
        _il.Emit(OpCodes.Ldc_I4_1);
        _il.Emit(OpCodes.Add);
        _il.Emit(OpCodes.Stloc, position);

        _il.Append(test);
        CallOn(enumerator, moveNext.Method);
        _il.Emit(OpCodes.Brtrue, turn);

        if (disposal != null)
        {
            _protected--;
            _il.Emit(OpCodes.Leave, end);

            var handler = _il.Create(OpCodes.Nop);
            _il.Append(handler);
            disposal();
            _il.Emit(OpCodes.Endfinally);

            _method.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Finally)
            {
                TryStart = start,
                TryEnd = handler,
                HandlerStart = handler,
                HandlerEnd = end,
            });
        }

        _il.Append(end);
    }

    private void CallOn(VariableDefinition instance, MethodReference method)
    {
        var byValue = instance.VariableType.IsValueType;
        _il.Emit(byValue ? OpCodes.Ldloca : OpCodes.Ldloc, instance);
        _il.Emit(byValue ? OpCodes.Call : OpCodes.Callvirt, method);
    }

    /// <summary>What disposes the enumerator, or null if there is nothing to dispose.</summary>
    private Action? DisposalOf(VariableDefinition enumerator)
    {
        var type = enumerator.VariableType;
        var definition = type.Resolve();
        var disposable = Implements(type, "System.IDisposable");

        if (type.IsValueType)
        {
            if (disposable)
            {
                return () =>
                {
                    _il.Emit(OpCodes.Ldloca, enumerator);
                    _il.Emit(OpCodes.Constrained, type);
                    _il.Emit(OpCodes.Callvirt, Library("System.IDisposable", "Dispose"));
                };
            }

            // A `ref struct` may have a `Dispose` without the interface.
            return PublicMember(type, "Dispose") is { } own ? () => CallOn(enumerator, own.Method) : null;
        }

        if (!disposable && definition is { IsSealed: true }) return null;

        // The interface may only be known when the code runs: `IEnumerator` does not extend `IDisposable`.
        return () =>
        {
            var held = NewTemp(Core("System.IDisposable"));
            var skip = _il.Create(OpCodes.Nop);

            _il.Emit(OpCodes.Ldloc, enumerator);
            if (!disposable) _il.Emit(OpCodes.Isinst, Core("System.IDisposable"));
            _il.Emit(OpCodes.Stloc, held);
            _il.Emit(OpCodes.Ldloc, held);
            _il.Emit(OpCodes.Brfalse, skip);
            _il.Emit(OpCodes.Ldloc, held);
            _il.Emit(OpCodes.Callvirt, Library("System.IDisposable", "Dispose"));
            _il.Append(skip);
        };
    }

    private static bool Implements(TypeReference type, string interfaceName)
    {
        if (type.FullName == interfaceName) return true;
        if (type.Resolve() is not { } definition) return false;

        return definition.Interfaces.Any(one => Implements(one.InterfaceType, interfaceName))
               || definition.BaseType != null && Implements(definition.BaseType, interfaceName);
    }

    /// <summary>
    /// The public method without parameters that a value of the type has by this name, its own or inherited, as it is
    /// called on that type (with the arguments of a generic one in place).
    /// </summary>
    private Member? PublicMember(TypeReference type, string name)
    {
        if (type.IsGenericParameter || type.Resolve() is not { } definition) return null;

        var host = type as GenericInstanceType;
        var method = definition.Methods.FirstOrDefault(candidate => candidate.Name == name && candidate.IsPublic
            && !candidate.IsStatic && candidate.Parameters.Count == 0 && !candidate.HasGenericParameters);

        if (method != null)
        {
            var used = host != null ? OnHost(_owner.Use(method), host) : _owner.Use(method);
            return new Member(used, Instantiate(used.ReturnType, [], host?.GenericArguments));
        }

        IEnumerable<TypeReference> parents = definition.IsInterface
            ? definition.Interfaces.Select(one => one.InterfaceType)
            : definition.BaseType != null ? [definition.BaseType] : [];

        foreach (var parent in parents)
        {
            var written = Imported(Instantiate(parent, [], host?.GenericArguments));
            if (PublicMember(written, name) is { } inherited) return inherited;
        }

        return null;
    }

    /// <summary>
    /// A type read from another module, as this one refers to it. The generic parameters in it are already the ones of
    /// this module: the ones of the type it was read from have been replaced by its arguments.
    /// </summary>
    private TypeReference Imported(TypeReference type)
    {
        switch (type)
        {
            case GenericParameter:
                return type;

            case ArrayType array:
                return new ArrayType(Imported(array.ElementType));

            case GenericInstanceType instance:
                var result = new GenericInstanceType(Imported(instance.ElementType));
                foreach (var argument in instance.GenericArguments) result.GenericArguments.Add(Imported(argument));
                return result;

            default:
                return type.Module == _target ? type : _target.ImportReference(type);
        }
    }
}
