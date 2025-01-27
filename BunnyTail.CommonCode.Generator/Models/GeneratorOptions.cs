namespace BunnyTail.CommonCode.Generator.Models;

internal record class GeneratorOptions
{
    public bool OutputClassName { get; set; }

    public string? NullLiteral { get; set; } = "null";
}
