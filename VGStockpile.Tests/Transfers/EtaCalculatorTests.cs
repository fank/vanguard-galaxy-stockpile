using VGStockpile.Transfers;
using Xunit;

namespace VGStockpile.Tests.Transfers;

public class EtaCalculatorTests
{
    private static readonly TransferConfig Cfg = TransferConfig.Defaults();

    [Fact]
    public void Compute_ZeroJumps_IsBaseClampedToMin()
    {
        // base=30, min=15, max=1800 → 30
        Assert.Equal(30f, EtaCalculator.ComputeSeconds(0, Cfg));
    }

    [Fact]
    public void Compute_ManyJumps_IsClampedToMax()
    {
        // 30 + 20*1000 = 20030, clamped to 1800
        Assert.Equal(1800f, EtaCalculator.ComputeSeconds(1000, Cfg));
    }

    [Fact]
    public void Compute_ScalesLinearlyBetweenBounds()
    {
        Assert.Equal(30f + 20f * 5f, EtaCalculator.ComputeSeconds(5, Cfg));
    }

    [Fact]
    public void Compute_BelowMin_ClampsToMin()
    {
        var cfg = Cfg with { EtaBaseSeconds = 1f, EtaPerJumpSeconds = 0f, EtaMinSeconds = 10f };
        Assert.Equal(10f, EtaCalculator.ComputeSeconds(0, cfg));
    }
}
