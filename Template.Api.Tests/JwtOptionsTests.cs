using Template.Api.Configuration;
using Xunit;

namespace Template.Api.Tests;

public class JwtOptionsTests
{
    [Theory]
    [InlineData("", false)]
    [InlineData("123456789", false)]
    [InlineData("01234567890123456789012345678901", true)]
    public void HasStrongKey_ConfiguredKey_EnforcesMinimumLength(string key, bool expected)
    {
        Assert.Equal(expected, new JwtOptions { Key = key }.HasStrongKey());
    }
}
