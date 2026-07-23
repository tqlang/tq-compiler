using Mono.Cecil;
using Tq.Core.Language.Members;
using Tq.Core.Misc;
using Tq.Core.Plugin;
using Tq.Plugin.Dotnet.ProgramMembers;

namespace Tq.Plugin.Dotnet;

public class DotnetModuleParser : ModuleParser
{
    public override string ModuleType => "dotnet";

    public override bool CanParse(string moduleType) => moduleType == ModuleType;
    public override BaseModule Parse(BuildModuleConfig moduleConfig)
    {
        if (moduleConfig.Type != ModuleType)
            throw new InvalidOperationException($"This plugin cannot parse modules of type '{moduleConfig.Type}'");

        var module = new DotnetModule(moduleConfig.Name);

        foreach (var descriptor in moduleConfig.References)
        {
            var reference = AssemblyLoader.LoadFromCustomFormat(descriptor);
            module.Assemblies.Add(reference);
            var mainModule = reference.MainModule;
            
            foreach (var typeDef in mainModule.Types)
            {
                if (typeDef.Name == "<Module>") continue;
                var ns = typeDef.Namespace ?? string.Empty;

                if (!module.TypeDefinitions.TryGetValue(ns, out var typeList))
                {
                    typeList          = [];
                    module.TypeDefinitions[ns] = typeList;
                }

                var refe = new TypeReference(typeDef.Namespace, typeDef.Name, typeDef.Module, typeDef.Scope);
                typeList.Add((refe, typeDef));
            }
            
            foreach (var exportedType in mainModule.ExportedTypes)
            {
                var ns = exportedType.Namespace ?? string.Empty;

                if (!module.TypeDefinitions.TryGetValue(ns, out var typeList))
                {
                    typeList          = [];
                    module.TypeDefinitions[ns] = typeList;
                }

                var def = exportedType.Resolve();
                var refe = new TypeReference(exportedType.Namespace, exportedType.Name, mainModule, exportedType.Scope);
                typeList.Add((refe, def));
            }
        }

        module.Assemblies.TrimExcess();
        foreach (var i in module.TypeDefinitions) i.Value.TrimExcess();
        module.TypeDefinitions.TrimExcess();
        
        return module;
    }
}
