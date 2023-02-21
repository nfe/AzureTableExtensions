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
        "Adapter class '{0}' should be public or internal", 
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

    public static readonly DiagnosticDescriptor SourcePropertyNotFound = new("AZTBGEN005",
        "Adapter class property not found for an attribute",
        "Adapter class '{0}' property not found for '{1}' attribute",
        Categories.Usage, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor InvalidSourcePropertyType = new("AZTBGEN006",
        "Adapter class source property invalid type",
        "Adapter class '{0}' property '{1}' should be of '{2}' type instead '{3}'",
        Categories.Usage, DiagnosticSeverity.Error, true);

    // @formatter:on
}