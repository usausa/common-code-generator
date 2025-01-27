namespace BunnyTail.CommonCode.Generator.Models;

using BunnyTail.CommonCode.Generator.Helpers;

internal sealed record TypeModel(
    string Namespace,
    string ClassName,
    bool IsValueType,
    EquatableArray<PropertyModel> Properties);
