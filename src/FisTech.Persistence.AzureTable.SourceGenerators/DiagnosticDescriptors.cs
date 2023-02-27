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
    
    // TODO: Create possible analyzers with code fix

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

    public static readonly DiagnosticDescriptor PropertyNotFound = new("AZTBGEN005",
        "Property not found",
        "Property '{0}' not found for '{1}' on adapter class '{2}'",
        Categories.Usage, DiagnosticSeverity.Error, true);
    
    public static readonly DiagnosticDescriptor PropertyTypeMismatch = new("AZTBGEN006",
        "Property type mismatch",
        "'{0}' attribute must be of type '{1}' on adapter class '{2}'",
        Categories.Usage, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor UnsupportedPropertyType = new("AZTBGEN007",
        "Unsupported property type",
        "Adapter class '{0}' does not support type '{1}' for property '{2}'",
        Categories.Usage, DiagnosticSeverity.Error, true);

    // @formatter:on
}