namespace FisTech.Persistence.AzureTable;

[AttributeUsage(AttributeTargets.Class)]
#pragma warning disable CA1710
public abstract class PropertyAttributeBase : Attribute
#pragma warning restore CA1710
{
    protected PropertyAttributeBase(string sourcePropertyName) => SourcePropertyName = sourcePropertyName;

    public string SourcePropertyName { get; }
}

public abstract class SchemaPropertyAttributeBase : PropertyAttributeBase
{
    protected SchemaPropertyAttributeBase(string sourcePropertyName) : base(sourcePropertyName) { }

    public bool IgnoreSourceProperty { get; set; } = true;
}

public sealed class PartitionKeyAttribute : SchemaPropertyAttributeBase
{
    public PartitionKeyAttribute(string sourcePropertyName) : base(sourcePropertyName) { }
}

public sealed class RowKeyAttribute : SchemaPropertyAttributeBase
{
    public RowKeyAttribute(string sourcePropertyName) : base(sourcePropertyName) { }
}

public sealed class TimestampAttribute : SchemaPropertyAttributeBase
{
    public TimestampAttribute(string sourcePropertyName) : base(sourcePropertyName) { }
}

public sealed class ETagAttribute : SchemaPropertyAttributeBase
{
    public ETagAttribute(string sourcePropertyName) : base(sourcePropertyName) { }
}