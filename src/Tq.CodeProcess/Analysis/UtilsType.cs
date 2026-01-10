using System.Diagnostics;
using Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree;
using Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Expressions;
using Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Values;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.CodeReferences;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.FieldReferences;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.FunctionReferences;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences.Builtin;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences.Builtin.Integer;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Expression;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Expression.TypeModifiers;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Value;

namespace Abstract.CodeProcess;

public partial class Analyzer
{

    private static TypeReference GetEffectiveTypeReference(IrExpression expr)
    {
        return expr switch
        {
            IrIntegerLiteral @lit => lit.Type,
            IRStringLiteral @lit => lit.Type,
            IRSolvedReference @solvedFuck => solvedFuck.Reference switch
            {
                IntegerTypeReference @intt => intt,
                SolvedStructTypeReference @structt => structt,
                SolvedFieldReference field => field.Field.Type,
                SolvedFunctionReference @func => new FunctionTypeReference(
                    func.Function.ReturnType, func.Function.Parameters.Select(e => e.Type).ToArray()),
        
                LocalReference @local => local.Local.Type,
                ParameterReference @param => param.Parameter.Type,
                
                _ => throw new NotImplementedException()
            },
            IRAccess @access => GetEffectiveTypeReference(access.B),
            IRInvoke @invoke => invoke.Type,
            
            IRBinaryExp @exp => exp.ResultType,
            IRUnaryExp @unexp => unexp.Operation != IRUnaryExp.UnaryOperation.Reference
                ? GetEffectiveTypeReference(unexp.Value)
                : new ReferenceTypeReference(GetEffectiveTypeReference(@unexp.Value)),
            IrIndex @idx => idx.ResultType,
            
            IrConv @conv => conv.Type,

            _ => throw new NotImplementedException()
            
        } ?? throw new UnreachableException("This function should not be called when this value is null");
    }
    
    /// <summary>
    /// Try to solve constant type forms (arrays, pointers, builtin types, etc.)
    /// and returns `UnsolvedTypeReference` if evaluation-dependent.
    /// </summary>
    /// <param name="node">The type representation</param>
    /// <returns>The evaluation result</returns>
    private static TypeReference SolveShallowType(SyntaxNode node)
    {
        while (true)
        {
            switch (node)
            {
                case AccessNode @idc:
                    if (idc.Children.Length != 1) return new UnsolvedTypeReference(idc);
                    node = idc.Children[0];
                    continue;

                case IdentifierNode @id:
                    var value = id.Value;
                    switch (value)
                    {
                        case "int": return new RuntimeIntegerTypeReference(true);
                        case "uint": return new RuntimeIntegerTypeReference(false);
                        case "byte": return new RuntimeIntegerTypeReference(false, 8);
                        
                        case "bool": return new BooleanTypeReference();
                        case "void": return new VoidTypeReference();
                        case "type": return new TypeTypeReference();
                        case "string": return new StringTypeReference(StringEncoding.Undefined);
                        case "anytype": return new AnytypeTypeReference();
                        case "noreturn": return new NoReturnTypeReference();
                    }

                    if (value.Length > 1 && value[0] is 'i' or 'u' && value[1..].All(char.IsNumber))
                        return new RuntimeIntegerTypeReference(value[0] == 'i', byte.Parse(value[1..]));


                    return new UnsolvedTypeReference(id);

                case ArrayTypeModifierNode @ar:
                    return new SliceTypeReference(SolveShallowType(ar.Type));

                case ReferenceTypeModifierNode @rf:
                    return new ReferenceTypeReference(SolveShallowType(rf.Type));

                case NullableTypeModifierNode @nullable:
                    return new NullableTypeReference(SolveShallowType(nullable.Type));
                
                case BinaryExpressionNode @b: return new UnsolvedTypeReference(b);

                default: throw new NotImplementedException();
            }
        }
    }


    /// <summary>
    /// With a desired type and a value node,
    /// returns a node that explicitly solves
    /// any applicable casting.
    /// Value must already have been evaluated!
    /// </summary>
    /// <param name="typeTo"> Target type </param>
    /// <param name="value"> Value to cast </param>
    /// <param name="explicit"> explicit flag </param>
    /// <returns></returns>
    private IrExpression SolveTypeCast(TypeReference typeTo, IrExpression value, bool @explicit = false)
        => SolveTypeCast(typeTo, value, null!, @explicit);
    
