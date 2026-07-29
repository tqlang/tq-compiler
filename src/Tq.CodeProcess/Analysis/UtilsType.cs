using System.Diagnostics;
using Tq.CodeProcess.Core.EvaluationData.IntermediateTree;
using Tq.CodeProcess.Core.EvaluationData.IntermediateTree.Expressions;
using Tq.CodeProcess.Core.EvaluationData.IntermediateTree.Values;
using Tq.CodeProcess.Core.EvaluationData.LanguageObjects;
using Tq.CodeProcess.Core.EvaluationData.LanguageObjects.CodeObjects;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.CodeReferences;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.FieldReferences;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.FunctionReferences;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypedefReferences;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin.Integer;

namespace Tq.CodeProcess;

public partial class Analyser
{

    private static ITypeReference GetEffectiveTypeReference(IrExpression expr, LangObject? parent = null)
    {
        ITypeReference? result;
        switch (expr)
        {
            case IrReference { IsSolved: true } solved:
                switch (solved.Reference)
                {
                    case TypeTypeReference ttr:
                        result = ttr.ReferencedType;
                        break;

                    case StructReference structt:
                        result = structt;
                        break;
                    
                    case SolvedTypedefNamedValueReference tdff:
                        result = tdff.Type;
                        break;

                    case SolvedFieldReference field:
                        result = (ITypeReference)field.Field.Type;
                        break;

                    case CallableReference func:
                        result = new FunctionTypeReference(
                            func.Callable.ReturnType,
                            func.Callable.Parameters.Select(e => e.Type).ToArray()
                        );
                        break;

                    case LocalReference local:
                        result = (ITypeReference)local.Local.Type!;
                        break;

                    case ParameterReference param:
                    {
                        // if (parent is ICallable { IsGeneric: true } @callable)
                        // {
                        //     var pidx = param.Parameter.Index;
                        //     if (callable.Parameters[pidx].Type is TypeTypeReference)
                        //     {
                        //         result = new GenericTypeReference(callable.Parameters[pidx]);
                        //         break;
                        //     }
                        // }
                        result = (ITypeReference)param.Parameter.Type;
                    } break;

                    default:
                        result = solved.Type;
                        break;
                }
                break;

            case IrAccess access:
                result = GetEffectiveTypeReference(access.B);
                break;

            default:
                result = expr.Type;
                break;
        }

        return result ?? throw new UnreachableException(
            "This function should not be called when this value is null"
        );
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
    private IrExpression SolveTypeCast(ITypeReference typeTo, IrExpression value, bool @explicit = false)
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
    private IrExpression SolveTypeCast(ITypeReference typeTo, IrExpression value, IrExpression origin, bool @explicit = false)
    {
        switch (value)
        {
            case IrIntegerLiteral @lit:
            {
                switch (lit.Type)
                {
                    case ComptimeIntegerTypeReference when typeTo is RuntimeIntegerTypeReference rint1:
                        return new IrIntegerLiteral(lit.Origin, lit.Value, rint1);
                    
                    case RuntimeIntegerTypeReference @typetoRi when typeTo is RuntimeIntegerTypeReference rint2:
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
                    
                    case ComptimeIntegerTypeReference when typeTo is ComptimeIntegerTypeReference:
                        throw new UnreachableException();
                    
                    default:
                        return origin;
                }
            }
            
            case IrCollectionLiteral clit when typeTo is SliceTypeReference @s:
            {
                var elmtype = (ITypeReference)s.ElementType;
                var items = clit.Items.Select(e => SolveTypeCast(elmtype, e)).ToArray();
                return new IrCollectionLiteral(clit.Origin, (Reference)elmtype, items);
            }
            
            // FIXME ignored for now
            case IrStringLiteral:
            case IrCharLiteral:
            case IRBooleanLiteral:
            case IrBinaryExp:
            case IrAccess:
            case IrReference:
            case IrConv:
            case IrIndex:
            case IrCall:
            case IrLenOf:
            case IrCompareExp:
            case IRUnaryExp:
            case IrLogicalExp:
            case IrNewObject:
                return origin ?? value;
            
            default: throw new UnreachableException();
        }
    }

    private static ITypeReference ConcretizeGeneric(ITypeReference generic,
        Dictionary<ParameterObject, ITypeReference?> genericTable)
    {
        switch (generic)
        {
            case SliceTypeReference slice:
                return new SliceTypeReference((Reference)ConcretizeGeneric((ITypeReference)slice.ElementType, genericTable));
                
            case ReferenceTypeReference reference:
                return new ReferenceTypeReference((Reference)ConcretizeGeneric(reference.Type, genericTable));
            
            case GenericTypeReference type: return genericTable[type.Parameter] ?? throw new NullReferenceException();
            default: throw new UnreachableException();
        }
    }
    
    private Suitability CalculateTypeSuitability(ITypeReference typeTo, ITypeReference typeFrom, bool allowImplicit)
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

            case CharTypeReference charParam:
                if (typeFrom is CharTypeReference) return Suitability.Perfect;
                return Suitability.None;
            
            case BooleanTypeReference:
                return typeFrom is BooleanTypeReference ? Suitability.Perfect : Suitability.None;

            case ReferenceTypeReference @refe:
                return typeFrom is ReferenceTypeReference @refArg 
                       && CalculateTypeSuitability((ITypeReference)refe.InternalType, (ITypeReference)refArg.InternalType, false) == Suitability.Perfect
                    ? Suitability.Perfect
                    : Suitability.None;
            
            case SliceTypeReference @refe:
                return typeFrom is SliceTypeReference @sliceArg && IsAssignableTo((ITypeReference)sliceArg.ElementType, (ITypeReference)refe.ElementType)
                    ? Suitability.Perfect
                    : Suitability.None;

            
            case StructReference @solvedstruct:
                if (typeFrom is StructReference @solvedstructarg)
                    return (Suitability)solvedstruct.CalculateSuitability(solvedstructarg);
                return Suitability.None;
            
            case TypedefReference @solvedTypedef:
                // FIXME numbers are a little more complex inside typedefs
                if (typeFrom is ComptimeIntegerTypeReference) return Suitability.Perfect;
                if (typeFrom is RuntimeIntegerTypeReference) return Suitability.NeedsSoftCast;
                
                if (typeFrom is not TypedefReference @solvedTypedefArg) return Suitability.None;
                
                return solvedTypedef.Typedef == solvedTypedefArg.Typedef ? Suitability.Perfect : Suitability.None;
            
            case TypeTypeReference:
                return typeFrom is TypeTypeReference ? Suitability.Perfect : Suitability.None;
            
            default: throw new UnreachableException();
        }
    }

    // FIXME polymorphism
    private static bool IsAssignableTo(ITypeReference typeFrom, ITypeReference typeTo)
    {
        switch (typeTo)
        {
            case AnytypeTypeReference: return true;
            
            case StructReference @toStruct:
            {
                if (typeFrom is StructReference @fromStruct && toStruct == fromStruct) return true;
            } break;

            
            case RuntimeIntegerTypeReference @toRuntime when typeFrom is RuntimeIntegerTypeReference @fromRuntime:
            {
                if (toRuntime.Signed != fromRuntime.Signed) return false;
                return toRuntime.BitSize >= fromRuntime.BitSize;
            }
            
            case BooleanTypeReference when typeFrom is BooleanTypeReference: return true;
        }
        
        return false;
    }

    private enum Suitability
    {
        None = 0,
        NeedsHardCast = 1,
        NeedsSoftCast = 2,
        Perfect = 3
    }

}