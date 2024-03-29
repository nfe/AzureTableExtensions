﻿namespace FisTech.Persistence.AzureTable.SourceGenerators;

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
        "Adapter class is abstract",
        "Adapter class '{0}' should not have the abstract modifier",
        Categories.Usage, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor InvalidGenericClass = new("AZTBGEN002",
        "Adapter class has generic type",
        "Adapter class '{0}' does not support generic types",
        Categories.Usage, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor ClassIsNotPartial = new("AZTBGEN003",
        "Adapter class is not partial",
        "Adapter class '{0}' must have to be partial",
        Categories.Usage, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor RequiredPropertyNotFound = new("AZTBGEN004",
        "Required property not found",
        "Required property '{0}' not found on adapter class '{1}'",
        Categories.Usage, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor PropertyNotFound = new("AZTBGEN005",
        "Property not found",
        "Property '{0}' not found on adapter class '{1}'",
        Categories.Usage, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor DuplicateProperty = new("AZTBGEN006",
        "Duplicate property",
        "Duplicate property '{0}' on adapter class '{1}'",
        Categories.Usage, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor PropertyTypeMismatch = new("AZTBGEN007",
        "Property type mismatch",
        "Property '{0}' must be of type '{1}' on adapter class '{2}'",
        Categories.Usage, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor UnsupportedPropertyType = new("AZTBGEN008",
        "Unsupported property type",
        "Unsupported type '{0}' on adapter class '{1}'",
        Categories.Usage, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor DuplicatePropertyNameChange = new("AZTBGEN009",
        "Duplicate property name change",
        "Duplicate name change for property '{0}' on adapter class '{1}'",
        Categories.Usage, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor ConverterSignatureMismatch = new("AZTBGEN010",
        "Converter method signature mismatch",
        "Converter method must contain a single parameter of type '{0}' on adapter class '{1}'",
        Categories.Usage, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor ConverterReturnTypeMismatch = new("AZTBGEN011",
        "Converter return type mismatch",
        "Converter return must be of type '{0}' for property {1} on adapter class '{2}'",
        Categories.Usage, DiagnosticSeverity.Error, true);

    // @formatter:on
}