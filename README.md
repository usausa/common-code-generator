# BunnyTail.CommonCode

[![NuGet](https://img.shields.io/nuget/v/BunnyTail.CommonCode.svg)](https://www.nuget.org/packages/BunnyTail.CommonCode)

## Reference

Add reference to CommonCodeGenerator and CommonCodeGenerator.SourceGenerator to csproj.

```xml
  <ItemGroup>
    <PackageReference Include="BunnyTail.CommonCode" Version="1.1.0" />
  </ItemGroup>
```

## ToString

### Source

```csharp
[GenerateToString]
public partial class Data
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;

    public int[] Values { get; set; } = default!;

    [IgnoreToString]
    public int Ignore { get; set; }
}
```

### Result

```csharp
var data = new Data { Id = 123, Name = "xyz", Values = [1, 2] };
var str = data.ToString();
Assert.Equal("{ Id = 123, Name = xyz, Values = [1, 2] }", str);
```
