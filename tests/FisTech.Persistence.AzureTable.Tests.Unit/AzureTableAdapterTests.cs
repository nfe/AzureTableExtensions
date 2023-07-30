using Azure;
using Azure.Data.Tables;

// Non-nullable field is uninitialized
#pragma warning disable CS8618

namespace FisTech.Persistence.AzureTable.UnitTests;

public class AzureTableAdapterTests
{
    [Fact]
    public void AdapterBase_SimpleModel_ReturnsAdapter()
    {
        // Arrange
        var item = new SimpleModel { State = "SP", Country = "Brazil" };

        var expected = new TableEntity(item.Country, item.State);

        var sut = new SimpleModelAdapter();

        // Act
        ITableEntity entity = sut.Adapt(item);

        // Assert
        entity.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void AdapterBase_SchemaProperties_ReturnsAdapter()
    {
        // Arrange
        var item = new SchemaPropertiesModel
        {
            MyPartitionKey = "MyPartitionKeyValue",
            MyRowKey = "MyRowKeyValue",
            MyTimestamp = DateTimeOffset.UtcNow,
            MyETag = "MyETagValue"
        };

        var expected = new TableEntity(item.MyPartitionKey, item.MyRowKey)
        {
            Timestamp = item.MyTimestamp, ETag = new ETag(item.MyETag)
        };

        var sut = new SchemaPropertiesAdapter();

        // Act
        ITableEntity entity = sut.Adapt(item);

        // Assert
        entity.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void AdapterBase_DoNotIgnoreSchemaProperties_ReturnsAdapter()
    {
        // Arrange
        var item = new SchemaPropertiesModel
        {
            MyPartitionKey = "MyPartitionKeyValue",
            MyRowKey = "MyRowKeyValue",
            MyTimestamp = DateTimeOffset.UtcNow,
            MyETag = "MyETagValue"
        };

        var expected = new TableEntity(item.MyPartitionKey, item.MyRowKey)
        {
            { "MyPartitionKey", item.MyPartitionKey },
            { "MyRowKey", item.MyRowKey },
            { "MyTimestamp", item.MyTimestamp },
            { "MyETag", item.MyETag }
        };

        expected.Timestamp = item.MyTimestamp;
        expected.ETag = new ETag(item.MyETag);

        var sut = new DoNotIgnoreSchemaPropertiesAdapter();

        // Act
        ITableEntity entity = sut.Adapt(item);

        // Assert
        entity.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void AdapterBase_SupportedTypes_ReturnsAdapter()
    {
        // Arrange
        var item = new SupportedTypesModel();

        var expected = new TableEntity(item.MyString, item.MyString)
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
            { "MyBinaryData", item.MyBinaryData }
        };

        var sut = new SupportedTypesAdapter();

        // Act
        ITableEntity entity = sut.Adapt(item);

        // Assert
        entity.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void AdapterBase_IgnoreNonPublicProperties_ReturnsAdapter()
    {
        // Arrange
        var item = new IgnoreNonPublicPropertiesModel
        {
            MyPartitionKey = "MyPartitionKeyValue", MyRowKey = "MyRowKeyValue"
        };

        var expected = new TableEntity(item.MyPartitionKey, item.MyRowKey);

        var sut = new IgnoreNonPublicPropertiesAdapter();

        // Act
        ITableEntity entity = sut.Adapt(item);

        // Assert
        entity.Should().BeEquivalentTo(expected);
    }
}

#region Models

// ReSharper disable PropertyCanBeMadeInitOnly.Global

public class SimpleModel
{
    public string State { get; set; }

    public string Country { get; set; }
}

public class SchemaPropertiesModel
{
    public string MyPartitionKey { get; set; }

    public string MyRowKey { get; set; }

    public DateTimeOffset? MyTimestamp { get; set; }

    public string? MyETag { get; set; }
}

public enum MyEnum { ValueA, ValueB, ValueC }

public class SupportedTypesModel
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

    public BinaryData MyBinaryData { get; set; } = new(new byte[] { 9, 8, 7, 6, 5 });
}

public class IgnoreNonPublicPropertiesModel
{
    public string MyPartitionKey { get; set; }

    public string MyRowKey { get; set; }

    internal string MyInternal { get; set; }

    private string MyPrivate { get; set; }

    private protected string MyPrivateProtected { get; set; }

    protected string MyProtected { get; set; }

    protected internal string MyProtectedInternal { get; set; }
}

#endregion

#region Adapters

[PartitionKey(nameof(SimpleModel.Country))]
[RowKey(nameof(SimpleModel.State))]
public partial class SimpleModelAdapter : AzureTableAdapterBase<SimpleModel> { }

[PartitionKey(nameof(SchemaPropertiesModel.MyPartitionKey))]
[RowKey(nameof(SchemaPropertiesModel.MyRowKey))]
[Timestamp(nameof(SchemaPropertiesModel.MyTimestamp))]
[ETag(nameof(SchemaPropertiesModel.MyETag))]
public partial class SchemaPropertiesAdapter : AzureTableAdapterBase<SchemaPropertiesModel> { }

[PartitionKey(nameof(SchemaPropertiesModel.MyPartitionKey), IgnoreSourceProperty = false)]
[RowKey(nameof(SchemaPropertiesModel.MyRowKey), IgnoreSourceProperty = false)]
[Timestamp(nameof(SchemaPropertiesModel.MyTimestamp), IgnoreSourceProperty = false)]
[ETag(nameof(SchemaPropertiesModel.MyETag), IgnoreSourceProperty = false)]
public partial class DoNotIgnoreSchemaPropertiesAdapter : AzureTableAdapterBase<SchemaPropertiesModel> { }

[PartitionKey(nameof(SupportedTypesModel.MyString), IgnoreSourceProperty = false)]
[RowKey(nameof(SupportedTypesModel.MyString), IgnoreSourceProperty = false)]
public partial class SupportedTypesAdapter : AzureTableAdapterBase<SupportedTypesModel> { }

[PartitionKey(nameof(IgnoreNonPublicPropertiesModel.MyPartitionKey))]
[RowKey(nameof(IgnoreNonPublicPropertiesModel.MyRowKey))]
public partial class IgnoreNonPublicPropertiesAdapter : AzureTableAdapterBase<IgnoreNonPublicPropertiesModel> { }

#endregion