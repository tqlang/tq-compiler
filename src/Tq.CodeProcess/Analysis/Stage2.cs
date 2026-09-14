using System.Diagnostics;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;
using Tq.CodeProcess.Core.EvaluationData.LanguageObjects;
using Tq.CodeProcess.Core.EvaluationData.LanguageObjects.CodeObjects;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.AttributeReferences;
using Tq.CodeProcess.Core.Language.SyntaxNodes;

namespace Tq.CodeProcess;


/*
 * Stage Two:
 *  Scans all the headers, unwraps the build-in
 *  attributes and evaluate header-level references.
 *  This step should run early, since later stages
 *  rely on encapsulation (Static/Public/...) already
 *  being resolved on every member of the tree.
 */

public partial class Analyser
{
    private void ScanHeadersMetadata()
    {
        // WalkMembers already surfaces function overloads, constructors
        // and destructors directly, and ProcessHeader already dispatches
        // to UnwrapStructureMeta/UnwrapFunctionMeta/etc per concrete
        // type internally — so no switch is needed here anymore (the
        // old code's explicit `UnwrapStructureMeta(struc)` call was
        // actually calling it a second time on top of what ProcessHeader
        // already did).
        foreach (var member in _modules.SelectMany(WalkMembers))
            ProcessHeader(member);
    }
    private void ProcessHeader(LangObject reference)
    {
        switch (reference)
        {
            case FunctionObject @a: UnwrapFunctionMeta(a); break;
            case StructObject @a: UnwrapStructureMeta(a); break;
            case TypedefObject @a: UnwrapTypedefMeta(a); break;
            case FieldObject @a: UnwrapFieldMeta(a); break;
            case ConstructorObject @a: UnwrapCtorMeta(a); break;
            case DestructorObject @a: UnwrapDtorMeta(a); break;
        }
        
        // Handling quick inheritance
        if (reference is not StructObject)
        {
            reference.SetFlag(BuiltinAttributes.Static, reference.Parent switch
            {
                BaseModuleObject or TqNamespaceObject => true,
                LangObject parentRef => parentRef.HasFlag(BuiltinAttributes.Static),
                _ => reference.HasFlag(BuiltinAttributes.Static)
            });
        }
        
        // Handling builtin attributes
        foreach (var attr in reference.Attributes)
        {
            if (attr is not BuiltInAttributeReference @builtInAttribute) continue;

            switch (builtInAttribute.Attribute)
            {
                case BuiltinAttributes.Static: reference.SetFlag(BuiltinAttributes.Static); break;
                case BuiltinAttributes.Public: reference.SetFlag(BuiltinAttributes.Public); break;
                case BuiltinAttributes.Private: reference.SetFlag(BuiltinAttributes.Public, false); break;
                case BuiltinAttributes.Internal: reference.SetFlag(BuiltinAttributes.Internal); break;
                case BuiltinAttributes.Final: reference.SetFlag(BuiltinAttributes.Final); break;
                case BuiltinAttributes.Abstract: reference.SetFlag(BuiltinAttributes.Abstract); break;
                case BuiltinAttributes.Interface: reference.SetFlag(BuiltinAttributes.Interface); break;
                case BuiltinAttributes.Virtual: reference.SetFlag(BuiltinAttributes.Virtual); break;
                case BuiltinAttributes.Override: reference.SetFlag(BuiltinAttributes.Override); break;
                case BuiltinAttributes.ConstExp: reference.SetFlag(BuiltinAttributes.ConstExp); break;
                
                case BuiltinAttributes.Extern:
                {
                    var node = builtInAttribute.syntaxNode;
                    
                    if (node.Children.Length != 3) throw new Exception("'Extern' expected arguments");
                    var args = (node.Children[2] as ArgumentCollectionNode)!.Arguments;
                    
                    switch (args.Length)
                    {
                        case 2:
                        {
                            if (args[0] is not StringLiteralNode @strlit1)
                                throw new Exception("'Extern' expected argument 0 as ComptimeString");
                            if (args[1] is not StringLiteralNode @strlit2)
                                throw new Exception("'Extern' expected argument 1 as ComptimeString");
                            
                            reference.SetValue(BuiltinAttributes.Extern, (strlit1.RawContent, strlit2.RawContent));
                            break;
                        }
                        default: throw new Exception($"'Extern' expected 2 arguments, found {args.Length}");
                    }
                } break;

                case BuiltinAttributes.Export:
                {
                    var node = builtInAttribute.syntaxNode;
                    
                    if (node.Children.Length != 3) throw new Exception("'Export' expected arguments");
                    var args = (node.Children[2] as ArgumentCollectionNode)!.Arguments;
                    
                    if (args.Length != 1) throw new Exception($"'Export' expected 1 arguments, found {args.Length}");
                    if (args[0] is not StringLiteralNode @strlit1)
                        throw new Exception("'Export' expected argument 0 as ComptimeString");

                    reference.SetValue(BuiltinAttributes.Export, strlit1.RawContent);
                } break;

                
                // TODO builtin attributes
                case BuiltinAttributes.Comptime:
                case BuiltinAttributes.Getter:
                    break;
                    
                case BuiltinAttributes.Align:
                case BuiltinAttributes.AllowAccessTo:
                case BuiltinAttributes.DenyAccessTo:
                case BuiltinAttributes.Inline:
                case BuiltinAttributes.Noinline:
                case BuiltinAttributes.Runtime:
                case BuiltinAttributes.CallConv:
                case BuiltinAttributes.Setter:
                case BuiltinAttributes.IndexerGetter:
                case BuiltinAttributes.IndexerSetter:
                case BuiltinAttributes.ExplicitConvert:
                case BuiltinAttributes.ImplicitConvert:
                case BuiltinAttributes.OverrideOperator:
                case BuiltinAttributes._undefined:
                default: throw new NotImplementedException();
            }
        }

    }

