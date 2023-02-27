using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using System.Collections.Immutable;
using System.Reflection;

namespace FisTech.Persistence.AzureTable.SourceGenerators.UnitTests;

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
            string[] args = { "/nullable:enable /warnaserror:nullable" };
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

public class AzureTableAdapterGeneratorTest : CSharpSourceGeneratorVerifier<AzureTableAdapterGenerator>.Test
{
    // TODO: Change compilation level from error to warning
    
    public AzureTableAdapterGeneratorTest()
    {
#if NET5_0
        ReferenceAssemblies = ReferenceAssemblies.Net.Net50;
#elif NET6_0
        ReferenceAssemblies = ReferenceAssemblies.Net.Net60;
#endif

        TestState.AdditionalReferences.Add(Assembly.Load("FisTech.Persistence.AzureTable"));
        TestState.AdditionalReferences.Add(Assembly.Load("Azure.Data.Tables"));
        TestState.AdditionalReferences.Add(Assembly.Load("Azure.Core"));
        TestState.AdditionalReferences.Add(Assembly.Load("System.Memory.Data"));
    }
}