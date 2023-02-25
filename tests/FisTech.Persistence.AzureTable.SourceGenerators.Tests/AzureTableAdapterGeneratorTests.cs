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
    public async Task Generator_AllTypesModel_ReturnsAdapter()
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
                public ITableEntity Adapt(TestModel item)
                {
                    var entity = new TableEntity(item.MyString, item.MyString);
                    entity.Add(nameof(TestModel.MyChar), item.MyChar.ToString());
                    entity.Add(nameof(TestModel.MyNullableChar), item.MyNullableChar?.ToString());
                    entity.Add(nameof(TestModel.MyString), item.MyString);
                    entity.Add(nameof(TestModel.MyNullableString), item.MyNullableString);
                    entity.Add(nameof(TestModel.MyBool), item.MyBool);
                    entity.Add(nameof(TestModel.MyNullableBool), item.MyNullableBool);
                    entity.Add(nameof(TestModel.MyByte), item.MyByte);
                    entity.Add(nameof(TestModel.MyNullableByte), item.MyNullableByte);
                    entity.Add(nameof(TestModel.MyShort), item.MyShort);
                    entity.Add(nameof(TestModel.MyNullableShort), item.MyNullableShort);
                    entity.Add(nameof(TestModel.MyInt), item.MyInt);
                    entity.Add(nameof(TestModel.MyNullableInt), item.MyNullableInt);
                    entity.Add(nameof(TestModel.MyLong), item.MyLong);
                    entity.Add(nameof(TestModel.MyNullableLong), item.MyNullableLong);
                    entity.Add(nameof(TestModel.MyFloat), item.MyFloat);
                    entity.Add(nameof(TestModel.MyNullableFloat), item.MyNullableFloat);
                    entity.Add(nameof(TestModel.MyDouble), item.MyDouble);
                    entity.Add(nameof(TestModel.MyNullableDouble), item.MyNullableDouble);
                    entity.Add(nameof(TestModel.MyDateTimeOffset), item.MyDateTimeOffset);
                    entity.Add(nameof(TestModel.MyNullableDateTimeOffset), item.MyNullableDateTimeOffset);
                    entity.Add(nameof(TestModel.MyGuid), item.MyGuid);
                    entity.Add(nameof(TestModel.MyNullableGuid), item.MyNullableGuid);
                    entity.Add(nameof(TestModel.MyEnum), (int)item.MyEnum);
                    entity.Add(nameof(TestModel.MyNullableEnum), (int?)item.MyNullableEnum);
                    entity.Add(nameof(TestModel.MyByteArray), item.MyByteArray);

                    return entity;
                }

                public TestModel Adapt(TableEntity entity)
                {
                    var item = new TestModel();
                    item.MyChar = entity.GetString(nameof(TestModel.MyChar))[0];
                    item.MyNullableChar = entity.GetString(nameof(TestModel.MyNullableChar))?[0];
                    item.MyString = entity.GetString(nameof(TestModel.MyString));
                    item.MyNullableString = entity.GetString(nameof(TestModel.MyNullableString));
                    item.MyBool = entity.GetBoolean(nameof(TestModel.MyBool)).Value;
                    item.MyNullableBool = entity.GetBoolean(nameof(TestModel.MyNullableBool));
                    item.MyByte = (byte)entity.GetInt32(nameof(TestModel.MyByte)).Value;
                    item.MyNullableByte = (byte?)entity.GetInt32(nameof(TestModel.MyNullableByte));
                    item.MyShort = (short)entity.GetInt32(nameof(TestModel.MyShort)).Value;
                    item.MyNullableShort = (short?)entity.GetInt32(nameof(TestModel.MyNullableShort));
                    item.MyInt = entity.GetInt32(nameof(TestModel.MyInt)).Value;
                    item.MyNullableInt = entity.GetInt32(nameof(TestModel.MyNullableInt));
                    item.MyLong = entity.GetInt64(nameof(TestModel.MyLong)).Value;
                    item.MyNullableLong = entity.GetInt64(nameof(TestModel.MyNullableLong));
                    item.MyFloat = (float)entity.GetDouble(nameof(TestModel.MyFloat)).Value;
                    item.MyNullableFloat = (float?)entity.GetDouble(nameof(TestModel.MyNullableFloat));
                    item.MyDouble = entity.GetDouble(nameof(TestModel.MyDouble)).Value;
                    item.MyNullableDouble = entity.GetDouble(nameof(TestModel.MyNullableDouble));
                    item.MyDateTimeOffset = entity.GetDateTimeOffset(nameof(TestModel.MyDateTimeOffset)).Value;
                    item.MyNullableDateTimeOffset = entity.GetDateTimeOffset(nameof(TestModel.MyNullableDateTimeOffset));
                    item.MyGuid = entity.GetGuid(nameof(TestModel.MyGuid)).Value;
                    item.MyNullableGuid = entity.GetGuid(nameof(TestModel.MyNullableGuid));
                    item.MyEnum = (TestNamespace.Models.MyEnum)entity.GetInt32(nameof(TestModel.MyEnum)).Value;
                    item.MyNullableEnum = (TestNamespace.Models.MyEnum?)entity.GetInt32(nameof(TestModel.MyNullableEnum));
                    item.MyByteArray = entity.GetBinary(nameof(TestModel.MyByteArray));

                    return item;
                }
            }
            """;

        var test = new VerifyCS.Test
        {
            TestState =
            {
                AdditionalReferences = { _fixture.AzureSdkAssemblyLocation, _fixture.AdapterAssemblyLocation },
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