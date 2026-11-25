using System.Collections.Immutable;
using Mono.Cecil;
using Tqc.Dotnet.ProgramMembers;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;

namespace Tqc.Dotnet.ModuleBuilder;

sealed internal class DotnetModuleBuilder(AssemblyDefinition[] assemblies)
{
    /// Identifica uma definição (não uma referência): o mesmo tipo alcançado por caminhos diferentes cai no mesmo nó.
    private readonly record struct TypeKey(string Assembly, string FullName)
    {
        public static TypeKey Of(TypeDefinition type) => new(type.Module.Assembly.FullName, type.FullName);
    }

    private readonly Dictionary<TypeKey, TypeNode> _nodes = new();
    private TqTypeTranslator? _translator;
    private MemberReferenceFactory? _members;

    private TqTypeTranslator Translator => _translator ??= new TqTypeTranslator(LookupNode);
    private MemberReferenceFactory Members => _members ??= new MemberReferenceFactory(new SignatureRemapper(MapNamedType), Translator);

    public DotnetModule Build(string moduleName)
    {
        // A ordem importa: os membros só podem ser construídos depois que TODOS os tipos do módulo
        // estão registrados, senão as assinaturas cairiam em nós externos com o escopo do assembly que define.
        var root = DiscoverTypes(moduleName);
        LinkBaseTypes();
        BuildMembers();
        RegisterExtensions();
        AttachContents();

        //TypeTreeDumper.Dump(root);

        return new DotnetModule
        {
            Assemblies = assemblies,
            RootNamespace = BuildNamespace(root),
        };
    }

    // ───────────────────────── 4. Saída ─────────────────────────

    /// <summary>
    /// Passa base type, membros e aninhados dos nós para os Dotnet*. Só depois de todos os tipos existirem,
    /// para que os membros possam apontar para qualquer tipo, inclusive o próprio.
    /// </summary>
    private void AttachContents()
    {
        foreach (var node in _nodes.Values.Where(n => !n.IsExternal))
        {
            switch (node.DotnetType)
            {
                case DotnetStaticClass staticClass:
                    // As estáticas aninhadas ficam só em NestedStaticClasses; GetAllMembers junta as duas coleções.
                    staticClass.Members = BuildMemberTable(node, includeNestedStaticClasses: false);
                    staticClass.NestedStaticClasses = BuildNestedStaticClasses(node);
                    break;

                case DotnetClass dotnetClass:
                    dotnetClass.BaseType = node.BaseType;
                    dotnetClass.Implements = node.Implements;
                    dotnetClass.Members = BuildMemberTable(node, includeNestedStaticClasses: true);
                    break;

                case DotnetStruct dotnetStruct:
                    dotnetStruct.ObjectType = node.BaseType;
                    dotnetStruct.Implements = node.Implements;
                    dotnetStruct.Members = BuildMemberTable(node, includeNestedStaticClasses: true);
                    break;

                // The values of an enum are static constant fields.
                case DotnetEnum dotnetEnum:
                    dotnetEnum.ObjectType = node.BaseType;
                    dotnetEnum.Members = BuildMemberTable(node, includeNestedStaticClasses: true);
                    break;

                case DotnetInterface dotnetInterface:
                    dotnetInterface.ObjectType = node.BaseType;
                    dotnetInterface.ParentInterfaces = node.ParentInterfaces;
                    dotnetInterface.Members = BuildMemberTable(node, includeNestedStaticClasses: true);
                    break;
            }
        }
    }

