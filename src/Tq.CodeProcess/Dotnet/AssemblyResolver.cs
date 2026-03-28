using AsmResolver.DotNet;
using AsmResolver.IO;

namespace Abstract.CodeProcess.Dotnet;

public class AssemblyResolver : DotNetCoreAssemblyResolver
{
    private static readonly Version ZeroVersion = new(0, 0, 0, 0);
    private static readonly Version NullVersion = new();
    private readonly List<string> _resolvingDirectories = [];

    public readonly AssemblyReference CorLibReference;
    
    public AssemblyResolver(Version runtimeVersion)
        : base(UncachedFileService.Instance, runtimeVersion)
    {
        var paths = new DotNetCorePathProvider().GetRuntimePathCandidates(runtimeVersion);
        _resolvingDirectories.AddRange(paths);

        CorLibReference = new AssemblyReference("System.Runtime", runtimeVersion)
            { PublicKeyOrToken = [0xb0, 0x3f, 0x5f, 0x7f, 0x11, 0xd5, 0x0a, 0x3a] };
        Resolve(CorLibReference);
    }
    
    protected override string? ProbeRuntimeDirectories(AssemblyDescriptor assembly)
    {
        throw new NotImplementedException();
    }
    protected override AssemblyDefinition? ResolveImpl(AssemblyDescriptor assembly)
    {
        var asmName = assembly.Name;
        var asmVersion = assembly.Version;

        var path = _resolvingDirectories
            .Select(dir => Path.Combine(dir, $"{asmName}.dll"))
            .Where(File.Exists)
            .FirstOrDefault(file => asmVersion == null!
                                    || asmVersion == ZeroVersion
                                    || asmVersion == NullVersion
                                    || AssemblyDefinition.FromFile(file).Version == asmVersion);
        
        return path == null ? null : LoadAssemblyFromFile(path);
    }
    
}
