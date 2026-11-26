using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Tqc.Backend.Cecil;

internal sealed class TqRuntimeAssembly
{
    public const string Name = "Tq.Runtime";
    public const string Namespace = "Tq";

    public AssemblyDefinition Assembly { get; }
    public TypeDefinition Failable { get; }
    public TypeDefinition FailableOf { get; }
    
    public TypeDefinition Formatter { get; }
    public FieldDefinition FormatterInstance { get; }
    public MethodDefinition FormatterFormatStatic { get; }

    /// <summary>`Tq.Ops`, with the helpers of the operators; by operator and type.</summary>
    public TypeDefinition Ops { get; }
    public Dictionary<string, MethodDefinition> OpsHelpers { get; } = [];

    public TqRuntimeAssembly(CoreTypes core)
    {
        Assembly = AssemblyDefinition.CreateAssembly(
            new AssemblyNameDefinition(Name, new Version(1, 0, 0, 0)),
            Name + ".dll",
            new ModuleParameters { Kind = ModuleKind.Dll, Runtime = TargetRuntime.Net_4_0 });

        var module = Assembly.MainModule;
        var @object = core.Require(module, "System.Object");
        var @string = core.Require(module, "System.String");
        var valueType = core.Require(module, "System.ValueType");

        {
            const TypeAttributes attributes =
                TypeAttributes.Public | TypeAttributes.Sealed
                | TypeAttributes.SequentialLayout | TypeAttributes.BeforeFieldInit;

            Failable = new TypeDefinition(Namespace, "Failable", attributes, valueType);
            Failable.Fields.Add(new FieldDefinition("Error", FieldAttributes.Public, @object));
            module.Types.Add(Failable);

            FailableOf = new TypeDefinition(Namespace, "Failable`1", attributes, valueType);
            var parameter = new GenericParameter("T", FailableOf);
            FailableOf.GenericParameters.Add(parameter);
            FailableOf.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public, parameter));
            FailableOf.Fields.Add(new FieldDefinition("Error", FieldAttributes.Public, @object));
            module.Types.Add(FailableOf);
        }

        (Formatter, FormatterInstance, FormatterFormatStatic) = BuildFormatter(core, module, @object);
        
        Ops = new TypeDefinition(Namespace, "Ops",
            TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit, @object);
        module.Types.Add(Ops);
    }
    
    private static (TypeDefinition Type, FieldDefinition Instance, MethodDefinition StaticFormat) BuildFormatter(
        CoreTypes core, ModuleDefinition module, TypeReference @object)
    {
        var stringType = core.Require(module, "System.String");
        var voidType = core.Require(module, "System.Void");
        var boolType = core.Require(module, "System.Boolean");
        var int32Type = core.Require(module, "System.Int32");
        var typeType = core.Require(module, "System.Type");
        var arrayType = core.Require(module, "System.Array");
        var formatProviderType = core.Require(module, "System.IFormatProvider");
        var customFormatterType = core.Require(module, "System.ICustomFormatter");
        var formattableType = core.Require(module, "System.IFormattable");
        var sbType = core.Require(module, "System.Text.StringBuilder");

        var formatter = new TypeDefinition(
            Namespace,
            "TqFormatter",
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
            @object);

        formatter.Interfaces.Add(new InterfaceImplementation(formatProviderType));
        formatter.Interfaces.Add(new InterfaceImplementation(customFormatterType));

        module.Types.Add(formatter);

        // .ctor()
        var ctor = new MethodDefinition(
            ".ctor",
            MethodAttributes.Public |
            MethodAttributes.HideBySig |
            MethodAttributes.SpecialName |
            MethodAttributes.RTSpecialName,
            voidType);

        var ctorIl = ctor.Body.GetILProcessor();

        ctorIl.Emit(OpCodes.Ldarg_0);
        ctorIl.Emit(OpCodes.Call, Ref("System.Object", ".ctor"));
        ctorIl.Emit(OpCodes.Ret);

        formatter.Methods.Add(ctor);

        // public static readonly TqFormatter Instance = new TqFormatter();
        var instanceField = new FieldDefinition(
            "Instance",
            FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.InitOnly,
            formatter);

        formatter.Fields.Add(instanceField);

        // .cctor()
        var cctor = new MethodDefinition(
            ".cctor",
            MethodAttributes.Private |
            MethodAttributes.Static |
            MethodAttributes.HideBySig |
            MethodAttributes.SpecialName |
            MethodAttributes.RTSpecialName,
            voidType);

        var cctorIl = cctor.Body.GetILProcessor();

        cctorIl.Emit(OpCodes.Newobj, ctor);
        cctorIl.Emit(OpCodes.Stsfld, instanceField);
        cctorIl.Emit(OpCodes.Ret);

        formatter.Methods.Add(cctor);

        // public object GetFormat(Type formatType)
        var getFormat = new MethodDefinition(
            "GetFormat",
            MethodAttributes.Public |
            MethodAttributes.Final |
            MethodAttributes.HideBySig |
            MethodAttributes.NewSlot |
            MethodAttributes.Virtual,
            @object);

        getFormat.Parameters.Add(
            new ParameterDefinition("formatType", ParameterAttributes.None, typeType));

        var gfIl = getFormat.Body.GetILProcessor();
        var notMatch = gfIl.Create(OpCodes.Nop);

        gfIl.Emit(OpCodes.Ldarg_1);
        gfIl.Emit(OpCodes.Ldtoken, customFormatterType);
        gfIl.Emit(OpCodes.Call, Ref("System.Type", "GetTypeFromHandle", "System.RuntimeTypeHandle"));
        gfIl.Emit(OpCodes.Call, Ref("System.Type", "op_Equality", "System.Type", "System.Type"));
        gfIl.Emit(OpCodes.Brfalse, notMatch);

        gfIl.Emit(OpCodes.Ldarg_0);
        gfIl.Emit(OpCodes.Ret);

        gfIl.Append(notMatch);
        gfIl.Emit(OpCodes.Ldnull);
        gfIl.Emit(OpCodes.Ret);

        formatter.Methods.Add(getFormat);

        // public string Format(string format, object arg, IFormatProvider formatProvider)
        var format = new MethodDefinition(
            "Format",
            MethodAttributes.Public |
            MethodAttributes.Final |
            MethodAttributes.HideBySig |
            MethodAttributes.NewSlot |
            MethodAttributes.Virtual,
            stringType);

        format.Parameters.Add(new ParameterDefinition("format", ParameterAttributes.None, stringType));
        format.Parameters.Add(new ParameterDefinition("arg", ParameterAttributes.None, @object));
        format.Parameters.Add(new ParameterDefinition("formatProvider", ParameterAttributes.None, formatProviderType));

        formatter.Methods.Add(format);

        var il = format.Body.GetILProcessor();

        format.Body.InitLocals = true;

        var notNull = il.Create(OpCodes.Nop);
        var notBool = il.Create(OpCodes.Nop);
        var boolTrue = il.Create(OpCodes.Nop);
        var notArray = il.Create(OpCodes.Nop);
        var loopCheck = il.Create(OpCodes.Nop);
        var loopBody = il.Create(OpCodes.Nop);
        var skipComma = il.Create(OpCodes.Nop);
        var notFormattable = il.Create(OpCodes.Nop);
        var nonNullStr = il.Create(OpCodes.Nop);

        var arrVar = new VariableDefinition(arrayType);
        var sbVar = new VariableDefinition(sbType);
        var idxVar = new VariableDefinition(int32Type);
        var lenVar = new VariableDefinition(int32Type);

        format.Body.Variables.Add(arrVar);
        format.Body.Variables.Add(sbVar);
        format.Body.Variables.Add(idxVar);
        format.Body.Variables.Add(lenVar);

        // if (arg == null) return "";
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Brtrue, notNull);

        il.Emit(OpCodes.Ldstr, "");
        il.Emit(OpCodes.Ret);

        il.Append(notNull);

        // if (arg is bool b) return b ? "true" : "false";
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Isinst, boolType);
        il.Emit(OpCodes.Brfalse, notBool);

        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Unbox_Any, boolType);
        il.Emit(OpCodes.Brtrue, boolTrue);

        il.Emit(OpCodes.Ldstr, "false");
        il.Emit(OpCodes.Ret);

        il.Append(boolTrue);

        il.Emit(OpCodes.Ldstr, "true");
        il.Emit(OpCodes.Ret);

        il.Append(notBool);

        // if (arg is Array arr)
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Isinst, arrayType);
        il.Emit(OpCodes.Stloc, arrVar);

        il.Emit(OpCodes.Ldloc, arrVar);
        il.Emit(OpCodes.Brfalse, notArray);

        // var sb = new StringBuilder();
        il.Emit(OpCodes.Newobj, Ref("System.Text.StringBuilder", ".ctor"));
        il.Emit(OpCodes.Stloc, sbVar);

        // sb.Append('[');
        il.Emit(OpCodes.Ldloc, sbVar);
        il.Emit(OpCodes.Ldc_I4, (int)'[');
        il.Emit(OpCodes.Callvirt,
            Ref("System.Text.StringBuilder", "Append", "System.Char"));
        il.Emit(OpCodes.Pop);

        // var len = arr.Length;
        il.Emit(OpCodes.Ldloc, arrVar);
        il.Emit(OpCodes.Callvirt, Ref("System.Array", "get_Length"));
        il.Emit(OpCodes.Stloc, lenVar);

        // var idx = 0;
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Stloc, idxVar);

        il.Emit(OpCodes.Br, loopCheck);

        // Loop body
        il.Append(loopBody);

        // if (idx != 0) sb.Append(", ");
        il.Emit(OpCodes.Ldloc, idxVar);
        il.Emit(OpCodes.Brfalse, skipComma);

        il.Emit(OpCodes.Ldloc, sbVar);
        il.Emit(OpCodes.Ldstr, ", ");
        il.Emit(OpCodes.Callvirt,
            Ref("System.Text.StringBuilder", "Append", "System.String"));
        il.Emit(OpCodes.Pop);

        il.Append(skipComma);

        // sb.Append(Format(null, arr.GetValue(idx), this));
        il.Emit(OpCodes.Ldloc, sbVar);

        il.Emit(OpCodes.Ldarg_0); // this
        il.Emit(OpCodes.Ldnull);  // format

        il.Emit(OpCodes.Ldloc, arrVar);
        il.Emit(OpCodes.Ldloc, idxVar);
        il.Emit(OpCodes.Callvirt, Ref("System.Array", "GetValue", "System.Int32"));

        il.Emit(OpCodes.Ldarg_0); // formatProvider
        il.Emit(OpCodes.Callvirt, format);
        il.Emit(OpCodes.Callvirt, Ref("System.Text.StringBuilder", "Append", "System.String"));

        il.Emit(OpCodes.Pop);

        // idx++;
        il.Emit(OpCodes.Ldloc, idxVar);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Stloc, idxVar);

        // while (idx < len)
        il.Append(loopCheck);

        il.Emit(OpCodes.Ldloc, idxVar);
        il.Emit(OpCodes.Ldloc, lenVar);
        il.Emit(OpCodes.Blt, loopBody);

        // sb.Append(']');
        il.Emit(OpCodes.Ldloc, sbVar);
        il.Emit(OpCodes.Ldc_I4, (int)']');
        il.Emit(OpCodes.Callvirt, Ref("System.Text.StringBuilder", "Append", "System.Char"));
        il.Emit(OpCodes.Pop);

        // return sb.ToString();
        il.Emit(OpCodes.Ldloc, sbVar);
        il.Emit(OpCodes.Callvirt, Ref("System.Object", "ToString"));
        il.Emit(OpCodes.Ret);

        il.Append(notArray);

        // if (arg is IFormattable)
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Isinst, formattableType);
        il.Emit(OpCodes.Brfalse, notFormattable);

        // return ((IFormattable)arg).ToString(format, formatProvider);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Castclass, formattableType);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldarg_3);

        il.Emit(OpCodes.Callvirt,
            Ref("System.IFormattable", "ToString",
                "System.String", "System.IFormatProvider"));

        il.Emit(OpCodes.Ret);

        il.Append(notFormattable);

        // return arg.ToString() ?? "";
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Callvirt, Ref("System.Object", "ToString"));

        il.Emit(OpCodes.Dup);
        il.Emit(OpCodes.Brtrue, nonNullStr);

        il.Emit(OpCodes.Pop);
        il.Emit(OpCodes.Ldstr, "");

        il.Append(nonNullStr);
        il.Emit(OpCodes.Ret);

        // public static string Format(object value)
        var staticFormat = new MethodDefinition(
            "Format",
            MethodAttributes.Public |
            MethodAttributes.Static |
            MethodAttributes.HideBySig,
            stringType);

        staticFormat.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, @object));

        var sfIl = staticFormat.Body.GetILProcessor();

        sfIl.Emit(OpCodes.Ldsfld, instanceField);
        sfIl.Emit(OpCodes.Ldnull);
        sfIl.Emit(OpCodes.Ldarg_0);
        sfIl.Emit(OpCodes.Ldsfld, instanceField);
        sfIl.Emit(OpCodes.Callvirt, format);
        sfIl.Emit(OpCodes.Ret);

        formatter.Methods.Add(staticFormat);

        return (formatter, instanceField, staticFormat);

        MethodReference Ref(string typeName, string name, params string[] paramTypes)
        {
            var type = core.Find(typeName)!;

            var method = type.Methods.First(m =>
                m.Name == name &&
                m.Parameters.Count == paramTypes.Length &&
                m.Parameters.Select(p => p.ParameterType.FullName)
                    .SequenceEqual(paramTypes));

            return module.ImportReference(method);
        }
    }
}
