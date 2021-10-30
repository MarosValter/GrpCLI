using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace SourceGenerator
{
    internal static class DiagnosticRules
    {
        internal static readonly DiagnosticDescriptor MissingDependencyRule = new("Dependency", "Missing package", "Unable to find 'System.CommandLine' dependency", "Compiler", DiagnosticSeverity.Error, true);
        internal static readonly DiagnosticDescriptor MissingPropertyRule = new("Property", "Missing property", "Unable to read 'ProtoName' build property", "Compiler", DiagnosticSeverity.Error, true);
        internal static readonly DiagnosticDescriptor MissingCodeRule = new("Dependency", "Missing generated code", "Unable to find code generated from Proto file", "Compiler", DiagnosticSeverity.Error, true);

    }
}
