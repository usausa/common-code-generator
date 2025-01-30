namespace BunnyTail.CommonCode.Generator.Models;

using SourceGenerateHelper;

internal sealed record TypeModel(
    string Namespace,
    string ClassName,
    bool IsValueType,
    EquatableArray<PropertyModel> Properties);
