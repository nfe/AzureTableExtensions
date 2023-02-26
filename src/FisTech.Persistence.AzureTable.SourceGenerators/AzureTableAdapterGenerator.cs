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

        IPropertySymbol? rowKeyProperty = GetSchemaPropertyFromAttribute(adapterSymbol, sourceSymbol,
            nameof(RowKeyAttribute), out var ignoreRowKeySourceProperty);

        if (rowKeyProperty is null)
        {
            ReportPropertyNotFound(nameof(RowKeyAttribute));
            return;
        }

        var ignoredProperties = new HashSet<IPropertySymbol>(SymbolEqualityComparer.Default);

        if (ignorePartitionKeySourceProperty)
            ignoredProperties.Add(partitionKeyProperty);

        if (ignoreRowKeySourceProperty)
            ignoredProperties.Add(rowKeyProperty);

        // TODO: Check IgnoreAttribute

        ImmutableArray<IPropertySymbol> sourceProperties = sourceSymbol.GetInstancePublicProperties()
            .Where(p => !ignoredProperties.Contains(p))
            .ToImmutableArray();

        var sourceTextBuilder = new StringBuilder();

        // Using statements

        var usingStatements = new HashSet<string>(StringComparer.Ordinal)
        {
            typeof(ITableEntity).Namespace,
            typeof(TableEntity).Namespace,
            typeof(IAzureTableAdapter<>).Namespace,
            sourceSymbol.ContainingNamespace.ToDisplayString()
        };

        foreach (var statement in usingStatements.OrderBy(s => s))
            sourceTextBuilder.AppendLine($"using {statement};");

        // Class declaration

        sourceTextBuilder.AppendLine($$"""
            
            namespace {{adapterSymbol.ContainingNamespace.ToDisplayString()}};
            
            public partial class {{adapterSymbol.Name}} : {{typeof(IAzureTableAdapter<>).GetNameWithoutArity()}}<{{sourceSymbol.Name}}>
            {
            """);

        // Item to entity adapt method

        sourceTextBuilder.AppendLine($$"""
                public {{nameof(ITableEntity)}} Adapt({{sourceSymbol.Name}} item)
                {
                    var entity = new {{nameof(TableEntity)}}(item.{{partitionKeyProperty.Name}}, item.{{rowKeyProperty.Name}});
            """);

        foreach (IPropertySymbol property in sourceProperties)
        {
            var setMethod = GetEntitySetMethod(property.Type, property.Name);

            if (setMethod is null)
            {
                ReportUnsupportedPropertyType(property);
                return;
            }

            sourceTextBuilder.AppendLine(
                $$"""        entity.Add(nameof({{sourceSymbol.Name}}.{{property.Name}}), {{setMethod}});""");
        }

        sourceTextBuilder.AppendLine("""
            
                    return entity;
                }
            """);

        // Entity to item adapt method

        sourceTextBuilder.AppendLine($$"""

                public {{sourceSymbol.Name}} Adapt({{nameof(TableEntity)}} entity)
                {
                    var item = new {{sourceSymbol.Name}}();
            """);

        if (ignorePartitionKeySourceProperty)
        {
            sourceTextBuilder.AppendLine($$"""
                        item.{{partitionKeyProperty.Name}} = entity.PartitionKey;
                """);
        }

        if (ignoreRowKeySourceProperty)
        {
            sourceTextBuilder.AppendLine($$"""
                        item.{{rowKeyProperty.Name}} = entity.RowKey;
                """);
        }

        foreach (IPropertySymbol property in sourceProperties)
        {
            var getMethod = GetEntityGetMethod(property.Type, sourceSymbol.Name, property.Name);

            if (getMethod is null)
            {
                ReportUnsupportedPropertyType(property);
                return;
            }

            sourceTextBuilder.AppendLine($$"""
                        item.{{property.Name}} = {{getMethod}};
                """);
        }

        sourceTextBuilder.Append("""
            
                    return item;
                }
            }
            """);
        
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
            "bool" => $"entity.GetBoolean({property}).Value",
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