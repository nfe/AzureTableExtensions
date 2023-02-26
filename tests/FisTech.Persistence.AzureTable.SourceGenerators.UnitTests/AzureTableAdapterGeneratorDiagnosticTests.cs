using Azure.Data.Tables;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS =
    FisTech.Persistence.AzureTable.SourceGenerators.UnitTests.CSharpSourceGeneratorVerifier<
        FisTech.Persistence.AzureTable.SourceGenerators.AzureTableAdapterGenerator>;

namespace FisTech.Persistence.AzureTable.SourceGenerators.UnitTests;

public class AzureTableAdapterGeneratorDiagnosticTests
{
    private const string ModelSource = """
        namespace TestNamespace.Models;

        public class TestModel
        {
            public string State { get; set; }

            public string Country { get; set; }
        }
        """;

    private static readonly string s_azureSdkReference = typeof(ITableEntity).Assembly.Location;
    private static readonly string s_adapterReference = typeof(IAzureTableAdapter<>).Assembly.Location;
    
    // TODO: Fix Generator_InvalidAdapterClassAccessibility_ReturnsDiagnosticErrorAZTBGEN001
    //     [Theory]
    //     [InlineData("private")]
    //     [InlineData("private protected")]
    //     [InlineData("protected")]
    //     [InlineData("protected internal")]
    //     public async Task Generator_InvalidAdapterClassAccessibility_ReturnsDiagnosticErrorAZTBGEN001(string accessModifiers)
    //     {
    //         var adapterSource = $$"""
    //             using FisTech.Persistence.AzureTable;
    //             using TestNamespace.Models;
    //
    //             namespace TestNamespace.Adapters;
    //
    //             [PartitionKey(nameof(TestModel.Country))]
    //             [RowKey(nameof(TestModel.State))]
    //             {{accessModifiers}} partial class TestModelAdapter : AzureTableAdapterBase<TestModel> { }
    //             """;
    //
    //         var test = new VerifyCS.Test
    //         {
    //             TestState =
    //             {
    //                 AdditionalReferences = { s_entityAssemblyLocation, s_adapterAssemblyLocation },
    //                 Sources = { ModelSource, adapterSource },
    //                 ExpectedDiagnostics =
    //                 {
    //                     DiagnosticResult.CompilerError("AZTBGEN001")
    //                         .WithSeverity(DiagnosticSeverity.Error)
    //                         .WithLocation("/0/Test1.cs", 8, 22)
    //                         .WithMessage(
    //                             $"Adapter class 'TestNamespace.Adapters.TestModelAdapter' should be public or internal")
    //                 }
    //             }
    //         };
    //
    //         await test.RunAsync();
    //     }

    [Fact]
    public async Task Generator_AbstractAdapterClass_ReturnsDiagnosticErrorAZTBGEN002()
    {
        const string adapterSource = """
             using FisTech.Persistence.AzureTable;
             using TestNamespace.Models;
             
             namespace TestNamespace.Adapters;
             
             [PartitionKey(nameof(TestModel.Country))]
             [RowKey(nameof(TestModel.State))]
             public abstract partial class TestModelAdapter : AzureTableAdapterBase<TestModel> { }
             """;

        var test = new VerifyCS.Test
        {
            TestState =
            {
                AdditionalReferences = { s_azureSdkReference, s_adapterReference },
                Sources = { ModelSource, adapterSource },
                ExpectedDiagnostics =
                {
                    DiagnosticResult.CompilerError("AZTBGEN002")
                        .WithSeverity(DiagnosticSeverity.Error)
                        .WithLocation("/0/Test1.cs", 8, 31)
                        .WithMessage(
                            "Adapter class 'TestNamespace.Adapters.TestModelAdapter' should not be abstract")
                }
            }
        };

        await test.RunAsync();
    }

    [Fact]
    public async Task Generator_GenericAdapterClass_ReturnsDiagnosticErrorAZTBGEN003()
    {
        const string adapterSource = """
             using FisTech.Persistence.AzureTable;
             using TestNamespace.Models;
             
             namespace TestNamespace.Adapters;
             
             [PartitionKey(nameof(TestModel.Country))]
             [RowKey(nameof(TestModel.State))]
             public partial class TestModelAdapter<T> : AzureTableAdapterBase<TestModel> { }
             """;

        var test = new VerifyCS.Test
        {
            TestState =
            {
                AdditionalReferences = { s_azureSdkReference, s_adapterReference },
                Sources = { ModelSource, adapterSource },
                ExpectedDiagnostics =
                {
                    DiagnosticResult.CompilerError("AZTBGEN003")
                        .WithSeverity(DiagnosticSeverity.Error)
                        .WithLocation("/0/Test1.cs", 8, 22)
                        .WithMessage(
                            "Adapter class 'TestNamespace.Adapters.TestModelAdapter<T>' does not support generic types")
                }
            }
        };

        await test.RunAsync();
    }

    [Fact]
    public async Task Generator_NonPartialAdapterClass_ReturnsDiagnosticErrorAZTBGEN004()
    {
        const string adapterSource = """
             using FisTech.Persistence.AzureTable;
             using TestNamespace.Models;
             
             namespace TestNamespace.Adapters;
             
             [PartitionKey(nameof(TestModel.Country))]
             [RowKey(nameof(TestModel.State))]
             public class TestModelAdapter : AzureTableAdapterBase<TestModel> { }
             """;

        var test = new VerifyCS.Test
        {
            TestState =
            {
                AdditionalReferences = { s_azureSdkReference, s_adapterReference },
                Sources = { ModelSource, adapterSource },
                ExpectedDiagnostics =
                {
                    DiagnosticResult.CompilerError("AZTBGEN004")
                        .WithSeverity(DiagnosticSeverity.Error)
                        .WithLocation("/0/Test1.cs", 8, 14)
                        .WithMessage(
                            "Adapter class 'TestNamespace.Adapters.TestModelAdapter' must have to be partial")
                }
            }
        };

        await test.RunAsync();
    }

    [Theory]
    [InlineData("[RowKey(nameof(TestModel.State))]", "PartitionKeyAttribute")]
    [InlineData("[PartitionKey(nameof(TestModel.Country))]", "RowKeyAttribute")]
    [InlineData("[PartitionKey(\"MyProperty\")]", "PartitionKeyAttribute")]
    public async Task Generator_InvalidAdapterClassPropertyAttribute_ReturnsDiagnosticErrorAZTBGEN005(
        string attributeSource, string attributeName)
    {
        var adapterSource = $$"""
            using FisTech.Persistence.AzureTable;
            using TestNamespace.Models;

            namespace TestNamespace.Adapters;

            {{attributeSource}}
            public partial class TestModelAdapter : AzureTableAdapterBase<TestModel> { }
            """;

        var test = new VerifyCS.Test
        {
            TestState =
            {
                AdditionalReferences = { s_azureSdkReference, s_adapterReference },
                Sources = { ModelSource, adapterSource },
                ExpectedDiagnostics =
                {
                    DiagnosticResult.CompilerError("AZTBGEN005")
                        .WithSeverity(DiagnosticSeverity.Error)
                        .WithLocation("/0/Test1.cs", 7, 22)
                        .WithMessage(
                            $"Adapter class 'TestNamespace.Adapters.TestModelAdapter' property not found for '{attributeName}' attribute")
                }
            }
        };

        await test.RunAsync();
    }
}