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
    // TODO: Use attribute span to determine the correct diagnostic location

    public static readonly DiagnosticDescriptor InvalidAbstractClass = new("AZTBGEN001",
        "Adapter class has abstract modifier", 
        "Adapter class '{0}' should not be abstract", 
        Categories.Usage, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor InvalidGenericClass = new("AZTBGEN002",
        "Adapter class has generic type", 
        "Adapter class '{0}' does not support generic types", 
        Categories.Usage, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor ClassIsNotPartial = new("AZTBGEN003",
        "Adapter class is not partial",
        "Adapter class '{0}' must have to be partial", 
        Categories.Usage, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor PropertyNotFound = new("AZTBGEN004",
        "Property not found",
        "Property '{0}' not found for '{1}' on adapter class '{2}'",
        Categories.Usage, DiagnosticSeverity.Error, true);
    
    public static readonly DiagnosticDescriptor PropertyTypeMismatch = new("AZTBGEN005",
        "Property type mismatch",
        "'{0}' attribute must be of type '{1}' on adapter class '{2}'",
        Categories.Usage, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor UnsupportedPropertyType = new("AZTBGEN006",
        "Unsupported property type",
        "Adapter class '{0}' does not support type '{1}' for property '{2}'",
        Categories.Usage, DiagnosticSeverity.Error, true);
    
    public static readonly DiagnosticDescriptor DuplicateNameChangeProperty = new("AZTBGEN007",
        "Duplicate name change property",
        "Duplicate name change for property '{0}' on adapter class '{1}'",
        Categories.Usage, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor DuplicateNameChangeTargetName = new("AZTBGEN008",
        "Target name already exists",
        "A name change with the same target '{0}' has already been added on adapter class '{1}'",
        Categories.Usage, DiagnosticSeverity.Error, true);
    
    public static readonly DiagnosticDescriptor NameChangeTargetNameConflict = new("AZTBGEN009",
        "Target name conflict",
        "The name change target '{0}' conflicts with an existing property on adapter class '{1}'",
        Categories.Usage, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor InvalidNameChangeTargetName = new("AZTBGEN010",
        "Invalid target name",
        "Target name is not valid for property '{0}' on adapter class '{1}'",
        Categories.Usage, DiagnosticSeverity.Error, true);

    // @formatter:on
}