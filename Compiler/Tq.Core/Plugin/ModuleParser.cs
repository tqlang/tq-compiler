using Tq.Core.Language.Members;
using Tq.Core.Misc;

namespace Tq.Core.Plugin;

public abstract class ModuleParser
{
    public abstract string ModuleType { get;  } 
    public abstract bool CanParse(string moduleType);
    public abstract BaseModule Parse(BuildModuleConfig moduleConfig);
}
