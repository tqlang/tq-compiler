using Tq.CodeProcess.Core.EvaluationData;

namespace Tq.CodeProcess;

public partial class Compiler
{
   
    private string launchConfig = 
        """
        {
            "runtimeOptions": {
                "tfm": "net10.0",
                "framework": {
                    "name": "Microsoft.NETCore.App",
                    "version": "10.0.0"
                },
                "configProperties": {
                    "System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization": false
                }
            }
        }
        """;
    
    public void Compile(ProgramObject program)
    {
        throw new NotImplementedException();
    }
    
}
