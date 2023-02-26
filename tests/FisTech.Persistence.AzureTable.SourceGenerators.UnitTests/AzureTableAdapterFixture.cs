using Azure.Data.Tables;

namespace FisTech.Persistence.AzureTable.SourceGenerators.UnitTests;

public class AzureTableAdapterFixture
{
    public string AzureSdkAssemblyLocation { get; } = typeof(ITableEntity).Assembly.Location;

    public string AdapterAssemblyLocation { get; } = typeof(IAzureTableAdapter<>).Assembly.Location;

    public string BinaryDataAssemblyLocation { get; } = typeof(BinaryData).Assembly.Location;

    public string SimpleModelSource { get; } = """
        namespace TestNamespace.Models;

        public class TestModel
        {
            public string State { get; set; }

            public string Country { get; set; }
        }
        """;

    public string AllTypesModelSource { get; } = """
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
}