using System.Numerics;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Tqc.Core;
using Tqc.Dotnet.ProgramMembers;
using Tqc.IR;
using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;
using CecilField = Mono.Cecil.FieldReference;
using IrField = Tqc.IR.References.FieldReference;
using ParameterReference = Tqc.IR.References.ParameterReference;

namespace Tqc.Backend.Cecil;

/// <summary>
/// The code of one function. The IR is already typed (<see cref="TypeTable"/>) and every name is bound, so each node
/// is a short sequence of instructions; the type of a node says what is on the stack after it.
///
/// A literal has no type of its own: it takes the one that is expected where it is used. An operand is converted to
/// the type of the operation. `a + b` is checked (it throws on overflow); `+%`, `-%` and `*%` wrap.
/// </summary>
internal sealed class BodyEmitter
{
    private readonly ModuleEmitter _owner;
    private readonly TqFunction _function;
    private MethodDefinition _method;
    private ILProcessor _il;
    private ModuleDefinition _target; // where the instructions go: the module, or `Tq.Runtime` while a helper is made
    private readonly TypeTable _types;

    private readonly Dictionary<IrLocal, VariableDefinition> _locals = [];
    private readonly Stack<Instruction> _breakTargets = new();

    public BodyEmitter(ModuleEmitter owner, TqFunction function, MethodDefinition method, ILProcessor il)
    {
        _owner = owner;
        _function = function;
        _method = method;
        _il = il;
        _target = owner.Module;
        _types = owner.Tq.Types;
    }

    public void Emit()
    {
        // A class starts by calling the constructor of what it extends.
        if (_function is TqConstructor && _function.Owner is TqStruct structure && ModuleEmitter.IsClass(structure)
            && _owner.FindBaseConstructor(structure) is { } baseConstructor)
        {
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Call, baseConstructor);
        }

