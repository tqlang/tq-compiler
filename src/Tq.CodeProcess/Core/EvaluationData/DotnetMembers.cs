using System.Diagnostics;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.CodeObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.Dotnet;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin.Integer;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using TypeReference = Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.TypeReference;

namespace Abstract.CodeProcess.Core.EvaluationData;

public static class DotnetMembers
{
    public static TypeReference DotnetTypeToRef(TypeSignature t, DotnetModuleObject module)
    {
        switch (t)
        {
            case ByReferenceTypeSignature byRef:
                return new ReferenceTypeReference(DotnetTypeToRef(byRef.BaseType, module));
            
            case PointerTypeSignature pt:
                return new ReferenceTypeReference(DotnetTypeToRef(pt.BaseType, module));
            
            case SzArrayTypeSignature sza:
                return new SliceTypeReference(DotnetTypeToRef(sza.BaseType, module));
            
            case GenericInstanceTypeSignature { GenericType.FullName: "System.Nullable`1" }:
                throw new NotImplementedException("nullable");
                //return new AnytypeTypeReference();

            case GenericInstanceTypeSignature generic when generic.GenericType.FullName.StartsWith("System.Func"):
                throw new NotImplementedException("lambda");
                //return new FunctionTypeReference(null, []);
            
            case GenericInstanceTypeSignature g:
            {
                var args = new TypeReference[g.TypeArguments.Count];
                for (var i = 0; i < g.TypeArguments.Count; i++) args[i] = DotnetTypeToRef(g.TypeArguments[i], module);
                return new DotnetGenericTypeReference((DotnetTypeObject)GetOrCreateTypeObject(g, module), g, args);
            }

            case GenericParameterSignature g:
            {
                return g.ParameterType switch
                {
                    GenericParameterType.Type => new DotnetGenericTypeParamReference(g.Index),
                    GenericParameterType.Method => new DotnetGenericMethodParamReference(g.Index),
                    _ => throw new UnreachableException()
                };
            }
            
            case CorLibTypeSignature corLib:
                return corLib.ElementType switch
                {
                    ElementType.None => new VoidTypeReference(),
                    ElementType.Void => new VoidTypeReference(),
                    ElementType.Object => new AnytypeTypeReference(),
                    
                    ElementType.I => new RuntimeIntegerTypeReference(true),
                    ElementType.U => new RuntimeIntegerTypeReference(false),
                    ElementType.I1 => new RuntimeIntegerTypeReference(true, 8),
                    ElementType.U1 => new RuntimeIntegerTypeReference(false, 8),
                    ElementType.I2 => new RuntimeIntegerTypeReference(true, 16),
                    ElementType.U2 => new RuntimeIntegerTypeReference(false, 16),
                    ElementType.I4 => new RuntimeIntegerTypeReference(true, 32),
                    ElementType.U4 => new RuntimeIntegerTypeReference(false, 32),
                    ElementType.I8 => new RuntimeIntegerTypeReference(true, 64),
                    ElementType.U8 => new RuntimeIntegerTypeReference(false, 64),
                    
                    ElementType.R4 => new RuntimeIntegerTypeReference(true, 32),
                    ElementType.R8 => new RuntimeIntegerTypeReference(true, 64),
                    
                    ElementType.Boolean => new BooleanTypeReference(),
                    ElementType.Char => new CharTypeReference(),
                    ElementType.String => new StringTypeReference(StringEncoding.Utf16),
                    ElementType.Type => new TypeTypeReference(null),
                    
                    ElementType.Ptr or
                    ElementType.ByRef or
                    ElementType.ValueType or
                    ElementType.Class or
                    ElementType.Var or
                    ElementType.Array or
                    ElementType.GenericInst or
                    ElementType.TypedByRef or
                    ElementType.FnPtr or
                    ElementType.SzArray or
                    ElementType.MVar or
                    ElementType.CModReqD or
                    ElementType.CModOpt or
                    ElementType.Internal or
                    ElementType.Modifier or
                    ElementType.Sentinel or
                    ElementType.Pinned or
                    ElementType.Boxed or
                    ElementType.Enum => throw new NotImplementedException(),
                    
                    _ => throw new NotImplementedException($"core lib type {corLib.FullName} not implemented")
                };
        }

        // Some last manual checking because apparently some types are able to not match the shit before
        switch (t.FullName)
        {
            case "System.Type": return new TypeTypeReference(null);
        }
        
        if (t is not TypeDefOrRefSignature) throw new Exception($"{t.FullName} is {t.GetType().FullName}");
        var to = (DotnetTypeObject)GetOrCreateTypeObject(t, module);
        return t.IsValueType ? new DotnetTypeReference(to) : new ReferenceTypeReference(new DotnetTypeReference(to));
            
    }

