namespace FisTech.Persistence.AzureTable.SourceGenerators;

internal static class DiagnosticDescriptors
{
    internal static class Categories
    {
        public const string Assertions = "Assertions";
        public const string CodeQuality = "CodeQuality";
        public const string Compatibility = "Compatibility";
        public const string Compiler = "Compiler";
        public const string Correctness = "Correctness";
        public const string Design = "Design";
        public const string Documentation = "Documentation";
        public const string Extensibility = "Extensibility";
        public const string Globalization = "Globalization";
        public const string Interoperability = "Interoperability";
        public const string Library = "Library";
        public const string Maintainability = "Maintainability";
        public const string Naming = "Naming";
        public const string Performance = "Performance";
        public const string Reliability = "Reliability";
        public const string Security = "Security";
        public const string Style = "Style";
        public const string Usage = "Usage";
    }

    // @formatter:off
    
    // TODO: Create analyzer for this with code fix

    public static readonly DiagnosticDescriptor InvalidClassAccessibility = new("AZTBGEN001",
        "Adapter class invalid accessibility modifier", 
        "Adapter class '{0}' should be public", 
        Categories.Usage, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor InvalidAbstractClass = new("AZTBGEN002",
        "Adapter class has abstract modifier", 
        "Adapter class '{0}' should not be abstract", 
        Categories.Usage, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor InvalidGenericClass = new("AZTBGEN003",
        "Adapter class has generic type", 
        "Adapter class '{0}' does not support generic types", 
        Categories.Usage, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor ClassIsNotPartial = new("AZTBGEN004",
        "Adapter class is not partial",
        "Adapter class '{0}' must have to be partial", 
        Categories.Usage, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor PartitionKeyAttributeNotFound = new("AZTBGEN005",
        "Adapter class does not have a PartitionKey attribute",
        "Adapter class '{0}' does not have a PartitionKeyAttribute", 
        Categories.Usage, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor RowKeyAttributeNotFound = new("AZTBGEN006",
        "Adapter class does not have a RowKey attribute", 
        "Adapter class '{0}' does not have a RowKeyAttribute",
        Categories.Usage, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor SourcePropertyNotFound = new("AZTBGEN007",
        "Adapter class source property does not exists on source type",
        "Adapter class '{0}' does not have a valid argument for '{1}': Source property '{2}' not found",
        Categories.Usage, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor InvalidSourcePropertyType = new("AZTBGEN008",
        "Adapter class source property invalid type",
        "Adapter class '{0}' does not have a valid argument for '{1}': Source property '{2}' should be of '{3}' type instead '{4}'",
        Categories.Usage, DiagnosticSeverity.Error, true);

    // @formatter:on
}