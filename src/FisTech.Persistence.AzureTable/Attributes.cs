using System.Diagnostics.CodeAnalysis;

namespace FisTech.Persistence.AzureTable;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
[SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix")]
public abstract class PropertyAttributeBase : Attribute
{
    protected PropertyAttributeBase(string sourcePropertyName) => SourcePropertyName = sourcePropertyName;

    public string SourcePropertyName { get; }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
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

public sealed class IgnoreAttribute : PropertyAttributeBase
{
    public IgnoreAttribute(string sourcePropertyName) : base(sourcePropertyName) { }
}

public sealed class NameChangeAttribute : PropertyAttributeBase
{
    public NameChangeAttribute(string sourcePropertyName, string targetName) : base(sourcePropertyName) =>
        TargetName = targetName;

    public string TargetName { get; }
}