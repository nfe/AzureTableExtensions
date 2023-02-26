using Microsoft.CodeAnalysis.Text;
using System.Text;
using VerifyCS =
    FisTech.Persistence.AzureTable.SourceGenerators.Tests.CSharpSourceGeneratorVerifier<
        FisTech.Persistence.AzureTable.SourceGenerators.AzureTableAdapterGenerator>;

namespace FisTech.Persistence.AzureTable.SourceGenerators.Tests;

public class AzureTableAdapterGeneratorTests : IClassFixture<AzureTableAdapterFixture>
{
    private readonly AzureTableAdapterFixture _fixture;

    public AzureTableAdapterGeneratorTests(AzureTableAdapterFixture fixture) => _fixture = fixture;

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
                public ITableEntity Adapt(TestModel item) => new TableEntity(item.Country, item.State);

                public TestModel Adapt(TableEntity entity) => new()
                {
                    Country = entity.PartitionKey,
                    State = entity.RowKey,
                };
            }
            """;

        var test = new VerifyCS.Test
        {
            TestState =
            {
                AdditionalReferences = { _fixture.AzureSdkAssemblyLocation, _fixture.AdapterAssemblyLocation },
                Sources = { _fixture.SimpleModelSource, adapterSource },
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
                public ITableEntity Adapt(TestModel item) => new TableEntity(item.Country, item.State)
                {
                    { nameof(TestModel.State), item.State },
                    { nameof(TestModel.Country), item.Country },
                };

                public TestModel Adapt(TableEntity entity) => new()
                {
                    State = entity.GetString(nameof(TestModel.State)),
                    Country = entity.GetString(nameof(TestModel.Country)),
                };
            }
            """;

        var test = new VerifyCS.Test
        {
            TestState =
            {
                AdditionalReferences = { _fixture.AzureSdkAssemblyLocation, _fixture.AdapterAssemblyLocation },
                Sources = { _fixture.SimpleModelSource, adapterSource },
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
    public async Task Generator_AllSupportedTypesModel_ReturnsAdapter()
    {
        const string adapterSource = """
            using FisTech.Persistence.AzureTable;
            using TestNamespace.Models;

            namespace TestNamespace.Adapters;

            [PartitionKey(nameof(TestModel.MyString), IgnoreSourceProperty = false)]
            [RowKey(nameof(TestModel.MyString), IgnoreSourceProperty = false)]
            public partial class TestModelAdapter : AzureTableAdapterBase<TestModel> { }
            """;

        const string expected = """
            using Azure.Data.Tables;
            using FisTech.Persistence.AzureTable;
            using TestNamespace.Models;

            namespace TestNamespace.Adapters;

            public partial class TestModelAdapter : IAzureTableAdapter<TestModel>
            {
                public ITableEntity Adapt(TestModel item) => new TableEntity(item.MyString, item.MyString)
                {
                    { nameof(TestModel.MyChar), item.MyChar.ToString() },
                    { nameof(TestModel.MyNullableChar), item.MyNullableChar?.ToString() },
                    { nameof(TestModel.MyString), item.MyString },
                    { nameof(TestModel.MyNullableString), item.MyNullableString },
                    { nameof(TestModel.MyBool), item.MyBool },
                    { nameof(TestModel.MyNullableBool), item.MyNullableBool },
                    { nameof(TestModel.MyByte), item.MyByte },
                    { nameof(TestModel.MyNullableByte), item.MyNullableByte },
                    { nameof(TestModel.MyShort), item.MyShort },
                    { nameof(TestModel.MyNullableShort), item.MyNullableShort },
                    { nameof(TestModel.MyInt), item.MyInt },
                    { nameof(TestModel.MyNullableInt), item.MyNullableInt },
                    { nameof(TestModel.MyLong), item.MyLong },
                    { nameof(TestModel.MyNullableLong), item.MyNullableLong },
                    { nameof(TestModel.MyFloat), item.MyFloat },
                    { nameof(TestModel.MyNullableFloat), item.MyNullableFloat },
                    { nameof(TestModel.MyDouble), item.MyDouble },
                    { nameof(TestModel.MyNullableDouble), item.MyNullableDouble },
                    { nameof(TestModel.MyDateTimeOffset), item.MyDateTimeOffset },
                    { nameof(TestModel.MyNullableDateTimeOffset), item.MyNullableDateTimeOffset },
                    { nameof(TestModel.MyGuid), item.MyGuid },
                    { nameof(TestModel.MyNullableGuid), item.MyNullableGuid },
                    { nameof(TestModel.MyEnum), (int)item.MyEnum },
                    { nameof(TestModel.MyNullableEnum), (int?)item.MyNullableEnum },
                    { nameof(TestModel.MyByteArray), item.MyByteArray },
                    { nameof(TestModel.MyBinaryData), item.MyBinaryData },
                };

                public TestModel Adapt(TableEntity entity) => new()
                {
                    MyChar = entity.GetString(nameof(TestModel.MyChar))[0],
                    MyNullableChar = entity.GetString(nameof(TestModel.MyNullableChar))?[0],
                    MyString = entity.GetString(nameof(TestModel.MyString)),
                    MyNullableString = entity.GetString(nameof(TestModel.MyNullableString)),
                    MyBool = entity.GetBoolean(nameof(TestModel.MyBool)).Value,
                    MyNullableBool = entity.GetBoolean(nameof(TestModel.MyNullableBool)),
                    MyByte = (byte)entity.GetInt32(nameof(TestModel.MyByte)).Value,
                    MyNullableByte = (byte?)entity.GetInt32(nameof(TestModel.MyNullableByte)),
                    MyShort = (short)entity.GetInt32(nameof(TestModel.MyShort)).Value,
                    MyNullableShort = (short?)entity.GetInt32(nameof(TestModel.MyNullableShort)),
                    MyInt = entity.GetInt32(nameof(TestModel.MyInt)).Value,
                    MyNullableInt = entity.GetInt32(nameof(TestModel.MyNullableInt)),
                    MyLong = entity.GetInt64(nameof(TestModel.MyLong)).Value,
                    MyNullableLong = entity.GetInt64(nameof(TestModel.MyNullableLong)),
                    MyFloat = (float)entity.GetDouble(nameof(TestModel.MyFloat)).Value,
                    MyNullableFloat = (float?)entity.GetDouble(nameof(TestModel.MyNullableFloat)),
                    MyDouble = entity.GetDouble(nameof(TestModel.MyDouble)).Value,
                    MyNullableDouble = entity.GetDouble(nameof(TestModel.MyNullableDouble)),
                    MyDateTimeOffset = entity.GetDateTimeOffset(nameof(TestModel.MyDateTimeOffset)).Value,
                    MyNullableDateTimeOffset = entity.GetDateTimeOffset(nameof(TestModel.MyNullableDateTimeOffset)),
                    MyGuid = entity.GetGuid(nameof(TestModel.MyGuid)).Value,
                    MyNullableGuid = entity.GetGuid(nameof(TestModel.MyNullableGuid)),
                    MyEnum = (TestNamespace.Models.MyEnum)entity.GetInt32(nameof(TestModel.MyEnum)).Value,
                    MyNullableEnum = (TestNamespace.Models.MyEnum?)entity.GetInt32(nameof(TestModel.MyNullableEnum)),
                    MyByteArray = entity.GetBinary(nameof(TestModel.MyByteArray)),
                    MyBinaryData = entity.GetBinaryData(nameof(TestModel.MyBinaryData)),
                };
            }
            """;

        var test = new VerifyCS.Test
        {
            TestState =
            {
                AdditionalReferences =
                {
                    _fixture.AzureSdkAssemblyLocation,
                    _fixture.AdapterAssemblyLocation,
                    _fixture.BinaryDataAssemblyLocation
                },
                Sources = { _fixture.AllTypesModelSource, adapterSource },
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