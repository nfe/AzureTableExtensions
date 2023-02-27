using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using System.Reflection;

namespace FisTech.Persistence.AzureTable.SourceGenerators.UnitTests;

public class AzureTableAdapterGeneratorTest : CSharpSourceGeneratorTest<AzureTableAdapterGenerator, XUnitVerifier>
{
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

        CompilerDiagnostics = CompilerDiagnostics.All;
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