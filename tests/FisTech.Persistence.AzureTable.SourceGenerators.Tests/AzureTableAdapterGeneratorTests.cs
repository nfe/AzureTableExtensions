using Azure.Data.Tables;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using VerifyCS =
    FisTech.Persistence.AzureTable.SourceGenerators.Tests.CSharpSourceGeneratorVerifier<
        FisTech.Persistence.AzureTable.SourceGenerators.AzureTableAdapterGenerator>;

namespace FisTech.Persistence.AzureTable.SourceGenerators.Tests;

public class AzureTableAdapterGeneratorTests
{
    private const string SimpleModelSource = """
        namespace TestNamespace.Models;

        public class TestModel
        {
            public string State { get; set; }

            public string Country { get; set; }
        }
        """;
    
    private static readonly string s_entityAssemblyLocation = typeof(ITableEntity).Assembly.Location;
    private static readonly string s_adapterAssemblyLocation = typeof(IAzureTableAdapter<>).Assembly.Location;

    [Fact]
    public async Task Generator_SimpleModel_ReturnsAdapter()
    {
        const string adapterSource = """
            using FisTech.Persistence.AzureTable;
            using TestNamespace.Models;

            namespace TestNamespace.Adapters;

            [PartitionKey(nameof(TestModel.Country))]
            [RowKey(nameof(TestModel.State))]
            public partial class TestModelAdapter : AzureTableAdapterBase<TestModel> { }
            """;

        const string expected = """
            using Azure.Data.Tables;
            using FisTech.Persistence.AzureTable;
            using TestNamespace.Models;

            namespace TestNamespace.Adapters;

            public partial class TestModelAdapter : IAzureTableAdapter<TestModel>
            {
                public ITableEntity Adapt(TestModel item)
                {
                    var entity = new TableEntity(item.Country, item.State);

                    return entity;
                }

                public TestModel Adapt(TableEntity entity)
                {
                    var item = new TestModel();
                    item.Country = entity.PartitionKey;
                    item.State = entity.RowKey;

                    return item;
                }
            }
            """;

        var test = new VerifyCS.Test
        {
            TestState =
            {
                AdditionalReferences = { s_entityAssemblyLocation, s_adapterAssemblyLocation },
                Sources = { SimpleModelSource, adapterSource },
                GeneratedSources =
                {
                    (typeof(AzureTableAdapterGenerator), "TestModelAdapter.g.cs",
                        SourceText.From(expected, Encoding.UTF8))
                }
            }
        };

        await test.RunAsync();
    }

    [Fact]
    public async Task Generator_AddSchemaSourceProperties_ReturnsAdapter()
    {
        const string adapterSource = """
            using FisTech.Persistence.AzureTable;
            using TestNamespace.Models;

            namespace TestNamespace.Adapters;

            [PartitionKey(nameof(TestModel.Country), IgnoreSourceProperty = false)]
            [RowKey(nameof(TestModel.State), IgnoreSourceProperty = false)]
            public partial class TestModelAdapter : AzureTableAdapterBase<TestModel> { }
            """;

        const string expected = """
            using Azure.Data.Tables;
            using FisTech.Persistence.AzureTable;
            using TestNamespace.Models;

            namespace TestNamespace.Adapters;

            public partial class TestModelAdapter : IAzureTableAdapter<TestModel>
            {
                public ITableEntity Adapt(TestModel item)
                {
                    var entity = new TableEntity(item.Country, item.State);
                    entity.Add(nameof(TestModel.State), item.State);
                    entity.Add(nameof(TestModel.Country), item.Country);

                    return entity;
                }

                public TestModel Adapt(TableEntity entity)
                {
                    var item = new TestModel();
                    item.State = entity.GetString(nameof(TestModel.State));
                    item.Country = entity.GetString(nameof(TestModel.Country));

                    return item;
                }
            }
            """;

        var test = new VerifyCS.Test
        {
            TestState =
            {
                AdditionalReferences = { s_entityAssemblyLocation, s_adapterAssemblyLocation },
                Sources = { SimpleModelSource, adapterSource },
                GeneratedSources =
                {
                    (typeof(AzureTableAdapterGenerator), "TestModelAdapter.g.cs",
                        SourceText.From(expected, Encoding.UTF8))
                }
            }
        };

        await test.RunAsync();
    }
    
    [Theory]
    [InlineData("""
        using FisTech.Persistence.AzureTable;
        using TestNamespace.Models;

        namespace TestNamespace.Adapters;

        [RowKey(nameof(TestModel.State))]
        public partial class TestModelAdapter : AzureTableAdapterBase<TestModel> { }
        """, "PartitionKeyAttribute")]
    [InlineData("""
        using FisTech.Persistence.AzureTable;
        using TestNamespace.Models;

        namespace TestNamespace.Adapters;

        [PartitionKey(nameof(TestModel.Country))]
        public partial class TestModelAdapter : AzureTableAdapterBase<TestModel> { }
        """, "RowKeyAttribute")]
    [InlineData("""
        using FisTech.Persistence.AzureTable;
        using TestNamespace.Models;

        namespace TestNamespace.Adapters;

        [PartitionKey("MyProperty")]
        public partial class TestModelAdapter : AzureTableAdapterBase<TestModel> { }
        """, "PartitionKeyAttribute")]
    public async Task Generator_InvalidPropertyAttribute_ReturnsDiagnosticErrorAZTBGEN005(string adapterSource, string attributeName)
    {
        var test = new VerifyCS.Test
        {
            TestState =
            {
                AdditionalReferences = { s_entityAssemblyLocation, s_adapterAssemblyLocation },
                Sources = { SimpleModelSource, adapterSource },
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