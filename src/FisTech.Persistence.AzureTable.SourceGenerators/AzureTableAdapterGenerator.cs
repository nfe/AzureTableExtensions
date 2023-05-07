using Azure;
using Azure.Data.Tables;
using System.Collections.Immutable;

namespace FisTech.Persistence.AzureTable.SourceGenerators;

[Generator]
public class AzureTableAdapterGenerator : ISourceGenerator
{
    private static readonly SymbolDisplayFormat s_defaultDisplayFormat =
        new(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes);

    // TODO: Use symbol types instead of strings
    private static readonly string[] s_supportedTypes =
    {
        "Char", "Char?", "String", "String?", "Boolean", "Boolean?", "Byte", "Byte?", "Int16", "Int16?", "Int32",
        "Int32?", "Int64", "Int64?", "Single", "Single?", "Double", "Double?", "DateTime", "DateTime?",
        "DateTimeOffset", "DateTimeOffset?", "Guid", "Guid?", "Byte[]", "BinaryData"
    };

    private GeneratorExecutionContext _executionContext;
    private AdapterContext _adapterContext;

    public void Initialize(GeneratorInitializationContext context) { }

    public void Execute(GeneratorExecutionContext context)
    {
        _executionContext = context;

        IEnumerable<INamedTypeSymbol> adapters = context.Compilation.Assembly.GlobalNamespace.DescendantTypeMembers(t =>
            t.BaseType is { Name: "AzureTableAdapterBase", IsGenericType: true, Arity: 1 });

        foreach (INamedTypeSymbol adapter in adapters)
            GenerateAdapter(adapter);
    }

    private void GenerateAdapter(INamedTypeSymbol adapter)
    {
        _adapterContext = new AdapterContext(adapter);

        if (!IsValidAdapterClass())
            return;

        if (!TryConfigureAttributes())
            return;

        if (!TryConfigureConverters())
            return;

        if (!TryConfigureProperties())
            return;

        if (!IsValidAdapterContext())
            return;

        GenerateSource();
    }

    private bool IsValidAdapterClass()
    {
        INamedTypeSymbol adapter = _adapterContext.Adapter;

        if (adapter.IsAbstract)
        {
            ReportClassError(DiagnosticDescriptors.InvalidAbstractClass);
            return false;
        }

        if (adapter.IsGenericType)
        {
            ReportClassError(DiagnosticDescriptors.InvalidGenericClass);
            return false;
        }

        if (!adapter.IsPartial())
        {
            ReportClassError(DiagnosticDescriptors.ClassIsNotPartial);
            return false;
        }

        return true;
    }

    private bool TryConfigureAttributes()
    {
        ImmutableArray<AttributeData> attributes = _adapterContext.Adapter.GetAttributes();

        foreach (AttributeData attribute in attributes)
        {
            switch (attribute.AttributeClass?.Name)
            {
                case nameof(PartitionKeyAttribute):
                    if (!TryConfigureSchemaProperty(nameof(ITableEntity.PartitionKey), attribute, new[] { "String" }))
                        return false;

                    break;

                case nameof(RowKeyAttribute):
                    if (!TryConfigureSchemaProperty(nameof(ITableEntity.RowKey), attribute, new[] { "String" }))
                        return false;

                    break;

                case nameof(TimestampAttribute):
                    if (!TryConfigureSchemaProperty(nameof(ITableEntity.Timestamp), attribute,
                        new[] { "DateTimeOffset", "DateTimeOffset?" }))
                        return false;

                    break;

                case nameof(ETagAttribute):
                    if (!TryConfigureSchemaProperty(nameof(ITableEntity.ETag), attribute,
                        new[] { "String", "String?" }))
                        return false;

                    break;

                case nameof(IgnoreAttribute):
                    if (!TryConfigureIgnoredProperty(attribute))
                        return false;

                    break;

                case nameof(NameChangeAttribute):
                    if (!TryConfigureNameChange(attribute))
                        return false;

                    break;

                default:
                    continue;
            }
        }

        return true;
    }

