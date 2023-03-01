using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using System.Collections.Immutable;

namespace FisTech.Persistence.AzureTable.SourceGenerators.UnitTests;

public class AzureTableAdapterGeneratorTest : CSharpSourceGeneratorTest<AzureTableAdapterGenerator, XUnitVerifier>
{
    public AzureTableAdapterGeneratorTest()
    {
        ReferenceAssemblies =
            DefaultReferenceAssemblies.WithPackages(
                ImmutableArray.Create(new PackageIdentity("Azure.Data.Tables", "12.7.1")));

        TestState.AdditionalReferences.Add(
            MetadataReference.CreateFromFile(typeof(AzureTableAdapterBase<>).Assembly.Location));

        CompilerDiagnostics = CompilerDiagnostics.All;
    }

    private static ReferenceAssemblies DefaultReferenceAssemblies
    {
        get
        {
#if NET5_0
            return ReferenceAssemblies.Net.Net50;
#elif NET6_0
            return ReferenceAssemblies.Net.Net60;
#endif
        }
    }

    protected override CompilationOptions CreateCompilationOptions() =>
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            .WithNullableContextOptions(NullableContextOptions.Enable)
            .WithSpecificDiagnosticOptions(new Dictionary<string, ReportDiagnostic>
            {
                // Missing XML comment for publicly visible type or member
                { "CS1591", ReportDiagnostic.Suppress },
                // Non-nullable property must contain a non-null value when exiting constructor
                { "CS8618", ReportDiagnostic.Suppress },
                // Assuming assembly reference "Assembly Name #1" matches "Assembly Name #2"
                { "CS1701", ReportDiagnostic.Suppress }
            });
}