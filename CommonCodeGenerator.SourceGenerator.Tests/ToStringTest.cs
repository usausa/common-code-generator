namespace CommonCodeGenerator;

public class ToStringTest
{
    [Fact]
    public void TestBasic()
    {
        Assert.Equal(
            "{ Id = 123, Name = xyz, Values = [1, 2] }",
            new Data { Id = 123, Name = "xyz", Values = [1, 2] }.ToString());
        Assert.Equal(
            "{ Id = 123, Name = xyz, Values = null }",
            new Data { Id = 123, Name = "xyz" }.ToString());
    }

    [Fact]
    public void TestGeneric()
    {
        Assert.Equal(
            "{ Value = 123 }",
            new GenericData<int> { Value = 123 }.ToString());
        Assert.Equal(
            "{ Value = xyz }",
            new GenericData<string> { Value = "xyz" }.ToString());
        Assert.Equal(
            "{ Value = null }",
            new GenericData<string?> { Value = null }.ToString());
    }
}
