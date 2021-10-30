using System.CommandLine;
using System.Threading.Tasks;

namespace GrpCLI
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var cliRoot = new GrpCLI.CliRoot("https://localhost:5001");
            await cliRoot.InvokeAsync(args);
        }
    }
}
