using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;

namespace Abstract.CodeProcess;

public partial class Compiler
{
    private void LoadArithmeticHelpers(TypeDefinition runtimeHelpers)
    {
        
        #region add_i8_Saturated
        {
            var t = _corLibFactory.SByte;
            var m = new MethodDefinition(
                "AddSaturatedI8",
                MethodAttributes.Assembly | MethodAttributes.Static,
                MethodSignature.CreateStatic(t, 0, t, t)
            );
            m.ParameterDefinitions.Add(new ParameterDefinition("left"));
            m.ParameterDefinitions.Add(new ParameterDefinition("right"));
            runtimeHelpers.Methods.Add(m);
            _runtimeHelpers[""].m[m.Name!] = m;
            
            var body = new CilMethodBody(m);
            m.CilMethodBody = body;
            var il = body.Instructions;
            
            var ifOverflow = new CilInstructionLabel();
            var ifUnderflow = new CilInstructionLabel();
            
            // operation
            il.Add(CilOpCodes.Ldarg_0);
            il.Add(CilOpCodes.Ldarg_1);
            il.Add(CilOpCodes.Add);
            
            // check overflow
            il.Add(CilOpCodes.Dup);
            il.Add(CilOpCodes.Ldc_I4, sbyte.MaxValue);
            il.Add(CilOpCodes.Bgt, ifOverflow);
            
            // check underflow
            il.Add(CilOpCodes.Dup);
            il.Add(CilOpCodes.Ldc_I4_S, sbyte.MinValue);
            il.Add(CilOpCodes.Blt, ifUnderflow);
            
            // return result
            il.Add(CilOpCodes.Conv_I1);
            il.Add(CilOpCodes.Ret);
            
            // return if overflow
            ifOverflow.Instruction = il.Add(CilOpCodes.Pop);
            il.Add(CilOpCodes.Ldc_I4_S, sbyte.MaxValue);
            il.Add(CilOpCodes.Conv_I1);
            il.Add(CilOpCodes.Ret);
            
            // return if underflow
            ifUnderflow.Instruction = il.Add(CilOpCodes.Pop);
            il.Add(CilOpCodes.Ldc_I4_S, sbyte.MinValue);
            il.Add(CilOpCodes.Ret);
        }
        #endregion
        #region add_u8_Saturated
        {
            var t = _corLibFactory.Byte;
            var m = new MethodDefinition(
                "AddSaturatedU8",
                MethodAttributes.Assembly | MethodAttributes.Static,
                MethodSignature.CreateStatic(t, 0, t, t)
            );
            m.ParameterDefinitions.Add(new ParameterDefinition("left"));
            m.ParameterDefinitions.Add(new ParameterDefinition("right"));
            runtimeHelpers.Methods.Add(m);
            _runtimeHelpers[""].m[m.Name!] = m;
            
            var body = new CilMethodBody(m);
            m.CilMethodBody = body;
            var il = body.Instructions;
            
            var ifOverflow = new CilInstructionLabel();
            
            // operation
            il.Add(CilOpCodes.Ldarg_0);
            il.Add(CilOpCodes.Ldarg_1);
            il.Add(CilOpCodes.Add);
            
            // check overflow
            il.Add(CilOpCodes.Dup);
            il.Add(CilOpCodes.Ldc_I4, byte.MaxValue);
            il.Add(CilOpCodes.Bgt_Un, ifOverflow);
            
            // return result
            il.Add(CilOpCodes.Conv_U1);
            il.Add(CilOpCodes.Ret);
            
            // return if overflow
            ifOverflow.Instruction = il.Add(CilOpCodes.Pop);
            il.Add(CilOpCodes.Ldc_I4_S, byte.MaxValue);
            il.Add(CilOpCodes.Ret);
        }
        #endregion
        
        #region add_i16_Saturated
        {
            var t = _corLibFactory.Int16;
            var m = new MethodDefinition(
                "AddSaturatedI16",
                MethodAttributes.Assembly | MethodAttributes.Static,
                MethodSignature.CreateStatic(t, 0, t, t)
            );
            m.ParameterDefinitions.Add(new ParameterDefinition("left"));
            m.ParameterDefinitions.Add(new ParameterDefinition("right"));
            runtimeHelpers.Methods.Add(m);
            _runtimeHelpers[""].m[m.Name!] = m;
            
            var body = new CilMethodBody(m);
            m.CilMethodBody = body;
            var il = body.Instructions;
            
            var ifOverflow = new CilInstructionLabel();
            var ifUnderflow = new CilInstructionLabel();
            
            // operation
            il.Add(CilOpCodes.Ldarg_0);
            il.Add(CilOpCodes.Ldarg_1);
            il.Add(CilOpCodes.Add);
            
            // check overflow
            il.Add(CilOpCodes.Dup);
            il.Add(CilOpCodes.Ldc_I4, short.MaxValue);
            il.Add(CilOpCodes.Bgt, ifOverflow);
            
            // check underflow
            il.Add(CilOpCodes.Dup);
            il.Add(CilOpCodes.Ldc_I4, short.MinValue);
            il.Add(CilOpCodes.Blt, ifUnderflow);
            
            // return result
            il.Add(CilOpCodes.Conv_I2);
            il.Add(CilOpCodes.Ret);
            
            // return if overflow
            ifOverflow.Instruction = il.Add(CilOpCodes.Pop);
            il.Add(CilOpCodes.Ldc_I4, short.MaxValue);
            il.Add(CilOpCodes.Conv_I2);
            il.Add(CilOpCodes.Ret);
            
            // return if underflow
            ifUnderflow.Instruction = il.Add(CilOpCodes.Pop);
            il.Add(CilOpCodes.Ldc_I4, short.MinValue);
            il.Add(CilOpCodes.Ret);
        }
        #endregion
        #region add_u16_Saturated
        {
            var t = _corLibFactory.UInt16;
            var m = new MethodDefinition(
                "AddSaturatedU16",
                MethodAttributes.Assembly | MethodAttributes.Static,
                MethodSignature.CreateStatic(t, 0, t, t)
            );
            m.ParameterDefinitions.Add(new ParameterDefinition("left"));
            m.ParameterDefinitions.Add(new ParameterDefinition("right"));
            runtimeHelpers.Methods.Add(m);
            _runtimeHelpers[""].m[m.Name!] = m;
            
            var body = new CilMethodBody(m);
            m.CilMethodBody = body;
            var il = body.Instructions;
            
            var ifOverflow = new CilInstructionLabel();
            
            // operation
            il.Add(CilOpCodes.Ldarg_0);
            il.Add(CilOpCodes.Ldarg_1);
            il.Add(CilOpCodes.Add);
            
            // check overflow
            il.Add(CilOpCodes.Dup);
            il.Add(CilOpCodes.Ldc_I4, ushort.MaxValue);
            il.Add(CilOpCodes.Bgt_Un, ifOverflow);
            
            // return result
            il.Add(CilOpCodes.Conv_U2);
            il.Add(CilOpCodes.Ret);
            
            // return if overflow
            ifOverflow.Instruction = il.Add(CilOpCodes.Pop);
            il.Add(CilOpCodes.Ldc_I4, ushort.MaxValue);
            il.Add(CilOpCodes.Ret);
        }
        #endregion
        
        
        #region sub_i8_Saturated
        {
            var t = _corLibFactory.SByte;
            var m = new MethodDefinition(
                "SubSaturatedI8",
                MethodAttributes.Assembly | MethodAttributes.Static,
                MethodSignature.CreateStatic(t, 0, t, t)
            );
            m.ParameterDefinitions.Add(new ParameterDefinition("left"));
            m.ParameterDefinitions.Add(new ParameterDefinition("right"));
            runtimeHelpers.Methods.Add(m);
            _runtimeHelpers[""].m[m.Name!] = m;
            
            var body = new CilMethodBody(m);
            m.CilMethodBody = body;
            var il = body.Instructions;
            
            var ifOverflow = new CilInstructionLabel();
            var ifUnderflow = new CilInstructionLabel();
            
            // operation
            il.Add(CilOpCodes.Ldarg_0);
            il.Add(CilOpCodes.Ldarg_1);
            il.Add(CilOpCodes.Sub);
            
            // check overflow
            il.Add(CilOpCodes.Dup);
            il.Add(CilOpCodes.Ldc_I4, sbyte.MaxValue);
            il.Add(CilOpCodes.Bgt, ifOverflow);
            
            // check underflow
            il.Add(CilOpCodes.Dup);
            il.Add(CilOpCodes.Ldc_I4_S, sbyte.MinValue);
            il.Add(CilOpCodes.Blt, ifUnderflow);
            
            // return result
            il.Add(CilOpCodes.Conv_I1);
            il.Add(CilOpCodes.Ret);
            
            // return if overflow
            ifOverflow.Instruction = il.Add(CilOpCodes.Pop);
            il.Add(CilOpCodes.Ldc_I4_S, sbyte.MaxValue);
            il.Add(CilOpCodes.Conv_I1);
            il.Add(CilOpCodes.Ret);
            
            // return if underflow
            ifUnderflow.Instruction = il.Add(CilOpCodes.Pop);
            il.Add(CilOpCodes.Ldc_I4_S, sbyte.MinValue);
            il.Add(CilOpCodes.Ret);
        }
        #endregion
        #region add_u8_Saturated
        {
            var t = _corLibFactory.Byte;
            var m = new MethodDefinition(
                "SubSaturatedU8",
                MethodAttributes.Assembly | MethodAttributes.Static,
                MethodSignature.CreateStatic(t, 0, t, t)
            );
            m.ParameterDefinitions.Add(new ParameterDefinition("left"));
            m.ParameterDefinitions.Add(new ParameterDefinition("right"));
            runtimeHelpers.Methods.Add(m);
            _runtimeHelpers[""].m[m.Name!] = m;
            
            var body = new CilMethodBody(m);
            m.CilMethodBody = body;
            var il = body.Instructions;
            
            var ifUnderflow = new CilInstructionLabel();
            
            // operation
            il.Add(CilOpCodes.Ldarg_0);
            il.Add(CilOpCodes.Ldarg_1);
            il.Add(CilOpCodes.Sub);
            
            // check overflow
            il.Add(CilOpCodes.Dup);
            il.Add(CilOpCodes.Ldc_I4, 0);
            il.Add(CilOpCodes.Blt, ifUnderflow);
            
            // return result
            il.Add(CilOpCodes.Conv_U1);
            il.Add(CilOpCodes.Ret);
            
            // return if underflow
            ifUnderflow.Instruction = il.Add(CilOpCodes.Pop);
            il.Add(CilOpCodes.Ldc_I4_S, 0);
            il.Add(CilOpCodes.Ret);
        }
        #endregion

    }
}
