using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace FisTech.Persistence.AzureTable.SourceGenerators.UnitTests;

public class AzureTableAdapterGeneratorTests
{
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

                    return entity;
                }

                public TestModel Adapt(TableEntity entity) => new()
                {
                    Country = entity.PartitionKey,
                    State = entity.RowKey,
                };
            }
            """;

        var test = new AzureTableAdapterGeneratorTest
        {
            TestState =
            {
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

    [Fact]
    public async Task Generator_AllSchemaProperties_ReturnsAdapter()
    {
        const string modelSource = """
            using System;

            namespace TestNamespace.Models;

            public class TestModel
            {
                public string MyPartitionKey { get; set; }

                public string MyRowKey { get; set; }

                public DateTimeOffset? MyTimestamp { get; set; }

                public string? MyETag { get; set; }
            }
            """;

        const string adapterSource = """
            using FisTech.Persistence.AzureTable;
            using TestNamespace.Models;

            namespace TestNamespace.Adapters;

            [PartitionKey(nameof(TestModel.MyPartitionKey))]
            [RowKey(nameof(TestModel.MyRowKey))]
            [Timestamp(nameof(TestModel.MyTimestamp))]
            [ETag(nameof(TestModel.MyETag))]
            public partial class TestModelAdapter : AzureTableAdapterBase<TestModel> { }
            """;

        const string expected = """
            using Azure;
            using Azure.Data.Tables;
            using FisTech.Persistence.AzureTable;
            using TestNamespace.Models;

            namespace TestNamespace.Adapters;

            public partial class TestModelAdapter : IAzureTableAdapter<TestModel>
            {
                public ITableEntity Adapt(TestModel item)
                {
                    var entity = new TableEntity(item.MyPartitionKey, item.MyRowKey);

                    if (item.MyTimestamp != default)
                        entity.Timestamp = item.MyTimestamp;

                    if (item.MyETag != default)
                        entity.ETag = new ETag(item.MyETag);

                    return entity;
                }

                public TestModel Adapt(TableEntity entity) => new()
                {
                    MyPartitionKey = entity.PartitionKey,
                    MyRowKey = entity.RowKey,
                    MyTimestamp = entity.Timestamp,
                    MyETag = entity.ETag.ToString(),
                };
            }
            """;

        var test = new AzureTableAdapterGeneratorTest
        {
            TestState =
            {
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

    [Fact]
    public async Task Generator_AddSchemaSourceProperties_ReturnsAdapter()
    {
        const string modelSource = """
            using System;

            namespace TestNamespace.Models;

            public class TestModel
            {
                public string MyPartitionKey { get; set; }

                public string MyRowKey { get; set; }

                public DateTimeOffset? MyTimestamp { get; set; }

                public string? MyETag { get; set; }
            }
            """;

        const string adapterSource = """
            using FisTech.Persistence.AzureTable;
            using TestNamespace.Models;

            namespace TestNamespace.Adapters;

            [PartitionKey(nameof(TestModel.MyPartitionKey), IgnoreSourceProperty = false)]
            [RowKey(nameof(TestModel.MyRowKey), IgnoreSourceProperty = false)]
            [Timestamp(nameof(TestModel.MyTimestamp), IgnoreSourceProperty = false)]
            [ETag(nameof(TestModel.MyETag), IgnoreSourceProperty = false)]
            public partial class TestModelAdapter : AzureTableAdapterBase<TestModel> { }
            """;

        const string expected = """
            using Azure;
            using Azure.Data.Tables;
            using FisTech.Persistence.AzureTable;
            using TestNamespace.Models;

            namespace TestNamespace.Adapters;

            public partial class TestModelAdapter : IAzureTableAdapter<TestModel>
            {
                public ITableEntity Adapt(TestModel item)
                {
                    var entity = new TableEntity(item.MyPartitionKey, item.MyRowKey)
                    {
                        { nameof(TestModel.MyPartitionKey), item.MyPartitionKey },
                        { nameof(TestModel.MyRowKey), item.MyRowKey },
                        { nameof(TestModel.MyTimestamp), item.MyTimestamp },
                        { nameof(TestModel.MyETag), item.MyETag },
                    };

                    if (item.MyTimestamp != default)
                        entity.Timestamp = item.MyTimestamp;

                    if (item.MyETag != default)
                        entity.ETag = new ETag(item.MyETag);

                    return entity;
                }

                public TestModel Adapt(TableEntity entity) => new()
                {
                    MyPartitionKey = entity.GetString(nameof(TestModel.MyPartitionKey)),
                    MyRowKey = entity.GetString(nameof(TestModel.MyRowKey)),
                    MyTimestamp = entity.GetDateTimeOffset(nameof(TestModel.MyTimestamp)),
                    MyETag = entity.GetString(nameof(TestModel.MyETag)),
                };
            }
            """;

        var test = new AzureTableAdapterGeneratorTest
        {
            TestState =
            {
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

    [Fact]
    public async Task Generator_AllSupportedTypes_ReturnsAdapter()
    {
        const string modelSource = """
            using System;

            namespace TestNamespace.Models;

            public class TestModel
            {
                public char MyChar { get; set; } = 'A';

                public char? MyNullableChar { get; set; }

                public string MyString { get; set; } = "Hello World";

                public string? MyNullableString { get; set; }

                public bool MyBool { get; set; } = true;

                public bool? MyNullableBool { get; set; }

                public byte MyByte { get; set; } = byte.MaxValue;

                public byte? MyNullableByte { get; set; }

                public short MyShort { get; set; } = short.MaxValue;

                public short? MyNullableShort { get; set; }

                public int MyInt { get; set; } = int.MaxValue;

                public int? MyNullableInt { get; set; }

                public long MyLong { get; set; } = long.MaxValue;

                public long? MyNullableLong { get; set; }

                public float MyFloat { get; set; } = float.MaxValue;

                public float? MyNullableFloat { get; set; }

                public double MyDouble { get; set; } = double.MaxValue;

                public double? MyNullableDouble { get; set; }

                // TODO: public decimal MyDecimal { get; set; } = decimal.MaxValue;

                // TODO: public decimal? MyNullableDecimal { get; set; }

                // TODO: public DateTime MyDateTime { get; set; } = DateTime.Now;

                // TODO: public DateTime? MyNullableDateTime { get; set; }

                public DateTimeOffset MyDateTimeOffset { get; set; } = DateTime.UtcNow;

                public DateTimeOffset? MyNullableDateTimeOffset { get; set; }

                public Guid MyGuid { get; set; } = Guid.NewGuid();

                public Guid? MyNullableGuid { get; set; }

                public MyEnum MyEnum { get; set; } = MyEnum.ValueB;

                public MyEnum? MyNullableEnum { get; set; }

                public byte[] MyByteArray { get; set; } = { 1, 2, 3, 4, 5 };

                public BinaryData MyBinaryData { get; set; } = new(new byte[] { 9, 8, 7, 6, 5});
            }

            public enum MyEnum { ValueA, ValueB, ValueC }
            """;

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
                    var entity = new TableEntity(item.MyString, item.MyString)
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

                    return entity;
                }

                public TestModel Adapt(TableEntity entity) => new()
                {
                    MyChar = entity.GetString(nameof(TestModel.MyChar))[0],
                    MyNullableChar = entity.GetString(nameof(TestModel.MyNullableChar))?[0],
                    MyString = entity.GetString(nameof(TestModel.MyString)),
                    MyNullableString = entity.GetString(nameof(TestModel.MyNullableString)),
                    MyBool = entity.GetBoolean(nameof(TestModel.MyBool)).GetValueOrDefault(),
                    MyNullableBool = entity.GetBoolean(nameof(TestModel.MyNullableBool)),
                    MyByte = (byte)entity.GetInt32(nameof(TestModel.MyByte)).GetValueOrDefault(),
                    MyNullableByte = (byte?)entity.GetInt32(nameof(TestModel.MyNullableByte)),
                    MyShort = (short)entity.GetInt32(nameof(TestModel.MyShort)).GetValueOrDefault(),
                    MyNullableShort = (short?)entity.GetInt32(nameof(TestModel.MyNullableShort)),
                    MyInt = entity.GetInt32(nameof(TestModel.MyInt)).GetValueOrDefault(),
                    MyNullableInt = entity.GetInt32(nameof(TestModel.MyNullableInt)),
                    MyLong = entity.GetInt64(nameof(TestModel.MyLong)).GetValueOrDefault(),
                    MyNullableLong = entity.GetInt64(nameof(TestModel.MyNullableLong)),
                    MyFloat = (float)entity.GetDouble(nameof(TestModel.MyFloat)).GetValueOrDefault(),
                    MyNullableFloat = (float?)entity.GetDouble(nameof(TestModel.MyNullableFloat)),
                    MyDouble = entity.GetDouble(nameof(TestModel.MyDouble)).GetValueOrDefault(),
                    MyNullableDouble = entity.GetDouble(nameof(TestModel.MyNullableDouble)),
                    MyDateTimeOffset = entity.GetDateTimeOffset(nameof(TestModel.MyDateTimeOffset)).GetValueOrDefault(),
                    MyNullableDateTimeOffset = entity.GetDateTimeOffset(nameof(TestModel.MyNullableDateTimeOffset)),
                    MyGuid = entity.GetGuid(nameof(TestModel.MyGuid)).GetValueOrDefault(),
                    MyNullableGuid = entity.GetGuid(nameof(TestModel.MyNullableGuid)),
                    MyEnum = (TestNamespace.Models.MyEnum)entity.GetInt32(nameof(TestModel.MyEnum)).GetValueOrDefault(),
                    MyNullableEnum = (TestNamespace.Models.MyEnum?)entity.GetInt32(nameof(TestModel.MyNullableEnum)),
                    MyByteArray = entity.GetBinary(nameof(TestModel.MyByteArray)),
                    MyBinaryData = entity.GetBinaryData(nameof(TestModel.MyBinaryData)),
                };
            }
            """;

        var test = new AzureTableAdapterGeneratorTest
        {
            TestState =
            {
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

    [Fact]
    public async Task Generator_IgnoreNonPublicProperties_ReturnsAdapter()
    {
        const string modelSource = """
            namespace TestNamespace.Models;

            public class TestModel
            {
                public string MyPartitionKey { get; set; }

                public string MyRowKey { get; set; }

                internal string MyInternal { get; set; }

                private string MyPrivate { get; set; }

                private protected string MyPrivateProtected { get; set; }

                protected string MyProtected { get; set; }

                protected internal string MyProtectedInternal { get; set; }
            }
            """;

        const string adapterSource = """
            using FisTech.Persistence.AzureTable;
            using TestNamespace.Models;

            namespace TestNamespace.Adapters;

            [PartitionKey(nameof(TestModel.MyPartitionKey))]
            [RowKey(nameof(TestModel.MyRowKey))]
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
                    var entity = new TableEntity(item.MyPartitionKey, item.MyRowKey);

                    return entity;
                }

                public TestModel Adapt(TableEntity entity) => new()
                {
                    MyPartitionKey = entity.PartitionKey,
                    MyRowKey = entity.RowKey,
                };
            }
            """;

        var test = new AzureTableAdapterGeneratorTest
        {
            TestState =
            {
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

    [Fact]
    public async Task Generator_IgnoreProperties_ReturnsAdapter()
    {
        const string modelSource = """
            namespace TestNamespace.Models;

            public class TestModel
            {
                public string State { get; set; }

                public string Country { get; set; }

                public string Abbreviation { get; set; }

                public string CapitalCity { get; set; }

                public int Population { get; set; }
            }
            """;

        const string adapterSource = """
            using FisTech.Persistence.AzureTable;
            using TestNamespace.Models;

            namespace TestNamespace.Adapters;

            [PartitionKey(nameof(TestModel.Country))]
            [RowKey(nameof(TestModel.State))]
            [Ignore(nameof(TestModel.Abbreviation))]
            [Ignore(nameof(TestModel.Population))]
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
                    var entity = new TableEntity(item.Country, item.State)
                    {
                        { nameof(TestModel.CapitalCity), item.CapitalCity },
                    };

                    return entity;
                }

                public TestModel Adapt(TableEntity entity) => new()
                {
                    Country = entity.PartitionKey,
                    State = entity.RowKey,
                    CapitalCity = entity.GetString(nameof(TestModel.CapitalCity)),
                };
            }
            """;

        var test = new AzureTableAdapterGeneratorTest
        {
            TestState =
            {
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

    [Fact]
    public async Task Generator_NameChanges_ReturnsAdapter()
    {
        const string modelSource = """
            namespace TestNamespace.Models;

            public class TestModel
            {
                public string State { get; set; }

                public string Country { get; set; }

                public string Abbreviation { get; set; }

                public string CapitalCity { get; set; }

                public int Population { get; set; }
            }
            """;

        const string adapterSource = """
            using FisTech.Persistence.AzureTable;
            using TestNamespace.Models;

            namespace TestNamespace.Adapters;

            [PartitionKey(nameof(TestModel.Country))]
            [RowKey(nameof(TestModel.State))]
            [NameChange(nameof(TestModel.Population), "Inhabitants")]
            [NameChange(nameof(TestModel.Abbreviation), "Acronym")]
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
                    var entity = new TableEntity(item.Country, item.State)
                    {
                        { "Acronym", item.Abbreviation },
                        { nameof(TestModel.CapitalCity), item.CapitalCity },
                        { "Inhabitants", item.Population },
                    };

                    return entity;
                }

                public TestModel Adapt(TableEntity entity) => new()
                {
                    Country = entity.PartitionKey,
                    State = entity.RowKey,
                    Abbreviation = entity.GetString("Acronym"),
                    CapitalCity = entity.GetString(nameof(TestModel.CapitalCity)),
                    Population = entity.GetInt32("Inhabitants").GetValueOrDefault(),
                };
            }
            """;

        var test = new AzureTableAdapterGeneratorTest
        {
            TestState =
            {
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