namespace BunnyTail.CommonCode.Generator.Models;

internal sealed record PropertyModel(
    string Name,
    bool HasElements,
    bool IsNullAssignable);
