using VGStockpile.Transfers;
using Xunit;

namespace VGStockpile.Tests.Transfers;

public class FeeCalculatorTests
{
    private static readonly TransferConfig Cfg = TransferConfig.Defaults();

    [Fact]
    public void Compute_ZeroDistanceZeroUnits_IsBaseFee()
    {
        Assert.Equal(100, FeeCalculator.Compute(0, 0, Cfg));
    }

    [Fact]
    public void Compute_FiveJumpsHundredUnits_AppliesAllFourTerms()
    {
        // 100 + 1*100 + 50*5 + 0.5*100*5 = 100 + 100 + 250 + 250 = 700
        Assert.Equal(700, FeeCalculator.Compute(5, 100, Cfg));
    }

    [Fact]
    public void Compute_RoundsCrossTermDown()
    {
        // base/perUnit/perJump zeroed; cross = 0.5*1*1 = 0.5 → int 0
        var cfg = Cfg with { FeeBase = 0, FeePerUnit = 0, FeePerJump = 0 };
        Assert.Equal(0, FeeCalculator.Compute(1, 1, cfg));
    }
}
