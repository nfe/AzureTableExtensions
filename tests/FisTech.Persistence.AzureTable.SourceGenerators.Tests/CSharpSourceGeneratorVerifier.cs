using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using System.Collections.Immutable;

namespace FisTech.Persistence.AzureTable.SourceGenerators.Tests;

public static class CSharpSourceGeneratorVerifier<TSourceGenerator> where TSourceGenerator : ISourceGenerator, new()
{
#pragma warning disable CA1034
    public class Test : CSharpSourceGeneratorTest<TSourceGenerator, XUnitVerifier>
#pragma warning restore CA1034
    {
        public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.Default;

        protected override CompilationOptions CreateCompilationOptions()
        {
            CompilationOptions compilationOptions = base.CreateCompilationOptions();
            return compilationOptions.WithSpecificDiagnosticOptions(
                compilationOptions.SpecificDiagnosticOptions.SetItems(GetNullableWarningsFromCompiler()));
        }

        private static ImmutableDictionary<string, ReportDiagnostic> GetNullableWarningsFromCompiler()
        {
            string[] args = { "/warnaserror:nullable" };
            CSharpCommandLineArguments commandLineArguments = CSharpCommandLineParser.Default.Parse(args,
                Environment.CurrentDirectory, Environment.CurrentDirectory);
            ImmutableDictionary<string, ReportDiagnostic> nullableWarnings =
                commandLineArguments.CompilationOptions.SpecificDiagnosticOptions;

            return nullableWarnings;
        }

        protected override ParseOptions CreateParseOptions() =>
            ((CSharpParseOptions)base.CreateParseOptions()).WithLanguageVersion(LanguageVersion);
    }
}