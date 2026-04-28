using VGStockpile.UI;
using Xunit;

namespace VGStockpile.Tests.UI;

public class CompactNumberTests
{
    [Theory]
    [InlineData(0, "")]
    [InlineData(1, "1")]
    [InlineData(999, "999")]
    [InlineData(1000, "1.0k")]
    [InlineData(1500, "1.5k")]
    [InlineData(12_345, "12.3k")]
    [InlineData(999_999, "1000.0k")]
    [InlineData(1_000_000, "1.0M")]
    [InlineData(1_500_000, "1.5M")]
    [InlineData(1_234_567_890, "1234.6M")]
    public void Format_Produces_Expected_String(int value, string expected)
    {
        Assert.Equal(expected, CompactNumber.Format(value));
    }

    [Fact]
    public void Format_NegativeIsTreatedAsBlank()
    {
        Assert.Equal("", CompactNumber.Format(-1));
    }
}
