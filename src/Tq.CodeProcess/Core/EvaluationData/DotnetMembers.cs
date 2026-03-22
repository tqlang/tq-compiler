using System.Diagnostics;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.CodeObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.Dotnet;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin.Integer;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures.Types;
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
                return new DotnetGenericTypeReference((DotnetTypeObject)GetOrCreateTypeObject(g, module), args);
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
                return corLib.FullName switch
                {
                    "System.Void" => new VoidTypeReference(),
                    "System.Object" => new AnytypeTypeReference(),
            
                    "System.IntPtr" => new RuntimeIntegerTypeReference(true),
                    "System.UIntPtr" => new RuntimeIntegerTypeReference(false),
                    "System.SByte" => new RuntimeIntegerTypeReference(true, 8),
                    "System.Byte" => new RuntimeIntegerTypeReference(false, 8),
                    "System.Int16" => new RuntimeIntegerTypeReference(true, 16),
                    "System.UInt16" => new RuntimeIntegerTypeReference(false, 16),
                    "System.Int32" => new RuntimeIntegerTypeReference(true, 32),
                    "System.UInt32" => new RuntimeIntegerTypeReference(false, 32),
                    "System.Int64" => new RuntimeIntegerTypeReference(true, 64),
                    "System.UInt64" => new RuntimeIntegerTypeReference(false, 64),
            
                    "System.Single" => new RuntimeIntegerTypeReference(true, 32),
                    "System.Double" => new RuntimeIntegerTypeReference(true, 64),
                    
                    "System.Boolean" => new BooleanTypeReference(),
                    "System.String" => new StringTypeReference(StringEncoding.Utf16),
                    "System.Char" => new CharTypeReference(),
                    "System.Type" => new TypeTypeReference(null),
            
                    _ => throw new NotImplementedException($"core lib type {corLib.FullName} not implemented")
                };
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
        var fullName = string.Join('.', (string)t.Namespace!, (string)t.Name!);
        
        if (module.Types.TryGetValue(fullName, out var value)) return value;
        var resolved = t.Resolve()!;

        ContainerObject type = resolved is { IsAbstract: true, IsSealed: true }
            ? new DotnetStaticClassObject(fullName, resolved)
            : new DotnetTypeObject(resolved);
        
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
                
        return new DotnetMethodObject(m.Name!, m, m,
            DotnetTypeToRef(m.Signature!.ReturnType, module), [..parameters]);
    }

    public static DotnetFieldObject GetOrCreateFieldObject(FieldDefinition f, DotnetModuleObject module)
    {
        return new DotnetFieldObject(f, DotnetTypeToRef(f.Signature!.FieldType, module));
    }
}