    /// <summary>
    /// With a desired type and a value node,
    /// returns a node that explicitly solves
    /// any applicable casting.
    /// Value must already have been evaluated!
    /// </summary>
    /// <param name="typeTo"> Target type </param>
    /// <param name="value"> Value to cast </param>
    /// <param name="origin"> Original node </param>
    /// <param name="explicit"> explicit flag </param>
    /// <returns></returns>
    private IrExpression SolveTypeCast(TypeReference typeTo, IrExpression value, IrExpression origin, bool @explicit = false)
    {
        switch (value)
        {
            case IrIntegerLiteral @lit:
            {
                if (lit.Type is ComptimeIntegerTypeReference && typeTo is RuntimeIntegerTypeReference rint1)
                    return new IrIntegerLiteral(lit.Origin, lit.Value, rint1);

                if (lit.Type is RuntimeIntegerTypeReference @typetoRi && typeTo is RuntimeIntegerTypeReference rint2)
                {
                    var valType = GetEffectiveTypeReference(value);
                    if (valType is not RuntimeIntegerTypeReference valueRi) return value;
                    
                    // If same type, do nothing
                    if (rint2.BitSize == valueRi.BitSize && typetoRi.Signed == valueRi.Signed) return value;
                    
                    var val = valueRi;
                    var tar = typetoRi;
                    var o = value.Origin;
                    
                    if (val.Signed == tar.Signed && val.BitSize == tar.BitSize) return value;
                    return new IrConv(o, value, tar);
                }

                return origin;
                throw new UnreachableException();
            }
            
            case IrCollectionLiteral clit when typeTo is SliceTypeReference @s:
            {
                var elmtype = s.InternalType;
                var items = clit.Items.Select(e => SolveTypeCast(elmtype, e)).ToArray();
                return new IrCollectionLiteral(clit.Origin, elmtype, items);
            }
            
            // FIXME ignored for now
            case IRStringLiteral:
            case IRBinaryExp:
            case IRAccess:
            case IRReference:
            case IrConv:
            case IrIndex:
                return origin ?? value;
            
            default: throw new UnreachableException();
    }
        
        // switch (typeTo)
        // {
        //     case RuntimeIntegerTypeReference typetoRi when value is IrIntegerLiteral @lit:
        //         return new IrIntegerLiteral(lit.Origin, lit.Value, typetoRi);
        //     
        //     case RuntimeIntegerTypeReference typetoRi:
        //     {
        //         var valType = GetEffectiveTypeReference(value);
        //         if (valType is not RuntimeIntegerTypeReference valueRi) return value;
        //         
        //         // If same type, do nothing
        //         if (typetoRi.BitSize == valueRi.BitSize && typetoRi.Signed == valueRi.Signed) return value;
        //         
        //         // If pointer sized, delegate check for backend
        //         else if (valueRi.PtrSized || typetoRi.PtrSized)
        //             return new IRIntCast(value.Origin, value, typetoRi);
        //
        //         var val = valueRi;
        //         var tar = typetoRi;
        //         var o = value.Origin;
        //
        //         if (val.Signed == tar.Signed && val.BitSize == tar.BitSize) return value;
        //         return new IRIntCast(o, value, tar);
        //     }
        //
        //     case StringTypeReference typetoSr:
        //     {
        //         if (value is IRStringLiteral @sr)
        //             return new IRStringLiteral(sr.Origin, typetoSr.Encoding, sr.Data);
        //
        //         else throw new Exception("TODO see how to handle this shit");
        //     }
        //     
        //     case SliceTypeReference @s when value is IrCollectionLiteral @clit:
        //     {
        //         var elmtype = s.InternalType;
        //         var items = clit.Items.Select(e => SolveTypeCast(elmtype, e)).ToArray();
        //         return new IrCollectionLiteral(clit.Origin, elmtype, items);
        //     }
        // }
        return origin ?? throw new UnreachableException();
    }
    
    private Suitability CalculateTypeSuitability(TypeReference typeTo, TypeReference typeFrom, bool allowImplicit)
    {
        switch (typeTo)
        { 
            case AnytypeTypeReference: return Suitability.NeedsSoftCast;
            
            case RuntimeIntegerTypeReference intParam:
                switch (typeFrom)
                {
                    case ComptimeIntegerTypeReference: return Suitability.Perfect;
                    case RuntimeIntegerTypeReference intArg:
                    {
                        if (intParam.BitSize == intArg.BitSize
                            && intParam.Signed == intArg.Signed) return Suitability.Perfect;

                        var val = intArg;
                        var tar = intParam;
                
                        if (val.Signed == tar.Signed)
                        {
                            if (val.BitSize == tar.BitSize) return Suitability.Perfect;
                            if (val.BitSize < tar.BitSize) return Suitability.NeedsSoftCast;
                            if (val.BitSize > tar.BitSize && @allowImplicit) return Suitability.NeedsSoftCast;
                            return 0;
                        }
                        if (!val.Signed && tar.Signed)
                        {

                            if (val.BitSize == tar.BitSize && @allowImplicit) return Suitability.NeedsHardCast;
                            if (val.BitSize < tar.BitSize) return Suitability.NeedsHardCast;
                            if (val.BitSize > tar.BitSize && @allowImplicit) return Suitability.NeedsHardCast;
                            return 0;
                        }
                        return allowImplicit
                            ? Suitability.NeedsHardCast
                            : Suitability.None;
                    }
                    default: return Suitability.None;
                }

            case StringTypeReference stringParam:
                if (typeFrom is StringTypeReference @strArg
                    && (strArg.Encoding == StringEncoding.Undefined
                        || strArg.Encoding == stringParam.Encoding)) return Suitability.Perfect;
                return Suitability.None;

            case BooleanTypeReference:
                return typeFrom is BooleanTypeReference ? Suitability.Perfect : Suitability.None;

            case ReferenceTypeReference @refe:
                return typeFrom is ReferenceTypeReference @refArg 
                       && CalculateTypeSuitability(refe.InternalType, refArg.InternalType, false) == Suitability.Perfect
                    ? Suitability.Perfect
                    : Suitability.None;
            
            case SolvedStructTypeReference @solvedstruct:
                if (typeFrom is SolvedStructTypeReference @solvedstructarg)
                    return (Suitability)solvedstruct.CalculateSuitability(solvedstructarg);
                return Suitability.None;
            
            case SolvedTypedefTypeReference @solvedTypedef:
                if (typeFrom is ComptimeIntegerTypeReference) return Suitability.Perfect;
                if (typeFrom is not SolvedTypedefTypeReference @solvedTypedefarg) return Suitability.None;
                return solvedTypedef.Typedef == solvedTypedefarg.Typedef ? Suitability.Perfect : Suitability.None;
        }
        throw new UnreachableException();
    }

    private enum Suitability
    {
        None = 0,
        NeedsHardCast = 1,
        NeedsSoftCast = 2,
        Perfect = 3
    }

}