    /// <summary>
    /// Tabela de membros por nome: campos e propriedades, um DotnetMethodGroup por nome de método
    /// (sobrecargas, e os construtores em ".ctor") e os tipos aninhados.
    /// Em colisão de nomes (só acontece em IL que o C# não gera) vale o primeiro.
    /// </summary>
    private static ImmutableDictionary<string, IMember> BuildMemberTable(TypeNode node, bool includeNestedStaticClasses)
    {
        var table = ImmutableDictionary.CreateBuilder<string, IMember>();
        var methods = new Dictionary<string, List<DotnetMethod>>();

        foreach (var member in node.Members)
        {
            switch (member)
            {
                case DotnetField field:
                    AddFirst(table, field.Name, field);
                    break;

                // An indexer is not reached by its name (`a[i]` uses it): it is listed under a key no identifier
                // can write, with its parameters because a type may have several.
                case DotnetProperty { IsIndexer: true } indexer:
                    AddFirst(table, $"{indexer.Name}[{string.Join(", ", indexer.IndexParameterTypes)}]", indexer);
                    break;

                case DotnetProperty property:
                    AddFirst(table, property.Name, property);
                    break;

                case DotnetEvent @event:
                    AddFirst(table, @event.Name, @event);
                    break;

                case DotnetMethod method:
                    if (!methods.TryGetValue(method.Name, out var overloads))
                    {
                        overloads = [];
                        methods.Add(method.Name, overloads);
                    }
                    overloads.Add(method);
                    break;
            }
        }

        // An extension is also a method of the type it extends: of this name, next to the ones the type has.
        var extensions = node.Extensions.GroupBy(extension => extension.Name).ToDictionary(group => group.Key, group => group.ToImmutableArray());
        foreach (var name in extensions.Keys.Where(name => !methods.ContainsKey(name))) methods[name] = [];

        foreach (var (name, overloads) in methods)
        {
            AddFirst(table, name, new DotnetMethodGroup(name)
            {
                Methods = overloads.ToImmutableArray(),
                Extensions = extensions.GetValueOrDefault(name, []),
            });
        }

        foreach (var nested in node.Nested)
        {
            // Delegates genéricos ainda não têm DotnetType (null).
            if (nested.DotnetType is not { } nestedType) continue;
            if (nestedType is DotnetStaticClass && !includeNestedStaticClasses) continue;

            AddFirst(table, nestedType.Name, nestedType);
        }

        return table.ToImmutable();
    }

    private static ImmutableDictionary<string, DotnetStaticClass> BuildNestedStaticClasses(TypeNode node)
    {
        var table = ImmutableDictionary.CreateBuilder<string, DotnetStaticClass>();

        foreach (var nested in node.Nested)
        {
            if (nested.DotnetType is DotnetStaticClass staticClass) AddFirst(table, staticClass.Name, staticClass);
        }

        return table.ToImmutable();
    }

    private static void AddFirst<T>(ImmutableDictionary<string, T>.Builder table, string name, T value)
    {
        if (!table.ContainsKey(name)) table.Add(name, value);
    }

    private static DotnetNamespace BuildNamespace(NamespaceNode node)
    {
        var namespaces = ImmutableDictionary.CreateBuilder<string, INamespace>();
        var types = ImmutableDictionary.CreateBuilder<string, IDotnetType>();

        // `Span<T>` is `Span`. When a name has several arities (`Action`, `Action<T>`...) the one with fewer
        // parameters keeps the name, and the others become `Name`N`, with N the number of generic parameters.
        foreach (var type in node.Types.OrderBy(type => type.Definition.GenericParameters.Count))
        {
            if (type.DotnetType is null) continue;

            var name = type.DotnetType.Name;
            var arity = type.Definition.GenericParameters.Count;

            if (type.DotnetType is DotnetStaticClass staticClass)
            {
                if (!namespaces.ContainsKey(name)) namespaces.Add(name, staticClass);
                continue;
            }

            if (!types.ContainsKey(name)) types.Add(name, type.DotnetType);
            else types.TryAdd($"{name}`{arity}", type.DotnetType);
        }

        foreach (var child in node.Namespaces.Values)
        {
            if (!namespaces.ContainsKey(child.Name)) namespaces.Add(child.Name, BuildNamespace(child));
        }

        return new DotnetNamespace(node.Name)
        {
            Namespaces = [..namespaces],
            Types = [.. types],
        };
    }

    // ───────────────────────── 1. Descoberta ─────────────────────────

