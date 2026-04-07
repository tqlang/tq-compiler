using System.Runtime.CompilerServices;
using System.Text;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;

namespace Abstract.CodeProcess;

public partial class Compiler
{
    
    private void LoadCoreLibResources()
    {
        var cl = _corLibFactory = _module.CorLibTypeFactory;
        
        var objectType = ImportType(cl.Object);
        var stringType = ImportType(cl.String);
        var typeType = ImportType(typeof(Type));
        var nullable = ImportType(typeof(Nullable<>));
        
        var valueType = ImportType(typeof(ValueType));
        var enumType = ImportType(typeof(Enum));
        var int128 = ImportType(typeof(Int128));
        var uint128 = ImportType(typeof(UInt128));
        
        var runtimeTypeHandle = ImportType(typeof(RuntimeTypeHandle));
        var spanType = ImportType(typeof(Span<>));
        var stringBuilder = ImportType(typeof(StringBuilder));
        var iEnumerator = ImportType(typeof(IEnumerator<>));
        var iEnumerable = ImportType(typeof(IEnumerable<>));
        
        var overflowException = ImportType(typeof(OverflowException));
        
        // Attributes
        var extensionAttribute = ImportType(typeof(ExtensionAttribute));
        
        var memExtensions = ImportType(typeof(MemoryExtensions));
        
        // --- Object ---
        {
            var (t, obj, methods) = objectType;

            methods["Equals"] = Inst(t, "Equals", cl.Boolean, cl.Object);
            methods["ToString"] = Inst(t, "ToString", cl.String);
            methods["MemberwiseClone"] = Inst(t, "MemberwiseClone", cl.Object);
            
            methods.TrimExcess();
        }
        
        // --- String ---
        {
            var (t, str, methods) = stringType;

            methods["charAt"] = Inst(t, "get_Chars", cl.Char, cl.Int32);
            methods["Concat_s0_s1"] = Static(t, "Concat", str, str, str);
            methods["Concat_s0_s1_s2"] = Static(t, "Concat", str, str, str, str);
            methods["Equals"] = Static(t, "Equals", cl.Boolean, str, str);
            methods["Substring"] = Inst(t, "Substring", str, cl.Int32, cl.Int32);
            methods["Join_str_enum"] = GenStatic(t, "Join", str, 1, str, Enum(Gm(0)));
            
            methods.TrimExcess();
        }
        
        // --- Type ---
        {
            var (t, dotnetType, methods) = typeType;

            methods["GetTypeFromHandle"] = Static(t, "GetTypeFromHandle", dotnetType, runtimeTypeHandle.sig);

            methods.TrimExcess();
        }
        
        // --- ValueType ---
        {
            
        }
        
        // --- Enum ---
        {
            
        }
        
        // --- Int128 ---
        {
            var (t, i128, methods) = int128;

            methods["new"] = Inst(t, ".ctor", cl.Void, cl.UInt64, cl.UInt64);

            methods["Parse"] = Static(t, "Parse", i128, cl.String);
            methods["Add"] = Static(t, "op_Addition", i128, i128, i128);
            methods["Sub"] = Static(t, "op_Subtraction", i128, i128, i128);
            methods["Mul"] = Static(t, "op_Multiply", i128, i128, i128);
            methods["Div"] = Static(t, "op_Division", i128, i128, i128);
            methods["Rem"] = Static(t, "op_Modulus", i128, i128, i128);

            methods["BitwiseAnd"] = Static(t, "op_BitwiseAnd", i128, i128, i128);
            methods["BitwiseOr"] = Static(t, "op_BitwiseOr", i128, i128, i128);
            methods["BitwiseXor"] = Static(t, "op_ExclusiveOr", i128, i128, i128);
            methods["BitwiseNot"] = Static(t, "op_OnesComplement", i128, i128);

            methods["LeftShift"] = Static(t, "op_LeftShift", i128, i128, cl.Int32);
            methods["RightShift"] = Static(t, "op_RightShift", i128, i128, cl.Int32);

            methods["Conv_from_i32"] = Static(t, "op_Implicit", i128, cl.Int32);
            methods["Conv_from_i64"] = Static(t, "op_Implicit", i128, cl.Int64);

            methods["Conv_to_i32"] = Static(t, "op_Explicit", cl.Int32, i128);
            methods["Conv_to_i64"] = Static(t, "op_Explicit", cl.Int64, i128);

            methods.TrimExcess();
        }

        // --- UInt128 ---
        {
            var (t, u128, methods) = uint128;

            methods["new"] = Inst(t, ".ctor", cl.Void, cl.UInt64, cl.UInt64);

            methods["Parse"] = Static(t, "Parse", u128, cl.String);
            methods["Add"] = Static(t, "op_Addition", u128, u128, u128);
            methods["Sub"] = Static(t, "op_Subtraction", u128, u128, u128);

            methods["Conv_from_u32"] = Static(t, "op_Implicit", u128, cl.UInt32);
            methods["Conv_to_u32"] = Static(t, "op_Explicit", cl.UInt32, u128);

            methods.TrimExcess();
        }

        // --- RuntimeTypeHandle ---
        {
            
        }
        
        // --- Span ---
        {
            var (t, span, methods) = spanType;
            
            methods["Equals"] = Inst(t, "op_Equality", cl.Boolean, span);
            methods["Get"] = Inst(t, "get_Item", Gt(0), cl.Int32);
            methods["Fill"] = Inst(t, "Fill", cl.Void, Gt(0));
            methods["CopySelfTo"] = Inst(t, "Fill", cl.Void, span);
            methods["Slice"] = Inst(t, "Slice", span, cl.Int32, cl.Int32);

            methods.TrimExcess();
        }
        
        // --- Mem Extensions ---
        {
            var (t, _, methods) = memExtensions;
            
            methods["AsSpan_i32"] = Static(t, "AsSpan", Gen(spanType.sig, Gm(0)), Arr(Gm(0)));

            methods.TrimExcess();
        }
        
        // --- StringBuilder ---
        {
            var (t, sb, methods) = stringBuilder;
            
            methods["new"] = Inst(t, ".ctor", cl.Void);
            methods["Append_char"] = Inst(t, "Append", sb, cl.Char);
            methods["Append_str"] = Inst(t, "Append", sb, cl.String);
            methods["get_Len"] = Inst(t, "get_Length", cl.Int32);
            methods["set_Len"] = Inst(t, "set_Length", cl.Void, cl.Int32);
            methods["ToString"] = Inst(t, "ToString", cl.String);

            methods.TrimExcess();
        }
        
        // --- IEnumerable ---
        {
            var (t, enumerable, methods) = iEnumerable;
            
            methods["GetEnumerator"] = Inst(t, "GetEnumerator", Enum(Gt(0)), Gen(iEnumerator.sig, Gt(0)));

            methods.TrimExcess();
        }
        
        // --- ExtensionAttribute ---
        {
            var (t, attrib, methods) = extensionAttribute;
            
            methods["new"] = Inst(t, ".ctor", cl.Void);

            methods.TrimExcess();
        }
        
        return;
        IMethodDescriptor Inst(ITypeDefOrRef type, string name, TypeSignature ret, params TypeSignature[] args)
            => CreateMethodRef(type, name, MethodSignature.CreateInstance(ret, args));
        IMethodDescriptor Static(ITypeDefOrRef type, string name, TypeSignature ret, params TypeSignature[] args)
            => CreateMethodRef(type, name, MethodSignature.CreateStatic(ret, args));
        IMethodDescriptor GenInst(ITypeDefOrRef type, string name, TypeSignature ret, int gargs, params TypeSignature[] args)
            => CreateMethodRef(type, name, MethodSignature.CreateInstance(ret, gargs, args));
        IMethodDescriptor GenStatic(ITypeDefOrRef type, string name, TypeSignature ret, int gargs, params TypeSignature[] args)
            => CreateMethodRef(type, name, MethodSignature.CreateStatic(ret, gargs, args));
        
        TypeSignature Arr(TypeSignature element) => new SzArrayTypeSignature(element);
        TypeSignature Enum(TypeSignature element) => new GenericInstanceTypeSignature(
            iEnumerable.sig.ToTypeDefOrRef(), false, element);
        TypeSignature Nil(TypeSignature element) => new GenericInstanceTypeSignature(
            nullable.sig.ToTypeDefOrRef(), true, element);
    }
    private void LoadRuntimeHelpers()
    {
        const TypeAttributes attributes = TypeAttributes.AnsiClass
                                          | TypeAttributes.Class
                                          | TypeAttributes.Sealed
                                          | TypeAttributes.Abstract
                                          | TypeAttributes.Public;
        
        var runtimeHelpers = new TypeDefinition(
            "<Module>", "RuntimeHelpers",
            attributes, _coreLib["System.Object"].t.ToTypeDefOrRef());
        _module.TopLevelTypes.Add(runtimeHelpers);

        #region SliceAsString
        {
            var m = new MethodDefinition(
                "AsString",
                MethodAttributes.Assembly | MethodAttributes.Static,
                MethodSignature.CreateStatic(
                    _corLibFactory.String, 1,
                    new SzArrayTypeSignature(new GenericParameterSignature(GenericParameterType.Method, 0)))
            );
            
            m.GenericParameters.Add(new GenericParameter("T"));
            m.ParameterDefinitions.Add(new ParameterDefinition("value"));
            
            m.CustomAttributes.Add(new CustomAttribute((MemberReference)_coreLib["System.Runtime.CompilerServices.ExtensionAttribute"].m["new"]));
            
            runtimeHelpers.Methods.Add(m);
            
            var body = new CilMethodBody(m);
            m.CilMethodBody = body;
            var gen = body.Instructions;
            
            var stringBuilder = _coreLib["System.Text.StringBuilder"];
            
            var elmTypeSpec = new TypeSpecification(new GenericParameterSignature(GenericParameterType.Method, 0));
            var elmType = _module.DefaultImporter.ImportType(elmTypeSpec);
            
            var sbNew = stringBuilder.m["new"];
            var sbAppendC = stringBuilder.m["Append_char"];
            var sbAppendS = stringBuilder.m["Append_str"];
            var sbToStr = stringBuilder.m["ToString"];
            
            var tmpSb = new CilLocalVariable(stringBuilder.t);
            var tmpI = new CilLocalVariable(_corLibFactory.Int32);
            var tmpLen = new CilLocalVariable(_corLibFactory.Int32);
            body.LocalVariables.Add(tmpSb);
            body.LocalVariables.Add(tmpI);
            body.LocalVariables.Add(tmpLen);
            
            var loopLbl = new CilInstructionLabel();
            var checkLbl = new CilInstructionLabel();
            var skipLbl = new CilInstructionLabel();
            
            gen.Add(CilOpCodes.Newobj, sbNew);
            gen.Add(CilOpCodes.Stloc, tmpSb);

            gen.Add(CilOpCodes.Ldloc, tmpSb);
            gen.Add(CilOpCodes.Ldc_I4_S, (byte)'[');
            gen.Add(CilOpCodes.Call, sbAppendC);
            gen.Add(CilOpCodes.Pop);
            
            gen.Add(CilOpCodes.Ldc_I4_0);
            gen.Add(CilOpCodes.Stloc, tmpI);
            
            gen.Add(CilOpCodes.Ldarg_0);
            gen.Add(CilOpCodes.Ldlen);
            gen.Add(CilOpCodes.Dup);
            gen.Add(CilOpCodes.Stloc, tmpLen);
            gen.Add(CilOpCodes.Brfalse, skipLbl);
            
            gen.Add(CilOpCodes.Br, checkLbl);
            // loop body
            {
                loopLbl.Instruction = gen.Add(CilOpCodes.Ldloc, tmpSb);
                gen.Add(CilOpCodes.Ldarg_0);
                gen.Add(CilOpCodes.Ldloc, tmpI);
                gen.Add(CilOpCodes.Ldelem, elmType);
                gen.Add(CilOpCodes.Box, elmType);
                gen.Add(CilOpCodes.Callvirt, _coreLib["System.Object"].m["ToString"]);
                gen.Add(CilOpCodes.Call, sbAppendS);
                
                gen.Add(CilOpCodes.Ldstr, ", ");
                gen.Add(CilOpCodes.Call, sbAppendS);
                gen.Add(CilOpCodes.Pop);
                
                gen.Add(CilOpCodes.Ldloc, tmpI);
                gen.Add(CilOpCodes.Ldc_I4_1);
                gen.Add(CilOpCodes.Add);
                gen.Add(CilOpCodes.Stloc, tmpI);
            }
            
            // loop check
            {
                checkLbl.Instruction = gen.Add(CilOpCodes.Ldloc, tmpI);
                gen.Add(CilOpCodes.Ldloc, tmpLen);
                gen.Add(CilOpCodes.Blt, loopLbl);
            }
            
            gen.Add(CilOpCodes.Ldloc, tmpSb);
            gen.Add(CilOpCodes.Dup);
            gen.Add(CilOpCodes.Call, stringBuilder.m["get_Len"]);
            gen.Add(CilOpCodes.Ldc_I4_2);
            gen.Add(CilOpCodes.Sub);
            gen.Add(CilOpCodes.Call, stringBuilder.m["set_Len"]);
            
            skipLbl.Instruction = gen.Add(CilOpCodes.Ldloc, tmpSb);
            gen.Add(CilOpCodes.Ldc_I4_S, (byte)']');
            gen.Add(CilOpCodes.Call, sbAppendC);
            
            gen.Add(CilOpCodes.Call, sbToStr);
            gen.Add(CilOpCodes.Ret);
            
            _runtimeHelpers["Array_AsString"] = m;
        }
        #endregion

        LoadArithmeticHelpers(runtimeHelpers);
    }
    