    private bool TryConfigureSchemaProperty(string schemaPropertyName, AttributeData attribute, string[] expectedTypes)
    {
        if (_adapterContext.SchemaPropertiesSetters.ContainsKey(schemaPropertyName))
        {
            ReportDuplicatedProperty(schemaPropertyName, attribute.GetLocation());
            return false;
        }

        if (!TryGetSchemaProperty(attribute, out IPropertySymbol property, out var ignoreSourceProperty))
            return false;

        if (!expectedTypes.Contains(property.Type.ToDisplayString(s_defaultDisplayFormat)))
        {
            ReportPropertyTypeMismatch(schemaPropertyName, expectedTypes, attribute.GetLocation());
            return false;
        }

        _adapterContext.SchemaPropertiesSetters.Add(schemaPropertyName, $"item.{property.Name}");

        if (!ignoreSourceProperty)
            return true;

        var getter = $"entity.{schemaPropertyName}";

        if (schemaPropertyName is nameof(ITableEntity.ETag))
            getter += ".ToString()";

        _adapterContext.Getters.Add(property.Name, getter);
        _adapterContext.IgnoredProperties.Add(property.Name);

        return true;
    }

    private bool TryGetSchemaProperty(AttributeData attribute, out IPropertySymbol property,
        out bool ignoreSourceProperty)
    {
        property = null!;
        ignoreSourceProperty = false;

        var propertyName = (string)attribute.ConstructorArguments[0].Value!;

        if (!TryGetProperty(propertyName, attribute.GetLocation(), out property))
            return false;

        ignoreSourceProperty =
            attribute?.NamedArguments
                .FirstOrDefault(n => n.Key == nameof(SchemaPropertyAttributeBase.IgnoreSourceProperty))
                .Value.Value is not (bool or true);

        return true;
    }

    private bool TryGetProperty(string propertyName, Location? location, out IPropertySymbol property)
    {
        property = null!;

        IPropertySymbol? prop = _adapterContext.Properties.FirstOrDefault(p => p.Name == propertyName);

        if (prop is null)
        {
            ReportPropertyNotFound(propertyName, location);
            return false;
        }

        property = prop;
        return true;
    }

    private bool TryConfigureIgnoredProperty(AttributeData attribute)
    {
        var propertyName = (string)attribute.ConstructorArguments[0].Value!;

        if (!TryGetProperty(propertyName, attribute.GetLocation(), out IPropertySymbol property))
            return false;

        _adapterContext.IgnoredProperties.Add(property.Name);

        return true;
    }

    private bool TryConfigureNameChange(AttributeData attribute)
    {
        var propertyName = (string)attribute.ConstructorArguments[0].Value!;
        var targetName = (string)attribute.ConstructorArguments[1].Value!;

        if (!TryGetProperty(propertyName, attribute.GetLocation(), out IPropertySymbol property))
            return false;

        if (_adapterContext.NameChanges.ContainsKey(property.Name))
        {
            ReportDuplicatePropertyNameChange(property.Name, attribute.GetLocation());
            return false;
        }

        _adapterContext.NameChanges.Add(property.Name, targetName);

        return true;
    }

    private bool TryConfigureConverters()
    {
        ImmutableArray<IMethodSymbol> methods = _adapterContext.Adapter.GetMethods().ToImmutableArray();

        foreach (IMethodSymbol method in methods) { }

        return true;
    }

    private bool TryConfigureProperties()
    {
        IEnumerable<IPropertySymbol> properties =
            _adapterContext.Properties.Where(p => !_adapterContext.IgnoredProperties.Contains(p.Name));

        foreach (IPropertySymbol property in properties)
        {
            var targetName = _adapterContext.NameChanges.TryGetValue(property.Name, out var nameChange)
                ? nameChange
                : property.Name;

            if (!TryConfigurePropertySetter(property, targetName))
                return false;

            if (!TryConfigurePropertyGetter(property, targetName))
                return false;
        }

        return true;
    }

