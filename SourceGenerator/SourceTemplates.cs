﻿using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using SourceGenerator.Models;
using System.Linq;
using System.Text;

namespace SourceGenerator
{
    internal static class SourceTemplates
    {
        internal const string ServiceClient = "Client";
        internal const string EndpointDescription = "";
        internal const string OutDescription = "";

        internal static SourceText CreateCliSource(ContractModel model)
        {
            const string CliDescription = "Call gRPC services via CLI tool.";

            var source = new StringBuilder(@$"// <auto-generated/>
#pragma warning disable 1591

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using {model.Namespace};

namespace GrpCLI
{{
    internal class CliRoot : RootCommand
    {{
        internal MessageHandler Client {{ get; }}

        internal CliRoot(string endpoint) : base(""{CliDescription}"")
        {{   
            Client = new MessageHandler();
            Command serviceCommand;
            Command methodCommand;
");
            foreach (var service in model.Services)
            {
                source.Append($@"
            serviceCommand = new Command(@""{service.Name}"", @""{service.Description}"");
");

                foreach (var method in service.Methods)
                {
                    source.Append($@"
            methodCommand = new Command(@""{method.Name}"", @""{method.Description}"")
            {{
                Handler = CommandHandler.Create(Client.Handle{method.Name}Async)
            }};");
                    if (!method.Request.IsEmpty)
                    {
                        source.Append($@"
            methodCommand.AddOption(new Option<string>(new[] {{""--request"", ""-r""}}, ""{method.Request.Description}"") {{ IsRequired = true }});");
                    }
                    source.Append($@"
            serviceCommand.Add(methodCommand);
");
                }

                source.Append($@"
            Add(serviceCommand);
            Add(new Option<string>(new[] {{""--endpoint"", ""-e""}}, ""{EndpointDescription}""));
            Add(new Option<string>(new[] {{""--outpath"", ""-o""}}, ""{OutDescription}""));");
            }

            source.Append(@$"
        }}
    }}
}}
");
            return ParseSourceFile(source.ToString());
        }

        internal static SourceText CreateHandlersSource(ContractModel model)
        {
            var source = new StringBuilder(@$"// <auto-generated/>
#pragma warning disable 1591

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using {model.Namespace};

namespace GrpCLI
{{
    internal class MessageHandler
    {{");
            foreach (var service in model.Services)
            {
                foreach (var method in service.Methods)
                {
                    source.Append(@$"

        internal async Task Handle{method.Name}Async(string request = null, string endpoint = null, string outPath = null, CancellationToken cancellationToken = default)
        {{
            var output = string.IsNullOrEmpty(outPath)
                ? Console.OpenStandardOutput()
                : File.OpenWrite(outPath);
            try
            {{
                var channel = GrpcChannel.ForAddress(endpoint, new GrpcChannelOptions {{ HttpHandler = AuthenticationHelper.CreateAuthenticatedHandler() }});
                var client = new {service.Name}.{service.Name}{ServiceClient}(channel);

                var message = MessageSerializer.Deserialize{method.Request.Type}(request);
                using var call = client.{method.Name}(message, cancellationToken: cancellationToken);
                await foreach (var response in call.ResponseStream.ReadAllAsync())
                {{
                    MessageSerializer.Serialize{method.Response.Type}(response, output);
                }}
                
            }}
            catch (InvalidJsonException e)
            {{
                var bytes = Encoding.UTF8.GetBytes(e.Message);
                await output.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
            }}
            catch (RpcException e)
            {{
                var bytes = Encoding.UTF8.GetBytes(e.Status.Detail);
                await output.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
            }}
            await output.FlushAsync();
            output.Dispose();
        }}");
                }
            }
            source.Append(@"
    }
}
");
            return ParseSourceFile(source.ToString());
        }

        internal static SourceText CreateSerializersSource(ContractModel model)
        {
            var source = new StringBuilder(@$"// <auto-generated/>
#pragma warning disable 1591

using System;
using System.IO;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using {model.Namespace};

namespace GrpCLI
{{
    internal static class MessageSerializer
    {{");
            foreach (var request in model.Services.SelectMany(s => s.Methods).Select(m => m.Request).Distinct())
            {
                source.Append(@$"
        internal static {request.Type} Deserialize{request.Type}(string serialized)
        {{");
                if (!request.IsEmpty)
                {
                    source.Append(@$"
            return {request.Type}.Parser.ParseJson(serialized);");
                }
                else
                {
                    source.Append(@"
            return new Empty();");
                }

                source.Append(@$"
        }}
");
            }
            foreach (var response in model.Services.SelectMany(s => s.Methods).Select(m => m.Response).Distinct())
            {
                source.Append(@$"
        internal static void Serialize{response.Type}({response.Type} message, Stream output)
        {{
            message.WriteTo(output);
        }}
");
            }
            source.Append(@"
    }
}
");
            return ParseSourceFile(source.ToString());
        }

        internal static SourceText CreateAuthenticationSource()
        {
            var source = new StringBuilder(@$"// <auto-generated/>
using System.Net.Http;

namespace GrpCLI
{{
    internal static class AuthenticationHelper
    {{
        internal static HttpClientHandler CreateAuthenticatedHandler()
        {{
            var handler = new HttpClientHandler
            {{
                ServerCertificateCustomValidationCallback = (message, cert, chain, policy) => true
            }};
            return handler;
        }}
    }}
}}
");
            return ParseSourceFile(source.ToString());
        }

        internal static SourceText ParseSourceFile(string source)
        {
            return SyntaxFactory.ParseCompilationUnit(source)
                                      //.NormalizeWhitespace()
                                      .GetText(Encoding.UTF8);
        }
    }
}
