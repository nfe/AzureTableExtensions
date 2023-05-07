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
    public async Task Generator_SchemaProperties_ReturnsAdapter()
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

                    var timestamp = item.MyTimestamp;
                    if (timestamp != default)
                        entity.Timestamp = timestamp;

                    var etag = item.MyETag;
                    if (etag != default)
                        entity.ETag = new ETag(etag);

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
    public async Task Generator_DoNotIgnoreSchemaProperties_ReturnsAdapter()
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
                        { "MyPartitionKey", item.MyPartitionKey },
                        { "MyRowKey", item.MyRowKey },
                        { "MyTimestamp", item.MyTimestamp },
                        { "MyETag", item.MyETag },
                    };

                    var timestamp = item.MyTimestamp;
                    if (timestamp != default)
                        entity.Timestamp = timestamp;

                    var etag = item.MyETag;
                    if (etag != default)
                        entity.ETag = new ETag(etag);

                    return entity;
                }

                public TestModel Adapt(TableEntity entity) => new()
                {
                    MyPartitionKey = entity.GetString("MyPartitionKey"),
                    MyRowKey = entity.GetString("MyRowKey"),
                    MyTimestamp = entity.GetDateTimeOffset("MyTimestamp"),
                    MyETag = entity.GetString("MyETag"),
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
    public async Task Generator_SupportedTypes_ReturnsAdapter()
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
                        { "MyChar", item.MyChar.ToString() },
                        { "MyNullableChar", item.MyNullableChar?.ToString() },
                        { "MyString", item.MyString },
                        { "MyNullableString", item.MyNullableString },
                        { "MyBool", item.MyBool },
                        { "MyNullableBool", item.MyNullableBool },
                        { "MyByte", item.MyByte },
                        { "MyNullableByte", item.MyNullableByte },
                        { "MyShort", item.MyShort },
                        { "MyNullableShort", item.MyNullableShort },
                        { "MyInt", item.MyInt },
                        { "MyNullableInt", item.MyNullableInt },
                        { "MyLong", item.MyLong },
                        { "MyNullableLong", item.MyNullableLong },
                        { "MyFloat", item.MyFloat },
                        { "MyNullableFloat", item.MyNullableFloat },
                        { "MyDouble", item.MyDouble },
                        { "MyNullableDouble", item.MyNullableDouble },
                        { "MyDateTimeOffset", item.MyDateTimeOffset },
                        { "MyNullableDateTimeOffset", item.MyNullableDateTimeOffset },
                        { "MyGuid", item.MyGuid },
                        { "MyNullableGuid", item.MyNullableGuid },
                        { "MyEnum", (int)item.MyEnum },
                        { "MyNullableEnum", (int?)item.MyNullableEnum },
                        { "MyByteArray", item.MyByteArray },
                        { "MyBinaryData", item.MyBinaryData },
                    };

                    return entity;
                }

                public TestModel Adapt(TableEntity entity) => new()
                {
                    MyChar = entity.GetString("MyChar")[0],
                    MyNullableChar = entity.GetString("MyNullableChar")?[0],
                    MyString = entity.GetString("MyString"),
                    MyNullableString = entity.GetString("MyNullableString"),
                    MyBool = entity.GetBoolean("MyBool").GetValueOrDefault(),
                    MyNullableBool = entity.GetBoolean("MyNullableBool"),
                    MyByte = (byte)entity.GetInt32("MyByte").GetValueOrDefault(),
                    MyNullableByte = (byte?)entity.GetInt32("MyNullableByte"),
                    MyShort = (short)entity.GetInt32("MyShort").GetValueOrDefault(),
                    MyNullableShort = (short?)entity.GetInt32("MyNullableShort"),
                    MyInt = entity.GetInt32("MyInt").GetValueOrDefault(),
                    MyNullableInt = entity.GetInt32("MyNullableInt"),
                    MyLong = entity.GetInt64("MyLong").GetValueOrDefault(),
                    MyNullableLong = entity.GetInt64("MyNullableLong"),
                    MyFloat = (float)entity.GetDouble("MyFloat").GetValueOrDefault(),
                    MyNullableFloat = (float?)entity.GetDouble("MyNullableFloat"),
                    MyDouble = entity.GetDouble("MyDouble").GetValueOrDefault(),
                    MyNullableDouble = entity.GetDouble("MyNullableDouble"),
                    MyDateTimeOffset = entity.GetDateTimeOffset("MyDateTimeOffset").GetValueOrDefault(),
                    MyNullableDateTimeOffset = entity.GetDateTimeOffset("MyNullableDateTimeOffset"),
                    MyGuid = entity.GetGuid("MyGuid").GetValueOrDefault(),
                    MyNullableGuid = entity.GetGuid("MyNullableGuid"),
                    MyEnum = (TestNamespace.Models.MyEnum)entity.GetInt32("MyEnum").GetValueOrDefault(),
                    MyNullableEnum = (TestNamespace.Models.MyEnum?)entity.GetInt32("MyNullableEnum"),
                    MyByteArray = entity.GetBinary("MyByteArray"),
                    MyBinaryData = entity.GetBinaryData("MyBinaryData"),
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
                        { "CapitalCity", item.CapitalCity },
                    };

                    return entity;
                }

                public TestModel Adapt(TableEntity entity) => new()
                {
                    Country = entity.PartitionKey,
                    State = entity.RowKey,
                    CapitalCity = entity.GetString("CapitalCity"),
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

                public float TotalArea { get; set; }
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
            [Ignore(nameof(TestModel.TotalArea))]
            [NameChange(nameof(TestModel.TotalArea), "TotalAreaKm")]
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
                        { "CapitalCity", item.CapitalCity },
                        { "Inhabitants", item.Population },
                    };

                    return entity;
                }

                public TestModel Adapt(TableEntity entity) => new()
                {
                    Country = entity.PartitionKey,
                    State = entity.RowKey,
                    Abbreviation = entity.GetString("Acronym"),
                    CapitalCity = entity.GetString("CapitalCity"),
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
    
    [Fact]
    public async Task Generator_Converters_ReturnsAdapter()
    {
        const string modelSource = """
            namespace TestNamespace.Models;

            public class TestModel
            {
                public string Country { get; set; }

                public string State { get; set; }

                public string StateAbbreviation { get; set; }

                public int CityCode { get; set; }

                public string City { get; set; }

                public int Population { get; set; }

                public Coordinates Coordinates { get; set; }

                public float TotalAreaKm { get; set; }

                public long Epoch { get; set; }

                public float? ETag { get; set; }
            }

            public struct Coordinates
            {
                public double Latitude { get; set; }

                public double Longitude { get; set; }
            }
            """;

        const string adapterSource = """
            using Azure.Data.Tables;
            using FisTech.Persistence.AzureTable;
            using TestNamespace.Models;
            using System;

            namespace TestNamespace.Adapters;

            [NameChange(nameof(TestModel.Epoch), "UnixTimestamp")]
            public partial class TestModelAdapter : AzureTableAdapterBase<TestModel>
            {
                private const float MileUnit = 0.621371f;

                [PartitionKeyConvert(nameof(TestModel.Country), nameof(TestModel.State), nameof(TestModel.StateAbbreviation))]
                private string SetPartitionKey(TestModel item) => $"{item.Country}:{item.StateAbbreviation}:{item.State}";

                [RowKeyConvert(nameof(TestModel.CityCode), nameof(TestModel.City))]
                private string SetRowKey(TestModel item) => $"{item.CityCode:000000}_{item.City}";

                // Does not ignore source property
                [TimestampConvert]
                private DateTimeOffset? SetTimestamp(TestModel item) => item.Epoch != 0 ? DateTimeOffset.FromUnixTimeSeconds(item.Epoch) : null;

                [ETagConvert(nameof(TestModel.ETag))]
                private string? SetETag(TestModel item) => item.ETag?.ToString();

                [Convert(nameof(TestModel.Coordinates))]
                private string SetCoordinates(TestModel item) => $"{item.Coordinates.Latitude:N6},{item.Coordinates.Longitude:N6}";

                // Convert kilometers to miles
                [Convert(nameof(TestModel.TotalAreaKm))]
                private float SetTotalArea(TestModel item) => item.TotalAreaKm * MileUnit;

                [ConvertBack(nameof(TestModel.Country))]
                private string GetCountry(TableEntity entity) => entity.PartitionKey.Split(':')[0];

                [ConvertBack(nameof(TestModel.State))]
                private string GetState(TableEntity entity) => entity.PartitionKey.Split(':')[2];

                [ConvertBack(nameof(TestModel.CityCode))]
                private int GetCityCode(TableEntity entity) => int.Parse(entity.RowKey.Split('_')[0]);

                [ConvertBack(nameof(TestModel.City))]
                private string GetCity(TableEntity entity) => entity.RowKey.Split('_')[1];

                [ConvertBack(nameof(TestModel.Coordinates))]
                private Coordinates GetCoordinates(TableEntity entity)
                {
                    var parts = entity.GetString(nameof(TestModel.Coordinates)).Split(',');
                    return new Coordinates
                    {
                        Latitude = double.Parse(parts[0]),
                        Longitude = double.Parse(parts[1]),
                    };
                }

                [ConvertBack(nameof(TestModel.TotalAreaKm))]
                private float GetTotalArea(TableEntity entity) => (float)entity.GetDouble(nameof(TestModel.TotalAreaKm)).GetValueOrDefault() / MileUnit;

                [ConvertBack(nameof(TestModel.ETag))]
                private float? GetETag(TableEntity entity) => float.TryParse(entity.ETag.ToString(), out var result) ? result : null;
            }
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
                    var entity = new TableEntity(SetPartitionKey(item), SetRowKey(item))
                    {
                        { "Coordinates", SetCoordinates(item) },
                        { "TotalAreaKm", SetTotalArea(item) },
                        { "Population", item.Population },
                        { "UnixTimestamp", item.Epoch },
                    };

                    var timestamp = SetTimestamp(item);
                    if (timestamp != default)
                        entity.Timestamp = timestamp;

                    var etag = SetETag(item);
                    if (etag != default)
                        entity.ETag = new ETag(etag);

                    return entity;
                }

                public TestModel Adapt(TableEntity entity) => new()
                {
                    Country = GetCountry(entity),
                    State = GetState(entity),
                    CityCode = GetCityCode(entity),
                    City = GetCity(entity),
                    Coordinates = GetCoordinates(entity),
                    TotalAreaKm = GetTotalArea(entity),
                    ETag = GetETag(entity),
                    Population = entity.GetInt32("Population").GetValueOrDefault(),
                    Epoch = entity.GetInt64("UnixTimestamp").GetValueOrDefault(),
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