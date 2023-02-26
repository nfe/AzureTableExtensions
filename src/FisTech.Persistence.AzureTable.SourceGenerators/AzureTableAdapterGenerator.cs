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
            nameof(PartitionKeyAttribute), out var ignorePartitionKeySourceProperty);

        if (partitionKeyProperty is null)
        {
            ReportPropertyNotFound(nameof(PartitionKeyAttribute));
            return;
        }

        // TODO: Validate PartitionKey property type

        IPropertySymbol? rowKeyProperty = GetSchemaPropertyFromAttribute(adapterSymbol, sourceSymbol,
            nameof(RowKeyAttribute), out var ignoreRowKeySourceProperty);

        if (rowKeyProperty is null)
        {
            ReportPropertyNotFound(nameof(RowKeyAttribute));
            return;
        }

        // TODO: Validate RowKey property type

        IPropertySymbol? timestampProperty = GetSchemaPropertyFromAttribute(adapterSymbol, sourceSymbol,
            nameof(TimestampAttribute), out var ignoreTimestampSourceProperty);

        // TODO: Validate Timestamp property type

        IPropertySymbol? eTagProperty = GetSchemaPropertyFromAttribute(adapterSymbol, sourceSymbol,
            nameof(ETagAttribute), out var ignoreETagSourceProperty);

        // TODO: Validate ETag property type

        var ignoredProperties = new HashSet<IPropertySymbol>(SymbolEqualityComparer.Default);

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

                sourceTextBuilder.AppendLine(
                    $$"""            { nameof({{sourceSymbol.Name}}.{{property.Name}}), {{setMethod}} },""");
            }

            sourceTextBuilder.AppendLine("        };");
        }

        if (timestampProperty is not null)
            // Apply default condition to avoid unnecessary serialization
            sourceTextBuilder.AppendLine($$"""

                        if (item.{{timestampProperty.Name}} != default)
                            entity.Timestamp = item.{{timestampProperty.Name}};
                """);

        if (eTagProperty is not null)
        {
            usingStatements.Add(typeof(ETag).Namespace);
            
            // Apply default condition to avoid unnecessary serialization
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
            var getMethod = GetEntityGetMethod(property.Type, sourceSymbol.Name, property.Name);

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

        void ReportPropertyNotFound(string attributeName) => context.ReportDiagnostic(
            Diagnostic.Create(DiagnosticDescriptors.PropertyNotFound, adapterSymbol.Locations.FirstOrDefault(),
                adapterSymbol.ToDisplayString(), attributeName));

        void ReportUnsupportedPropertyType(IPropertySymbol propertySymbol) => context.ReportDiagnostic(
            Diagnostic.Create(DiagnosticDescriptors.UnsupportedPropertyType, adapterSymbol.Locations.FirstOrDefault(),
                adapterSymbol.ToDisplayString(), propertySymbol.Name, propertySymbol.Type.ToDisplayString()));
    }

    private static bool IsValidAdapterClass(GeneratorExecutionContext context, INamedTypeSymbol adapterSymbol)
    {
        if (adapterSymbol.DeclaredAccessibility is not Accessibility.Public or Accessibility.Internal)
        {
            ReportClassError(DiagnosticDescriptors.InvalidClassAccessibility);
            return false;
        }

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
        ITypeSymbol sourceTypeSymbol, string attributeName, out bool ignoreSourceProperty)
    {
        AttributeData? attribute = adapterSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == attributeName);

        ignoreSourceProperty =
            attribute?.NamedArguments
                .FirstOrDefault(n => n.Key == nameof(SchemaPropertyAttributeBase.IgnoreSourceProperty))
                .Value.Value is not (bool or true);

        return attribute is not null ? GetPropertyFromAttribute(sourceTypeSymbol, attribute) : null;
    }

    private static IPropertySymbol? GetPropertyFromAttribute(INamedTypeSymbol adapterSymbol,
        ITypeSymbol sourceTypeSymbol, string attributeName)
    {
        AttributeData? attribute = adapterSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == attributeName);

        return attribute is not null ? GetPropertyFromAttribute(sourceTypeSymbol, attribute) : null;
    }

    private static IPropertySymbol? GetPropertyFromAttribute(ITypeSymbol sourceTypeSymbol, AttributeData attribute)
    {
        // TODO: Validate PropertyAttributeBase base type 

        var propertyName = (string)attribute.ConstructorArguments[0].Value!;

        return sourceTypeSymbol.GetInstancePublicProperties().FirstOrDefault(p => p.Name == propertyName);
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

    private static string? GetEntityGetMethod(ITypeSymbol typeSymbol, string sourceSymbolName, string propertyName)
    {
        var property = $"nameof({sourceSymbolName}.{propertyName})";
        var typeName = typeSymbol.ToString();

        if (typeSymbol.TypeKind == TypeKind.Enum)
            return $"({typeName})entity.GetInt32({property}).Value";

        if (typeSymbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsNullableTypeKind(TypeKind.Enum))
            return $"({typeName})entity.GetInt32({property})";

        return typeName switch
        {
            "char" => $"entity.GetString({property})[0]",
            "char?" => $"entity.GetString({property})?[0]",
            "string" or "string?" => $"entity.GetString({property})",
            "bool" => $"entity.GetBoolean({property}).Value", // TODO: Check for null
            "bool?" => $"entity.GetBoolean({property})",
            "byte" => $"(byte)entity.GetInt32({property}).Value",
            "byte?" => $"(byte?)entity.GetInt32({property})",
            "short" => $"(short)entity.GetInt32({property}).Value",
            "short?" => $"(short?)entity.GetInt32({property})",
            "int" => $"entity.GetInt32({property}).Value",
            "int?" => $"entity.GetInt32({property})",
            "long" => $"entity.GetInt64({property}).Value",
            "long?" => $"entity.GetInt64({property})",
            "float" => $"(float)entity.GetDouble({property}).Value",
            "float?" => $"(float?)entity.GetDouble({property})",
            "double" => $"entity.GetDouble({property}).Value",
            "double?" => $"entity.GetDouble({property})",
            "System.DateTimeOffset" => $"entity.GetDateTimeOffset({property}).Value",
            "System.DateTimeOffset?" => $"entity.GetDateTimeOffset({property})",
            "System.Guid" => $"entity.GetGuid({property}).Value",
            "System.Guid?" => $"entity.GetGuid({property})",
            "byte[]" => $"entity.GetBinary({property})",
            "System.BinaryData" => $"entity.GetBinaryData({property})",
            _ => null
        };
    }
}