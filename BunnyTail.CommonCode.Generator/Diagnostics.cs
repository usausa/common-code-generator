namespace BunnyTail.CommonCode.Generator;

using Microsoft.CodeAnalysis;

internal static class Diagnostics
{
    public static DiagnosticDescriptor InvalidTypeDefinition { get; } = new(
        id: "BTTS0001",
        title: "Invalid type definition",
        messageFormat: "Type must be partial. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
