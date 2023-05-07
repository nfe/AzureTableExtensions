; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 0.1.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|--------------------
 AZTBGEN001 | Usage    | Error    | AZTBGEN001_InvalidAbstractClass
 AZTBGEN002 | Usage    | Error    | AZTBGEN002_InvalidGenericClass
 AZTBGEN003 | Usage    | Error    | AZTBGEN003_ClassIsNotPartial
 AZTBGEN004 | Usage    | Error    | AZTBGEN004_RequiredPropertyNotFound
 AZTBGEN005 | Usage    | Error    | AZTBGEN005_PropertyNotFound
 AZTBGEN006 | Usage    | Error    | AZTBGEN006_DuplicateProperty
 AZTBGEN007 | Usage    | Error    | AZTBGEN007_PropertyTypeMismatch
 AZTBGEN008 | Usage    | Error    | AZTBGEN008_UnsupportedPropertyType
 AZTBGEN009 | Usage    | Error    | AZTBGEN009_DuplicatePropertyNameChange
 AZTBGEN010 | Usage    | Error    | AZTBGEN010_ConverterSignatureMismatch
 AZTBGEN011 | Usage    | Error    | AZTBGEN011_ConverterReturnTypeMismatch