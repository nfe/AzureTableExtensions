using Azure.Data.Tables;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using VerifyCS =
    FisTech.Persistence.AzureTable.SourceGenerators.Tests.CSharpSourceGeneratorVerifier<
        FisTech.Persistence.AzureTable.SourceGenerators.AzureTableAdapterGenerator>;

namespace FisTech.Persistence.AzureTable.SourceGenerators.Tests;

public class AzureTableAdapterGeneratorTests
{
    private static readonly string s_entityAssemblyLocation = typeof(ITableEntity).Assembly.Location;
    private static readonly string s_adapterAssemblyLocation = typeof(IAzureTableAdapter<>).Assembly.Location;

    [Fact]
    public async Task Generator_SimpleModel_ReturnsAdapter()
    {
        const string modelSource = """
            namespace TestNamespace.Models;

            public class TestModel
            {
                public string State { get; set; }

                public string Country { get; set; }
            }
            """;

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
                Sources = { modelSource, adapterSource },
                GeneratedSources =
                {
                    (typeof(AzureTableAdapterGenerator), "TestModelAdapter.g.cs",
                        SourceText.From(expected, Encoding.UTF8))
                }
            }
        };

        await test.RunAsync();
    }
}