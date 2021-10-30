using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGenerator.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;

namespace SourceGenerator
{
    [Generator]
    public class CliGenerator : ISourceGenerator
    {

        public void Execute(GeneratorExecutionContext context)
        {
            if (!context.Compilation.ReferencedAssemblyNames.Any(a => a.Name.Equals("System.CommandLine", StringComparison.OrdinalIgnoreCase)))
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticRules.MissingDependencyRule, Location.None));
            }

            //if (!context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.ProtoName", out var protoName))
            //{
            //    context.ReportDiagnostic(Diagnostic.Create(MissingPropertyRule, Location.None));
            //}

            if (!context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.ProtoPath", out var protoPath))
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticRules.MissingPropertyRule, Location.None));
            }

            var protoName = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(
                                Path.GetFileNameWithoutExtension(protoPath));

            const string CliSourceName = "Cli";
            const string SerializersSourceName = "Serializer";
            const string HandlersSourceName = "Handler";

            var builderModel = CreateModel(protoName, context);
            if (builderModel == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticRules.MissingCodeRule, Location.None));
                return;
            }

            var cliSource = SourceTemplates.CreateCliSource(builderModel);
            var serializersSource = SourceTemplates.CreateSerializersSource(builderModel);
            var handlersSource = SourceTemplates.CreateHandlersSource(builderModel);
            var authenticationSource = SourceTemplates.CreateAuthenticationSource();

            context.AddSource($"{protoName}{CliSourceName}", cliSource);
            context.AddSource($"{protoName}{SerializersSourceName}", serializersSource);
            context.AddSource($"{protoName}{HandlersSourceName}", handlersSource);
            context.AddSource("AuthenticationHelper", authenticationSource);
        }

        public void Initialize(GeneratorInitializationContext context)
        {
//#if DEBUGGENERATOR
            //if (!Debugger.IsAttached)
            //{
            //    Debugger.Launch();
            //}
//#endif
            // No initialization required for this one
        }

        private ContractModel CreateModel(string contractName, GeneratorExecutionContext context)
        {
            var (serviceTree, dataTree) = GetServiceAndDataSyntaxTrees(contractName, context);
            if (serviceTree == null || dataTree == null)
            {
                // TODO report warning
                return null;
            }

            var serviceClasses = GetServiceClassDeclarations(serviceTree, context.CancellationToken);
            var dataClasses = GetDataClassDeclarations(dataTree, context.CancellationToken);

            if (serviceClasses == null || dataClasses == null)
            {
                // TODO report warning
                return null;
            }

            var serviceModel = context.Compilation.GetSemanticModel(serviceTree);
            var dataModel = context.Compilation.GetSemanticModel(dataTree);

            return new ContractModel
            {
                Name = contractName,
                Namespace = GetNamespaceName(serviceTree, context.CancellationToken),
                Services = CreateServiceModels(serviceClasses, dataClasses, serviceModel, dataModel, context.CancellationToken).ToArray()
            };
        }

        private IEnumerable<ServiceModel> CreateServiceModels(
            IEnumerable<(ClassDeclarationSyntax, IEnumerable<MethodDeclarationSyntax>)> serviceDeclarations,
            IEnumerable<ClassDeclarationSyntax> dataDeclarations,
            SemanticModel serviceSemanticModel,
            SemanticModel dataSemanticModel,
            CancellationToken cancellationToken)
        {
            foreach (var (clientDeclaration, methodDeclarations) in serviceDeclarations)
            {
                var serviceDescription = GetDocumentationFromDeclaration(clientDeclaration, serviceSemanticModel, cancellationToken);
                yield return new ServiceModel
                {
                    Name = clientDeclaration.Identifier.ValueText,
                    Description = serviceDescription,
                    Methods = CreateMethodModels(methodDeclarations, dataDeclarations, serviceSemanticModel, dataSemanticModel, cancellationToken).ToArray()
                };
            }
        }

        private IEnumerable<MethodModel> CreateMethodModels(
            IEnumerable<MethodDeclarationSyntax> methodDeclarations,
            IEnumerable<ClassDeclarationSyntax> dataDeclarations,
            SemanticModel serviceSemanticModel,
            SemanticModel dataSemanticModel,
            CancellationToken cancellationToken)
        {
            foreach (var methodDeclaration in methodDeclarations)
            {
                var methodDescription = GetDocumentationFromDeclaration(methodDeclaration, serviceSemanticModel, cancellationToken);
                var methodRequest = methodDeclaration.ParameterList.Parameters.First().Type;
                var methodResponse = methodDeclaration.ReturnType;

                var result = new MethodModel
                {
                    Name = methodDeclaration.Identifier.ValueText,
                    Description = methodDescription,
                    Request = SyntaxHelper.GetTypeInfo(methodRequest),
                    Response = SyntaxHelper.GetTypeInfo(methodResponse)
                };

                var requestDeclaration = dataDeclarations.FirstOrDefault(c => c.Identifier.ValueText.Equals(result.Request.Type));
                if (requestDeclaration is not null)
                {
                    var requestDescription = GetDocumentationFromDeclaration(requestDeclaration, dataSemanticModel, cancellationToken);
                    result.Request.Description = requestDescription;
                }

                yield return result;
            }
        }

        private string GetDocumentationFromDeclaration(MemberDeclarationSyntax declarationSyntax, SemanticModel model, CancellationToken cancellationToken)
        {
            const string Summary = "summary";
            var symbol = model.GetDeclaredSymbol(declarationSyntax, cancellationToken);

            try
            {
                var documentationXml = symbol?.GetDocumentationCommentXml(cancellationToken: cancellationToken);
                var xmlElement = XElement.Parse(documentationXml);
                var summary = xmlElement.Element(Summary).Value.Trim();

                return summary;
            }
            catch
            {
                var triviaList = declarationSyntax.GetLeadingTrivia();
                foreach (var trivia in triviaList)
                {
                    if (trivia.GetStructure() is DocumentationCommentTriviaSyntax structure)
                    {
                        var summary = GetXmlElements(structure.Content, Summary).FirstOrDefault();
                        var content = summary?.Content.OfType<XmlTextSyntax>().FirstOrDefault();
                        var values = content?.TextTokens.Select(t => t.ValueText.Trim()).Where(s => !string.IsNullOrEmpty(s));
                        return string.Join(" ", values);
                    }
                }
            }

            return string.Empty;
        }

        private string GetNamespaceName(SyntaxTree syntaxTree, CancellationToken cancellationToken)
        {
            var root = syntaxTree.GetCompilationUnitRoot(cancellationToken);
            var namespaceDeclaration = (NamespaceDeclarationSyntax)root.Members[0];
            return namespaceDeclaration.Name.ToString();
        }

        private IEnumerable<(ClassDeclarationSyntax, IEnumerable<MethodDeclarationSyntax>)>
            GetServiceClassDeclarations(SyntaxTree serviceSyntaxTree, CancellationToken cancellationToken)
        {
            const string NewInstance = "NewInstance";
            var root = serviceSyntaxTree.GetCompilationUnitRoot(cancellationToken);
            var helperDeclarations = ((NamespaceDeclarationSyntax)root.Members[0]).Members.OfType<ClassDeclarationSyntax>();

            return helperDeclarations.Select(helperDeclaration =>
            {
                var clientDeclaration =
                    helperDeclaration.ChildNodes()
                        .OfType<ClassDeclarationSyntax>()
                        .Single(d => d.Identifier.ValueText
                                        .Replace(SourceTemplates.ServiceClient, string.Empty)
                                        .Equals(helperDeclaration.Identifier.ValueText,
                                                StringComparison.OrdinalIgnoreCase));

                var methodDeclarations = clientDeclaration.Members
                    .OfType<MethodDeclarationSyntax>()
                    .Where(m => !m.Identifier.ValueText
                        .Equals(NewInstance, StringComparison.OrdinalIgnoreCase))
                    .GroupBy(m => m.Identifier.ValueText, (k, g) => g.ElementAt(0));

                return (helperDeclaration, methodDeclarations);
            });
        }

        private IEnumerable<ClassDeclarationSyntax> GetDataClassDeclarations(SyntaxTree serviceSyntaxTree, CancellationToken cancellationToken)
        {
            const string Reflection = "Reflection";
            var root = serviceSyntaxTree.GetCompilationUnitRoot(cancellationToken);
            var namespaceDeclaration = (NamespaceDeclarationSyntax)root.Members[0];
            return namespaceDeclaration.Members
                .OfType<ClassDeclarationSyntax>()
                .Where(c => !c.Identifier.ValueText.EndsWith(Reflection, StringComparison.OrdinalIgnoreCase));
        }

        private (SyntaxTree service, SyntaxTree data) GetServiceAndDataSyntaxTrees(string contractName, GeneratorExecutionContext context)
        {
            const string Grpc = "Grpc";
            var trees = context.Compilation.SyntaxTrees.Where(t =>
            {
                var fileName = Path.GetFileNameWithoutExtension(t.FilePath);
                return Path.GetExtension(t.FilePath).Equals(".cs") &&
                       fileName.Equals(contractName, StringComparison.OrdinalIgnoreCase) ||
                       fileName.Equals($"{contractName}{Grpc}", StringComparison.OrdinalIgnoreCase);
            }).OrderBy(t => t.FilePath.Length).ToArray();

            return trees.Length == 2
                ? (trees[1], trees[0])
                : default;
        }

        private static IEnumerable<XmlElementSyntax> GetXmlElements(SyntaxList<XmlNodeSyntax> content, string elementName)
        {
            return content.OfType<XmlElementSyntax>().Where(e => elementName.Equals(e.StartTag?.Name?.ToString()));
        }
    }
}