    public static DotnetNamespaceObject GetOrCreateNamespaceObject(string n, DotnetModuleObject module)
    {
        if (module.Namespaces.TryGetValue(n, out var value)) return (DotnetNamespaceObject)value;
        
        var namespaceObject = new DotnetNamespaceObject(n);
        module.Namespaces.Add(n, namespaceObject);
        return namespaceObject;
    }
    public static ContainerObject GetOrCreateParentObject(TypeDefinition n, DotnetModuleObject module)
    {
        var fullNamespace = string.Join('.', n.Namespace, n.Name);
        if (module.Namespaces.TryGetValue(fullNamespace, out var value)) return value;
        
        var staticTypeObject = new DotnetStaticClassObject(fullNamespace, n)
        {
            Parent = n.DeclaringType == null
                ? GetOrCreateNamespaceObject(n.Namespace!, module)!
                : GetOrCreateParentObject(n.DeclaringType, module)!
        };

        module.Namespaces.Add(fullNamespace, staticTypeObject);
        return staticTypeObject;
    }
    
    public static ContainerObject GetOrCreateTypeObject(TypeSignature tsig, DotnetModuleObject module)
        => GetOrCreateTypeObject(tsig.Resolve()!, module);
    public static ContainerObject GetOrCreateTypeObject(ITypeDefOrRef t, DotnetModuleObject module)
    {
        var fullName = NormalizeTypeName(string.Join('.', (string)t.Namespace!, (string)t.Name!));
        
        if (module.Types.TryGetValue(fullName, out var value)) return value;
        var resolved = t.Resolve()!;
        
        ContainerObject type = resolved is { IsAbstract: true, IsSealed: true }
            ? new DotnetStaticClassObject(fullName, resolved)
            : new DotnetTypeObject(fullName, resolved);
        
        type.Parent = resolved.DeclaringType == null
            ? GetOrCreateNamespaceObject(resolved.Namespace!, module)!
            : GetOrCreateParentObject(resolved.DeclaringType, module)!;

        switch (type)
        {
            case DotnetTypeObject @typeObject:
            {
                if (resolved.BaseType != null) typeObject.ParentType = (DotnetTypeObject)GetOrCreateTypeObject(resolved.BaseType, module);
                module.Types[fullName] = typeObject;
                
                var ctors = resolved.Methods.Where(e => e.Name == ".ctor");
                foreach (var ctor in ctors) typeObject.Constructors.Add(GetOrCreateFunctionObject(ctor, module));
            } break;
            case DotnetStaticClassObject @staticClassObject: module.Namespaces[fullName] = staticClassObject; break;
        } 
        return type;
    }
    
    public static DotnetMethodObject GetOrCreateFunctionObject(MethodDefinition m, DotnetModuleObject module)
    {
        var parameters = new List<ParameterObject>();

        foreach (var p in m.GenericParameters)
            parameters.Add(new ParameterObject(new TypeTypeReference(null), p.Name!));
                
        foreach (var p in m.Parameters)
            parameters.Add(new ParameterObject(DotnetTypeToRef(p.ParameterType, module), p.Name!));

        var method = new DotnetMethodObject(m.Name!, m, m, DotnetTypeToRef(m.Signature!.ReturnType, module), [..parameters]);
        return method;
    }

    public static DotnetFieldObject GetOrCreateFieldObject(FieldDefinition f, DotnetModuleObject module)
    {
        return new DotnetFieldObject(f, DotnetTypeToRef(f.Signature!.FieldType, module));
    }

    public static string NormalizeTypeName(string s)
    {
        var i = s.LastIndexOf('`');
        return i == -1 ? s : s[..i];
    }
}
