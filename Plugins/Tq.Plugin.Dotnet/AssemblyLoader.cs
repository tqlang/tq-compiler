using System;
using System.IO;
using Mono.Cecil;

namespace Tq.Plugin.Dotnet;

public static class AssemblyLoader
{
    public static AssemblyDefinition LoadFromCustomFormat(string descriptor, string? baseDirectory = null)
    {
        var (pathOrName, expectedVersion) = ParseDescriptor(descriptor);
        
        // 1. Cria o resolver configurado com todas as pastas de busca (incluindo Runtime do .NET)
        var resolver = CreateAssemblyResolver(baseDirectory);

        // 2. Tenta resolver o caminho físico, seja um arquivo ou um nome simples de Assembly
        string absolutePath = ResolveAssemblyLocation(pathOrName, baseDirectory, resolver);

        // 3. Se o caminho do arquivo mudou após a resolução, adiciona a pasta dele ao resolver também
        var assemblyFolder = Path.GetDirectoryName(absolutePath);
        if (!string.IsNullOrEmpty(assemblyFolder))
        {
            resolver.AddSearchDirectory(assemblyFolder);
        }

        var readerParameters = new ReaderParameters
        {
            AssemblyResolver = resolver,
            InMemory = true,
            ReadWrite = false,
            ReadSymbols = false,
        };
        
        var assembly = AssemblyDefinition.ReadAssembly(absolutePath, readerParameters);
        ValidateVersion(assembly, expectedVersion);

        return assembly;
    }

    private static (string pathOrName, string version) ParseDescriptor(string descriptor)
    {
        if (string.IsNullOrWhiteSpace(descriptor))
            throw new ArgumentNullException(nameof(descriptor));

        var parts = descriptor.Split(':');
        if (parts.Length != 3 || !parts[0].Equals("dotnet", StringComparison.OrdinalIgnoreCase))
        {
            throw new FormatException(
                $"The descriptor format is invalid '{descriptor}'. Expected: 'dotnet:{{asm_name_or_path}}:{{version}}'");
        }
        return (parts[1], parts[2]);
    }

    private static string ResolveAssemblyLocation(string pathOrName, string? baseDirectory, DefaultAssemblyResolver resolver)
    {
        // ESTRATÉGIA A: É um caminho de arquivo direto ou relativo explicito?
        bool isPathLike = pathOrName.Contains('/') || 
                         pathOrName.Contains('\\') || 
                         pathOrName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);

        if (isPathLike)
        {
            string fullPath = pathOrName;
            if (!Path.IsPathRooted(fullPath))
            {
                string root = baseDirectory ?? AppContext.BaseDirectory;
                fullPath = Path.Combine(root, fullPath);
            }

            if (!Path.HasExtension(fullPath)) fullPath += ".dll";
            fullPath = Path.GetFullPath(fullPath);

            if (File.Exists(fullPath)) return fullPath;
        }

        // ESTRATÉGIA B: Tenta resolver como nome simples em baseDirectory / AppContext
        string candidatePath = Path.Combine(baseDirectory ?? AppContext.BaseDirectory, 
            pathOrName.EndsWith(".dll") ? pathOrName : pathOrName + ".dll");

        if (File.Exists(candidatePath))
        {
            return Path.GetFullPath(candidatePath);
        }

        // ESTRATÉGIA C: É um nome de Assembly puro (ex: "System.Text.Json" ou "Newtonsoft.Json")
        // Usa o próprio Cecil Resolver para vasculhar as pastas registradas (Runtime, BaseDir, etc)
        try
        {
            var cleanName = pathOrName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) 
                ? Path.GetFileNameWithoutExtension(pathOrName) 
                : pathOrName;

            var assemblyName = new AssemblyNameReference(cleanName, new Version(0, 0, 0, 0));
            var resolvedDefinition = resolver.Resolve(assemblyName);
            
            if (resolvedDefinition?.MainModule?.FileName != null)
            {
                return resolvedDefinition.MainModule.FileName;
            }
        }
        catch
        {
            // Falha silenciosa no Resolve() para lançar a exceção formatada abaixo
        }

        throw new FileNotFoundException($"Could not resolve assembly from descriptor name or path: '{pathOrName}'");
    }

    private static DefaultAssemblyResolver CreateAssemblyResolver(string? baseDirectory)
    {
        var resolver = new DefaultAssemblyResolver();

        // Pasta customizada informada
        if (!string.IsNullOrEmpty(baseDirectory) && Directory.Exists(baseDirectory))
            resolver.AddSearchDirectory(baseDirectory);

        // Pasta de execução do App
        resolver.AddSearchDirectory(AppContext.BaseDirectory);

        // Pasta de Plugins (se existir)
        var pluginsFolder = Path.Combine(AppContext.BaseDirectory, "plugins");
        if (Directory.Exists(pluginsFolder))
            resolver.AddSearchDirectory(pluginsFolder);

        // Pasta do SDK/Runtime do .NET (System.Private.CoreLib, System.Runtime, etc.)
        var runtimeFolder = Path.GetDirectoryName(typeof(object).Assembly.Location);
        if (!string.IsNullOrEmpty(runtimeFolder) && Directory.Exists(runtimeFolder))
            resolver.AddSearchDirectory(runtimeFolder);

        return resolver;
    }

    private static void ValidateVersion(AssemblyDefinition assembly, string expectedVersion)
    {
        if (!Version.TryParse(expectedVersion, out var targetVersion)) return;
        if (assembly.Name.Version != targetVersion)
        {
            Console.WriteLine($"[Warning] Assembly version ({assembly.Name.Version}) differs from the expected ({targetVersion}).");
        }
    }
}
