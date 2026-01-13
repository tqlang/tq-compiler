using System.Diagnostics;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageObjects.Attributes;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageObjects.CodeObjects;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.AttributeReferences;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences.Builtin;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Expression;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Misc;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Value;

namespace Abstract.CodeProcess;


/*
 * Stage Two:
 *  Scans all the headers, unwraps the build-in
 *  attributes and evaluate header-level references.
 *  This step should be done early as it may dump
 *  more shit into `_globalReferenceTable`.
 */

public partial class Analyzer
{
    private void ScanHeadersMetadata()
    {
        foreach (var reference in _globalReferenceTable.ToArray())
        {
            var langObj = reference.Value;

            if (langObj is FunctionGroupObject @funcg)
                foreach (var o in funcg.Overloads) ProcessHeader(o);
            else ProcessHeader(langObj);
        }
    }
    private void ProcessHeader(LangObject reference)
    {
        switch (reference)
        {
            case FunctionObject @a: UnwrapFunctionMeta(a); break;
            case StructObject @a: UnwrapStructureMeta(a); break;
            case FieldObject @a: UnwrapFieldMeta(a); break;
            case ConstructorObject @a: UnwrapCtorMeta(a); break;
            case DestructorObject @a: UnwrapDtorMeta(a); break;
        }
        
        // Handling quick inheritance
        if (reference is IStaticModifier @refStatic and not StructObject)
        {
            refStatic.Static = reference.Parent switch
            {
                ModuleObject or NamespaceObject => true,
                IStaticModifier @parentStatic => parentStatic.Static,
                _ => refStatic.Static
            };
        }
        
        // Handling builtin attributes
        foreach (var attr in reference.Attributes)
        {
            if (attr is not BuiltInAttributeReference @builtInAttribute) continue;

            switch (builtInAttribute.Attribute)
            {
                case BuiltinAttributes.Static: if (reference is IStaticModifier @s) s.Static = true; break;
                case BuiltinAttributes.Public: if (reference is IPublicModifier @p) p.Public = true; break;
                case BuiltinAttributes.Private: if (reference is IPublicModifier @p2) p2.Public = false; break;
                case BuiltinAttributes.Internal: if (reference is IInternalModifier @i) i.Internal = true; break;
                case BuiltinAttributes.Final: if (reference is StructObject @f) f.Final = true; break;
                case BuiltinAttributes.Abstract: if (reference is IAbstractModifier @a) a.Abstract = true; break;
                case BuiltinAttributes.Interface: if (reference is StructObject @i2) i2.Interface = true; break;
                case BuiltinAttributes.Virtual: if (reference is IVirtualModifier @v) v.Virtual = true; break;
                case BuiltinAttributes.Override: if (reference is IOverrideAttribute @o) o.Override = true; break;
                case BuiltinAttributes.ConstExp: if (reference is FunctionObject @c) c.ConstExp = true; break;
                
                case BuiltinAttributes.Extern:
                {
                    var node = builtInAttribute.syntaxNode;
                    
                    if (reference is not IExternModifier @externModifier)
                        throw new Exception($"Attribute {attr} is not suitable to {reference.GetType().Name}");
                    
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
                            
                            externModifier.Extern = (strlit1.RawContent, strlit2.RawContent);
                            break;
                        }
                        default: throw new Exception($"'Extern' expected 2 arguments, found {args.Length}");
                    }
                } break;

                case BuiltinAttributes.Export:
                {
                    var node = builtInAttribute.syntaxNode;
                    
                    if (reference is not IExportModifier @exportModifier)
                        throw new Exception($"Attribute {attr} is not suitable to {reference.GetType().Name}");
                    
                    if (node.Children.Length != 3) throw new Exception("'Export' expected arguments");
                    var args = (node.Children[2] as ArgumentCollectionNode)!.Arguments;
                    
                    if (args.Length != 1) throw new Exception($"'Export' expected 1 arguments, found {args.Length}");
                    if (args[0] is not StringLiteralNode @strlit1)
                        throw new Exception("'Export' expected argument 0 as ComptimeString");

                    exportModifier.Export = strlit1.RawContent;
                } break;

                case BuiltinAttributes.DotnetImport:
                {
                    var node = builtInAttribute.syntaxNode;
                    var args = node.Children.Length != 3 ? [] : (node.Children[2] as ArgumentCollectionNode)!.Arguments;

                    switch (reference)
                    {
                        case IDotnetImportMethodModifier @dotnetImportMethod:
                            if (args == null) throw new Exception($"'dotnetImport' expected 3 arguments, found 0");
                            switch (args.Length)
                            {
                                case 0:
                                {
                                    dotnetImportMethod.DotnetImport = new DotnetImportMethodData
                                    {
                                        AssemblyName = null,
                                        ClassName = null,
                                        MethodName = reference.Name
                                    };
                                } break;
                                case 1:
                                {
                                    if (args[0] is not StringLiteralNode @strlit1)
                                        throw new Exception("'dotnetImport' expected argument 0 as ComptimeString");
                                    
                                    dotnetImportMethod.DotnetImport = new DotnetImportMethodData
                                    {
                                        AssemblyName = null,
                                        ClassName = null,
                                        MethodName = strlit1.RawContent
                                    };
                                } break;
                                case 3:
                                {
                                    if (args[0] is not StringLiteralNode @strlit1)
                                        throw new Exception("'dotnetImport' expected argument 0 as ComptimeString");
                                    if (args[1] is not StringLiteralNode @strlit2)
                                        throw new Exception("'dotnetImport' expected argument 1 as ComptimeString");
                                    if (args[2] is not StringLiteralNode @strlit3)
                                        throw new Exception("'dotnetImport' expected argument 2 as ComptimeString");
                            
                                    dotnetImportMethod.DotnetImport = new DotnetImportMethodData
                                    {
                                        AssemblyName = strlit1.RawContent,
                                        ClassName = strlit2.RawContent,
                                        MethodName = strlit3.RawContent
                                    };
                                } break;
                                default: throw new Exception($"'dotnetImport' expected 3 arguments, found {args.Length}");
                            } break;
                        
                        case IDotnetImportTypeModifier @dotnetImportType:
                            if (args == null) throw new Exception($"'dotnetImport' expected 3 arguments, found 0");
                            switch (args.Length)
                            {
                                case 2:
                                {
                                    if (args[0] is not StringLiteralNode @strlit1)
                                        throw new Exception("'dotnetImport' expected argument 0 as ComptimeString");
                                    if (args[1] is not StringLiteralNode @strlit2)
                                        throw new Exception("'dotnetImport' expected argument 1 as ComptimeString");
                                
                                    dotnetImportType.DotnetImport = new DotnetImportTypeData
                                    {
                                        AssemblyName = strlit1.RawContent,
                                        ClassName = strlit2.RawContent,
                                    };
                                    break;
                                }
                                default: throw new Exception($"'dotnetImport' expected 3 arguments, found {args.Length}");
                            } break;
                        
                        default: throw new Exception($"Attribute {attr.syntaxNode} is not suitable to {reference.GetType().Name}");
                    }
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
        var node = function.syntaxNode;
        var paramc = node.ParameterCollection;
        var returnType = node.ReturnType;

        foreach (var i in paramc.Items)
        {
            var typeref = SolveShallowType(i.Type);
            var name = i.Identifier.Value;

            if (typeref is
                TypeTypeReference or
                AnytypeTypeReference) function.Generic = true;
            
            function.AddParameter(new ParameterObject(typeref, name));
        }

        function.ReturnType = returnType == null ? new VoidTypeReference() : SolveShallowType(returnType);
        
    }
    private void UnwrapStructureMeta(StructObject structure)
    {
        var node = structure.SyntaxNode;

        if (node.Children.Length == 4)
        {
            var extendsImplements = new Queue<SyntaxNode>(((ExtendsImplementsNode)node.Children[2]).Children);

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

            structure.Extends = extendsVal == null ? null : new UnsolvedTypeReference(extendsVal);
        }
        
    }
    private void UnwrapFieldMeta(FieldObject field)
    {
        var node = field.SyntaxNode;
        field.Type = SolveShallowType(node.Type);
    }
    private void UnwrapCtorMeta(ConstructorObject ctor)
    {
        
    }
    private void UnwrapDtorMeta(DestructorObject dtor)
    {
        
    }
}