    private bool TryConfigurePropertySetter(IPropertySymbol property, string targetName)
    {
        Location? location = property.Locations.FirstOrDefault();

        if (_adapterContext.Setters.ContainsKey(targetName))
        {
            ReportDuplicatedProperty(targetName, location);
            return false;
        }

        var setter = GetSetter(property.Type, property.Name);

        if (setter is null)
        {
            ReportUnsupportedPropertyType(property.Type.Name, location);
            return false;
        }

        _adapterContext.Setters.Add(targetName, setter);
        return true;
    }

    private static string? GetSetter(ITypeSymbol propertyType, string propertyName)
    {
        if (propertyType.TypeKind == TypeKind.Enum)
            return $"(int)item.{propertyName}";

        if (propertyType is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsNullableTypeKind(TypeKind.Enum))
            return $"(int?)item.{propertyName}";

        var typeName = propertyType.ToDisplayString(s_defaultDisplayFormat);

        return typeName switch
        {
            "Char" => $"item.{propertyName}.ToString()",
            "Char?" => $"item.{propertyName}?.ToString()",
            not null when s_supportedTypes.Contains(typeName) => $"item.{propertyName}",
            _ => null
        };
    }

    private bool TryConfigurePropertyGetter(IPropertySymbol property, string targetName)
    {
        Location? location = property.Locations.FirstOrDefault();

        if (_adapterContext.Getters.ContainsKey(targetName))
        {
            ReportDuplicatedProperty(targetName, location);
            return false;
        }

        var getter = GetGetter(property.Type, targetName);

        if (getter is null)
        {
            ReportUnsupportedPropertyType(property.Type.Name, location);
            return false;
        }

        _adapterContext.Getters.Add(property.Name, getter);
        return true;
    }

    private static string? GetGetter(ITypeSymbol propertyType, string targetName)
    {
        var typeName = propertyType.ToDisplayString(new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces));

        if (propertyType.TypeKind == TypeKind.Enum)
            return $"({typeName})entity.GetInt32(\"{targetName}\").GetValueOrDefault()";

        if (propertyType is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsNullableTypeKind(TypeKind.Enum))
            return $"({typeName})entity.GetInt32(\"{targetName}\")";

        typeName = propertyType.ToDisplayString(s_defaultDisplayFormat);