    private (ITypeDefOrRef type, TypeSignature sig, Dictionary<string, IMethodDescriptor> methods) ImportType(string ns, string name)
    {
        var type = _module.DefaultImporter.ImportType(_module.CorLibTypeFactory.CorLibScope.CreateTypeReference(ns, name));
        var sig = _module.DefaultImporter.ImportTypeSignature(type.ToTypeSignature());
        var met = new Dictionary<string, IMethodDescriptor>();
        _coreLib.Add(type.FullName, (sig, met));
        return (type, sig, met);
    }
    private (ITypeDefOrRef type, TypeSignature sig, Dictionary<string, IMethodDescriptor> methods) ImportType(Type type)
        => ImportType(type.Namespace!, type.Name);
    private (ITypeDefOrRef type, TypeSignature sig, Dictionary<string, IMethodDescriptor> methods) ImportType(CorLibTypeSignature type)
    {
        var met = new Dictionary<string, IMethodDescriptor>();
        _coreLib.Add(type.FullName, (type, met));
        return (_module.DefaultImporter.ImportType(type.ToTypeDefOrRef()), type, met);
    }

    GenericParameterSignature Gt(int i) => new (GenericParameterType.Type, i);
    GenericParameterSignature Gm(int i) => new (GenericParameterType.Method, i);
    GenericInstanceTypeSignature Gen(TypeSignature t, TypeSignature e) => new GenericInstanceTypeSignature(t.ToTypeDefOrRef(), t.IsValueType, e);
}
