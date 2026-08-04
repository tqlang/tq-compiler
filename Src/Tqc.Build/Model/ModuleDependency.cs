namespace Tqc.Build.Model;

public abstract record ModuleDependency;

public sealed record ModuleReference(string Name, ModuleDefinition Definition = null!) : ModuleDependency;
public sealed record DotnetAssemblyReference(string Name, Version Version) : ModuleDependency;
