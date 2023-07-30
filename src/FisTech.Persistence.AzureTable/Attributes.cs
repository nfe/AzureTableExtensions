namespace FisTech.Persistence.AzureTable;

#pragma warning disable CA1710 // Identifiers should have correct suffix

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public abstract class PropertyAttributeBase : Attribute
{
    protected PropertyAttributeBase(string sourcePropertyName) => SourcePropertyName = sourcePropertyName;

    public string SourcePropertyName { get; }
}

[AttributeUsage(AttributeTargets.Class)]
public abstract class SchemaPropertyAttributeBase : PropertyAttributeBase
{
    protected SchemaPropertyAttributeBase(string sourcePropertyName) : base(sourcePropertyName) { }

    public bool IgnoreSourceProperty { get; set; } = true;
}

[AttributeUsage(AttributeTargets.Method)]
public abstract class ConvertAttributeBase : Attribute
{
    protected ConvertAttributeBase(params string[] ignoreSourceProperties) =>
        IgnoreSourceProperties = ignoreSourceProperties;

    public string[] IgnoreSourceProperties { get; }
}

#pragma warning restore CA1710

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

public sealed class PartitionKeyConvertAttribute : ConvertAttributeBase
{
    public PartitionKeyConvertAttribute(params string[] ignoreSourceProperties) : base(ignoreSourceProperties) { }
}

public sealed class RowKeyConvertAttribute : ConvertAttributeBase
{
    public RowKeyConvertAttribute(params string[] ignoreSourceProperties) : base(ignoreSourceProperties) { }
}

public sealed class TimestampConvertAttribute : ConvertAttributeBase
{
    public TimestampConvertAttribute(params string[] ignoreSourceProperties) : base(ignoreSourceProperties) { }
}

public sealed class ETagConvertAttribute : ConvertAttributeBase
{
    public ETagConvertAttribute(params string[] ignoreSourceProperties) : base(ignoreSourceProperties) { }
}

public sealed class ConvertAttribute : ConvertAttributeBase
{
#pragma warning disable CA1019
    public ConvertAttribute(string sourcePropertyName) : this(sourcePropertyName, sourcePropertyName) { }
#pragma warning restore CA1019

    public ConvertAttribute(string targetName, params string[] ignoreSourceProperties) : base(ignoreSourceProperties) =>
        TargetName = targetName;

    public string TargetName { get; }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class ConvertBackAttribute : PropertyAttributeBase
{
    public ConvertBackAttribute(string sourcePropertyName) : base(sourcePropertyName) { }
}