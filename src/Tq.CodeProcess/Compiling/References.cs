using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet.Signatures.Types;

namespace Abstract.CodeProcess;

public partial class Compiler
{
    
    private void LoadCoreLibResources()
    {
        var cl = _corLibFactory = _module.CorLibTypeFactory;
        
        Dictionary<string, IMethodDescriptor> methods;
        TypeReference typeref;
        ITypeDefOrRef type;
        TypeSignature self;
        
        methods = [];
        type = _module.DefaultImporter.ImportType(TypeDescriptorExtensions.CreateTypeReference(cl.CorLibScope, "System", "ValueType"));
        self = _module.DefaultImporter.ImportTypeSignature(type.ToTypeSignature());
        {
            
        }
        methods.TrimExcess();
        _coreLib.Add(type.Name!, (self, []));
        
        methods = [];
        self = cl.Object;
        type = _module.DefaultImporter.ImportType(self.ToTypeDefOrRef());
        {
            methods.Add("ToString", CreateMethodRef(type, "ToString", MethodSignature.CreateInstance(cl.String)));
            methods.Add("MemberwiseClone", CreateMethodRef(type, "MemberwiseClone", MethodSignature.CreateInstance(cl.Object)));
        }
        methods.TrimExcess();
        _coreLib.Add(type.Name!, (self, methods));
        
        methods = [];
        type = _module.DefaultImporter.ImportType(TypeDescriptorExtensions.CreateTypeReference(cl.CorLibScope, "System", "Enum"));
        self = _module.DefaultImporter.ImportTypeSignature(type.ToTypeSignature());
        {
            
        }
        methods.TrimExcess();
        _coreLib.Add(type.Name!, (self, []));
        
        
        methods = [];
        type = _module.DefaultImporter.ImportType(TypeDescriptorExtensions.CreateTypeReference(cl.CorLibScope, "System", "Int128"));
        self = _module.DefaultImporter.ImportTypeSignature(type.ToTypeSignature());
        {
            methods.Add("new", CreateMethodRef(type, ".ctor", MethodSignature.CreateInstance(cl.Void, cl.UInt64, cl.UInt64)));
            
            methods.Add("Parse", CreateMethodRef(type, "Parse", MethodSignature.CreateStatic(self, [cl.String])));
            methods.Add("Add_ovf", CreateMethodRef(type, "op_Addition", MethodSignature.CreateStatic(self, self, self)));
            methods.Add("Add", CreateMethodRef(type, "op_CheckedAddition", MethodSignature.CreateStatic(self, self, self)));
            methods.Add("Sub_ovf", CreateMethodRef(type, "op_Subtraction", MethodSignature.CreateStatic(self, self, self)));
            methods.Add("Sub", CreateMethodRef(type, "op_CheckedSubtraction", MethodSignature.CreateStatic(self, self, self)));
            methods.Add("Mul", CreateMethodRef(type, "op_Multiply", MethodSignature.CreateStatic(self, self, self)));
            methods.Add("Div", CreateMethodRef(type, "op_Division", MethodSignature.CreateStatic(self, self, self)));
            methods.Add("Rem", CreateMethodRef(type, "op_Modulus", MethodSignature.CreateStatic(self, self, self)));
            
            methods.Add("BitwiseAnd", CreateMethodRef(type, "op_BitwiseAnd", MethodSignature.CreateStatic(self, self, self)));
            methods.Add("BitwiseOr", CreateMethodRef(type, "op_BitwiseOr", MethodSignature.CreateStatic(self, self, self)));
            methods.Add("BitwiseXor", CreateMethodRef(type, "op_ExclusiveOr", MethodSignature.CreateStatic(self, self, self)));
            methods.Add("BitwiseNot", CreateMethodRef(type, "op_OnesComplement", MethodSignature.CreateStatic(self, self)));
            methods.Add("LeftShift", CreateMethodRef(type, "op_LeftShift", MethodSignature.CreateStatic(self, self, cl.Int32)));
            methods.Add("RightShift", CreateMethodRef(type, "op_RightShift", MethodSignature.CreateStatic(self, self, cl.Int32)));
            
            methods.Add("Conv_from_i8", CreateMethodRef(type, "op_Implicit", MethodSignature.CreateStatic(self, [cl.SByte])));
            methods.Add("Conv_from_u8", CreateMethodRef(type, "op_Implicit", MethodSignature.CreateStatic(self, [cl.Byte])));
            methods.Add("Conv_from_i16", CreateMethodRef(type, "op_Implicit", MethodSignature.CreateStatic(self, [cl.Int16])));
            methods.Add("Conv_from_u16", CreateMethodRef(type, "op_Implicit", MethodSignature.CreateStatic(self, [cl.UInt16])));
            methods.Add("Conv_from_i32", CreateMethodRef(type, "op_Implicit", MethodSignature.CreateStatic(self, [cl.Int32])));
            methods.Add("Conv_from_u32", CreateMethodRef(type, "op_Implicit", MethodSignature.CreateStatic(self, [cl.UInt32])));
            methods.Add("Conv_from_i64", CreateMethodRef(type, "op_Implicit", MethodSignature.CreateStatic(self, [cl.Int64])));
            methods.Add("Conv_from_u64", CreateMethodRef(type, "op_Implicit", MethodSignature.CreateStatic(self, [cl.UInt64])));
            
            methods.Add("Conv_to_i8", CreateMethodRef(type, "op_Explicit", MethodSignature.CreateStatic(cl.SByte, self)));
            methods.Add("Conv_to_u8", CreateMethodRef(type, "op_Explicit", MethodSignature.CreateStatic(cl.Byte, self)));
            methods.Add("Conv_to_i16", CreateMethodRef(type, "op_Explicit", MethodSignature.CreateStatic(cl.Int16, self)));
            methods.Add("Conv_to_u16", CreateMethodRef(type, "op_Explicit", MethodSignature.CreateStatic(cl.UInt16, self)));
            methods.Add("Conv_to_i32", CreateMethodRef(type, "op_Explicit", MethodSignature.CreateStatic(cl.Int32, self)));
            methods.Add("Conv_to_u32", CreateMethodRef(type, "op_Explicit", MethodSignature.CreateStatic(cl.UInt32, self)));
            methods.Add("Conv_to_i64", CreateMethodRef(type, "op_Explicit", MethodSignature.CreateStatic(cl.Int64, self)));
            methods.Add("Conv_to_u64", CreateMethodRef(type, "op_Explicit", MethodSignature.CreateStatic(cl.UInt64, self)));
        }
        methods.TrimExcess();
        _coreLib.Add(type.Name!, (self, methods));
        
        methods = [];
        type = _module.DefaultImporter.ImportType(TypeDescriptorExtensions.CreateTypeReference(cl.CorLibScope, "System", "UInt128"));
        self = _module.DefaultImporter.ImportTypeSignature(type.ToTypeSignature());
        {
            methods.Add("new", CreateMethodRef(type, ".ctor", MethodSignature.CreateInstance(cl.Void, cl.UInt64, cl.UInt64)));
            
            methods.Add("Parse", CreateMethodRef(type, "Parse", MethodSignature.CreateStatic(self, [cl.String])));
            methods.Add("Add_ovf", CreateMethodRef(type, "op_Addition", MethodSignature.CreateStatic(self, self, self)));
            methods.Add("Add", CreateMethodRef(type, "op_CheckedAddition", MethodSignature.CreateStatic(self, self, self)));
            methods.Add("Sub_ovf", CreateMethodRef(type, "op_Subtraction", MethodSignature.CreateStatic(self, self, self)));
            methods.Add("Sub", CreateMethodRef(type, "op_CheckedSubtraction", MethodSignature.CreateStatic(self, self, self)));
            methods.Add("Mul", CreateMethodRef(type, "op_Multiply", MethodSignature.CreateStatic(self, self, self)));
            methods.Add("Div", CreateMethodRef(type, "op_Division", MethodSignature.CreateStatic(self, self, self)));
            methods.Add("Rem", CreateMethodRef(type, "op_Modulus", MethodSignature.CreateStatic(self, self, self)));
            
            methods.Add("BitwiseAnd", CreateMethodRef(type, "op_BitwiseAnd", MethodSignature.CreateStatic(self, self, self)));
            methods.Add("BitwiseOr", CreateMethodRef(type, "op_BitwiseOr", MethodSignature.CreateStatic(self, self, self)));
            methods.Add("BitwiseXor", CreateMethodRef(type, "op_ExclusiveOr", MethodSignature.CreateStatic(self, self, self)));
            methods.Add("BitwiseNot", CreateMethodRef(type, "op_OnesComplement", MethodSignature.CreateStatic(self, self)));
            methods.Add("LeftShift", CreateMethodRef(type, "op_LeftShift", MethodSignature.CreateStatic(self, self, cl.Int32)));
            methods.Add("RightShift", CreateMethodRef(type, "op_RightShift", MethodSignature.CreateStatic(self, self, cl.Int32)));
            
            methods.Add("Conv_from_i8", CreateMethodRef(type, "op_Explicit", MethodSignature.CreateStatic(self, [cl.SByte])));
            methods.Add("Conv_from_u8", CreateMethodRef(type, "op_Implicit", MethodSignature.CreateStatic(self, [cl.Byte])));
            methods.Add("Conv_from_i16", CreateMethodRef(type, "op_Explicit", MethodSignature.CreateStatic(self, [cl.Int16])));
            methods.Add("Conv_from_u16", CreateMethodRef(type, "op_Implicit", MethodSignature.CreateStatic(self, [cl.UInt16])));
            methods.Add("Conv_from_i32", CreateMethodRef(type, "op_Explicit", MethodSignature.CreateStatic(self, [cl.Int32])));
            methods.Add("Conv_from_u32", CreateMethodRef(type, "op_Implicit", MethodSignature.CreateStatic(self, [cl.UInt32])));
            methods.Add("Conv_from_i64", CreateMethodRef(type, "op_Explicit", MethodSignature.CreateStatic(self, [cl.Int64])));
            methods.Add("Conv_from_u64", CreateMethodRef(type, "op_Implicit", MethodSignature.CreateStatic(self, [cl.UInt64])));
            
            methods.Add("Conv_to_i8", CreateMethodRef(type, "op_Explicit", MethodSignature.CreateStatic(cl.SByte, self)));
            methods.Add("Conv_to_u8", CreateMethodRef(type, "op_Explicit", MethodSignature.CreateStatic(cl.Byte, self)));
            methods.Add("Conv_to_i16", CreateMethodRef(type, "op_Explicit", MethodSignature.CreateStatic(cl.Int16, self)));
            methods.Add("Conv_to_u16", CreateMethodRef(type, "op_Explicit", MethodSignature.CreateStatic(cl.UInt16, self)));
            methods.Add("Conv_to_i32", CreateMethodRef(type, "op_Explicit", MethodSignature.CreateStatic(cl.Int32, self)));
            methods.Add("Conv_to_u32", CreateMethodRef(type, "op_Explicit", MethodSignature.CreateStatic(cl.UInt32, self)));
            methods.Add("Conv_to_i64", CreateMethodRef(type, "op_Explicit", MethodSignature.CreateStatic(cl.Int64, self)));
            methods.Add("Conv_to_u64", CreateMethodRef(type, "op_Explicit", MethodSignature.CreateStatic(cl.UInt64, self)));
        }
        methods.TrimExcess();
        _coreLib.Add(type.Name!, (self, methods));
        
        methods = [];
        self = cl.String;
        type = _module.DefaultImporter.ImportType(self.ToTypeDefOrRef());
        {
            methods.Add("charAt", CreateMethodRef(type, "get_Chars", MethodSignature.CreateInstance((TypeSignature)cl.Char, [cl.Int32])));
            methods.Add("Concat", CreateMethodRef(type, "Concat", MethodSignature.CreateStatic(TypeDescriptorExtensions.MakeArrayType(cl.String, 1))));
            methods.Add("Equals", CreateMethodRef(type, "Equals", MethodSignature.CreateStatic(cl.Boolean, cl.String, cl.String)));
        }
        methods.TrimExcess();
        _coreLib.Add(type.Name!, (self, methods));
        
    }
    
}