        // A struct by value that extends another one starts its `__base` with the constructor of that one, if it has one.
        else if (_function is TqConstructor && _function.Owner is TqStruct derived
                 && _owner.Backend.BaseFields.TryGetValue(_method.DeclaringType.FullName, out var baseField)
                 && _owner.FindBaseConstructor(derived) is { } valueBaseConstructor)
        {
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldflda, _owner.Use(baseField));
            _il.Emit(OpCodes.Call, valueBaseConstructor);
        }

        if (_function is TqConstructor) EmitInitializers();
        if (_function.Body != null) EmitStatement(_function.Body);

        // The end of the function: what a function without `return` at its end gives.
        var returnType = _method.ReturnType;
        if (IsVoid(returnType))
            _il.Emit(OpCodes.Ret);
        else if (IsFailable(returnType, out var result) && IsVoid(result))
            EmitDefaultReturn();
        else
            _owner.EmitStub(_il, $"Tq: '{_function.Name}' reached its end without returning a value");
    }

    // ---- Helpers

    private void Report(string message, SourceLocation? location) => _owner.Report(message, location);
    private TypeReference Core(string name) => _owner.Backend.Core.Require(_target, name);

    private static bool IsVoid(TypeReference? type) => type == null || type.FullName == "System.Void";

    /// <summary>`Tq.Failable` (`!void`) and `Tq.Failable&lt;T&gt;` (`!T`); `result` is `T`, or void.</summary>
    private bool IsFailable(TypeReference type, out TypeReference result)
    {
        result = Core("System.Void");

        if (type.FullName == "Tq.Failable") return true;
        if (type is GenericInstanceType generic && generic.ElementType.FullName == "Tq.Failable`1")
        {
            result = generic.GenericArguments[0];
            return true;
        }

        return false;
    }

    /// <summary>The initial values of the fields of the struct, which a constructor gives before its own code.</summary>
    internal void EmitInitializers()
    {
        if (_function.Owner is not TqStruct structure) return;

        foreach (var (field, definition) in _owner.InstanceInitializers(structure))
        {
            _il.Emit(OpCodes.Ldarg_0);
            if (definition.DeclaringType.IsValueType) EmitBasePath(_method.DeclaringType, definition.DeclaringType);

            var host = TqHost(null, definition.DeclaringType);
            EmitValue(field.Value!, host != null ? Instantiate(definition.FieldType, [], host.GenericArguments) : definition.FieldType);
            _il.Emit(OpCodes.Stfld, host != null ? OnHost(_owner.Use(definition), host) : _owner.Use(definition));
        }
    }

    /// <summary>The values of the constants that are not literals, which the type initializer gives, in the order they are written.</summary>
    internal void EmitStaticInitializers(List<(TqField Field, FieldDefinition Definition)> pending)
    {
        foreach (var (field, definition) in pending.OrderBy(item => item.Field.SourceLocation.span.LineStart)
                     .ThenBy(item => item.Field.SourceLocation.span.ColStart))
        {
            EmitValue(field.Value!, definition.FieldType);
            _il.Emit(OpCodes.Stsfld, definition);
        }
    }

    private VariableDefinition NewTemp(TypeReference type)
    {
        var variable = new VariableDefinition(type);
        _method.Body.Variables.Add(variable);
        return variable;
    }

    /// <summary>An expression for the messages: what it is and the type the type checker gave it (`?` if none).</summary>
    private string Shape(IrExpression expression)
    {
        var text = expression switch
        {
            IrReference reference => NameOf(reference.Reference),
            IrAccess access => $"{Shape(access.baseExpr)}.{NameOf(access.access.Reference)}",
            IrCall call => $"a call of {Shape(call.callee)}",
            _ => $"a {expression.GetType().Name}",
        };

        return $"'{text}' : {_types.Get(expression)?.ToString() ?? "?"}";
    }

    private static string NameOf(Ref reference) => reference switch
    {
        UnsolvedRef unsolved => $"{unsolved.Value} (unresolved name)",
        FunctionGroupTypeReference group => $"{group.FunctionGroup.Name} (group of overloads)",
        FunctionTypeReference function => function.Function.Name,
        _ => reference.ToString() ?? reference.GetType().Name,
    };

    private TypeReference? TypeOfNode(IrExpression expression) =>
        _types.Get(expression) is { } type ? _owner.MapType(type, expression.Location) : null;

    // ---- Arrays and slices

    private static bool IsMemory(TypeReference? type, out GenericInstanceType memory) => ModuleEmitter.IsMemory(type, out memory);

    private GenericInstanceType SpanOf(TypeReference element)
    {
        var span = new GenericInstanceType(Core("System.Span`1"));
        span.GenericArguments.Add(element);
        return span;
    }

    /// <summary>`Memory<T>` of the array on the stack, which it refers to.</summary>
    private void EmitMemoryOf(GenericInstanceType memory) =>
        _il.Emit(OpCodes.Newobj, _owner.GenericMember("System.Memory`1", memory, ".ctor", 1, parameter => parameter.ParameterType is ArrayType));

    /// <summary>
    /// The `Memory<T>` on the stack becomes the `ref T` of the element whose index `emitIndex` leaves on the stack
    /// (`memory.Span[index]`; the `Span` is a temporary, which is fine because a `ref` to an element outlives it).
    /// </summary>
    private void EmitElementAddress(GenericInstanceType memory, Action emitIndex)
    {
        var span = SpanOf(memory.GenericArguments[0]);

        var memoryTemp = NewTemp(memory);
        _il.Emit(OpCodes.Stloc, memoryTemp);
        _il.Emit(OpCodes.Ldloca, memoryTemp);
        _il.Emit(OpCodes.Call, _owner.GenericMember("System.Memory`1", memory, "get_Span", 0));

        var spanTemp = NewTemp(span);
        _il.Emit(OpCodes.Stloc, spanTemp);
        _il.Emit(OpCodes.Ldloca, spanTemp);
        emitIndex();
        _il.Emit(OpCodes.Call, _owner.GenericMember("System.Span`1", span, "get_Item", 1));
    }

    /// <summary>The length, as an `int`, of the array or the `Memory<T>` on the stack.</summary>
    private TypeReference EmitLength(TypeReference actual)
    {
        if (IsMemory(actual, out var memory))
        {
            var temp = NewTemp(memory);
            _il.Emit(OpCodes.Stloc, temp);
            _il.Emit(OpCodes.Ldloca, temp);
            _il.Emit(OpCodes.Call, _owner.GenericMember("System.Memory`1", memory, "get_Length", 0));
        }
        else
        {
            _il.Emit(OpCodes.Ldlen);
            _il.Emit(OpCodes.Conv_I4);
        }

        return Core("System.Int32");
    }

    /// <summary>`memory[from .. to]` (both included): the `Memory<T>` on the stack, or the array, as the part that starts at `from`.</summary>
    private TypeReference EmitSliceOf(TypeReference actual, IrRange range)
    {
        GenericInstanceType memory;
        if (actual is ArrayType array)
        {
            memory = _owner.MemoryOf(array.ElementType);
            EmitMemoryOf(memory);
        }
        else if (!IsMemory(actual, out memory))
        {
            Report("This value cannot be sliced", range.Location);
            return actual;
        }

        var int32 = Core("System.Int32");
        var memoryTemp = NewTemp(memory);
        var from = NewTemp(int32);
        var to = NewTemp(int32);

        _il.Emit(OpCodes.Stloc, memoryTemp);
        EmitValue(range.From, int32);
        _il.Emit(OpCodes.Stloc, from);
        EmitValue(range.To, int32);
        _il.Emit(OpCodes.Stloc, to);

        _il.Emit(OpCodes.Ldloca, memoryTemp);
        _il.Emit(OpCodes.Ldloc, from);
        _il.Emit(OpCodes.Ldloc, to);
        _il.Emit(OpCodes.Ldloc, from);
        _il.Emit(OpCodes.Sub);
        _il.Emit(OpCodes.Ldc_I4_1);
        _il.Emit(OpCodes.Add);
        _il.Emit(OpCodes.Call, _owner.GenericMember("System.Memory`1", memory, "Slice", 2));
        return memory;
    }

    private ParameterDefinition ParameterOf(TqParameter parameter)
    {
        // The parameters that are types are not parameters of the method: they are its generic parameters.
        var index = 0;
        foreach (var candidate in _function.Parameters ?? [])
        {
            if (GenericParameterTypeRef.IsTypeParameter(candidate))
            {
                if (ReferenceEquals(candidate, parameter))
                    throw new BackendError($"'{parameter.Name}' is a type: it exists only while compiling and has no value in .NET", _function.SourceLocation);
                continue;
            }

            if (ReferenceEquals(candidate, parameter) && index < _method.Parameters.Count) return _method.Parameters[index];
            index++;
        }

        throw new BackendError($"The parameter '{parameter.Name}' does not belong to '{_function.Name}'", _function.SourceLocation);
    }

    private VariableDefinition GetLocal(IrLocal local)
    {
        if (_locals.TryGetValue(local, out var known)) return known;

        TypeReference type;
        if (local.type != null && !IsUntypedConstant(local.type))
        {
            type = _owner.MapExpression(local.type);
        }
        else if (_types.Get(local) is { } inferred)
        {
            type = _owner.MapType(inferred, local.Location);
        }
        else
        {
            Report($"The type of the variable '{local.name}' is not known", local.Location);
            type = Core("System.Object");
        }

        var variable = NewTemp(type);
        _locals[local] = variable;
        return variable;
    }

    /// <summary>`const x = 1`: the type is the one of the value.</summary>
    private static bool IsUntypedConstant(IrExpression type) =>
        type is IrReference { Reference: TypeMetaReference { Type: GenericInstance { Arguments: [var inner] } instance } }
        && ReferenceEquals(instance.Definition, CompilerStd.Types.Const)
        && ModuleEmitter.IsUntyped(inner);

    private MethodReference Library(string typeName, string name, params string[] parameterTypes)
    {
        var type = _owner.Backend.Core.Find(typeName)
                   ?? throw new BackendError($"The type '{typeName}' was not found in 'System.Runtime'");

        var method = type.Methods.FirstOrDefault(candidate => candidate.Name == name
                                                              && candidate.Parameters.Count == parameterTypes.Length
                                                              && candidate.Parameters.Select(parameter => parameter.ParameterType.FullName)
                                                                  .SequenceEqual(parameterTypes))
                     ?? throw new BackendError($"The method '{typeName}.{name}' was not found in 'System.Runtime'");

        return _target.ImportReference(method);
    }

    // ---- Numbers

    private readonly record struct Num(bool Float, bool Signed, int Bits, bool Native);

    private static bool TryNum(TypeReference? type, out Num num)
    {
        switch (type?.FullName)
        {
            case "System.SByte": num = new Num(false, true, 8, false); return true;
            case "System.Byte": num = new Num(false, false, 8, false); return true;
            case "System.Int16": num = new Num(false, true, 16, false); return true;
            case "System.UInt16": num = new Num(false, false, 16, false); return true;
            case "System.Char": num = new Num(false, false, 16, false); return true;
            case "System.Int32": num = new Num(false, true, 32, false); return true;
            case "System.UInt32": num = new Num(false, false, 32, false); return true;
            case "System.Int64": num = new Num(false, true, 64, false); return true;
            case "System.UInt64": num = new Num(false, false, 64, false); return true;
            case "System.IntPtr": num = new Num(false, true, 0, true); return true;
            case "System.UIntPtr": num = new Num(false, false, 0, true); return true;
            case "System.Single": num = new Num(true, true, 32, false); return true;
            case "System.Double": num = new Num(true, true, 64, false); return true;
            default: num = default; return false;
        }
    }

    private static int Rank(Num num) => num.Float ? 1000 + num.Bits : num.Native ? 64 : num.Bits;

    /// <summary>An integer or decimal literal, as a value of the expected type (`i32` or `f64` when nothing is expected).</summary>
    private TypeReference EmitNumber(decimal value, bool isDecimal, TypeReference? expected, SourceLocation location)
    {
        var target = expected != null && TryNum(expected, out var num) ? expected : Core(isDecimal ? "System.Double" : "System.Int32");
        TryNum(target, out num);

        try
        {
            if (num.Float)
            {
                if (num.Bits == 32) _il.Emit(OpCodes.Ldc_R4, (float)value);
                else _il.Emit(OpCodes.Ldc_R8, (double)value);
            }
            else if (isDecimal)
            {
                Report($"The decimal literal {value} cannot be a '{target.FullName}'", location);
                _il.Emit(OpCodes.Ldc_I4_0);
            }
            else if (num.Native)
            {
                _il.Emit(OpCodes.Ldc_I4, unchecked((int)(long)value));
                _il.Emit(num.Signed ? OpCodes.Conv_I : OpCodes.Conv_U);
            }
            else if (num.Bits == 64)
            {
                _il.Emit(OpCodes.Ldc_I8, num.Signed ? (long)value : unchecked((long)(ulong)value));
            }
            else
            {
                _il.Emit(OpCodes.Ldc_I4, num.Signed ? (int)value : unchecked((int)(uint)value));
            }
        }
        catch (OverflowException)
        {
            Report($"The literal {value} does not fit '{target.FullName}'", location);
            _il.Emit(OpCodes.Ldc_I4_0);
        }

        return target;
    }

    /// <summary>The value of a constant of the metadata (a literal field of .NET, the case of an enum).</summary>
    private void EmitConstant(object? constant, SourceLocation? location)
    {
        switch (constant)
        {
            case null: _il.Emit(OpCodes.Ldnull); break;
            case bool boolean: _il.Emit(boolean ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0); break;
            case string text: _il.Emit(OpCodes.Ldstr, text); break;
            case char character: _il.Emit(OpCodes.Ldc_I4, (int)character); break;
            case sbyte or byte or short or ushort or int: _il.Emit(OpCodes.Ldc_I4, Convert.ToInt32(constant)); break;
            case uint unsigned: _il.Emit(OpCodes.Ldc_I4, unchecked((int)unsigned)); break;
            case long signed64: _il.Emit(OpCodes.Ldc_I8, signed64); break;
            case ulong unsigned64: _il.Emit(OpCodes.Ldc_I8, unchecked((long)unsigned64)); break;
            case float single: _il.Emit(OpCodes.Ldc_R4, single); break;
            case double number: _il.Emit(OpCodes.Ldc_R8, number); break;
            default: Report($"The constant '{constant}' cannot be emitted to .NET yet", location); _il.Emit(OpCodes.Ldnull); break;
        }
    }

    private void ConvertNumber(Num from, Num to)
    {
        if (to.Float)
        {
            if (from.Float && from.Bits == to.Bits) return;
            if (!from.Float && !from.Signed) _il.Emit(OpCodes.Conv_R_Un);
            _il.Emit(to.Bits == 32 ? OpCodes.Conv_R4 : OpCodes.Conv_R8);
            return;
        }

        if (to.Native)
        {
            if (from.Native) return;
            _il.Emit(from.Float || from.Signed ? OpCodes.Conv_I : OpCodes.Conv_U);
            return;
        }

        switch (to.Bits)
        {
            case 64:
                if (from.Bits == 64 && !from.Native && !from.Float) return;
                _il.Emit((from.Float ? to.Signed : from.Signed) ? OpCodes.Conv_I8 : OpCodes.Conv_U8);
                return;

            case 32:
                if (from.Bits <= 32 && !from.Native && !from.Float) return;
                _il.Emit(to.Signed ? OpCodes.Conv_I4 : OpCodes.Conv_U4);
                return;

            default:
                // What is already in range of the smaller type needs nothing.
                if (!from.Float && !from.Native && from.Bits == to.Bits && from.Signed == to.Signed) return;
                if (!from.Float && !from.Native && from.Bits < to.Bits && (to.Signed || !from.Signed)) return;

                if (to.Bits == 8) _il.Emit(to.Signed ? OpCodes.Conv_I1 : OpCodes.Conv_U1);
                else _il.Emit(to.Signed ? OpCodes.Conv_I2 : OpCodes.Conv_U2);
                return;
        }
    }

    // ---- Statements

    private void EmitStatement(IrNode node)
    {
        switch (node)
        {
            case IrBody body:
                foreach (var child in body.Children) EmitStatement(child);
                break;

            case IrReturnStatement @return:
                EmitReturn(@return);
                break;

            case IrExpressionStatement statement:
                EmitDiscard(statement.Expression);
                break;

            case IrIf @if:
                EmitIf(@if);
                break;

            case IrWhile @while:
                EmitWhile(@while);
                break;

            case IrBreak @break:
                if (_breakTargets.Count == 0) Report("'break' outside of a loop", @break.Location);
                else _il.Emit(OpCodes.Br, _breakTargets.Peek());
                break;

            case IrExpression expression:
                EmitDiscard(expression);
                break;

            default:
                Report($"The node {node.GetType().Name} cannot be emitted to .NET yet", node.location);
                break;
        }
    }

    private void EmitDiscard(IrExpression expression)
    {
        if (Emit(expression, null, discard: true) != null) _il.Emit(OpCodes.Pop);
    }

    private void EmitIf(IrIf statement)
    {
        var otherwise = _il.Create(OpCodes.Nop);

        EmitValue(statement.Condition, Core("System.Boolean"));
        _il.Emit(OpCodes.Brfalse, otherwise);
        EmitStatement(statement.Then);

        if (statement.Else == null)
        {
            _il.Append(otherwise);
            return;
        }

        var end = _il.Create(OpCodes.Nop);
        _il.Emit(OpCodes.Br, end);
        _il.Append(otherwise);
        EmitStatement(statement.Else);
        _il.Append(end);
    }

    /// <summary>The declaration runs once; the condition is tested before each turn and the step runs after it.</summary>
    private void EmitWhile(IrWhile loop)
    {
        var start = _il.Create(OpCodes.Nop);
        var end = _il.Create(OpCodes.Nop);

        if (loop.Declaration != null) EmitDiscard(loop.Declaration);

        _il.Append(start);
        EmitValue(loop.Condition, Core("System.Boolean"));
        _il.Emit(OpCodes.Brfalse, end);

        _breakTargets.Push(end);
        EmitStatement(loop.Body);
        _breakTargets.Pop();

        if (loop.Step != null) EmitDiscard(loop.Step);
        _il.Emit(OpCodes.Br, start);
        _il.Append(end);
    }

    private void EmitReturn(IrReturnStatement statement)
    {
        var returnType = _method.ReturnType;

        if (statement.ReturnValue == null)
        {
            if (IsVoid(returnType)) _il.Emit(OpCodes.Ret);
            else EmitDefaultReturn();
            return;
        }

        // `return f()` where `f` gives nothing.
        if (IsVoid(returnType))
        {
            EmitDiscard(statement.ReturnValue);
            _il.Emit(OpCodes.Ret);
            return;
        }

        // `!T` is returned as a `T` and wrapped: no error.
        if (IsFailable(returnType, out var result))
        {
            if (IsVoid(result))
            {
                EmitDiscard(statement.ReturnValue);
                EmitDefaultReturn();
                return;
            }

            var wrapper = NewTemp(returnType);
            var value = _owner.Backend.Runtime.FailableOf.Fields.First(field => field.Name == "Value");

            _il.Emit(OpCodes.Ldloca, wrapper);
            EmitValue(statement.ReturnValue, result);
            _il.Emit(OpCodes.Stfld, new CecilField(value.Name, value.FieldType, returnType));
            _il.Emit(OpCodes.Ldloc, wrapper);
            _il.Emit(OpCodes.Ret);
            return;
        }

        EmitValue(statement.ReturnValue, returnType);
        _il.Emit(OpCodes.Ret);
    }

    /// <summary>The value of `return` with nothing to say: a `Tq.Failable` with no error.</summary>
    private void EmitDefaultReturn()
    {
        var returnType = _method.ReturnType;
        if (IsVoid(returnType))
        {
            _il.Emit(OpCodes.Ret);
            return;
        }

        var empty = NewTemp(returnType);
        _il.Emit(OpCodes.Ldloca, empty);
        _il.Emit(OpCodes.Initobj, returnType);
        _il.Emit(OpCodes.Ldloc, empty);
        _il.Emit(OpCodes.Ret);
    }

    // ---- Expressions

    /// <summary>
    /// `null` of a `?T`: a reference is `ldnull`; for a value, `?T` is a `Nullable<T>`, which is empty when it is all zeros.
    /// The type is the one that is expected, or the one the type checker gave this `null`.
    /// </summary>
    private TypeReference EmitNull(IrNull nothing, TypeReference? expected)
    {
        var type = expected;
        if (type == null && _types.Get(nothing) is { } given and not NullTypeRef) type = _owner.MapType(given, nothing.Location);

        if (type is { IsValueType: true })
        {
            var empty = NewTemp(type);
            _il.Emit(OpCodes.Ldloca, empty);
            _il.Emit(OpCodes.Initobj, type);
            _il.Emit(OpCodes.Ldloc, empty);
            return type;
        }

        _il.Emit(OpCodes.Ldnull);
        return type ?? Core("System.Object");
    }

    /// <summary>Emits the expression; the type of what it left on the stack, or null if it left nothing.</summary>
    private TypeReference? Emit(IrExpression expression, TypeReference? expected, bool discard = false)
    {
        switch (expression)
        {
            case IrComptimeInt integer:
                // `u8.max` is an integer constant that already has its type; a literal takes the one it is expected to be.
                if (_types.Get(integer) is { } sized and not ComptimeIntegerTypeRef) expected = _owner.MapType(sized, integer.Location);
                return EmitInteger(integer.BigInteger, expected, integer.Location);

            case IrComptimeDecimal number:
                return EmitNumber(number.Value, true, expected, number.Location);

            case IrComptimeBool boolean:
                _il.Emit(boolean.Value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                return Core("System.Boolean");

            case IrStringConstant text:
                _il.Emit(OpCodes.Ldstr, text.Value);
                return Core("System.String");

            case IrNull nothing: return EmitNull(nothing, expected);

            case IrStringConcat concat: return EmitConcat(concat);
            case IrReference reference: return EmitReference(reference);
            case IrAccess access: return EmitAccess(access);
            case IrCall call: return EmitCall(call);
            case IrNew creation: return EmitNew(creation);
            case IrConvert conversion: return EmitConvert(conversion);
            case IrAssign assign: return EmitAssign(assign, discard);
            case IrBinary binary: return EmitBinary(binary);
            case IrUnary unary: return EmitUnary(unary, expected, discard);
            case IrIndex index: return EmitIndex(index);
            case IrArray array: return EmitArray(array);
            case IrAddressOf addressOf: return EmitAddressOf(addressOf);
            case IrMatch match: return EmitMatch(match, expected, discard);

            // `let T x`: the variable only exists.
            case IrLocal local:
            {
                var variable = GetLocal(local);

                // An array exists as soon as it is declared: memory with room for all of its elements.
                if (local.type != null && TypeExpressions.Raw(local.type) is ArrayTypeRef { Length: var length } && variable.VariableType is ArrayType storage)
                {
                    _il.Emit(OpCodes.Ldc_I4, length);
                    _il.Emit(OpCodes.Newarr, storage.ElementType);
                    _il.Emit(OpCodes.Stloc, variable);
                }

                return null;
            }

            default:
                Report($"The expression {expression.GetType().Name} cannot be emitted to .NET yet", expression.Location);
                return null;
        }
    }

    /// <summary>The expression as a value of the expected type (converted if it takes a conversion).</summary>
    private TypeReference EmitValue(IrExpression expression, TypeReference? expected)
    {
        var actual = Emit(expression, expected);
        if (actual == null)
        {
            Report("This expression has no value", expression.Location);
            return expected ?? Core("System.Object");
        }

        if (expected == null || actual.FullName == expected.FullName) return actual;

        // The array of .NET (what it gives and takes) and the slice of Tq: `T[]` is wrapped in a `Memory<T>`; the other way a
        // `Memory<T>` is the array of it, which is a copy unless it is all of one (a change made by .NET is not seen then).
        if (actual is ArrayType && IsMemory(expected, out var wrapper))
        {
            EmitMemoryOf(wrapper);
            return expected;
        }

        if (IsMemory(actual, out var viewed) && expected is ArrayType)
        {
            var viewTemp = NewTemp(viewed);
            _il.Emit(OpCodes.Stloc, viewTemp);
            _il.Emit(OpCodes.Ldloca, viewTemp);
            _il.Emit(OpCodes.Call, _owner.GenericMember("System.Memory`1", viewed, "ToArray", 0));
            return expected;
        }

        // A struct of a hierarchy where `*S` is expected: a reference to a new object that has a copy of it.
        if (IsRefInterface(expected) && actual.IsValueType && _owner.Backend.RefBoxes.TryGetValue(actual.FullName, out var box))
        {
            _il.Emit(OpCodes.Newobj, _owner.Use(box.Methods.First(method => method.IsConstructor && method.HasParameters)));
            return expected;
        }

        // `*D` as `*B` needs nothing (the class of D extends the one of B); `x as *D` is a cast.
        if (IsRefInterface(expected) && IsRefInterface(actual))
        {
            _il.Emit(OpCodes.Castclass, expected);
            return expected;
        }

        // A struct of a hierarchy as the one it extends: a copy of the part that is the base.
        if (actual.IsValueType && expected.IsValueType && actual.FullName != expected.FullName
            && _owner.Backend.BaseFields.ContainsKey(actual.FullName))
        {
            var copy = NewTemp(actual);
            _il.Emit(OpCodes.Stloc, copy);
            _il.Emit(OpCodes.Ldloca, copy);
            EmitBasePath(actual, expected);
            _il.Emit(OpCodes.Ldobj, expected);
            return expected;
        }

        // `x as string`: whatever it is, its text (a null gives an empty one).
        if (expected.FullName == "System.String")
        {
            if (actual.IsValueType || actual is GenericParameter) _il.Emit(OpCodes.Box, actual);
            _il.Emit(OpCodes.Call, Library("System.Convert", "ToString", "System.Object"));
            return expected;
        }

        if (TryNum(actual, out var from) && TryNum(expected, out var to)) ConvertNumber(from, to);
        else if ((actual.IsValueType || actual is GenericParameter) && !expected.IsValueType) _il.Emit(OpCodes.Box, actual);

        return expected;
    }

    private TypeReference? EmitConvert(IrConvert conversion)
    {
        var target = _owner.MapExpression(conversion.toType);
        EmitValue(conversion.Expression, target);
        return target;
    }

    private TypeReference EmitConcat(IrStringConcat concat)
    {
        var @object = Core("System.Object");

        _il.Emit(OpCodes.Ldc_I4, concat.Parts.Length);
        _il.Emit(OpCodes.Newarr, @object);

        for (var i = 0; i < concat.Parts.Length; i++)
        {
            _il.Emit(OpCodes.Dup);
            _il.Emit(OpCodes.Ldc_I4, i);

            var part = Emit(concat.Parts[i], null);
            if (part == null) Report("This expression has no value", concat.Parts[i].Location);
            else if (part.IsValueType) _il.Emit(OpCodes.Box, part);

            _il.Emit(OpCodes.Stelem_Ref);
        }

        _il.Emit(OpCodes.Call, Library("System.String", "Concat", "System.Object[]"));
        return Core("System.String");
    }

    /// <summary>
    /// `match`: the value is kept in a variable, each arm tests its conditions and jumps to its body, and `default` (wherever it is
    /// written) is the one that runs when none fits. As a value, the result is kept in a variable too, so the stack is the same wherever the arms end.
    /// </summary>
    private TypeReference? EmitMatch(IrMatch match, TypeReference? expected, bool discard)
    {
        if (match.SubjectLocal == null || match.Arms.Any(arm => !arm.IsDefault && arm.Conditions == null))
        {
            Report("This match was not checked", match.Location);
            return null;
        }

        TypeReference? resultType = null;
        VariableDefinition? result = null;
        if (!discard)
        {
            resultType = expected ?? (_types.Get(match) is { } known ? _owner.MapType(known, match.Location) : null);
            if (resultType == null)
            {
                Report("The arms of this 'match' do not have a type in common, so it cannot be used as a value", match.Location);
                return null;
            }

            if (match.Arms.Any(arm => arm.Body is IrBody))
                Report("A 'match' used as a value cannot have an arm that is a block", match.Location);

            result = NewTemp(resultType);
        }

        var subject = GetLocal(match.SubjectLocal);
        EmitValue(match.Subject, subject.VariableType);
        _il.Emit(OpCodes.Stloc, subject);

        var end = _il.Create(OpCodes.Nop);
        var labels = match.Arms.Select(_ => _il.Create(OpCodes.Nop)).ToArray();

        for (var i = 0; i < match.Arms.Length; i++)
        {
            foreach (var condition in match.Arms[i].Conditions ?? [])
            {
                EmitValue(condition, Core("System.Boolean"));
                _il.Emit(OpCodes.Brtrue, labels[i]);
            }
        }

        var fallback = Array.FindIndex(match.Arms, arm => arm.IsDefault);
        _il.Emit(OpCodes.Br, fallback >= 0 ? labels[fallback] : end);

        for (var i = 0; i < match.Arms.Length; i++)
        {
            _il.Append(labels[i]);

            switch (match.Arms[i].Body)
            {
                case IrBody block:
                    EmitStatement(block);
                    break;

                case IrExpression value when discard:
                    EmitDiscard(value);
                    break;

                case IrExpression value:
                    EmitValue(value, resultType);
                    _il.Emit(OpCodes.Stloc, result!);
                    break;
            }

            _il.Emit(OpCodes.Br, end);
        }

        _il.Append(end);
        if (result == null) return null;

        _il.Emit(OpCodes.Ldloc, result);
        return resultType;
    }

    /// <summary>`&array`: the slice that refers to the array.</summary>
    private TypeReference? EmitAddressOf(IrAddressOf addressOf)
    {
        var actual = Emit(addressOf.Operand, null);
        if (actual is not ArrayType array)
        {
            if (actual != null) Report("'&' takes an array", addressOf.Location);
            return actual;
        }

        var memory = _owner.MemoryOf(array.ElementType);
        EmitMemoryOf(memory);
        return memory;
    }

    private TypeReference? EmitArray(IrArray array)
    {
        if (_types.Get(array) is not ArrayTypeRef slice)
        {
            Report("The type of the elements of this array is not known", array.Location);
            return null;
        }

        var element = _owner.MapType(slice.Element, array.Location);

        _il.Emit(OpCodes.Ldc_I4, array.Elements.Length);
        _il.Emit(OpCodes.Newarr, element);

        for (var i = 0; i < array.Elements.Length; i++)
        {
            _il.Emit(OpCodes.Dup);
            _il.Emit(OpCodes.Ldc_I4, i);
            EmitValue(array.Elements[i], element);
            _il.Emit(OpCodes.Stelem_Any, element);
        }

        return new ArrayType(element);
    }

    private TypeReference? EmitIndex(IrIndex index)
    {
        if (index.Indices.Length != 1)
        {
            Report("Only one index is supported", index.Location);
            return null;
        }

        var target = Emit(index.Target, null);

        if (index.Indices[0] is IrRange range && target != null)
            return EmitSliceOf(target, range);

        if (target is ArrayType array)
        {
            EmitValue(index.Indices[0], Core("System.Int32"));
            _il.Emit(OpCodes.Ldelem_Any, array.ElementType);
            return array.ElementType;
        }

        if (IsMemory(target, out var memory))
        {
            var element = memory.GenericArguments[0];
            EmitElementAddress(memory, () => EmitValue(index.Indices[0], Core("System.Int32")));
            _il.Emit(OpCodes.Ldobj, element);
            return element;
        }

        if (target?.FullName == "System.String")
        {
            EmitValue(index.Indices[0], Core("System.Int32"));
            _il.Emit(OpCodes.Callvirt, Library("System.String", "get_Chars", "System.Int32"));
            return Core("System.Char");
        }

        Report("This value cannot be indexed", index.Location);
        return null;
    }

    // ---- Names

    private TypeReference? EmitReference(IrReference reference)
    {
        switch (reference.Reference)
        {
            case LocalReference local:
            {
                var variable = GetLocal(local.Local);
                _il.Emit(OpCodes.Ldloc, variable);
                return variable.VariableType;
            }

            case ParameterReference { Parameter: var typeParameter } when GenericParameterTypeRef.IsTypeParameter(typeParameter):
                Report($"'{typeParameter.Name}' is a type: it exists only while compiling, and as a value it has no .NET equivalent", reference.Location);
                return null;

            case ParameterReference parameter:
            {
                var definition = ParameterOf(parameter.Parameter);
                _il.Emit(OpCodes.Ldarg, definition);

                // `*T` of a value: the value itself is wanted.
                if (definition.ParameterType is not ByReferenceType reference2) return definition.ParameterType;
                _il.Emit(OpCodes.Ldobj, reference2.ElementType);
                return reference2.ElementType;
            }

            case ThisReference:
                _il.Emit(OpCodes.Ldarg_0);
                if (_method.DeclaringType.IsValueType) _il.Emit(OpCodes.Ldobj, _method.DeclaringType);
                return _method.DeclaringType;

            case IrField field:
                return LoadField(field.Field, null, reference.Location);

            case BackingFieldReference backing:
                return LoadField(backing.Property, null, reference.Location);

            default:
                Report($"'{reference.Reference}' cannot be used as a value in .NET yet", reference.Location);
                return null;
        }
    }

    private TypeReference? EmitAccess(IrAccess access)
    {
        switch (access.access.Reference)
        {
            case IrField field:
                return LoadField(field.Field, access.baseExpr, access.Location);

            case BackingFieldReference backing:
                return LoadField(backing.Property, null, access.Location);

            // `slice.len`: the length of the array, as an `int`.
            case UnsolvedRef { Value: "len" } when _types.Get(access.baseExpr) is SliceTypeRef or ArrayTypeRef:
                return EmitLength(EmitValue(access.baseExpr, null));

            default:
                Report($"'{access.access.Reference}' cannot be used as a value in .NET yet", access.Location);
                return null;
        }
    }

    /// <summary>
    /// A field, a constant or a property of .NET. `baseExpr` is the value it is read from; without one it is a name
    /// of the struct the code is in (or of the namespace), so an instance field is of `this`.
    /// </summary>
    private TypeReference? LoadField(object member, IrExpression? baseExpr, SourceLocation location)
    {
        switch (member)
        {
            case TqNamedCase named when _owner.Backend.Definitions.TryGetValue(named, out var caseDefinition) && caseDefinition is FieldDefinition caseField:
                EmitConstant(caseField.Constant, location);
                return caseField.DeclaringType;

            case TqField field when _owner.Backend.Definitions.TryGetValue(field, out var definition) && definition is FieldDefinition fieldDefinition:
                if (fieldDefinition.HasConstant)
                {
                    EmitConstant(fieldDefinition.Constant, location);
                    return fieldDefinition.FieldType;
                }

                if (fieldDefinition.IsStatic)
                {
                    _il.Emit(OpCodes.Ldsfld, _owner.Use(fieldDefinition));
                }
                else
                {
                    EmitFieldReceiver(baseExpr, fieldDefinition.DeclaringType);

                    // A field of a generic struct is read through the type with its arguments.
                    var tqHost = TqHost(baseExpr, fieldDefinition.DeclaringType);
                    _il.Emit(OpCodes.Ldfld, tqHost != null ? OnHost(_owner.Use(fieldDefinition), tqHost) : _owner.Use(fieldDefinition));
                    return tqHost != null ? Instantiate(fieldDefinition.FieldType, [], tqHost.GenericArguments) : fieldDefinition.FieldType;
                }

                return fieldDefinition.FieldType;

            case TqProperty property when _owner.Backend.BackingFields.TryGetValue(property, out var backing):
                if (backing.IsStatic)
                {
                    _il.Emit(OpCodes.Ldsfld, _owner.Use(backing));
                    return backing.FieldType;
                }

                _il.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Ldfld, _owner.Use(backing));
                return backing.FieldType;

            case DotnetField dotnetField:
            {
                var reference = _owner.Module.ImportReference(dotnetField.Reference);

                // A field of `List(i8)` is read through that type, with its own type in terms of `i8`.
                var host = dotnetField.IsStatic ? null : HostOf(baseExpr);
                if (host != null) reference = OnHost(reference, host);

                if (dotnetField.IsConstant)
                {
                    EmitConstant(dotnetField.Constant, location);
                }
                else if (dotnetField.IsStatic)
                {
                    _il.Emit(OpCodes.Ldsfld, reference);
                }
                else
                {
                    EmitFieldReceiver(baseExpr, reference.DeclaringType);
                    _il.Emit(OpCodes.Ldfld, reference);
                }

                return Instantiate(reference.FieldType, [], host?.GenericArguments);
            }

            case DotnetProperty { Getter: { } getter }:
            {
                var reference = _owner.Module.ImportReference(getter);
                var host = reference.HasThis ? HostOf(baseExpr) : null;
                if (host != null) reference = OnHost(reference, host);

                if (reference.HasThis) EmitFieldReceiver(baseExpr, reference.DeclaringType);
                _il.Emit(reference.HasThis && !reference.DeclaringType.IsValueType ? OpCodes.Callvirt : OpCodes.Call, reference);
                return Instantiate(reference.ReturnType, [], host?.GenericArguments);
            }

            default:
                Report($"The member '{(member as IMember)?.Name ?? member.ToString()}' cannot be read in .NET yet", location);
                return null;
        }
    }

    /// <summary>What a member of an instance is read from: the value before the dot, or `this`.</summary>
    private void EmitFieldReceiver(IrExpression? baseExpr, TypeReference declaring)
    {
        if (baseExpr == null)
        {
            // `this`; what a struct inherits is reached through its `__base`.
            _il.Emit(OpCodes.Ldarg_0);
            if (declaring.IsValueType) EmitBasePath(_method.DeclaringType, declaring);
        }
        else
        {
            EmitReceiver(baseExpr, declaring);
        }
    }

    /// <summary>
    /// With the address of a struct on the stack, the address of the struct it extends that is `to`: through `__base`,
    /// as many times as it takes. Nothing if `to` is not above it.
    /// </summary>
    private void EmitBasePath(TypeReference from, TypeReference to)
    {
        if (from is ByReferenceType reference) from = reference.ElementType;

        var current = from.FullName;
        while (current != to.FullName && _owner.Backend.BaseFields.TryGetValue(current, out var field))
        {
            _il.Emit(OpCodes.Ldflda, _owner.Use(field));
            current = field.FieldType.FullName;
        }
    }

    private bool IsRefInterface(TypeReference? type) => type != null && _owner.Backend.RefInterfaceNames.Contains(type.FullName);

    /// <summary>A value type is called and read through its address; a class through the reference.</summary>
    private void EmitReceiver(IrExpression receiver, TypeReference declaring)
    {
        if (!declaring.IsValueType)
        {
            // A value that calls what it inherits from `System.Object` or `ValueType` is boxed first.
            var received = EmitValue(receiver, null);
            if (received.IsValueType || received is GenericParameter) _il.Emit(OpCodes.Box, received);
            return;
        }

        var actual = TypeOfNode(receiver);

        // A reference to a struct of a hierarchy: its value is read through the interface, which has it.
        if (IsRefInterface(actual) && _owner.Backend.RefGetters.TryGetValue(declaring.FullName, out var getter))
        {
            EmitValue(receiver, null);
            _il.Emit(OpCodes.Callvirt, _owner.Use(getter));
            return;
        }

        EmitAddress(receiver);
        if (actual != null) EmitBasePath(actual, declaring);
    }

    /// <summary>The address of a value, for what takes a `this` that is a struct. Whatever has none is copied first.</summary>
    private void EmitAddress(IrExpression expression)
    {
        switch (expression)
        {
            case IrReference { Reference: LocalReference local }:
                _il.Emit(OpCodes.Ldloca, GetLocal(local.Local));
                return;

            case IrReference { Reference: ParameterReference parameter }:
            {
                var definition = ParameterOf(parameter.Parameter);
                _il.Emit(definition.ParameterType is ByReferenceType ? OpCodes.Ldarg : OpCodes.Ldarga, definition);
                return;
            }

            // The `this` of a struct is already an address.
            case IrReference { Reference: ThisReference }:
                _il.Emit(OpCodes.Ldarg_0);
                return;

            case IrAccess { access.Reference: IrField { Field: TqField field } } access
                when _owner.Backend.Definitions.TryGetValue(field, out var definition) && definition is FieldDefinition { HasConstant: false } fieldDefinition:
                if (fieldDefinition.IsStatic)
                {
                    _il.Emit(OpCodes.Ldsflda, _owner.Use(fieldDefinition));
                }
                else
                {
                    EmitReceiver(access.baseExpr, fieldDefinition.DeclaringType);
                    var addressHost = TqHost(access.baseExpr, fieldDefinition.DeclaringType);
                    _il.Emit(OpCodes.Ldflda, addressHost != null ? OnHost(_owner.Use(fieldDefinition), addressHost) : _owner.Use(fieldDefinition));
                }
                return;
        }

        var type = EmitValue(expression, null);
        var copy = NewTemp(type);
        _il.Emit(OpCodes.Stloc, copy);
        _il.Emit(OpCodes.Ldloca, copy);
    }

    // ---- Calls

    private TypeReference? EmitCall(IrCall call)
    {
        var reference = call.callee switch
        {
            IrReference named => named.Reference,
            IrAccess { access.Reference: var member } => member,
            _ => null,
        };
        var receiver = (call.callee as IrAccess)?.baseExpr;

        if (reference is not FunctionTypeReference { Function: var function })
        {
            var arguments = string.Join(", ", call.ArgumentsList.arguments.Select(Shape));
            Report($"This call could not be resolved to a function: the callee is {Shape(call.callee)}, with the arguments ({arguments}). "
                   + "The type checker did not choose an overload for it (see --dump-ir)", call.Location);
            return null;
        }

        switch (function)
        {
            case TqFunction tq when _owner.Backend.Definitions.TryGetValue(tq, out var definition) && definition is MethodDefinition method:
            {
                // Through a reference to a struct of a hierarchy the call goes to the interface, so that the most derived function runs.
                if (!method.IsStatic && receiver != null && method.DeclaringType.IsValueType && IsRefInterface(TypeOfNode(receiver))
                    && _owner.Backend.RefMethods.TryGetValue(tq, out var interfaceMethod))
                {
                    EmitValue(receiver, null);
                    EmitArguments(call.ArgumentsList.arguments, method.Parameters, tq.Parameters);
                    _il.Emit(OpCodes.Callvirt, _owner.Use(interfaceMethod));
                    return IsVoid(method.ReturnType) ? null : method.ReturnType;
                }

                if (!method.IsStatic) EmitFieldReceiver(receiver, method.DeclaringType);

                // A function of a generic struct is called through the type with its arguments: its signature says `T`, the type says `u32`.
                var tqHost = method.IsStatic ? null : TqHost(receiver, method.DeclaringType);
                var typeArguments = EmitArguments(call.ArgumentsList.arguments, method.Parameters, tq.Parameters, tqHost?.GenericArguments);

                // The types a generic function is given are the type arguments of the method.
                MethodReference target = tqHost != null ? OnHost(_owner.Use(method), tqHost) : _owner.Use(method);
                var returned = Instantiate(method.ReturnType, [], tqHost?.GenericArguments);
                if (typeArguments.Count > 0)
                {
                    var instance = new GenericInstanceMethod(target);
                    foreach (var typeArgument in typeArguments) instance.GenericArguments.Add(typeArgument);
                    target = instance;
                    returned = Instantiate(returned, typeArguments, tqHost?.GenericArguments);
                }

                var instanceOfClass = !method.IsStatic && !method.DeclaringType.IsValueType;
                _il.Emit(instanceOfClass ? OpCodes.Callvirt : OpCodes.Call, target);
                return IsVoid(returned) ? null : returned;
            }

            case DotnetMethod dotnet:
            {
                var method = _owner.Module.ImportReference(dotnet.Reference);

                // A method of `List(i8)` is called through that type: its signature says `T`, the type says `i8`.
                var host = method.HasThis ? HostOf(receiver) : null;
                if (host != null) method = OnHost(method, host);
                var hostArguments = host?.GenericArguments;

                if (method.HasThis) EmitFieldReceiver(receiver, method.DeclaringType);

                // The generic parameters of the method come first in the call, then its own parameters.
                TqParameter[]? declared = null;
                if (dotnet.TypeParameters.Count > 0)
                {
                    declared =
                    [
                        ..dotnet.TypeParameters,
                        ..method.Parameters.Select(parameter => new TqParameter(
                            new IrReference(default, new TypeMetaReference(CompilerStd.Types.AnyType)), parameter.Name)),
                    ];
                }
                var dotnetTypeArguments = EmitArguments(call.ArgumentsList.arguments, method.Parameters, declared, hostArguments);

                MethodReference dotnetTarget = method;
                var dotnetReturned = Instantiate(method.ReturnType, [], hostArguments);
                if (dotnetTypeArguments.Count > 0)
                {
                    var instance = new GenericInstanceMethod(method);
                    foreach (var typeArgument in dotnetTypeArguments) instance.GenericArguments.Add(typeArgument);
                    dotnetTarget = instance;
                    dotnetReturned = Instantiate(method.ReturnType, dotnetTypeArguments, hostArguments);
                }

                _il.Emit(method.HasThis && !method.DeclaringType.IsValueType ? OpCodes.Callvirt : OpCodes.Call, dotnetTarget);
                return IsVoid(dotnetReturned) ? null : dotnetReturned;
            }

            default:
                Report($"The function '{function.Name}' is not emitted, so it cannot be called", call.Location);
                return null;
        }
    }

    /// <summary>
    /// The arguments, each as its parameter wants it: a value converted to the type, or the address when it is a `*T` of a
    /// value. What the call leaves out is the default of the parameter. The arguments that are for `type` parameters
    /// emit nothing: they are the types the method is instantiated with, which are returned.
    /// </summary>
    private List<TypeReference> EmitArguments(
        IrExpression[] arguments, IList<ParameterDefinition> parameters, TqParameter[]? declared,
        IList<TypeReference>? hostArguments = null)
    {
        var types = new List<TypeReference>();

        // A Tq function lists the parameters of the type `type` too; .NET lists only the values.
        var count = declared?.Length ?? parameters.Count;
        var runtime = 0;

        for (var i = 0; i < count; i++)
        {
            var argument = i < arguments.Length ? arguments[i] : declared?[i].DefaultValue;

            if (declared != null && GenericParameterTypeRef.IsTypeParameter(declared[i]))
            {
                if (argument == null) Report($"The type '{declared[i].Name}' is missing", _function.SourceLocation);
                else types.Add(_owner.MapExpression(argument));
                continue;
            }

            // The types come first, so what the parameter says in terms of them is known here.
            var type = Instantiate(parameters[runtime].ParameterType, types, hostArguments);
            var name = parameters[runtime].Name;
            runtime++;

            if (argument == null)
            {
                Report($"The argument '{name}' is missing", _function.SourceLocation);
                continue;
            }

            if (type is ByReferenceType) EmitAddress(argument);
            else EmitValue(argument, type);
        }

        return types;
    }

    /// <summary>A type of the signature of a generic method, with the generic parameters replaced by the type arguments.</summary>
    /// <param name="typeArguments">The ones of the method (`!!0`).</param>
    /// <param name="hostArguments">The ones of the generic type the member is of (`!0`): `i8` in `List(i8)`.</param>
    private static TypeReference Instantiate(
        TypeReference type, IList<TypeReference> typeArguments, IList<TypeReference>? hostArguments = null)
    {
        switch (type)
        {
            case GenericParameter { Type: GenericParameterType.Method } generic when generic.Position < typeArguments.Count:
                return typeArguments[generic.Position];

            case GenericParameter { Type: GenericParameterType.Type } generic when hostArguments != null && generic.Position < hostArguments.Count:
                return hostArguments[generic.Position];

            case ArrayType array:
                return new ArrayType(Instantiate(array.ElementType, typeArguments, hostArguments));

            case ByReferenceType byRef:
                return new ByReferenceType(Instantiate(byRef.ElementType, typeArguments, hostArguments));

            case GenericInstanceType instance when instance.GenericArguments.Any(argument => argument.ContainsGenericParameter):
                var result = new GenericInstanceType(instance.ElementType);
                foreach (var argument in instance.GenericArguments)
                    result.GenericArguments.Add(Instantiate(argument, typeArguments, hostArguments));
                return result;

            default:
                return type;
        }
    }

    /// <summary>
    /// The generic type that a value is of, when it is one of .NET (`List(i8)`): what a member of it is called through.
    /// Null for anything else, and for no value.
    /// </summary>
    private GenericInstanceType? HostOf(IrExpression? value)
    {
        if (value == null || _types.Get(value) is not { } type) return null;
        if (CompilerStd.Types.IsRef(type, out var referred)) type = referred;

        return type is StructTypeReference { Struct: GenericInstance { Definition: DotnetClass or DotnetStruct } }
            && _owner.MapType(type, value.Location) is GenericInstanceType host
            ? host
            : null;
    }

    /// <summary>
    /// The generic type of Tq that a member is of, with its arguments: the type of the value it is read from
    /// (`DynamicArray(u32)`), or, in the code of the struct itself (`this`), the struct with its own parameters. Null if the
    /// struct is not generic.
    /// </summary>
    private GenericInstanceType? TqHost(IrExpression? receiver, TypeReference declaring)
    {
        if (!declaring.HasGenericParameters) return null;

        if (receiver != null && _types.Get(receiver) is { } type)
        {
            if (CompilerStd.Types.IsRef(type, out var referred)) type = referred;
            if (_owner.MapType(type, receiver.Location) is GenericInstanceType host && host.ElementType.FullName == declaring.FullName)
                return host;
        }

        var self = new GenericInstanceType(declaring);
        foreach (var parameter in declaring.GenericParameters) self.GenericArguments.Add(parameter);
        return self;
    }

    /// <summary>The same method as a member of this instance of its generic type; the signature stays in terms of `!0`.</summary>
    internal static MethodReference OnHost(MethodReference method, GenericInstanceType host)
    {
        var rebased = new MethodReference(method.Name, method.ReturnType, host)
        {
            HasThis = method.HasThis,
            ExplicitThis = method.ExplicitThis,
            CallingConvention = method.CallingConvention,
        };

        foreach (var parameter in method.Parameters) rebased.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
        foreach (var parameter in method.GenericParameters) rebased.GenericParameters.Add(new GenericParameter(parameter.Name, rebased));
        return rebased;
    }

    private static CecilField OnHost(CecilField field, GenericInstanceType host) => new(field.Name, field.FieldType, host);

    private TypeReference? EmitNew(IrNew creation)
    {
        var type = _owner.MapExpression(creation.Type);
        var arguments = creation.ArgumentsList.arguments;

        switch (creation.Constructor)
        {
            case TqFunction tq when _owner.Backend.Definitions.TryGetValue(tq, out var definition) && definition is MethodDefinition constructor:
                var createdHost = type as GenericInstanceType;
                EmitArguments(arguments, constructor.Parameters, tq.Parameters, createdHost?.GenericArguments);
                _il.Emit(OpCodes.Newobj, createdHost != null ? OnHost(_owner.Use(constructor), createdHost) : _owner.Use(constructor));
                return type;

            case DotnetMethod dotnet:
            {
                var constructor = _owner.Module.ImportReference(dotnet.Reference);
                var hostArguments = (type as GenericInstanceType)?.GenericArguments;
                if (type is GenericInstanceType host) constructor = OnHost(constructor, host);

                EmitArguments(arguments, constructor.Parameters, null, hostArguments);
                _il.Emit(OpCodes.Newobj, constructor);
                return type;
            }
        }

        // No constructor was chosen: a class without one has the one it was given, a struct is all zeros.
        var tqStruct = TypeExpressions.Raw(creation.Type) switch
        {
            StructTypeReference { Struct: TqStruct direct } => direct,
            StructTypeReference { Struct: GenericInstance { Definition: TqStruct generic } } => generic,
            _ => null,
        };

        // A class has the constructor it was given; so has a struct by value that has initial values for its fields.
        if (tqStruct != null && _owner.Backend.DefaultConstructors.TryGetValue(tqStruct, out var defaultConstructor))
        {
            _il.Emit(OpCodes.Newobj, type is GenericInstanceType defaultHost ? OnHost(_owner.Use(defaultConstructor), defaultHost) : _owner.Use(defaultConstructor));
            return type;
        }

        if (type.IsValueType)
        {
            var value = NewTemp(type);
            _il.Emit(OpCodes.Ldloca, value);
            _il.Emit(OpCodes.Initobj, type);
            _il.Emit(OpCodes.Ldloc, value);
            return type;
        }

        Report($"There is no constructor to create '{type.Name}'", creation.Location);
        return null;
    }

    // ---- Operators

    private TypeReference? CommonType(IrExpression left, IrExpression right)
    {
        var leftType = _types.Get(left);
        var rightType = _types.Get(right);

        if (leftType == null || rightType == null)
        {
            Report($"The type of an operand is not known: the left one is {Shape(left)} and the right one is {Shape(right)}", left.Location);
            return null;
        }

        var leftLiteral = leftType is ComptimeIntegerTypeRef or ComptimeDecimalTypeRef;
        var rightLiteral = rightType is ComptimeIntegerTypeRef or ComptimeDecimalTypeRef;

        // A literal goes with the type of its partner; two literals are the default number.
        if (leftLiteral && rightLiteral)
            return Core(leftType is ComptimeDecimalTypeRef || rightType is ComptimeDecimalTypeRef ? "System.Double" : "System.Int32");
        if (leftLiteral) return _owner.MapType(rightType, right.Location);
        if (rightLiteral) return _owner.MapType(leftType, left.Location);

        var a = _owner.MapType(leftType, left.Location);
        var b = _owner.MapType(rightType, right.Location);
        if (a.FullName == b.FullName) return a;

        if (TryNum(a, out var numberA) && TryNum(b, out var numberB)) return Rank(numberA) >= Rank(numberB) ? a : b;
        return a;
    }

    private TypeReference? EmitBinary(IrBinary binary)
    {
        var common = CommonType(binary.Left, binary.Right);
        if (common == null) return null;

        var isComparison = binary.Operator >= BinaryOperator.Equal;
        var result = isComparison ? Core("System.Boolean") : common;

        if (common.FullName == "System.String")
        {
            EmitStringOperator(binary, common);
            return result;
        }

        // Of a type that is not known until it is used (`T`, `anytype`), the values are compared as objects.
        if ((common is GenericParameter || common.FullName == "System.Object")
            && binary.Operator is BinaryOperator.Equal or BinaryOperator.ExactEqual or BinaryOperator.NotEqual or BinaryOperator.NotExactEqual)
        {
            EmitValue(binary.Left, common);
            if (common is GenericParameter) _il.Emit(OpCodes.Box, common);
            EmitValue(binary.Right, common);
            if (common is GenericParameter) _il.Emit(OpCodes.Box, common);

            _il.Emit(OpCodes.Call, Library("System.Object", "Equals", "System.Object", "System.Object"));
            if (binary.Operator is BinaryOperator.NotEqual or BinaryOperator.NotExactEqual)
            {
                _il.Emit(OpCodes.Ldc_I4_0);
                _il.Emit(OpCodes.Ceq);
            }

            return result;
        }

        // Saturating arithmetic, rounded division and powers have no instruction: they are helpers of `Tq.Ops`.
        if (UsesHelper(binary.Operator) && common.FullName != "System.Char") return EmitHelperOperator(binary, common);

        EmitValue(binary.Left, common);
        EmitValue(binary.Right, common);

        // The 128 bits integers are structs: they are compared with their own operators, not with `ceq`.
        if (isComparison && common.FullName is "System.Int128" or "System.UInt128")
        {
            var name = binary.Operator switch
            {
                BinaryOperator.Equal or BinaryOperator.ExactEqual => "op_Equality",
                BinaryOperator.NotEqual or BinaryOperator.NotExactEqual => "op_Inequality",
                BinaryOperator.Less => "op_LessThan",
                BinaryOperator.Greater => "op_GreaterThan",
                BinaryOperator.LessEqual => "op_LessThanOrEqual",
                _ => "op_GreaterThanOrEqual",
            };

            _il.Emit(OpCodes.Call, Library(common.FullName, name, common.FullName, common.FullName));
            return result;
        }

        var known = TryNum(common, out var number);

        switch (binary.Operator)
        {
            case BinaryOperator.Add or BinaryOperator.AddWrap or BinaryOperator.Sub or BinaryOperator.SubWrap
                or BinaryOperator.Mul or BinaryOperator.MulWrap or BinaryOperator.Div or BinaryOperator.Rest:
                if (!known || common.FullName == "System.Char")
                    Report($"The operator '{binary.Operator}' does not exist for '{common.Name}' in .NET", binary.Location);
                else
                    EmitArithmetic(binary.Operator, number, binary.Location);
                break;

            case BinaryOperator.Equal or BinaryOperator.ExactEqual:
                _il.Emit(OpCodes.Ceq);
                break;

            case BinaryOperator.NotEqual or BinaryOperator.NotExactEqual:
                _il.Emit(OpCodes.Ceq);
                _il.Emit(OpCodes.Ldc_I4_0);
                _il.Emit(OpCodes.Ceq);
                break;

            case BinaryOperator.Less:
                _il.Emit(known && !number.Float && !number.Signed ? OpCodes.Clt_Un : OpCodes.Clt);
                break;

            case BinaryOperator.Greater:
                _il.Emit(known && !number.Float && !number.Signed ? OpCodes.Cgt_Un : OpCodes.Cgt);
                break;

            // `a <= b` is not `a > b` (a number that is not a number is neither): unordered counts as greater.
            case BinaryOperator.LessEqual:
                _il.Emit(known && (number.Float || !number.Signed) ? OpCodes.Cgt_Un : OpCodes.Cgt);
                _il.Emit(OpCodes.Ldc_I4_0);
                _il.Emit(OpCodes.Ceq);
                break;

            case BinaryOperator.GreaterEqual:
                _il.Emit(known && (number.Float || !number.Signed) ? OpCodes.Clt_Un : OpCodes.Clt);
                _il.Emit(OpCodes.Ldc_I4_0);
                _il.Emit(OpCodes.Ceq);
                break;

            default:
                Report($"The operator '{binary.Operator}' cannot be emitted to .NET yet", binary.Location);
                break;
        }

        return result;
    }

    /// <summary>An integer constant, as a value of the expected type; the 128 bits ones are built with their constructor.</summary>
    private TypeReference EmitInteger(BigInteger value, TypeReference? expected, SourceLocation location)
    {
        if (expected?.FullName is "System.Int128" or "System.UInt128")
        {
            var signed = expected.FullName == "System.Int128";
            var fits = signed
                ? value >= -(BigInteger.One << 127) && value < BigInteger.One << 127
                : value >= 0 && value < BigInteger.One << 128;

            if (!fits)
            {
                Report($"The literal {value} does not fit '{expected.FullName}'", location);
                value = 0;
            }

            var bits = value & ((BigInteger.One << 128) - 1);
            var constructor = _owner.Backend.Core.Find(expected.FullName)!.Methods.First(candidate =>
                candidate.IsConstructor && candidate.Parameters.Count == 2
                && candidate.Parameters.All(parameter => parameter.ParameterType.FullName == "System.UInt64"));

            _il.Emit(OpCodes.Ldc_I8, unchecked((long)(ulong)(bits >> 64)));
            _il.Emit(OpCodes.Ldc_I8, unchecked((long)(ulong)(bits & ulong.MaxValue)));
            _il.Emit(OpCodes.Newobj, _owner.Module.ImportReference(constructor));
            return Core(expected.FullName);
        }

        if (value >= (BigInteger)decimal.MinValue && value <= (BigInteger)decimal.MaxValue)
            return EmitNumber((decimal)value, false, expected, location);

        Report($"The literal {value} is too large for a number of .NET that is not 128 bits wide", location);
        _il.Emit(OpCodes.Ldc_I4_0);
        return Core("System.Int32");
    }

    // ---- Operators with no instruction: small static methods of `Tq.Ops`, in `Tq.Runtime`

    private static bool UsesHelper(BinaryOperator @operator) => @operator is
        BinaryOperator.AddOnBounds or BinaryOperator.SubOnBounds or BinaryOperator.MulOnBounds
        or BinaryOperator.DivFloor or BinaryOperator.DivCeil
        or BinaryOperator.Pow or BinaryOperator.PowWrap or BinaryOperator.PowOnBounds;

    private static bool TryIntegerRange(string typeName, out bool signed, out int bits)
    {
        (var known, signed, bits) = typeName switch
        {
            "System.SByte" => (true, true, 8),
            "System.Byte" => (true, false, 8),
            "System.Int16" => (true, true, 16),
            "System.UInt16" => (true, false, 16),
            "System.Int32" => (true, true, 32),
            "System.UInt32" => (true, false, 32),
            "System.Int64" => (true, true, 64),
            "System.UInt64" => (true, false, 64),
            "System.Int128" => (true, true, 128),
            "System.UInt128" => (true, false, 128),
            _ => (false, false, 0),
        };

        return known;
    }

    /// <summary>
    /// A static method `Name_Type(Type a, Type b)` of `Tq.Ops`, in `Tq.Runtime`: one copy for the whole build, made by
    /// the first module that needs it (the body is emitted with this emitter pointed at the new method and at that
    /// assembly). The call site is then `a`, `b` and a `call`.
    /// </summary>
    private MethodReference OpsHelper(string name, TypeReference type, Action emitBody)
    {
        var runtime = _owner.Backend.Runtime;
        var key = $"{name}|{type.FullName}";
        if (runtime.OpsHelpers.TryGetValue(key, out var existing)) return _target.ImportReference(existing);

        var module = runtime.Assembly.MainModule;
        var parameterType = _owner.Backend.Core.Require(module, type.FullName);

        var helper = new MethodDefinition($"{name}_{type.Name}",
            MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig, parameterType);
        helper.Parameters.Add(new ParameterDefinition("a", ParameterAttributes.None, parameterType));
        helper.Parameters.Add(new ParameterDefinition("b", ParameterAttributes.None, parameterType));
        runtime.Ops.Methods.Add(helper);
        runtime.OpsHelpers[key] = helper; // before the body: a helper can call another one

        var (il, method, target) = (_il, _method, _target);
        (_il, _method, _target) = (helper.Body.GetILProcessor(), helper, module);
        try { emitBody(); }
        finally { (_il, _method, _target) = (il, method, target); }

        return _target.ImportReference(helper);
    }

    /// <summary>
    /// `+|`, `-|`, `*|`, `/_`, `/^`, `**`, `**%` and `**|` of integers are calls to a helper of `Tq.Ops` (`Tq.Runtime`). Floats have
    /// `**`, `/_` and `/^` with `Math`. The 128 bits integers have none of them yet.
    /// </summary>
    private TypeReference? EmitHelperOperator(IrBinary binary, TypeReference common)
    {
        var @operator = binary.Operator;

        if (TryNum(common, out var number) && number.Float)
        {
            switch (@operator)
            {
                case BinaryOperator.Pow:
                {
                    EmitValue(binary.Left, common);
                    if (number.Bits == 32) _il.Emit(OpCodes.Conv_R8);
                    EmitValue(binary.Right, common);
                    if (number.Bits == 32) _il.Emit(OpCodes.Conv_R8);
                    _il.Emit(OpCodes.Call, Library("System.Math", "Pow", "System.Double", "System.Double"));
                    if (number.Bits == 32) _il.Emit(OpCodes.Conv_R4);
                    return common;
                }

                case BinaryOperator.DivFloor or BinaryOperator.DivCeil:
                {
                    EmitValue(binary.Left, common);
                    EmitValue(binary.Right, common);
                    _il.Emit(OpCodes.Div);
                    if (number.Bits == 32) _il.Emit(OpCodes.Conv_R8);
                    _il.Emit(OpCodes.Call, Library("System.Math", @operator == BinaryOperator.DivFloor ? "Floor" : "Ceiling", "System.Double"));
                    if (number.Bits == 32) _il.Emit(OpCodes.Conv_R4);
                    return common;
                }

                default:
                    Report($"The operator '{@operator}' does not exist for '{common.Name}': a float has no bounds to saturate at or to wrap around", binary.Location);
                    return common;
            }
        }

        if (!TryIntegerRange(common.FullName, out var signed, out var bits))
        {
            Report($"The operator '{@operator}' does not exist for '{common.Name}' in .NET", binary.Location);
            return common;
        }

        if (bits == 128)
        {
            Report($"The operator '{@operator}' is not available for '{common.Name}' in .NET yet", binary.Location);
            return common;
        }

        var helper = OpsHelper(@operator.ToString(), common, () =>
        {
            switch (@operator)
            {
                case BinaryOperator.AddOnBounds or BinaryOperator.SubOnBounds or BinaryOperator.MulOnBounds:
                    if (bits <= 32) EmitSmallSaturated(@operator, signed, bits);
                    else EmitWideSaturated(@operator, signed);
                    break;

                case BinaryOperator.DivFloor or BinaryOperator.DivCeil:
                    EmitRoundedDivision(@operator == BinaryOperator.DivCeil, signed, bits);
                    break;

                default:
                    EmitPower(@operator, common, signed, bits);
                    break;
            }

            _il.Emit(OpCodes.Ret);
        });

        EmitValue(binary.Left, common);
        EmitValue(binary.Right, common);
        _il.Emit(OpCodes.Call, helper);
        return common;
    }

    /// <summary>The 64 bits domain where up to 32 bits integers are computed: no operation of two of them overflows it.</summary>
    private void EmitWiden(bool signed, int bits)
    {
        if (bits < 64) _il.Emit(signed ? OpCodes.Conv_I8 : OpCodes.Conv_U8);
    }

    /// <summary>Back from the 64 bits domain to the size: cut (`wrap`) or checked, which throws when it does not fit.</summary>
    private void EmitNarrow(bool signed, int bits, bool checkedConversion)
    {
        if (bits >= 64) return;

        if (bits == 8) _il.Emit(checkedConversion ? (signed ? OpCodes.Conv_Ovf_I1 : OpCodes.Conv_Ovf_U1_Un) : (signed ? OpCodes.Conv_I1 : OpCodes.Conv_U1));
        else if (bits == 16) _il.Emit(checkedConversion ? (signed ? OpCodes.Conv_Ovf_I2 : OpCodes.Conv_Ovf_U2_Un) : (signed ? OpCodes.Conv_I2 : OpCodes.Conv_U2));
        else _il.Emit(checkedConversion ? (signed ? OpCodes.Conv_Ovf_I4 : OpCodes.Conv_Ovf_U4_Un) : (signed ? OpCodes.Conv_I4 : OpCodes.Conv_U4));
    }

    private void CallMath(string name, string type) => _il.Emit(OpCodes.Call, Library("System.Math", name, type, type));

    /// <summary>
    /// With the result of `+`, `-` or `*` in the 64 bits domain on the stack: cut at the bounds of the type.
    /// (`u32 * u32` does not fit a signed 64 bits, so it is cut as unsigned.)
    /// </summary>
    private void EmitClamp(BinaryOperator @operator, bool signed, int bits)
    {
        var maximum = signed ? (1L << (bits - 1)) - 1 : (1L << bits) - 1;
        var minimum = signed ? -(1L << (bits - 1)) : 0L;

        if (signed)
        {
            _il.Emit(OpCodes.Ldc_I8, minimum);
            CallMath("Max", "System.Int64");
            _il.Emit(OpCodes.Ldc_I8, maximum);
            CallMath("Min", "System.Int64");
        }
        else if (@operator is BinaryOperator.MulOnBounds)
        {
            _il.Emit(OpCodes.Ldc_I8, maximum);
            CallMath("Min", "System.UInt64");
        }
        else if (@operator is BinaryOperator.AddOnBounds)
        {
            _il.Emit(OpCodes.Ldc_I8, maximum);
            CallMath("Min", "System.Int64");
        }
        else
        {
            _il.Emit(OpCodes.Ldc_I8, 0L);
            CallMath("Max", "System.Int64");
        }
    }

    /// <summary>`+|`, `-|` and `*|` of up to 32 bits: computed in 64, cut at the bounds, narrowed.</summary>
    private void EmitSmallSaturated(BinaryOperator @operator, bool signed, int bits)
    {
        _il.Emit(OpCodes.Ldarg_0);
        EmitWiden(signed, bits);
        _il.Emit(OpCodes.Ldarg_1);
        EmitWiden(signed, bits);
        _il.Emit(@operator switch
        {
            BinaryOperator.AddOnBounds => OpCodes.Add,
            BinaryOperator.SubOnBounds => OpCodes.Sub,
            _ => OpCodes.Mul,
        });

        EmitClamp(@operator, signed, bits);
        EmitNarrow(signed, bits, false);
    }

    private MethodReference Conversion(string typeName, string from, string to)
    {
        var type = _owner.Backend.Core.Find(typeName) ?? throw new BackendError($"The type '{typeName}' was not found");
        var method = type.Methods.FirstOrDefault(candidate => candidate.Name is "op_Implicit" or "op_Explicit"
                                                              && candidate.Parameters.Count == 1
                                                              && candidate.Parameters[0].ParameterType.FullName == from
                                                              && candidate.ReturnType.FullName == to)
                     ?? throw new BackendError($"'{typeName}' has no conversion from '{from}' to '{to}'");

        return _target.ImportReference(method);
    }

    /// <summary>`+|`, `-|` and `*|` of 64 bits: `Int128` and `UInt128` hold the exact result (and `u64` `+`/`-` are only compared).</summary>
    private void EmitWideSaturated(BinaryOperator @operator, bool signed)
    {
        var name = @operator switch
        {
            BinaryOperator.AddOnBounds => "op_Addition",
            BinaryOperator.SubOnBounds => "op_Subtraction",
            _ => "op_Multiply",
        };

        if (signed)
        {
            var implicitConversion = Conversion("System.Int128", "System.Int64", "System.Int128");

            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Call, implicitConversion);
            _il.Emit(OpCodes.Ldarg_1);
            _il.Emit(OpCodes.Call, implicitConversion);
            _il.Emit(OpCodes.Call, Library("System.Int128", name, "System.Int128", "System.Int128"));

            _il.Emit(OpCodes.Ldc_I8, long.MinValue);
            _il.Emit(OpCodes.Call, implicitConversion);
            _il.Emit(OpCodes.Call, Library("System.Int128", "Max", "System.Int128", "System.Int128"));
            _il.Emit(OpCodes.Ldc_I8, long.MaxValue);
            _il.Emit(OpCodes.Call, implicitConversion);
            _il.Emit(OpCodes.Call, Library("System.Int128", "Min", "System.Int128", "System.Int128"));

            _il.Emit(OpCodes.Call, Conversion("System.Int128", "System.Int128", "System.Int64"));
            return;
        }

        switch (@operator)
        {
            case BinaryOperator.AddOnBounds:
            {
                var sum = NewTemp(Core("System.UInt64"));
                var fits = _il.Create(OpCodes.Nop);

                _il.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Ldarg_1);
                _il.Emit(OpCodes.Add);
                _il.Emit(OpCodes.Stloc, sum);
                _il.Emit(OpCodes.Ldloc, sum);
                _il.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Bge_Un, fits); // it wrapped around if the sum is below one of them
                _il.Emit(OpCodes.Ldc_I8, -1L);
                _il.Emit(OpCodes.Stloc, sum);
                _il.Append(fits);
                _il.Emit(OpCodes.Ldloc, sum);
                break;
            }

            case BinaryOperator.SubOnBounds:
            {
                var zero = _il.Create(OpCodes.Nop);
                var done = _il.Create(OpCodes.Nop);

                _il.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Ldarg_1);
                _il.Emit(OpCodes.Blt_Un, zero);
                _il.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Ldarg_1);
                _il.Emit(OpCodes.Sub);
                _il.Emit(OpCodes.Br, done);
                _il.Append(zero);
                _il.Emit(OpCodes.Ldc_I8, 0L);
                _il.Append(done);
                break;
            }

            default:
            {
                var implicitConversion = Conversion("System.UInt128", "System.UInt64", "System.UInt128");

                _il.Emit(OpCodes.Ldarg_0);
                _il.Emit(OpCodes.Call, implicitConversion);
                _il.Emit(OpCodes.Ldarg_1);
                _il.Emit(OpCodes.Call, implicitConversion);
                _il.Emit(OpCodes.Call, Library("System.UInt128", "op_Multiply", "System.UInt128", "System.UInt128"));
                _il.Emit(OpCodes.Ldc_I8, -1L);
                _il.Emit(OpCodes.Call, implicitConversion);
                _il.Emit(OpCodes.Call, Library("System.UInt128", "Min", "System.UInt128", "System.UInt128"));
                _il.Emit(OpCodes.Call, Conversion("System.UInt128", "System.UInt128", "System.UInt64"));
                break;
            }
        }
    }

    /// <summary>
    /// `a /_ b` and `a /^ b` in the 64 bits domain: the division truncates toward zero, so when something is left over and
    /// the quotient was cut the wrong way, it moves one step (down for floor, up for ceil). Throws when it does not fit.
    /// </summary>
    private void EmitRoundedDivision(bool ceiling, bool signed, int bits)
    {
        var domain = Core(signed ? "System.Int64" : "System.UInt64");
        var dividend = NewTemp(domain);
        var divisor = NewTemp(domain);
        var quotient = NewTemp(domain);
        var rest = NewTemp(domain);

        _il.Emit(OpCodes.Ldarg_0);
        EmitWiden(signed, bits);
        _il.Emit(OpCodes.Stloc, dividend);
        _il.Emit(OpCodes.Ldarg_1);
        EmitWiden(signed, bits);
        _il.Emit(OpCodes.Stloc, divisor);

        _il.Emit(OpCodes.Ldloc, dividend);
        _il.Emit(OpCodes.Ldloc, divisor);
        _il.Emit(signed ? OpCodes.Div : OpCodes.Div_Un);
        _il.Emit(OpCodes.Stloc, quotient);

        _il.Emit(OpCodes.Ldloc, dividend);
        _il.Emit(OpCodes.Ldloc, divisor);
        _il.Emit(signed ? OpCodes.Rem : OpCodes.Rem_Un);
        _il.Emit(OpCodes.Stloc, rest);

        var done = _il.Create(OpCodes.Nop);
        _il.Emit(OpCodes.Ldloc, rest);
        _il.Emit(OpCodes.Brfalse, done);

        if (signed)
        {
            // 1 when the rest and the divisor have the same sign: the true quotient is above the truncated one.
            _il.Emit(OpCodes.Ldloc, rest);
            _il.Emit(OpCodes.Ldc_I8, 0L);
            _il.Emit(OpCodes.Clt);
            _il.Emit(OpCodes.Ldloc, divisor);
            _il.Emit(OpCodes.Ldc_I8, 0L);
            _il.Emit(OpCodes.Clt);
            _il.Emit(OpCodes.Ceq);
            _il.Emit(ceiling ? OpCodes.Brfalse : OpCodes.Brtrue, done);
        }
        else if (!ceiling)
        {
            _il.Emit(OpCodes.Br, done); // an unsigned division already rounds down
        }

        _il.Emit(OpCodes.Ldloc, quotient);
        _il.Emit(OpCodes.Ldc_I8, 1L);
        _il.Emit(ceiling ? OpCodes.Add : OpCodes.Sub);
        _il.Emit(OpCodes.Stloc, quotient);

        _il.Append(done);
        _il.Emit(OpCodes.Ldloc, quotient);
        EmitNarrow(signed, bits, true);
    }

    /// <summary>
    /// `**`, `**%` and `**|` by squaring: no allocation, and exact (`Math.Pow` is a `double`, which holds 53 bits).
    /// It is done in the 64 bits domain: checked (`**`) uses `mul.ovf`, wrapped (`**%`) uses `mul` and cuts the result,
    /// saturated (`**|`) cuts at the bounds after each product (with 64 bits, with the saturated `*|` of `Tq.Ops`).
    /// </summary>
    private void EmitPower(BinaryOperator @operator, TypeReference type, bool signed, int bits)
    {
        var domain = Core(signed ? "System.Int64" : "System.UInt64");
        var factor = NewTemp(domain);
        var exponent = NewTemp(domain);
        var result = NewTemp(domain);

        _il.Emit(OpCodes.Ldarg_0);
        EmitWiden(signed, bits);
        _il.Emit(OpCodes.Stloc, factor);
        _il.Emit(OpCodes.Ldarg_1);
        EmitWiden(signed, bits);
        _il.Emit(OpCodes.Stloc, exponent);

        if (signed)
        {
            var valid = _il.Create(OpCodes.Nop);
            _il.Emit(OpCodes.Ldloc, exponent);
            _il.Emit(OpCodes.Ldc_I8, 0L);
            _il.Emit(OpCodes.Bge, valid);
            _il.Emit(OpCodes.Ldstr, "The exponent of an integer power must not be negative");
            _il.Emit(OpCodes.Newobj, _target.ImportReference(_owner.Backend.Core.Find("System.OverflowException")!.Methods.First(
                candidate => candidate.IsConstructor && candidate.Parameters.Count == 1 && candidate.Parameters[0].ParameterType.FullName == "System.String")));
            _il.Emit(OpCodes.Throw);
            _il.Append(valid);
        }

        void Multiply()
        {
            switch (@operator)
            {
                case BinaryOperator.Pow:
                    _il.Emit(signed ? OpCodes.Mul_Ovf : OpCodes.Mul_Ovf_Un);
                    break;

                case BinaryOperator.PowWrap:
                    _il.Emit(OpCodes.Mul);
                    break;

                case BinaryOperator.PowOnBounds when bits == 64:
                    _il.Emit(OpCodes.Call, OpsHelper("MulOnBounds", type, () =>
                    {
                        EmitWideSaturated(BinaryOperator.MulOnBounds, signed);
                        _il.Emit(OpCodes.Ret);
                    }));
                    break;

                default:
                    _il.Emit(OpCodes.Mul);
                    EmitClamp(BinaryOperator.MulOnBounds, signed, bits);
                    break;
            }
        }

        _il.Emit(OpCodes.Ldc_I8, 1L);
        _il.Emit(OpCodes.Stloc, result);

        var loop = _il.Create(OpCodes.Nop);
        var skip = _il.Create(OpCodes.Nop);
        var done = _il.Create(OpCodes.Nop);

        _il.Append(loop);
        _il.Emit(OpCodes.Ldloc, exponent);
        _il.Emit(OpCodes.Ldc_I8, 1L);
        _il.Emit(OpCodes.And);
        _il.Emit(OpCodes.Brfalse, skip);
        _il.Emit(OpCodes.Ldloc, result);
        _il.Emit(OpCodes.Ldloc, factor);
        Multiply();
        _il.Emit(OpCodes.Stloc, result);

        _il.Append(skip);
        _il.Emit(OpCodes.Ldloc, exponent);
        _il.Emit(OpCodes.Ldc_I4_1);
        _il.Emit(OpCodes.Shr_Un);
        _il.Emit(OpCodes.Stloc, exponent);
        _il.Emit(OpCodes.Ldloc, exponent);
        _il.Emit(OpCodes.Brfalse, done);

        _il.Emit(OpCodes.Ldloc, factor);
        _il.Emit(OpCodes.Ldloc, factor);
        Multiply();
        _il.Emit(OpCodes.Stloc, factor);
        _il.Emit(OpCodes.Br, loop);

        _il.Append(done);
        _il.Emit(OpCodes.Ldloc, result);
        EmitNarrow(signed, bits, @operator == BinaryOperator.Pow);
    }

    /// <summary>
    /// Checked arithmetic throws when the result does not fit; the `Wrap` operators and the floats do not. A type
    /// smaller than 32 bits is computed as 32 and then cut to its size, checking it too.
    /// </summary>
    private void EmitArithmetic(BinaryOperator @operator, Num number, SourceLocation location)
    {
        var wraps = @operator is BinaryOperator.AddWrap or BinaryOperator.SubWrap or BinaryOperator.MulWrap;
        var checkedOperation = !wraps && !number.Float;

        switch (@operator)
        {
            case BinaryOperator.Add or BinaryOperator.AddWrap:
                _il.Emit(!checkedOperation ? OpCodes.Add : number.Signed ? OpCodes.Add_Ovf : OpCodes.Add_Ovf_Un);
                break;

            case BinaryOperator.Sub or BinaryOperator.SubWrap:
                _il.Emit(!checkedOperation ? OpCodes.Sub : number.Signed ? OpCodes.Sub_Ovf : OpCodes.Sub_Ovf_Un);
                break;

            case BinaryOperator.Mul or BinaryOperator.MulWrap:
                _il.Emit(!checkedOperation ? OpCodes.Mul : number.Signed ? OpCodes.Mul_Ovf : OpCodes.Mul_Ovf_Un);
                break;

            case BinaryOperator.Div:
                _il.Emit(number.Float || number.Signed ? OpCodes.Div : OpCodes.Div_Un);
                return;

            case BinaryOperator.Rest:
                _il.Emit(number.Float || number.Signed ? OpCodes.Rem : OpCodes.Rem_Un);
                return;

            default:
                Report($"The operator '{@operator}' cannot be emitted to .NET yet", location);
                return;
        }

        if (number.Float || number.Native || number.Bits >= 32) return;

        if (number.Bits == 8) _il.Emit(checkedOperation ? (number.Signed ? OpCodes.Conv_Ovf_I1 : OpCodes.Conv_Ovf_U1) : (number.Signed ? OpCodes.Conv_I1 : OpCodes.Conv_U1));
        else _il.Emit(checkedOperation ? (number.Signed ? OpCodes.Conv_Ovf_I2 : OpCodes.Conv_Ovf_U2) : (number.Signed ? OpCodes.Conv_I2 : OpCodes.Conv_U2));
    }

    private void EmitStringOperator(IrBinary binary, TypeReference text)
    {
        EmitValue(binary.Left, text);
        EmitValue(binary.Right, text);

        switch (binary.Operator)
        {
            case BinaryOperator.Add:
                _il.Emit(OpCodes.Call, Library("System.String", "Concat", "System.String", "System.String"));
            break;

            case BinaryOperator.Equal or BinaryOperator.ExactEqual:
                _il.Emit(OpCodes.Call, Library("System.String", "op_Equality", "System.String", "System.String"));
            break;

            case BinaryOperator.NotEqual or BinaryOperator.NotExactEqual:
                _il.Emit(OpCodes.Call, Library("System.String", "op_Inequality", "System.String", "System.String"));
            break;

            case BinaryOperator.Less or BinaryOperator.Greater or BinaryOperator.LessEqual or BinaryOperator.GreaterEqual:
                _il.Emit(OpCodes.Call, Library("System.String", "CompareOrdinal", "System.String", "System.String"));
                _il.Emit(OpCodes.Ldc_I4_0);

                if (binary.Operator == BinaryOperator.Less)
                {
                    _il.Emit(OpCodes.Clt);
                }
                else if (binary.Operator == BinaryOperator.Greater)
                {
                    _il.Emit(OpCodes.Cgt);
                }
                else
                {
                    _il.Emit(binary.Operator == BinaryOperator.LessEqual ? OpCodes.Cgt : OpCodes.Clt);
                    _il.Emit(OpCodes.Ldc_I4_0);
                    _il.Emit(OpCodes.Ceq);
                }
            break;

            default:
                Report($"The operator '{binary.Operator}' does not exist for strings", binary.Location);
            break;
        }
    }

    private TypeReference? EmitUnary(IrUnary unary, TypeReference? expected, bool discard)
    {
        switch (unary.Operator)
        {
            case UnaryOperator.Not:
                EmitValue(unary.Operand, Core("System.Boolean"));
                _il.Emit(OpCodes.Ldc_I4_0);
                _il.Emit(OpCodes.Ceq);
                return Core("System.Boolean");

            case UnaryOperator.Negate:
            {
                var type = Emit(unary.Operand, expected);
                if (type == null) return null;
                _il.Emit(OpCodes.Neg);
                return type;
            }

            case UnaryOperator.Increment or UnaryOperator.Decrement:
                return EmitIncrement(unary, discard);

            default:
                Report($"The operator '{unary.Operator}' cannot be emitted to .NET yet", unary.Location);
                return null;
        }
    }

    /// <summary>`++x`, `x++`, `--x` and `x--`. The value is only made when someone wants it.</summary>
    private TypeReference? EmitIncrement(IrUnary unary, bool discard)
    {
        var target = ResolveTarget(unary.Operand);
        if (target == null) return null;

        if (target.Slots > 1 || !TryNum(target.Type, out var number))
        {
            Report("Only a number that is a variable, a parameter or a field can be incremented in .NET yet", unary.Location);
            return null;
        }

        var step = unary.Operator == UnaryOperator.Increment ? BinaryOperator.Add : BinaryOperator.Sub;

        target.Prefix();
        if (target.Slots == 1) _il.Emit(OpCodes.Dup);
        target.Load();

        VariableDefinition? kept = null;
        if (!discard && unary.IsPostfix)
        {
            kept = NewTemp(target.Type);
            _il.Emit(OpCodes.Dup);
            _il.Emit(OpCodes.Stloc, kept);
        }

        EmitNumber(1, false, target.Type, unary.Location);
        EmitArithmetic(step, number, unary.Location);

        if (!discard && !unary.IsPostfix)
        {
            kept = NewTemp(target.Type);
            _il.Emit(OpCodes.Dup);
            _il.Emit(OpCodes.Stloc, kept);
        }

        target.Store();

        if (kept == null) return null;
        _il.Emit(OpCodes.Ldloc, kept);
        return target.Type;
    }

    // ---- Assignment

    /// <summary>Something that can be written (and read): what comes before the value, and how it is stored and loaded.</summary>
    private sealed record Target(TypeReference Type, int Slots, Action Prefix, Action Store, Action Load);

    private TypeReference? EmitAssign(IrAssign assign, bool discard)
    {
        var target = ResolveTarget(assign.Target);
        if (target == null) return null;

        // An array is memory that stays where it is: `a = [1, 2]` writes the elements into `a`. A declaration (`let [2]int a = b`)
        // is the exception, as it makes new memory, which is a copy of `b` (a literal already is new).
        if (target.Type is ArrayType && _types.Get(assign.Source) is ArrayTypeRef { Length: var length })
            return EmitArrayAssign(assign, target, length, discard);

        target.Prefix();
        EmitValue(assign.Source, target.Type);

        VariableDefinition? kept = null;
        if (!discard)
        {
            kept = NewTemp(target.Type);
            _il.Emit(OpCodes.Dup);
            _il.Emit(OpCodes.Stloc, kept);
        }

        target.Store();

        if (kept == null) return null;
        _il.Emit(OpCodes.Ldloc, kept);
        return target.Type;
    }

    private TypeReference? EmitArrayAssign(IrAssign assign, Target target, int length, bool discard)
    {
        if (assign.Target is IrLocal)
        {
            target.Prefix();
            EmitValue(assign.Source, target.Type);

            if (assign.Source is not IrArray)
            {
                _il.Emit(OpCodes.Callvirt, Library("System.Array", "Clone"));
                _il.Emit(OpCodes.Castclass, target.Type);
            }
        }
        else
        {
            // `Array.Copy(source, target, length)`: the source is evaluated first, then the array it is written to.
            EmitValue(assign.Source, target.Type);
            target.Prefix();
            target.Load();
            _il.Emit(OpCodes.Ldc_I4, length);
            _il.Emit(OpCodes.Call, Library("System.Array", "Copy", "System.Array", "System.Array", "System.Int32"));

            if (discard) return null;
            target.Prefix();
            target.Load();
            return target.Type;
        }

        if (discard)
        {
            target.Store();
            return null;
        }

        var kept = NewTemp(target.Type);
        _il.Emit(OpCodes.Dup);
        _il.Emit(OpCodes.Stloc, kept);
        target.Store();
        _il.Emit(OpCodes.Ldloc, kept);
        return target.Type;
    }

    private Target? ResolveTarget(IrExpression expression)
    {
        switch (expression)
        {
            case IrLocal local:
                return LocalTarget(GetLocal(local));

            case IrReference { Reference: LocalReference local }:
                return LocalTarget(GetLocal(local.Local));

            case IrReference { Reference: ParameterReference parameter }:
            {
                var definition = ParameterOf(parameter.Parameter);

                // `*T` of a value: what it points to is written.
                if (definition.ParameterType is ByReferenceType reference)
                {
                    return new Target(reference.ElementType, 1, () => _il.Emit(OpCodes.Ldarg, definition),
                        () => _il.Emit(OpCodes.Stobj, reference.ElementType), () => _il.Emit(OpCodes.Ldobj, reference.ElementType));
                }

                return new Target(definition.ParameterType, 0, () => { },
                    () => _il.Emit(OpCodes.Starg, definition), () => _il.Emit(OpCodes.Ldarg, definition));
            }

            case IrReference { Reference: IrField field }:
                return MemberTarget(field.Field, null, expression.Location);

            case IrReference { Reference: BackingFieldReference backing }:
                return MemberTarget(backing.Property, null, expression.Location);

            case IrAccess { access.Reference: IrField field } access:
                return MemberTarget(field.Field, access.baseExpr, expression.Location);

            case IrAccess { access.Reference: BackingFieldReference backing }:
                return MemberTarget(backing.Property, null, expression.Location);

            // An element of a slice.
            case IrIndex { Indices.Length: 1 } index:
            {
                // An element of a slice: the `ref T` of `memory.Span[i]`, so it is read with `ldobj` and written with `stobj`.
                if (_types.Get(index.Target) is SliceTypeRef sliceType)
                {
                    var memory = _owner.MemoryOf(_owner.MapType(sliceType.Element, index.Location));
                    var elementType = memory.GenericArguments[0];
                    return new Target(elementType, 1,
                        () =>
                        {
                            EmitValue(index.Target, memory);
                            EmitElementAddress(memory, () => EmitValue(index.Indices[0], Core("System.Int32")));
                        },
                        () => _il.Emit(OpCodes.Stobj, elementType),
                        () => _il.Emit(OpCodes.Ldobj, elementType));
                }

                var arrayType = TypeOfNode(index.Target) as ArrayType;
                if (arrayType == null && _types.Get(index.Target) is ArrayTypeRef array)
                    arrayType = new ArrayType(_owner.MapType(array.Element, index.Location));

                if (arrayType == null)
                {
                    Report("Only an element of a slice can be assigned by index in .NET yet", index.Location);
                    return null;
                }

                var element = arrayType.ElementType;
                return new Target(element, 2,
                    () =>
                    {
                        EmitValue(index.Target, null);
                        EmitValue(index.Indices[0], Core("System.Int32"));
                    },
                    () => _il.Emit(OpCodes.Stelem_Any, element),
                    () => _il.Emit(OpCodes.Ldelem_Any, element));
            }

            default:
                Report("This cannot be assigned to in .NET yet", expression.Location);
                return null;
        }
    }

    private Target LocalTarget(VariableDefinition variable) => new(
        variable.VariableType, 0, () => { }, () => _il.Emit(OpCodes.Stloc, variable), () => _il.Emit(OpCodes.Ldloc, variable));

    private Target? MemberTarget(object member, IrExpression? baseExpr, SourceLocation location)
    {
        switch (member)
        {
            case TqField field when _owner.Backend.Definitions.TryGetValue(field, out var definition) && definition is FieldDefinition fieldDefinition:
            {
                var tqHost = fieldDefinition.IsStatic ? null : TqHost(baseExpr, fieldDefinition.DeclaringType);
                return tqHost != null
                    ? FieldTarget(OnHost(_owner.Use(fieldDefinition), tqHost), false,
                        Instantiate(fieldDefinition.FieldType, [], tqHost.GenericArguments), fieldDefinition.DeclaringType, baseExpr)
                    : FieldTarget(_owner.Use(fieldDefinition), fieldDefinition.IsStatic, fieldDefinition.FieldType, fieldDefinition.DeclaringType, baseExpr);
            }

            case TqProperty property when _owner.Backend.BackingFields.TryGetValue(property, out var backing):
                return FieldTarget(_owner.Use(backing), backing.IsStatic, backing.FieldType, backing.DeclaringType, null);

            case DotnetField dotnetField when !dotnetField.IsConstant:
            {
                var reference = _owner.Module.ImportReference(dotnetField.Reference);
                var host = dotnetField.IsStatic ? null : HostOf(baseExpr);
                if (host != null) reference = OnHost(reference, host);
                return FieldTarget(reference, dotnetField.IsStatic, Instantiate(reference.FieldType, [], host?.GenericArguments),
                    reference.DeclaringType, baseExpr);
            }

            case DotnetProperty { Getter: { } getter, Setter: { } setter }:
            {
                var get = _owner.Module.ImportReference(getter);
                var set = _owner.Module.ImportReference(setter);
                var isInstance = set.HasThis;

                var host = isInstance ? HostOf(baseExpr) : null;
                if (host != null)
                {
                    get = OnHost(get, host);
                    set = OnHost(set, host);
                }

                return new Target(Instantiate(get.ReturnType, [], host?.GenericArguments), isInstance ? 1 : 0,
                    () => { if (isInstance) EmitFieldReceiver(baseExpr, set.DeclaringType); },
                    () => _il.Emit(isInstance && !set.DeclaringType.IsValueType ? OpCodes.Callvirt : OpCodes.Call, set),
                    () => _il.Emit(isInstance && !get.DeclaringType.IsValueType ? OpCodes.Callvirt : OpCodes.Call, get));
            }

            default:
                Report($"The member '{(member as IMember)?.Name ?? member.ToString()}' cannot be assigned in .NET yet", location);
                return null;
        }
    }

    private Target FieldTarget(CecilField field, bool isStatic, TypeReference type, TypeReference declaring, IrExpression? baseExpr)
    {
        if (isStatic)
            return new Target(type, 0, () => { }, () => _il.Emit(OpCodes.Stsfld, field), () => _il.Emit(OpCodes.Ldsfld, field));

        return new Target(type, 1, () => EmitFieldReceiver(baseExpr, declaring),
            () => _il.Emit(OpCodes.Stfld, field), () => _il.Emit(OpCodes.Ldfld, field));
    }
}