        return typeName switch
        {
            "Char" => $"entity.GetString(\"{targetName}\")[0]",
            "Char?" => $"entity.GetString(\"{targetName}\")?[0]",
            "String" or "String?" => $"entity.GetString(\"{targetName}\")",
            "Boolean" => $"entity.GetBoolean(\"{targetName}\").GetValueOrDefault()",
            "Boolean?" => $"entity.GetBoolean(\"{targetName}\")",
            "Byte" => $"(byte)entity.GetInt32(\"{targetName}\").GetValueOrDefault()",
            "Byte?" => $"(byte?)entity.GetInt32(\"{targetName}\")",
            "Int16" => $"(short)entity.GetInt32(\"{targetName}\").GetValueOrDefault()",
            "Int16?" => $"(short?)entity.GetInt32(\"{targetName}\")",
            "Int32" => $"entity.GetInt32(\"{targetName}\").GetValueOrDefault()",
            "Int32?" => $"entity.GetInt32(\"{targetName}\")",
            "Int64" => $"entity.GetInt64(\"{targetName}\").GetValueOrDefault()",
            "Int64?" => $"entity.GetInt64(\"{targetName}\")",
            "Single" => $"(float)entity.GetDouble(\"{targetName}\").GetValueOrDefault()",
            "Single?" => $"(float?)entity.GetDouble(\"{targetName}\")",
            "Double" => $"entity.GetDouble(\"{targetName}\").GetValueOrDefault()",
            "Double?" => $"entity.GetDouble(\"{targetName}\")",
            "DateTimeOffset" => $"entity.GetDateTimeOffset(\"{targetName}\").GetValueOrDefault()",
            "DateTimeOffset?" => $"entity.GetDateTimeOffset(\"{targetName}\")",
            "Guid" => $"entity.GetGuid(\"{targetName}\").GetValueOrDefault()",
            "Guid?" => $"entity.GetGuid(\"{targetName}\")",
            "Byte[]" => $"entity.GetBinary(\"{targetName}\")",
            "BinaryData" => $"entity.GetBinaryData(\"{targetName}\")",
            _ => null
        };
    }

    private bool IsValidAdapterContext()
    {
        if (!_adapterContext.SchemaPropertiesSetters.ContainsKey(nameof(ITableEntity.PartitionKey)))
        {
            ReportRequiredPropertyNotFound(nameof(ITableEntity.PartitionKey));
            return false;
        }

        if (!_adapterContext.SchemaPropertiesSetters.ContainsKey(nameof(ITableEntity.RowKey)))
        {
            ReportRequiredPropertyNotFound(nameof(ITableEntity.RowKey));
            return false;
        }

        return true;
    }

    private void GenerateSource()
    {
        var sourceTextBuilder = new StringBuilder();

        var usingStatements = new HashSet<string>(StringComparer.Ordinal)
        {
            typeof(ITableEntity).Namespace,
            typeof(TableEntity).Namespace,
            typeof(IAzureTableAdapter<>).Namespace,
            _adapterContext.ItemType.ContainingNamespace.ToDisplayString()
        };

        GenerateClassDeclaration();

        GenerateItemToEntityAdapter();

        GenerateEntityToItemAdapter();

        GenerateUsingStatements();

        var sourceText = sourceTextBuilder.ToString();

        _executionContext.AddSource($"{_adapterContext.Adapter.Name}.g.cs", SourceText.From(sourceText, Encoding.UTF8));

        void GenerateClassDeclaration()
        {
            sourceTextBuilder.AppendLine($$"""

            namespace {{_adapterContext.Adapter.ContainingNamespace.ToDisplayString()}};

            public partial class {{_adapterContext.Adapter.Name}} : IAzureTableAdapter<{{_adapterContext.ItemType.Name}}>
            {
            """);
        }

        void GenerateItemToEntityAdapter()
        {
            var partitionKeySetter = _adapterContext.SchemaPropertiesSetters[nameof(ITableEntity.PartitionKey)];
            var rowKeySetter = _adapterContext.SchemaPropertiesSetters[nameof(ITableEntity.RowKey)];

            sourceTextBuilder.Append($$"""
                public ITableEntity Adapt({{_adapterContext.ItemType.Name}} item)
                {
                    var entity = new TableEntity({{partitionKeySetter}}, {{rowKeySetter}})
            """);

            if (_adapterContext.Setters.Count == 0)
                sourceTextBuilder.AppendLine(";");
            else
            {
                sourceTextBuilder.AppendLine("\r\n        {");

                foreach (KeyValuePair<string, string> setter in _adapterContext.Setters)
                    sourceTextBuilder.AppendLine($$"""
                                { "{{setter.Key}}", {{setter.Value}} },
                    """);

                sourceTextBuilder.AppendLine("        };");
            }

            if (_adapterContext.SchemaPropertiesSetters.TryGetValue(nameof(ITableEntity.Timestamp),
                    out var timestampSetter))
                // Apply default comparison to avoid unnecessary serialization
                sourceTextBuilder.AppendLine($$"""

                        if ({{timestampSetter}} != default)
                            entity.Timestamp = {{timestampSetter}};
                """);

            if (_adapterContext.SchemaPropertiesSetters.TryGetValue(nameof(ITableEntity.ETag), out var etagSetter))
            {
                usingStatements.Add(typeof(ETag).Namespace);

                // Apply default comparison to avoid unnecessary serialization
                sourceTextBuilder.AppendLine($$"""

                        if ({{etagSetter}} != default)
                            entity.ETag = new ETag({{etagSetter}});
                """);
            }

            sourceTextBuilder.AppendLine("""

                    return entity;
                }
            """);
        }

        void GenerateEntityToItemAdapter()
        {
            sourceTextBuilder.AppendLine($$"""

                public {{_adapterContext.ItemType.Name}} Adapt(TableEntity entity) => new()
                {
            """);

            foreach (KeyValuePair<string, string> getter in _adapterContext.Getters)
                sourceTextBuilder.AppendLine($$"""
                        {{getter.Key}} = {{getter.Value}},
                """);

            sourceTextBuilder.Append("""
                };
            }
            """);
        }

        void GenerateUsingStatements()
        {
            foreach (var statement in usingStatements.OrderByDescending(s => s))
                sourceTextBuilder.Insert(0, $"using {statement};\r\n");
        }
    }

    private void ReportClassError(DiagnosticDescriptor descriptor) => _executionContext.ReportDiagnostic(
        Diagnostic.Create(descriptor, _adapterContext.Location, _adapterContext.DisplayString));

    private void ReportRequiredPropertyNotFound(string propertyName) => _executionContext.ReportDiagnostic(
        Diagnostic.Create(DiagnosticDescriptors.RequiredPropertyNotFound, _adapterContext.Location, propertyName,
            _adapterContext.DisplayString));

    private void ReportDuplicatedProperty(string propertyName, Location? location) =>
        _executionContext.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.DuplicateProperty,
            location ?? _adapterContext.Location, propertyName, _adapterContext.DisplayString));

    private void ReportPropertyNotFound(string propertyName, Location? location) => _executionContext.ReportDiagnostic(
        Diagnostic.Create(DiagnosticDescriptors.PropertyNotFound, location ?? _adapterContext.Location, propertyName,
            _adapterContext.DisplayString));

    private void ReportPropertyTypeMismatch(string propertyName, string[] expectedTypes, Location? location) =>
        _executionContext.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.PropertyTypeMismatch,
            location ?? _adapterContext.Location, propertyName, string.Join(" or ", expectedTypes),
            _adapterContext.DisplayString));

    private void ReportUnsupportedPropertyType(string actualType, Location? location) =>
        _executionContext.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.UnsupportedPropertyType,
            location ?? _adapterContext.Location, actualType, _adapterContext.DisplayString));

    private void ReportConverterSignatureMismatch(Location location) => _executionContext.ReportDiagnostic(
        Diagnostic.Create(DiagnosticDescriptors.ConverterSignatureMismatch, location,
            _adapterContext.ItemType.ToDisplayString(), _adapterContext.DisplayString));

    private void ReportDuplicatePropertyNameChange(string propertyName, Location? location) =>
        _executionContext.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.DuplicatePropertyNameChange,
            location, propertyName, _adapterContext.DisplayString));

    private struct AdapterContext
    {
        public AdapterContext(INamedTypeSymbol adapter)
        {
            Adapter = adapter;
            Location = Adapter.Locations.First();
            DisplayString = Adapter.ToDisplayString();
            ItemType = Adapter.BaseType!.TypeArguments[0];
            Properties = ItemType.GetInstancePublicProperties().ToImmutableArray();
        }

        public INamedTypeSymbol Adapter { get; }

        public Location Location { get; }

        public string DisplayString { get; }

        public ITypeSymbol ItemType { get; }

        public Dictionary<string, string> SchemaPropertiesSetters { get; } = new(StringComparer.Ordinal);

        public ImmutableArray<IPropertySymbol> Properties { get; }

        public Dictionary<string, string> Setters { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, string> Getters { get; } = new(StringComparer.Ordinal);

        public HashSet<string> IgnoredProperties { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, string> NameChanges { get; } = new(StringComparer.Ordinal);
    }
}