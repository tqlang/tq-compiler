namespace Tq.Core.Misc;

public class BuildModuleConfig
{
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public List<string> References { get; set; } = new();

    public Dictionary<string, object> ExtraFields { get; set; } = new();
}
