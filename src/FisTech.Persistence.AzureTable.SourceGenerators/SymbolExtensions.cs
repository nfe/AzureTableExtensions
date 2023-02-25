namespace FisTech.Persistence.AzureTable.SourceGenerators;

internal static class SymbolExtensions
{
    public static IEnumerable<INamedTypeSymbol> DescendantTypeMembers(this INamespaceSymbol namespaceSymbol,
        Func<INamedTypeSymbol, bool> predicate)
    {
        if (namespaceSymbol is null)
            throw new ArgumentNullException(nameof(namespaceSymbol));

        foreach (INamedTypeSymbol? typeMember in namespaceSymbol.GetNamespaceMembers()
            .SelectMany(namespaceMember => DescendantTypeMembers(namespaceMember, predicate)))
            yield return typeMember;

        foreach (INamedTypeSymbol? typeMember in namespaceSymbol.GetTypeMembers().Where(predicate))
            yield return typeMember;
    }

    public static bool IsPartial(this ITypeSymbol typeSymbol)
    {
        const string partialClassDeclaration = "partial class";

        if (typeSymbol is null)
            throw new ArgumentNullException(nameof(typeSymbol));

        SyntaxReference? syntaxReference = typeSymbol.DeclaringSyntaxReferences.FirstOrDefault();

        if (syntaxReference is null)
            return false;

        ReadOnlySpan<char> sourceText = syntaxReference.SyntaxTree.ToString()
            .AsSpan(syntaxReference.Span.Start, syntaxReference.Span.Length);

        return sourceText.Contains(partialClassDeclaration.AsSpan(), StringComparison.Ordinal);
    }

    public static bool IsNullableTypeKind(this INamedTypeSymbol namedTypeSymbol, TypeKind typeKind) =>
        namedTypeSymbol.OriginalDefinition.Name == "Nullable" && namedTypeSymbol.TypeArguments[0].TypeKind == typeKind;

    public static IEnumerable<IPropertySymbol> GetInstancePublicProperties(this ITypeSymbol typeSymbol) => typeSymbol
        .GetMembers()
        .Where(m => m is IPropertySymbol { DeclaredAccessibility: Accessibility.Public, IsStatic: false })
        .Cast<IPropertySymbol>();

    public static string GetNameWithoutArity(this Type type) =>
        !type.IsGenericType ? type.Name : type.Name.Substring(0, type.Name.IndexOf('`'));
}