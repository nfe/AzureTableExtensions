using System.Collections.Immutable;

namespace FisTech.Persistence.AzureTable.SourceGenerators;

[Generator]
public class AzureTableAdapterGenerator : ISourceGenerator
{
    // TODO: Use nameof references instead of string literals
    private const string EntityTypeNamespace = "Azure.Data.Tables";
    private const string EntityInterfaceName = "ITableEntity";
    private const string EntityTypeName = "TableEntity";

    private const string AdapterNamespace = "FisTech.Persistence.AzureTable";
    private const string AdapterInterfaceName = "IAzureTableAdapter";
    private const string AdapterBaseTypeName = "AzureTableAdapterBase";

    private const string PartitionKeyAttributeName = "PartitionKeyAttribute";
    private const string RowKeyAttributeName = "RowKeyAttribute";

    private const string IgnoreSourcePropertyAttributeNamedArgument = "IgnoreSourceProperty";

    public void Initialize(GeneratorInitializationContext context) { }

    public void Execute(GeneratorExecutionContext context)
    {
        foreach (INamedTypeSymbol? adapter in context.Compilation.Assembly.GlobalNamespace.DescendantTypeMembers(t =>
            t.BaseType is { Name: AdapterBaseTypeName, IsGenericType: true, Arity: 1 }))
            GenerateAdapter(context, adapter);
    }

    private static void GenerateAdapter(GeneratorExecutionContext context, INamedTypeSymbol adapterSymbol)
    {
        if (!IsValidAdapterClass(context, adapterSymbol))
            return;

        ITypeSymbol sourceSymbol = adapterSymbol.BaseType!.TypeArguments[0];

        IPropertySymbol? partitionKeyProperty = GetSchemaPropertyFromAttribute(adapterSymbol, sourceSymbol,
            PartitionKeyAttributeName, out var ignorePartitionKeySourceProperty);

        if (partitionKeyProperty is null)
        {
            ReportSourcePropertyNotFound(PartitionKeyAttributeName);
            return;
        }

        IPropertySymbol? rowKeyProperty = GetSchemaPropertyFromAttribute(adapterSymbol, sourceSymbol,
            RowKeyAttributeName, out var ignoreRowKeySourceProperty);

        if (rowKeyProperty is null)
        {
            ReportSourcePropertyNotFound(RowKeyAttributeName);
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

        AppendUsingStatements();

        AppendClassDeclaration();

        AppendItemToEntityAdaptMethod();

        AppendEntityToItemAdaptMethod();

        AppendClosingBraces();

        context.AddSource($"{adapterSymbol.Name}.g.cs", SourceText.From(sourceTextBuilder.ToString(), Encoding.UTF8));

        void ReportSourcePropertyNotFound(string attributeName) => context.ReportDiagnostic(
            Diagnostic.Create(DiagnosticDescriptors.SourcePropertyNotFound, adapterSymbol.Locations.FirstOrDefault(),
                adapterSymbol.ToDisplayString(), attributeName));

        void AppendUsingStatements()
        {
            var usingStatements = new HashSet<string>(StringComparer.Ordinal)
            {
                EntityTypeNamespace, AdapterNamespace, sourceSymbol.ContainingNamespace.ToDisplayString()
            };

            foreach (var statement in usingStatements.OrderBy(s => s))
                sourceTextBuilder.AppendLine($"using {statement};");
        }

        void AppendClassDeclaration() => sourceTextBuilder.AppendLine($$"""
            
            namespace {{adapterSymbol.ContainingNamespace.ToDisplayString()}};
            
            public partial class {{adapterSymbol.Name}} : {{AdapterInterfaceName}}<{{sourceSymbol.Name}}>
            {
            """);

        void AppendItemToEntityAdaptMethod()
        {
            sourceTextBuilder.AppendLine($$"""
                    public {{EntityInterfaceName}} Adapt({{sourceSymbol.Name}} item)
                    {
                        var entity = new {{EntityTypeName}}(item.{{partitionKeyProperty.Name}}, item.{{rowKeyProperty.Name}});
                """);

            foreach (IPropertySymbol property in sourceProperties)
            {
                sourceTextBuilder.AppendLine(
                    $$"""        entity.Add(nameof({{sourceSymbol.Name}}.{{property.Name}}), item.{{property.Name}});""");
            }

            sourceTextBuilder.AppendLine("""
                
                        return entity;
                    }
                
                """);
        }

        void AppendEntityToItemAdaptMethod()
        {
            sourceTextBuilder.AppendLine($$"""
                    public {{sourceSymbol.Name}} Adapt({{EntityTypeName}} entity)
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
                var typeName = property.Type.ToString();
                var getMethod = GetTableEntityGetMethod(typeName);

                sourceTextBuilder.AppendLine($$"""
                            item.{{property.Name}} = entity.{{getMethod}}(nameof({{sourceSymbol.Name}}.{{property.Name}}));
                    """);
            }

            sourceTextBuilder.AppendLine("""
                
                        return item;
                    }
                """);
        }

        void AppendClosingBraces() => sourceTextBuilder.Append('}');
    }

    private static bool IsValidAdapterClass(GeneratorExecutionContext context, INamedTypeSymbol adapterSymbol)
    {
        if (adapterSymbol.DeclaredAccessibility is not Accessibility.Public)
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
            attribute?.NamedArguments.FirstOrDefault(n => n.Key == IgnoreSourcePropertyAttributeNamedArgument)
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

    // TODO: Auto convert unsupported types
    private static string GetTableEntityGetMethod(string typeName) => typeName switch
    {
        "string" or "string?" => "GetString",
        "bool" or "bool?" => "GetBoolean",
        "int" or "int?" => "GetInt32",
        "long" or "long?" => "GetInt64",
        "float" or "float?" => "GetDouble",
        "double" or "double?" => "GetDouble",
        "decimal" or "decimal?" => "GetDouble",
        "byte[]" => "GetBinary",
        "System.BinaryData" => "System.GetBinaryData",
        "System.DateTimeOffset" or "System.DateTimeOffset?" => "GetDateTimeOffset",
        "System.Guid" or "System.Guid?" => "GetGuid",
        // TODO: Report diagnostic error
        _ => throw new NotSupportedException($"Unsupported property type '{typeName}'")
    };
}