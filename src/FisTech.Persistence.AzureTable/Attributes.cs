namespace FisTech.Persistence.AzureTable;

[AttributeUsage(AttributeTargets.Class)]
#pragma warning disable CA1710
public abstract class PropertyAttributeBase : Attribute
#pragma warning restore CA1710
{
    protected PropertyAttributeBase(string sourcePropertyName) => SourcePropertyName = sourcePropertyName;

    public string SourcePropertyName { get; }
}

public sealed class PartitionKeyAttribute : PropertyAttributeBase
{
    public PartitionKeyAttribute(string sourcePropertyName) : base(sourcePropertyName) { }
}

public sealed class RowKeyAttribute : PropertyAttributeBase
{
    public RowKeyAttribute(string sourcePropertyName) : base(sourcePropertyName) { }
}