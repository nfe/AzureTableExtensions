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

    public void Initialize(GeneratorInitializationContext context) { }

    public void Execute(GeneratorExecutionContext context)
    {
        foreach (INamedTypeSymbol? adapter in context.Compilation.Assembly.GlobalNamespace.DescendantTypeMembers(t =>
            t.BaseType is { Name: AdapterBaseTypeName, IsGenericType: true, Arity: 1 }))
        {
            if (!IsValidAdapterClass(context, adapter))
                continue;

            ImmutableArray<AttributeData> adapterAttributes = adapter.GetAttributes();

            ITypeSymbol sourceType = adapter.BaseType!.TypeArguments[0];
            ImmutableArray<IPropertySymbol> sourceProperties =
                sourceType.GetInstancePublicProperties().ToImmutableArray();

            if (!TryGetPropertyFromAdapterAttributes(PartitionKeyAttributeName,
                out IPropertySymbol? partitionKeyProperty, nameof(String)))
                continue;

            if (!TryGetPropertyFromAdapterAttributes(RowKeyAttributeName, out IPropertySymbol? rowKeyProperty,
                nameof(String)))
                continue;

            var sourceTextBuilder = new StringBuilder();

            AppendUsingStatements();

            AppendClassDeclaration();

            AppendItemToEntityAdaptMethod();

            AppendEntityToItemAdaptMethod();

            AppendClosingBraces();

            context.AddSource($"{adapter.Name}.g.cs", SourceText.From(sourceTextBuilder.ToString(), Encoding.UTF8));

            bool TryGetPropertyFromAdapterAttributes(string attributeName, out IPropertySymbol? sourcePropertySymbol,
                string? targetTypeName = null) => TryGetProperty(context, adapter, adapterAttributes, attributeName,
                sourceProperties, out sourcePropertySymbol, targetTypeName);

            void AppendUsingStatements()
            {
                var usingStatements = new HashSet<string>(StringComparer.Ordinal)
                {
                    EntityTypeNamespace, AdapterNamespace, sourceType.ContainingNamespace.ToDisplayString()
                };

                foreach (var statement in usingStatements)
                    sourceTextBuilder.AppendLine($"using {statement};");
            }

            void AppendClassDeclaration() => sourceTextBuilder.AppendLine($$"""
                    
                    namespace {{adapter.ContainingNamespace.ToDisplayString()}};
                    
                    public partial class {{adapter.Name}} : {{AdapterInterfaceName}}<{{sourceType.Name}}>
                    {
                    """);

            void AppendItemToEntityAdaptMethod()
            {
                sourceTextBuilder.AppendLine($$"""
                        public {{EntityInterfaceName}} Adapt({{sourceType.Name}} item)
                        {
                            var entity = new {{EntityTypeName}}(item.{{partitionKeyProperty!.Name}}, item.{{rowKeyProperty!.Name}});
                    """);

                foreach (IPropertySymbol property in sourceProperties)
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

                foreach (IPropertySymbol property in sourceProperties)
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

    private static bool TryGetProperty(GeneratorExecutionContext context, INamedTypeSymbol adapterSymbol,
        ImmutableArray<AttributeData> attributes, string attributeName,
        ImmutableArray<IPropertySymbol> sourcePropertiesSymbols, out IPropertySymbol? sourcePropertySymbol,
        string? targetTypeName = null)
    {
        sourcePropertySymbol = null;

        AttributeData? attribute = attributes.FirstOrDefault(a => a.AttributeClass?.Name == attributeName);

        if (attribute is null)
        {
            ReportAttributeNotFoundError();
            return false;
        }

        var propertyName = (string)attribute.ConstructorArguments[0].Value!;

        IPropertySymbol? property = sourcePropertiesSymbols.FirstOrDefault(p => p.Name == propertyName);

        if (property is null)
        {
            ReportPropertyNotFoundError();
            return false;
        }

        // TODO: Allow supported types
        if (targetTypeName is not null && property.Type.Name != targetTypeName)
        {
            ReportInvalidPropertyTypeError();
            return false;
        }

        sourcePropertySymbol = property;

        return true;

        void ReportAttributeNotFoundError() => context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.PartitionKeyAttributeNotFound, adapterSymbol.Locations.FirstOrDefault(),
            adapterSymbol.ToDisplayString()));

        void ReportPropertyNotFoundError() => context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.SourcePropertyNotFound, adapterSymbol.Locations.FirstOrDefault(),
            adapterSymbol.ToDisplayString(), attribute.AttributeClass!.ToDisplayString(), propertyName));

        void ReportInvalidPropertyTypeError() => context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.InvalidSourcePropertyType, adapterSymbol.Locations.FirstOrDefault(),
            adapterSymbol.ToDisplayString(), attribute.AttributeClass!.ToDisplayString(), propertyName, targetTypeName,
            property.Type.ToDisplayString()));
    }

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