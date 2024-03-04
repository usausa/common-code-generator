namespace CommonCodeGenerator;

#pragma warning disable CA1819
[GenerateToString]
public partial class Data
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;

    public int[] Values { get; set; } = default!;
}
#pragma warning restore CA1819
