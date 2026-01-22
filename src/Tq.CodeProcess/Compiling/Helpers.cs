using System.Diagnostics;
using System.Text;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin.Integer;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using Parameter = AsmResolver.DotNet.Collections.Parameter;
using FieldDefinition = AsmResolver.DotNet.FieldDefinition;
using MethodDefinition = AsmResolver.DotNet.MethodDefinition;
using TypeDefinition = AsmResolver.DotNet.TypeDefinition;
using TypeReference = Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.TypeReference;

namespace Abstract.CodeProcess;

public partial class Compiler
{
    
    private void DumpModule()
    {
        var sb = new StringBuilder();
        foreach (var type in _module.GetAllTypes())
        {
            sb.Append('\n');
            sb.Append(type.IsValueType ? "struct " : "class ");
            sb.AppendLine($"{type.FullName} extends {type.BaseType} {{");

            foreach (var field in type.Fields)
            {
                sb.Append("\tfield ");
                sb.Append(field.IsPublic ? "public " : "private ");
                sb.Append(field.IsStatic ? "static " : "instance ");
                sb.AppendLine($"{field.Signature} {field.Name}");
            }
                
            foreach (var method in type.Methods)
            {
                sb.Append("\n\tmethod ");
                sb.Append(method.IsPublic ? "public " : "private ");
                sb.Append(method.IsStatic ? "static " : "instance ");
                sb.Append($"{method.Name} ({string.Join<Parameter>(", ", method.Parameters)}) ");
                sb.Append($"{method.Signature!.ReturnType} ");
                sb.AppendLine("{");
                if (method.CilMethodBody != null)
                {
                    foreach (var local in method.CilMethodBody.LocalVariables)
                        sb.AppendLine($"\t\t.locals init ({local.VariableType} {local})");
                        
                    foreach (var inst in method.CilMethodBody.Instructions)
                        sb.AppendLine($"\t\t{inst}");
                }
                sb.AppendLine("\t}");
            }
            sb.AppendLine("}");
        }
            
        File.WriteAllText(".abs-cache/debug/dlldump.il", sb.ToString());
    }
    
    private TypeSignature TypeFromRef(TypeReference? typeRef)
    {
        if (typeRef == null) return _corLibFactory.Void;
        switch (typeRef)
        {
            case UnsolvedTypeReference: throw new Exception("Type reference is unsolved!");
            
            case ReferenceTypeReference @r:
            {
                var b = TypeFromRef(r.InternalType);
                return b.IsValueType ? b.MakeByReferenceType() : b;
            }
            case SliceTypeReference @s:
                return new SzArrayTypeSignature(TypeFromRef(s.InternalType));
            
            
            case RuntimeIntegerTypeReference @i:
            {
                return i.BitSize.Bits switch
                {
                    <= 8 => i.Signed ? _corLibFactory.SByte : _corLibFactory.Byte,
                    <= 16 => i.Signed ? _corLibFactory.Int16 : _corLibFactory.UInt16,
                    <= 32 => i.Signed ? _corLibFactory.Int32 : _corLibFactory.UInt32,
                    <= 64 => i.Signed ? _corLibFactory.Int64 : _corLibFactory.UInt64,
                    <= 128 => _coreLib[i.Signed ? "Int128" : "UInt128"].t,
                    _ => throw new UnreachableException()
                };
            }
    
            case CharTypeReference: return _corLibFactory.Char;
            case StringTypeReference: return _corLibFactory.String;
            case BooleanTypeReference: return _corLibFactory.Boolean;
            
            case NoReturnTypeReference:
            case VoidTypeReference: return _corLibFactory.Void;

            case AnytypeTypeReference: return _corLibFactory.Object;
            
            case SolvedStructTypeReference @i: return _typesMap[i.Struct].ToTypeSignature();
            case SolvedTypedefTypeReference @t: return _enumsMap[t.Typedef].ToTypeSignature();
            
            default: throw new UnreachableException();
        }
    }
    
    private IMethodDescriptor CreateMethodRef(ITypeDefOrRef basetype, string name, MethodSignature signature)
    {
        var importedsig = _module.DefaultImporter.ImportMethodSignature(signature);
        var meth = basetype.CreateMemberReference(name, importedsig);
        return _module.DefaultImporter.ImportMethod(meth);
    }

