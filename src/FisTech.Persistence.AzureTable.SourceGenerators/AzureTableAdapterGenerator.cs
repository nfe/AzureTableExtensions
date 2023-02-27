using Azure;
using Azure.Data.Tables;
using System.Collections.Immutable;

namespace FisTech.Persistence.AzureTable.SourceGenerators;

[Generator]
public class AzureTableAdapterGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context) { }

    public void Execute(GeneratorExecutionContext context)
    {
        var adapterBaseTypeName = typeof(AzureTableAdapterBase<>).GetNameWithoutArity();

        foreach (INamedTypeSymbol? adapter in context.Compilation.Assembly.GlobalNamespace.DescendantTypeMembers(t =>
            t.BaseType?.Name == adapterBaseTypeName && t.BaseType.IsGenericType && t.BaseType.Arity == 1))
            GenerateAdapter(context, adapter);
    }

    private static void GenerateAdapter(GeneratorExecutionContext context, INamedTypeSymbol adapterSymbol)
    {
        if (!IsValidAdapterClass(context, adapterSymbol))
            return;

        ITypeSymbol sourceSymbol = adapterSymbol.BaseType!.TypeArguments[0];

        IPropertySymbol? partitionKeyProperty = GetSchemaPropertyFromAttribute(adapterSymbol, sourceSymbol,
            nameof(PartitionKeyAttribute), out var partitionKeyPropertyName, out var ignorePartitionKeySourceProperty);

        if (partitionKeyProperty is null)
        {
            ReportPropertyNotFound(partitionKeyPropertyName, nameof(PartitionKeyAttribute));
            return;
        }

        if (partitionKeyProperty.Type.ToString() is not "string")
        {
            ReportPropertyTypeMismatch(nameof(PartitionKeyAttribute), "string");
            return;
        }

        IPropertySymbol? rowKeyProperty = GetSchemaPropertyFromAttribute(adapterSymbol, sourceSymbol,
            nameof(RowKeyAttribute), out var rowKeyPropertyName, out var ignoreRowKeySourceProperty);

        if (rowKeyProperty is null)
        {
            ReportPropertyNotFound(rowKeyPropertyName, nameof(RowKeyAttribute));
            return;
        }

        if (rowKeyProperty.Type.ToString() is not "string")
        {
            ReportPropertyTypeMismatch(nameof(RowKeyAttribute), "string");
            return;
        }

        IPropertySymbol? timestampProperty = GetSchemaPropertyFromAttribute(adapterSymbol, sourceSymbol,
            nameof(TimestampAttribute), out _, out var ignoreTimestampSourceProperty);

        if (timestampProperty is not null
            && timestampProperty.Type.ToString() is not "System.DateTimeOffset?" or "System.DateTimeOffset")
        {
            ReportPropertyTypeMismatch(nameof(TimestampAttribute), "System.DateTimeOffset? or System.DateTimeOffset");
            return;
        }

        IPropertySymbol? eTagProperty = GetSchemaPropertyFromAttribute(adapterSymbol, sourceSymbol,
            nameof(ETagAttribute), out _, out var ignoreETagSourceProperty);

        if (eTagProperty is not null && eTagProperty.Type.ToString() is not "string?" or "string")
        {
            ReportPropertyTypeMismatch(nameof(ETagAttribute), "string? or string");
            return;
        }

        var ignoredProperties = new HashSet<IPropertySymbol>(SymbolEqualityComparer.Default);

        foreach (AttributeData ignoreAttribute in adapterSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.Name == nameof(IgnoreAttribute)))
        {
            IPropertySymbol? property = GetPropertyFromAttribute(sourceSymbol, ignoreAttribute, out var propertyName);

            if (property is null)
            {
                ReportPropertyNotFound(propertyName, nameof(IgnoreAttribute));
                return;
            }

            ignoredProperties.Add(property);
        }

        if (ignorePartitionKeySourceProperty)
            ignoredProperties.Add(partitionKeyProperty);

        if (ignoreRowKeySourceProperty)
            ignoredProperties.Add(rowKeyProperty);

        if (timestampProperty is not null && ignoreTimestampSourceProperty)
            ignoredProperties.Add(timestampProperty);

        if (eTagProperty is not null && ignoreETagSourceProperty)
            ignoredProperties.Add(eTagProperty);

        ImmutableArray<IPropertySymbol> sourceProperties = sourceSymbol.GetInstancePublicProperties()
            .Where(p => !ignoredProperties.Contains(p))
            .ToImmutableArray();

        var nameChanges = new Dictionary<IPropertySymbol, string>(SymbolEqualityComparer.Default);

        foreach (AttributeData nameChangeAttribute in adapterSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.Name == nameof(NameChangeAttribute)))
        {
            IPropertySymbol? property = GetPropertyFromAttribute(sourceSymbol, nameChangeAttribute,
                out var propertyName);

            if (property is null)
            {
                ReportPropertyNotFound(propertyName, nameof(NameChangeAttribute));
                return;
            }

            if (nameChanges.ContainsKey(property))
            {
                ReportDuplicateNameChangeProperty(property.Name);
                return;
            }

            if (nameChangeAttribute.ConstructorArguments[1].Value is not string targetName
                || string.IsNullOrWhiteSpace(targetName))
            {
                ReportInvalidNameChangeTargetName(property.Name);
                return;
            }

            if (nameChanges.ContainsValue(targetName))
            {
                ReportDuplicateNameChangeTargetName(targetName);
                return;
            }

            nameChanges.Add(property, targetName);
        }

        KeyValuePair<IPropertySymbol, string> nameChangeTargetNameConflict = nameChanges.FirstOrDefault(n =>
        {
            IPropertySymbol? property = sourceProperties.FirstOrDefault(p => p.Name == n.Value);

            if (property is null)
                return false;

            return !nameChanges.ContainsKey(property);
        });

        if (nameChangeTargetNameConflict.Key != default)
        {
            ReportNameChangeTargetNameConflict(nameChangeTargetNameConflict.Value);
            return;
        }

        // TODO: Validate all source properties types are supported and segregate source generation
        var sourceTextBuilder = new StringBuilder();

        var usingStatements = new HashSet<string>(StringComparer.Ordinal)
        {
            typeof(ITableEntity).Namespace,
            typeof(TableEntity).Namespace,
            typeof(IAzureTableAdapter<>).Namespace,
            sourceSymbol.ContainingNamespace.ToDisplayString()
        };

        // Class declaration

        sourceTextBuilder.AppendLine($$"""
            
            namespace {{adapterSymbol.ContainingNamespace.ToDisplayString()}};
            
            public partial class {{adapterSymbol.Name}} : {{typeof(IAzureTableAdapter<>).GetNameWithoutArity()}}<{{sourceSymbol.Name}}>
            {
            """);

        // Item to entity adapt method

        sourceTextBuilder.Append($$"""    
                public {{nameof(ITableEntity)}} Adapt({{sourceSymbol.Name}} item)
                {
                    var entity = new {{nameof(TableEntity)}}(item.{{partitionKeyProperty.Name}}, item.{{rowKeyProperty.Name}})
            """);

        if (sourceProperties.Length == 0)
            sourceTextBuilder.AppendLine(";");
        else
        {
            sourceTextBuilder.AppendLine("\r\n        {");

            foreach (IPropertySymbol property in sourceProperties)
            {
                var setMethod = GetEntitySetMethod(property.Type, property.Name);

                if (setMethod is null)
                {
                    ReportUnsupportedPropertyType(property);
                    return;
                }

                var key = nameChanges.TryGetValue(property, out var targetName)
                    ? $"\"{targetName}\""
                    : $"nameof({sourceSymbol.Name}.{property.Name})";

                sourceTextBuilder.AppendLine($$"""            { {{key}}, {{setMethod}} },""");
            }

            sourceTextBuilder.AppendLine("        };");
        }

        if (timestampProperty is not null)
            // Apply default comparison to avoid unnecessary serialization
            sourceTextBuilder.AppendLine($$"""

                        if (item.{{timestampProperty.Name}} != default)
                            entity.Timestamp = item.{{timestampProperty.Name}};
                """);

        if (eTagProperty is not null)
        {
            usingStatements.Add(typeof(ETag).Namespace);

            // Apply default comparison to avoid unnecessary serialization
            sourceTextBuilder.AppendLine($$"""

                        if (item.{{eTagProperty.Name}} != default)
                            entity.ETag = new ETag(item.{{eTagProperty.Name}});
                """);
        }

        sourceTextBuilder.AppendLine("""

                    return entity;
                }
            """);

        // Entity to item adapt method

        sourceTextBuilder.AppendLine($$"""

                public {{sourceSymbol.Name}} Adapt({{nameof(TableEntity)}} entity) => new()
                {
            """);

        if (ignorePartitionKeySourceProperty)
            sourceTextBuilder.AppendLine($$"""        {{partitionKeyProperty.Name}} = entity.PartitionKey,""");

        if (ignoreRowKeySourceProperty)
            sourceTextBuilder.AppendLine($$"""        {{rowKeyProperty.Name}} = entity.RowKey,""");

        if (timestampProperty is not null && ignoreTimestampSourceProperty)
            sourceTextBuilder.AppendLine($$"""        {{timestampProperty.Name}} = entity.Timestamp,""");

        if (eTagProperty is not null && ignoreETagSourceProperty)
            sourceTextBuilder.AppendLine($$"""        {{eTagProperty.Name}} = entity.ETag.ToString(),""");

        foreach (IPropertySymbol property in sourceProperties)
        {
            var key = nameChanges.TryGetValue(property, out var targetName)
                ? $"\"{targetName}\""
                : $"nameof({sourceSymbol.Name}.{property.Name})";

            var getMethod = GetEntityGetMethod(property.Type, key);

            if (getMethod is null)
            {
                ReportUnsupportedPropertyType(property);
                return;
            }

            sourceTextBuilder.AppendLine($$"""        {{property.Name}} = {{getMethod}},""");
        }

        sourceTextBuilder.Append("""
                };
            }
            """);

        // Using statements

        foreach (var statement in usingStatements.OrderByDescending(s => s))
            sourceTextBuilder.Insert(0, $"using {statement};\r\n");

        var sourceText = sourceTextBuilder.ToString();

        context.AddSource($"{adapterSymbol.Name}.g.cs", SourceText.From(sourceText, Encoding.UTF8));

        void ReportPropertyNotFound(string? propertyName, string attributeName) => context.ReportDiagnostic(
            Diagnostic.Create(DiagnosticDescriptors.PropertyNotFound, adapterSymbol.Locations.FirstOrDefault(),
                propertyName ?? "null", attributeName, adapterSymbol.ToDisplayString()));

        void ReportPropertyTypeMismatch(string attributeName, string expectedType) => context.ReportDiagnostic(
            Diagnostic.Create(DiagnosticDescriptors.PropertyTypeMismatch, adapterSymbol.Locations.FirstOrDefault(),
                attributeName, expectedType, adapterSymbol.ToDisplayString()));

        void ReportUnsupportedPropertyType(IPropertySymbol propertySymbol) => context.ReportDiagnostic(
            Diagnostic.Create(DiagnosticDescriptors.UnsupportedPropertyType, adapterSymbol.Locations.FirstOrDefault(),
                adapterSymbol.ToDisplayString(), propertySymbol.Type.ToDisplayString(), propertySymbol.Name));

        void ReportDuplicateNameChangeProperty(string propertyName) => context.ReportDiagnostic(
            Diagnostic.Create(DiagnosticDescriptors.DuplicateNameChangeProperty,
                adapterSymbol.Locations.FirstOrDefault(), propertyName, adapterSymbol.ToDisplayString()));

        void ReportInvalidNameChangeTargetName(string propertyName) => context.ReportDiagnostic(
            Diagnostic.Create(DiagnosticDescriptors.InvalidNameChangeTargetName,
                adapterSymbol.Locations.FirstOrDefault(), propertyName, adapterSymbol.ToDisplayString()));

        void ReportDuplicateNameChangeTargetName(string targetName) => context.ReportDiagnostic(
            Diagnostic.Create(DiagnosticDescriptors.DuplicateNameChangeTargetName,
                adapterSymbol.Locations.FirstOrDefault(), targetName, adapterSymbol.ToDisplayString()));

        void ReportNameChangeTargetNameConflict(string targetName) => context.ReportDiagnostic(
            Diagnostic.Create(DiagnosticDescriptors.NameChangeTargetNameConflict,
                adapterSymbol.Locations.FirstOrDefault(), targetName, adapterSymbol.ToDisplayString()));
    }

    private static bool IsValidAdapterClass(GeneratorExecutionContext context, INamedTypeSymbol adapterSymbol)
    {
        if (adapterSymbol.IsAbstract)
        {
            ReportClassError(DiagnosticDescriptors.InvalidAbstractClass);
            return false;
        }

        if (adapterSymbol.IsGenericType)
        {
            ReportClassError(DiagnosticDescriptors.InvalidGenericClass);
            return false;
        }

        if (!adapterSymbol.IsPartial())
        {
            ReportClassError(DiagnosticDescriptors.ClassIsNotPartial);
            return false;
        }

        return true;

        void ReportClassError(DiagnosticDescriptor descriptor) => context.ReportDiagnostic(Diagnostic.Create(descriptor,
            adapterSymbol.Locations.FirstOrDefault(), adapterSymbol.ToDisplayString()));
    }

    private static IPropertySymbol? GetSchemaPropertyFromAttribute(INamedTypeSymbol adapterSymbol,
        ITypeSymbol sourceTypeSymbol, string attributeName, out string? propertyName, out bool ignoreSourceProperty)
    {
        AttributeData? attribute = adapterSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == attributeName);

        ignoreSourceProperty =
            attribute?.NamedArguments
                .FirstOrDefault(n => n.Key == nameof(SchemaPropertyAttributeBase.IgnoreSourceProperty))
                .Value.Value is not (bool or true);

        return GetPropertyFromAttribute(sourceTypeSymbol, attribute, out propertyName);
    }

    private static IPropertySymbol? GetPropertyFromAttribute(INamedTypeSymbol adapterSymbol,
        ITypeSymbol sourceTypeSymbol, string attributeName, out string? propertyName)
    {
        AttributeData? attribute = adapterSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == attributeName);

        return GetPropertyFromAttribute(sourceTypeSymbol, attribute, out propertyName);
    }

    private static IPropertySymbol? GetPropertyFromAttribute(ITypeSymbol sourceTypeSymbol, AttributeData? attribute,
        out string? propertyName)
    {
        var sourcePropertyName = attribute?.ConstructorArguments[0].Value as string;
        propertyName = sourcePropertyName;

        return sourcePropertyName is not null
            ? sourceTypeSymbol.GetInstancePublicProperties().FirstOrDefault(p => p.Name == sourcePropertyName)
            : null;
    }

    private static string? GetEntitySetMethod(ITypeSymbol typeSymbol, string propertyName)
    {
        if (typeSymbol.TypeKind == TypeKind.Enum)
            return $"(int)item.{propertyName}";

        if (typeSymbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsNullableTypeKind(TypeKind.Enum))
            return $"(int?)item.{propertyName}";

        var typeName = typeSymbol.ToString();

        return typeName switch
        {
            "char" => $"item.{propertyName}.ToString()",
            "char?" => $"item.{propertyName}?.ToString()",
            "string" or "string?" or "bool" or "bool?" or "byte" or "byte?" or "short" or "short?" or "int" or "int?"
                or "long" or "long?" or "float" or "float?" or "double" or "double?" or "System.DateTime"
                or "System.DateTime?" or "System.DateTimeOffset" or "System.DateTimeOffset?" or "System.Guid"
                or "System.Guid?" or "byte[]" or "System.BinaryData" => $"item.{propertyName}",
            _ => null
        };
    }

    private static string? GetEntityGetMethod(ITypeSymbol typeSymbol, string key)
    {
        var typeName = typeSymbol.ToString();

        if (typeSymbol.TypeKind == TypeKind.Enum)
            return $"({typeName})entity.GetInt32({key}).GetValueOrDefault()";

        if (typeSymbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsNullableTypeKind(TypeKind.Enum))
            return $"({typeName})entity.GetInt32({key})";

        // TODO: Use symbol instead of string literals
        return typeName switch
        {
            "char" => $"entity.GetString({key})[0]",
            "char?" => $"entity.GetString({key})?[0]",
            "string" or "string?" => $"entity.GetString({key})",
            "bool" => $"entity.GetBoolean({key}).GetValueOrDefault()",
            "bool?" => $"entity.GetBoolean({key})",
            "byte" => $"(byte)entity.GetInt32({key}).GetValueOrDefault()",
            "byte?" => $"(byte?)entity.GetInt32({key})",
            "short" => $"(short)entity.GetInt32({key}).GetValueOrDefault()",
            "short?" => $"(short?)entity.GetInt32({key})",
            "int" => $"entity.GetInt32({key}).GetValueOrDefault()",
            "int?" => $"entity.GetInt32({key})",
            "long" => $"entity.GetInt64({key}).GetValueOrDefault()",
            "long?" => $"entity.GetInt64({key})",
            "float" => $"(float)entity.GetDouble({key}).GetValueOrDefault()",
            "float?" => $"(float?)entity.GetDouble({key})",
            "double" => $"entity.GetDouble({key}).GetValueOrDefault()",
            "double?" => $"entity.GetDouble({key})",
            "System.DateTimeOffset" => $"entity.GetDateTimeOffset({key}).GetValueOrDefault()",
            "System.DateTimeOffset?" => $"entity.GetDateTimeOffset({key})",
            "System.Guid" => $"entity.GetGuid({key}).GetValueOrDefault()",
            "System.Guid?" => $"entity.GetGuid({key})",
            "byte[]" => $"entity.GetBinary({key})",
            "System.BinaryData" => $"entity.GetBinaryData({key})",
            _ => null
        };
    }
}