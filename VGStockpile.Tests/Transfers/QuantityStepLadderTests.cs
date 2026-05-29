using VGStockpile.Transfers;
using Xunit;

namespace VGStockpile.Tests.Transfers;

public class QuantityStepLadderTests
{
    [Fact]
    public void Default_NoShift_IsSmallLargeMaxLargeSmall()
    {
        var ladder = QuantityStepLadder.Build(small: 1, large: 20, shiftMul: 5, shiftHeld: false);
        Assert.Equal(1, ladder.LeftSmall);
        Assert.Equal(20, ladder.LeftLarge);
        Assert.Equal(20, ladder.RightLarge);
        Assert.Equal(1, ladder.RightSmall);
        Assert.Equal(QuantityStepKind.Max, ladder.Center);
    }

    [Fact]
    public void Shift_MultipliesBothSteps()
    {
        var ladder = QuantityStepLadder.Build(small: 1, large: 20, shiftMul: 5, shiftHeld: true);
        Assert.Equal(5, ladder.LeftSmall);
        Assert.Equal(100, ladder.LeftLarge);
        Assert.Equal(100, ladder.RightLarge);
        Assert.Equal(5, ladder.RightSmall);
    }
}
