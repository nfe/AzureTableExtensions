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

        ITypeSymbol sourceType = adapterSymbol.BaseType!.TypeArguments[0];
        ImmutableArray<AttributeData> adapterAttributes = adapterSymbol.GetAttributes();
        ImmutableArray<IPropertySymbol> sourceProperties = sourceType.GetInstancePublicProperties().ToImmutableArray();
        var ignoreProperties = new HashSet<IPropertySymbol>(SymbolEqualityComparer.Default);

        if (!TryGetSchemaPropertyFromAdapterAttributes(PartitionKeyAttributeName,
            out IPropertySymbol? partitionKeyProperty, out var ignorePartitionKeySourceProperty))
            return;

        if (ignorePartitionKeySourceProperty)
            ignoreProperties.Add(partitionKeyProperty!);

        if (!TryGetSchemaPropertyFromAdapterAttributes(RowKeyAttributeName, out IPropertySymbol? rowKeyProperty,
            out var ignoreRowKeySourceProperty))
            return;

        if (ignoreRowKeySourceProperty)
            ignoreProperties.Add(rowKeyProperty!);

        var sourceTextBuilder = new StringBuilder();

        AppendUsingStatements();

        AppendClassDeclaration();

        AppendItemToEntityAdaptMethod();

        AppendEntityToItemAdaptMethod();

        AppendClosingBraces();

        context.AddSource($"{adapterSymbol.Name}.g.cs", SourceText.From(sourceTextBuilder.ToString(), Encoding.UTF8));

        bool TryGetSchemaPropertyFromAdapterAttributes(string attributeName, out IPropertySymbol? sourcePropertySymbol,
            out bool ignoreSourceProperty) => TryGetSchemaPropertyFromAttribute(context, adapterSymbol,
            sourceProperties, adapterAttributes, attributeName, out sourcePropertySymbol, out ignoreSourceProperty);

        void AppendUsingStatements()
        {
            var usingStatements = new HashSet<string>(StringComparer.Ordinal)
            {
                EntityTypeNamespace, AdapterNamespace, sourceType.ContainingNamespace.ToDisplayString()
            };

            foreach (var statement in usingStatements.OrderBy(s => s))
                sourceTextBuilder.AppendLine($"using {statement};");
        }

        void AppendClassDeclaration() => sourceTextBuilder.AppendLine($$"""
            
            namespace {{adapterSymbol.ContainingNamespace.ToDisplayString()}};
            
            public partial class {{adapterSymbol.Name}} : {{AdapterInterfaceName}}<{{sourceType.Name}}>
            {
            """);

        void AppendItemToEntityAdaptMethod()
        {
            sourceTextBuilder.AppendLine($$"""
                    public {{EntityInterfaceName}} Adapt({{sourceType.Name}} item)
                    {
                        var entity = new {{EntityTypeName}}(item.{{partitionKeyProperty!.Name}}, item.{{rowKeyProperty!.Name}});
                """);

            foreach (IPropertySymbol property in sourceProperties.Where(p => !ignoreProperties.Contains(p)))
            {
                sourceTextBuilder.AppendLine(
                    $$"""        entity.Add(nameof({{sourceType.Name}}.{{property.Name}}), item.{{property.Name}});""");
            }

            sourceTextBuilder.AppendLine("""
                
                        return entity;
                    }
                
                """);
        }

        void AppendEntityToItemAdaptMethod()
        {
            sourceTextBuilder.AppendLine($$"""
                    public {{sourceType.Name}} Adapt({{EntityTypeName}} entity)
                    {
                        var item = new {{sourceType.Name}}();
                """);

            if (ignorePartitionKeySourceProperty)
            {
                sourceTextBuilder.AppendLine($$"""
                            item.{{partitionKeyProperty!.Name}} = entity.PartitionKey;
                    """);
            }
            
            if (ignoreRowKeySourceProperty)
            {
                sourceTextBuilder.AppendLine($$"""
                            item.{{rowKeyProperty!.Name}} = entity.RowKey;
                    """);
            }

            foreach (IPropertySymbol property in sourceProperties.Where(p => !ignoreProperties.Contains(p)))
            {
                var typeName = property.Type.ToString();
                var getMethod = GetTableEntityGetMethod(typeName);

                sourceTextBuilder.AppendLine($$"""
                            item.{{property.Name}} = entity.{{getMethod}}(nameof({{sourceType.Name}}.{{property.Name}}));
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

    private static bool TryGetSchemaPropertyFromAttribute(GeneratorExecutionContext context,
        INamedTypeSymbol adapterSymbol, ImmutableArray<IPropertySymbol> sourcePropertiesSymbols,
        ImmutableArray<AttributeData> attributes, string attributeName, out IPropertySymbol? sourcePropertySymbol,
        out bool ignoreSourceProperty)
    {
        sourcePropertySymbol = default!;
        // TODO: Get default value from attribute declaration
        ignoreSourceProperty = true;

        AttributeData? attribute = attributes.FirstOrDefault(a => a.AttributeClass?.Name == attributeName);

        if (attribute is null)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.PartitionKeyAttributeNotFound,
                adapterSymbol.Locations.FirstOrDefault(), adapterSymbol.ToDisplayString()));

            return false;
        }

        if (attribute.NamedArguments.FirstOrDefault(a => a.Key == IgnoreSourcePropertyAttributeNamedArgument)
                .Value.Value is bool ignoreSourcePropertyValue)
            ignoreSourceProperty = ignoreSourcePropertyValue;

        return TryGetPropertyFromAttribute(context, adapterSymbol, sourcePropertiesSymbols, attribute,
            out sourcePropertySymbol);
    }

    private static bool TryGetPropertyFromAttribute(GeneratorExecutionContext context, INamedTypeSymbol adapterSymbol,
        ImmutableArray<IPropertySymbol> sourcePropertiesSymbols, AttributeData propertyAttribute,
        out IPropertySymbol? sourcePropertySymbol)
    {
        // TODO: Validate PropertyAttributeBase type

        var propertyName = (string)propertyAttribute.ConstructorArguments[0].Value!;

        sourcePropertySymbol = sourcePropertiesSymbols.FirstOrDefault(p => p.Name == propertyName);

        if (sourcePropertySymbol is null)
        {
            // TODO: Check multiple locations
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.SourcePropertyNotFound,
                propertyAttribute.AttributeClass!.Locations.FirstOrDefault(), adapterSymbol.ToDisplayString(),
                propertyName));
        }

        return true;
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