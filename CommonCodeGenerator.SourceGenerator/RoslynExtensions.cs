namespace CommonCodeGenerator.SourceGenerator;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal static class RoslynExtensions
{
    public static string GetClassName(this ClassDeclarationSyntax syntax)
    {
        var identifier = syntax.Identifier.ToString();
        return syntax.TypeParameterList is not null
            ? $"{identifier}<{String.Join(", ", syntax.TypeParameterList.Parameters.Select(static p => p.Identifier.ToString()))}>"
            : identifier;
    }

    public static bool IsGenericType(this ITypeSymbol symbol) =>
        symbol is INamedTypeSymbol { IsGenericType: true } ||
        symbol is ITypeParameterSymbol;
}
