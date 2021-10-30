using Google.Protobuf.Reflection;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace GrpCLI
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var cliRoot = new GrpCLI.CliRoot("https://localhost:5001");
            await cliRoot.InvokeAsync(args);

            //var descriptor = GetProtoDescriptor();
            //var service = descriptor.Services[0];
            //var method = service.Methods[2];
            //var input = method.InputType;
            //var field = input.Fields[1];
            ////HmProvider.HmProviderClient
            //var protoPath = Environment.GetEnvironmentVariable("PROTO_PATH");

            //Console.WriteLine(JsonSerializer.Serialize(service.FullName));
            //Console.WriteLine(JsonSerializer.Serialize(method.FullName));
            //Console.WriteLine(JsonSerializer.Serialize(input.FullName));
            //Console.WriteLine(JsonSerializer.Serialize(field.FullName));
            //Console.ReadKey();
        }

        //private static FileDescriptor GetProtoDescriptor()
        //{
        //    var descriptors = Assembly.GetExecutingAssembly()
        //        .ExportedTypes
        //        .Select(t =>
        //            t.GetProperty("Descriptor", BindingFlags.Public | BindingFlags.Static, null, typeof(FileDescriptor), Array.Empty<Type>(), null))
        //        .Where(x => x != null)
        //        .Select(x => (FileDescriptor)x.GetValue(null))
        //        .ToList();

        //    return descriptors.FirstOrDefault();
        //}
    }
}
