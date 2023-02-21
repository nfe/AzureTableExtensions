using Azure.Data.Tables;

namespace FisTech.Persistence.AzureTable.SourceGenerators.Tests;

public class AzureTableAdapterFixture
{
    public string EntityAssemblyLocation { get; } = typeof(ITableEntity).Assembly.Location;

    public string AdapterAssemblyLocation { get; } = typeof(IAzureTableAdapter<>).Assembly.Location;

    public string SimpleModelSource { get; } = """
        namespace TestNamespace.Models;

        public class TestModel
        {
            public string State { get; set; }

            public string Country { get; set; }
        }
        """;
}