    private bool IsExplicitInteger(CorLibTypeSignature typeSig, out bool signed, out int size)
    {
        switch (typeSig.ElementType)
        {
            case ElementType.I1: signed = true; size = 1; return true;
            case ElementType.I2: signed = true; size = 2; return true;
            case ElementType.I4: signed = true; size = 4; return true;
            case ElementType.I:
            case ElementType.I8: signed = true; size = 8; return true;
            
            case ElementType.U1: signed = false; size = 1; return true;
            case ElementType.U2: signed = false; size = 2; return true;
            case ElementType.U4: signed = false; size = 4; return true;
            case ElementType.U:
            case ElementType.U8: signed = false; size = 8; return true;
            
            default: signed = false; size = 0; return false;
        }
    }
    
    private class StructData(ITypeDefOrRef typedef)
    {
        public readonly ITypeDefOrRef Type = typedef ?? throw new ArgumentNullException();
        public readonly TypeDefinition Def = typedef.Resolve() ?? throw new ArgumentNullException();
        
        public bool IsValueType => Def.IsValueType;
        
        public MethodDefinition PrimaryCtor = null!;
        public MethodDefinition Clone = null!;
        
        public TypeSignature ToTypeSignature() => Type.ToTypeSignature();
    }
    private class EnumData(ITypeDefOrRef typedef, IFieldDescriptor valueField)
    {
        public readonly ITypeDefOrRef Type = typedef ?? throw new ArgumentNullException();
        public readonly TypeDefinition Def = typedef.Resolve() ?? throw new ArgumentNullException();
        
        public readonly IFieldDescriptor Field = valueField;
        public Dictionary<TypedefNamedValue, FieldDefinition> Items = [];
        
        public TypeSignature ToTypeSignature() => Type.ToTypeSignature();
        public IFieldDescriptor GetItem(TypedefNamedValue namedValue) => Items[namedValue];
    }
    private class FunctionData(IMethodDefOrRef methoddef)
    {
        public readonly IMethodDefOrRef Function = methoddef ?? throw new ArgumentNullException();
        public readonly MethodDefinition Def = methoddef.Resolve() ?? throw new ArgumentNullException();
        
        public bool IsStatic => Def.IsStatic;
        public bool ReturnsValue => Function.Signature!.ReturnsValue;
        public TypeSignature ReturnType => Function.Signature!.ReturnType;
        public TypeDefinition? InstanceType => IsStatic ? null : Def.DeclaringType;
    }
    
    private class Context(ITypeDefOrRef? selfType, CilMethodBody body, Parameter[] args, CilLocalVariable[] locals)
    {
        public readonly ITypeDefOrRef? SelfType = selfType;
        public CilMethodBody Body = body;
        public CilInstructionCollection Gen = body.Instructions;
        public List<TypeSignature> Stack = [];
        public Stack<ContextFrame> Frame = [];
        
        private Parameter[] _args = args;
        private CilLocalVariable[] _locals = locals;
        private Dictionary<TypeSignature, CilLocalVariable> _tmp = [];

        public void MarkLabel(CilInstructionLabel label) => label.Instruction = Gen.Add(CilOpCodes.Nop);
        
        public void StackPush(TypeSignature type) => Stack.Add(type);
        public void StackPop() => Stack.RemoveAt(Stack.Count - 1);
        public void StackPop(int count) => Stack.RemoveRange(Stack.Count - count, count);
        
        public void FramePush(ContextFrame frame) => Frame.Push(frame);
        public void FramePop() => Frame.Pop();
        public ContextFrame GetFrame() => Frame.Peek();
        
        public Parameter GetArg(int i) => _args[i];
        public CilLocalVariable GetLoc(int i) => _locals[i];

        public CilLocalVariable AllocTmp(TypeSignature type)
        {
            if (_tmp.TryGetValue(type, out var tmp)) return tmp;
            
            var l = new CilLocalVariable(type);
            Body.LocalVariables.Add(l);
            _tmp.Add(type, l);
            return l;
        }
    }
    
    private abstract class ContextFrame {}
    private class ConditionalExpressionFrame(CilInstructionLabel iftrue, CilInstructionLabel iffalse) : ContextFrame
    {
        public readonly CilInstructionLabel IfTrue = iftrue;
        public readonly CilInstructionLabel IfFalse = iffalse;
    }
}
