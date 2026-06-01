using Credfeto.Defi.Services.Utils;
using FunFair.Test.Common;
using Xunit;

namespace Credfeto.Defi.Server.Tests;

public sealed class SlugUtilsTests : TestBase
{
    [Theory]
    [InlineData("Aave", "aave")]
    [InlineData("Compound Finance", "compound-finance")]
    [InlineData("Uniswap V3", "uniswap-v3")]
    [InlineData("  leading space  ", "leading-space")]
    [InlineData("special!chars#here", "special-chars-here")]
    public void ToSlug_NormalisesCorrectly(string input, string expected)
    {
        string result = SlugUtils.ToSlug(input);
        Assert.Equal(expected: expected, actual: result);
    }

    [Theory]
    [InlineData("aave-v3", "aave")]
    [InlineData("compound-v2", "compound")]
    [InlineData("uniswap-v3", "uniswap")]
    [InlineData("aave", "aave")]
    [InlineData("curve-v1-stable", "curve")]
    public void BaseSlug_StripsVersionSuffix(string input, string expected)
    {
        string result = SlugUtils.BaseSlug(input);
        Assert.Equal(expected: expected, actual: result);
    }
}
