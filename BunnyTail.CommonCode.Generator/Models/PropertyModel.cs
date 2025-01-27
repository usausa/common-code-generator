namespace BunnyTail.CommonCode.Generator.Models;

internal sealed record PropertyModel(
    string Name,
    string Type,
    bool HasElements,
    bool IsNullAssignable);
