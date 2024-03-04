namespace CommonCodeGenerator;

public class ToStringTest
{
    [Fact]
    public void Test1()
    {
        Assert.Equal(
            "{ Id = 123, Name = xyz, Values = [1, 2] }",
            new Data { Id = 123, Name = "xyz", Values = [1, 2] }.ToString());
        Assert.Equal(
            "{ Id = 123, Name = xyz, Values = null }",
            new Data { Id = 123, Name = "xyz" }.ToString());
    }
}