    private NamespaceNode DiscoverTypes(string moduleName)
    {
        var root = new NamespaceNode(moduleName);
        var scopes = assemblies.Select(TypeReferenceFactory.ScopeOf).ToArray();

        // Definições diretas primeiro: se um tipo é definido E exportado por assemblies do módulo,
        // a referência direta vence. Entre exports, vale o primeiro assembly da lista.
        for (var i = 0; i < assemblies.Length; i++)
        {
            foreach (var type in assemblies[i].MainModule.Types)
            {
                if (IsExposed(type)) Register(root, type, scopes[i]);
            }
        }

        // Tipos exportados: a referência é relativa ao assembly que exporta, não ao que define.
        for (var i = 0; i < assemblies.Length; i++)
        {
            foreach (var exported in assemblies[i].MainModule.ExportedTypes)
            {
                // Aninhados são alcançados pelo tipo que os declara.
                if (exported.DeclaringType != null) continue;

                var type = exported.Resolve();
                if (type != null && IsExposed(type)) Register(root, type, scopes[i]);
            }
        }

        return root;
    }

    private void Register(NamespaceNode root, TypeDefinition type, AssemblyNameReference scope)
    {
        if (_nodes.ContainsKey(TypeKey.Of(type))) return;

        var node = CreateNode(type, TypeReferenceFactory.Create(type, scope), isExternal: false);

        var ns = root;
        foreach (var segment in type.Namespace.Split('.', StringSplitOptions.RemoveEmptyEntries))
            ns = ns.GetOrAddNamespace(segment);

        ns.Types.Add(node);
    }

    private TypeNode CreateNode(TypeDefinition definition, TypeReference reference, bool isExternal)
    {
        var kind = TypeClassifier.Classify(definition);

        // A delegate is only useful with its `Invoke` (what a function has to look like to become it), so its members are always read.
        if (kind == TypeKind.Delegate) isExternal = false;

        var typeParameters = kind is TypeKind.Class or TypeKind.Struct or TypeKind.Interface or TypeKind.Delegate
            ? MemberReferenceFactory.TypeParametersOf(definition.GenericParameters)
            : Array.Empty<TqParameter>();
        var node = new TypeNode(TypeClassifier.GetTypeName(definition), kind, reference, definition, isExternal)
        {
            DotnetType = CreateDotnetType(kind, reference, typeParameters),
            TypeParameters = typeParameters,
        };
        _nodes.Add(TypeKey.Of(definition), node);

        // Tipos externos só entram como referência: não precisamos da estrutura deles.
        if (isExternal) return node;

        foreach (var nested in definition.NestedTypes)
        {
            if (!IsVisibleNested(nested)) continue;
            node.Nested.Add(CreateNode(nested, TypeReferenceFactory.CreateNested(nested, reference), isExternal: false));
        }

        return node;
    }

    private static bool IsExposed(TypeDefinition type) => type.Name != "<Module>" && type.IsPublic;

    private static bool IsVisibleNested(TypeDefinition type) =>
        type.IsNestedPublic || type.IsNestedFamily || type.IsNestedFamilyOrAssembly;