    private void UnwrapFunctionMeta(FunctionObject function)
    {
        var node = function.SyntaxNode;
        var paramc = node.ParameterCollection;
        var returnType = node.ReturnType;

        foreach (var i in paramc.Items)
        {
            var typeref = new UnknownReference(i.Type);
            var name = i.Identifier.Value;
            function.AddParameter(new ParameterObject(typeref, name));
        }

        function.ReturnType = returnType == null
            ? new VoidTypeReference()
            : new UnknownReference(returnType);
    }
    private void UnwrapStructureMeta(StructObject structure)
    {
        var node = structure.SyntaxNode;

        int genericParametersIndex = -1;
        int extendsImplementsIndex = -1;

        foreach (var (i, c) in structure.SyntaxNode.Children.Index())
        {
            switch (c)
            {
                case ParameterCollectionNode: genericParametersIndex = i; break;
                case ExtendsImplementsNode: extendsImplementsIndex = i; break;
            }
        }

        if (genericParametersIndex != -1)
        {
            
        }
        
        if (extendsImplementsIndex != -1)
        {
            var extendsImplements = new Queue<SyntaxNode>(((ExtendsImplementsNode)node.Children[extendsImplementsIndex]).Children);

            ExpressionNode? extendsVal = null;
            List<ExpressionNode> implementsVal = [];

            if (extendsImplements.Count > 0 && extendsImplements.Dequeue() is TokenNode { Value: "extends" })
            {
                var identifier = (ExpressionNode)extendsImplements.Dequeue();
                extendsVal = identifier;
            }

            if (extendsImplements.Count > 0 && extendsImplements.Dequeue() is TokenNode { Value: "implements" })
            {
                while (extendsImplements.Count > 0) throw new UnreachableException();
            }

            structure.Extends = extendsVal == null ? null : new UnknownReference(extendsVal);
        }
        
    }
    private void UnwrapTypedefMeta(TypedefObject typedef)
    {
        var node = typedef.syntaxNode;

        if (node.Children.Length == 4 && node.BackType != null)
        {
            if (node.BackType.Arguments.Length != 1)
                throw new Exception($"'{node}' backing type must be one argument");
            
            typedef.BackType = new UnknownReference(node.BackType.Arguments[0]);
        }
    }
    private void UnwrapFieldMeta(FieldObject field)
    {
        var node = field.SyntaxNode;
        field.Type = new UnknownReference(node.Type);
    }
    private void UnwrapCtorMeta(ConstructorObject ctor)
    {
        var node = ctor.SyntaxNode;
        var paramc = node.ParameterCollection;
        
        foreach (var i in paramc.Items)
        {
            var typeref = new UnknownReference(i.Type);
            var name = i.Identifier.Value;
            ctor.AddParameter(new ParameterObject(typeref, name));
        }
        
        if (node.Returns != null) ctor.ReturnTypeOverride = new UnknownReference(node.Returns);
    }
    private void UnwrapDtorMeta(DestructorObject dtor)
    {
        throw new NotImplementedException();
    }
}