    // O Dotnet* guarda a Reference (como o tipo é endereçado), nunca a Definition.
    private static IDotnetType? CreateDotnetType(
        TypeKind kind, TypeReference reference, IReadOnlyList<TqParameter> typeParameters) => kind switch
    {
        TypeKind.StaticClass => new DotnetStaticClass(reference, TypeClassifier.GetTypeName(reference)),
        TypeKind.Class       => new DotnetClass(reference, TypeClassifier.GetTypeName(reference)) { Parameters = typeParameters },
        TypeKind.Struct      => new DotnetStruct(reference, TypeClassifier.GetTypeName(reference)) { Parameters = typeParameters },
        TypeKind.Enum        => new DotnetEnum(reference, TypeClassifier.GetTypeName(reference)),
        TypeKind.Interface   => typeParameters.Count > 0
            ? new DotnetGenericInterface(reference, TypeClassifier.GetTypeName(reference)) { Parameters = typeParameters }
            : new DotnetInterface(reference, TypeClassifier.GetTypeName(reference)),
        TypeKind.Delegate    => new DotnetDelegate(reference, TypeClassifier.GetTypeName(reference)) { Parameters = typeParameters },

        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    // ───────────────────────── 2. Tipos base ─────────────────────────

    /// <summary>
    /// The base of each type and the interfaces of an interface. What a type inherits is looked up in them, so a base that
    /// is not a type of the module (it was only found by reference) gets its members read too.
    /// </summary>
    private void LinkBaseTypes()
    {
        var pending = new Queue<TypeNode>(_nodes.Values.Where(n => !n.IsExternal && n.Kind != TypeKind.Delegate));
        var done = new HashSet<TypeNode>(ReferenceEqualityComparer.Instance);

        while (pending.TryDequeue(out var node))
        {
            if (!done.Add(node)) continue;

            // An interface has no base, but a value of it is an object: it has what `System.Object` has.
            var baseReference = node.Kind == TypeKind.Interface ? node.Definition.Module.TypeSystem.Object : node.Definition.BaseType;
            if (baseReference != null)
            {
                node.BaseType = ResolveBaseType(baseReference);
                Promote(baseReference, pending);
            }

            if (node.Kind is not (TypeKind.Interface or TypeKind.Class or TypeKind.Struct)) continue;

            // `IEnumerable<T>` as `IEnumerable(T)`, with the `T` of the type itself. An interface that cannot be written
            // (it has an argument that is not supported) is left out.
            ITypeRef? OfType(GenericParameter parameter) =>
                parameter.Type == GenericParameterType.Type && parameter.Position < node.TypeParameters.Count
                    ? new GenericParameterTypeRef(node.TypeParameters[parameter.Position])
                    : null;

            foreach (var parent in node.Definition.Interfaces)
            {
                Promote(parent.InterfaceType, pending);

                var translated = Translator.Translate(parent.InterfaceType, OfType);
                if (translated is UnsupportedTypeRef) continue;

                if (node.Kind == TypeKind.Interface) node.ParentInterfaces.Add(translated);
                else node.Implements.Add(translated);
            }
        }
    }

    /// <summary>A type that was only found by reference, and is now needed for what it has.</summary>
    private void Promote(TypeReference reference, Queue<TypeNode> pending)
    {
        var element = reference is GenericInstanceType generic ? generic.ElementType : reference;

        TypeDefinition? definition;
        try { definition = element.Resolve(); }
        catch (AssemblyResolutionException) { return; }

        if (definition == null || !_nodes.TryGetValue(TypeKey.Of(definition), out var node) || !node.IsExternal) return;

        node.IsExternal = false;
        pending.Enqueue(node);
    }

    private ITypeRef? ResolveBaseType(TypeReference baseType)
    {
        // Os argumentos genéricos do tipo base ainda não têm representação na IR: usa o tipo aberto.
        var element = baseType is GenericInstanceType generic ? generic.ElementType : baseType;

        // `ValueType` and `Enum` have nothing to call that `System.Object` has not: they are skipped.
        if (TypeClassifier.IsImplicitBase(element.FullName))
        {
            try { return element.Resolve()?.BaseType is { } above ? ResolveBaseType(above) : null; }
            catch (AssemblyResolutionException) { return null; }
        }

        return Translator.Translate(element);
    }

    // ───────────────────────── 3. Membros ─────────────────────────

    private void BuildMembers()
    {
        // Snapshot: remapear assinaturas pode criar nós em _nodes. Os externos ficam como estão, mas um delegate
        // sempre tem os membros lidos: o que um membro cria é lido na volta seguinte.
        var built = new HashSet<TypeNode>(ReferenceEqualityComparer.Instance);
        while (true)
        {
            var next = _nodes.Values.Where(n => !n.IsExternal && n.DotnetType != null && !built.Contains(n)).ToArray();
            if (next.Length == 0) break;

            foreach (var node in next)
            {
                built.Add(node);
                BuildMembers(node);
            }
        }
    }

    /// <summary>
    /// Each extension method of the module (`static Run(this IView view)`) is given to the type of its first parameter, so that
    /// `window.Run()` finds it as a method of `IView` that `IWindow` inherits, whether or not the namespace of the static class is
    /// imported. Only for a receiver that is a type of the module, without type parameters of its own; a method with type
    /// parameters of its own (LINQ) is left out.
    /// </summary>
    private void RegisterExtensions()
    {
        var byType = _nodes.Values
            .Where(node => !node.IsExternal && node.DotnetType != null)
            .GroupBy(node => (object)node.DotnetType!, ReferenceEqualityComparer.Instance)
            .ToDictionary(group => group.Key, group => group.First(), ReferenceEqualityComparer.Instance);

        foreach (var node in _nodes.Values.Where(node => !node.IsExternal && node.DotnetType is DotnetStaticClass))
        foreach (var method in node.Members.OfType<DotnetMethod>())
        {
            if (!method.IsExtension || method.TypeParameters.Count > 0) continue;

            var receiver = CompilerStd.Types.IsRef(method.ParameterTypes[0], out var referred) ? referred : method.ParameterTypes[0];
            object? target = receiver switch
            {
                ProtocolTypeReference { Protocol: var protocol } => protocol,
                StructTypeReference { Struct: GenericInstance } => null,
                StructTypeReference { Struct: var structure } => structure,
                _ => null,
            };

            if (target != null && byType.TryGetValue(target, out var targetNode)) targetNode.Extensions.Add(method);
        }
    }

    private void BuildMembers(TypeNode node)
    {
        var definition = node.Definition;
        var declaring = node.Reference; // relativa ao assembly que exporta

        // value__ de enums e afins não é um campo utilizável.
        foreach (var field in definition.Fields)
        {
            if (field.IsRuntimeSpecialName || !MemberVisibility.IsVisible(field)) continue;
            node.Members.Add(Members.Field(field, declaring, node.TypeParameters));
        }

        // Acessores pertencem à propriedade, não entram de novo como métodos.
        var accessors = new HashSet<MethodDefinition>();
        foreach (var property in definition.Properties)
        {
            if (property.GetMethod is { } get) accessors.Add(get);
            if (property.SetMethod is { } set) accessors.Add(set);

            if (MemberVisibility.IsVisible(property))
                node.Members.Add(Members.Property(property, declaring, node.TypeParameters));
        }

        // An event whose delegate is not supported (a generic one) is kept: using it says why it cannot be.
        foreach (var @event in definition.Events)
        {
            if (@event.AddMethod is not { } add || !MemberVisibility.IsVisible(add)) continue;

            node.Members.Add(Members.Event(@event, declaring, node.TypeParameters));
        }

        foreach (var method in definition.Methods)
        {
            if (!MemberVisibility.IsVisible(method) || accessors.Contains(method) || method.Name == ".cctor") continue;
            node.Members.Add(Members.Method(method, declaring, node.TypeParameters));
        }
    }

    /// <summary>
    /// Troca a referência de metadata de um tipo nomeado pela Reference do builder.
    /// Se o tipo não for resolvível, mantém a original em vez de derrubar o build.
    /// </summary>
    private TypeReference MapNamedType(TypeReference type) =>
        TryGetOrCreateNode(type, out var node) ? node.Reference : type;

    // ───────────────────────── Resolução de nós ─────────────────────────

    private TypeNode? LookupNode(TypeReference reference) => TryGetOrCreateNode(reference, out var node) ? node : null;

    /// <summary>
    /// Devolve o nó do tipo. Se ele não pertence ao módulo, cria um nó externo cuja Reference é
    /// a própria referência vista no ponto de uso (já relativa ao assembly correto).
    /// </summary>
    private bool TryGetOrCreateNode(TypeReference reference, out TypeNode node)
    {
        // List<int> -> List<>: os argumentos genéricos não mudam qual tipo é referenciado.
        var element = reference is GenericInstanceType generic ? generic.ElementType : reference;

        TypeDefinition? definition = null;
        try
        {
            definition = element.Resolve();
        }
        catch (AssemblyResolutionException)
        {
            // Fallback: Procura manualmente nos assemblies passados no construtor se o Cecil se perder
            foreach (var asm in assemblies)
            {
                definition = asm.MainModule.Types.FirstOrDefault(t => t.FullName == element.FullName);
                if (definition != null) break;
            }
        }

        if (definition == null)
        {
            node = null!;
            return false;
        }

        node = _nodes.TryGetValue(TypeKey.Of(definition), out var existing)
            ? existing
            : CreateNode(definition, element, isExternal: true);
        return true;
    }
}