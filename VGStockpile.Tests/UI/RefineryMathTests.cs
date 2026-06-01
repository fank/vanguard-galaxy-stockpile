using VGStockpile.UI.Refinery;
using Xunit;

namespace VGStockpile.Tests.UI;

public class RefineryMathTests
{
    [Fact]
    public void Eta_Single_Unit_Just_Started()
    {
        // 1 unit, 10s each, no progress -> 10s.
        Assert.Equal(10f, RefineryMath.EtaSeconds(progress: 0f, refineTime: 10f, remainingAmount: 1));
    }

    [Fact]
    public void Eta_Single_Unit_Half_Done()
    {
        // 10s unit, 4s of progress -> 6s left.
        Assert.Equal(6f, RefineryMath.EtaSeconds(progress: 4f, refineTime: 10f, remainingAmount: 1));
    }

    [Fact]
    public void Eta_Multi_Unit_Includes_Remaining_Units()
    {
        // 3 remaining, 10s each, current unit 2s in -> 3*10 - 2 = 28s.
        Assert.Equal(28f, RefineryMath.EtaSeconds(progress: 2f, refineTime: 10f, remainingAmount: 3));
    }

    [Fact]
    public void Eta_Is_Never_Negative()
    {
        Assert.Equal(0f, RefineryMath.EtaSeconds(progress: 15f, refineTime: 10f, remainingAmount: 1));
    }

    [Fact]
    public void Eta_Zero_When_Nothing_Remaining_Or_No_RefineTime()
    {
        Assert.Equal(0f, RefineryMath.EtaSeconds(5f, 10f, remainingAmount: 0));
        Assert.Equal(0f, RefineryMath.EtaSeconds(5f, 0f, remainingAmount: 3));
    }

    [Fact]
    public void Progress_Fresh_Job_Is_Zero()
    {
        Assert.Equal(0f, RefineryMath.ProgressFraction(initialAmount: 5, remainingAmount: 5, progress: 0f, refineTime: 10f));
    }

    [Fact]
    public void Progress_Counts_Completed_Units_Plus_Current_Fraction()
    {
        // 5 units, 2 done, current unit half (5s/10s) -> (2 + 0.5)/5 = 0.5.
        Assert.Equal(0.5f, RefineryMath.ProgressFraction(initialAmount: 5, remainingAmount: 3, progress: 5f, refineTime: 10f));
    }

    [Fact]
    public void Progress_Near_End_Approaches_One()
    {
        // 5 units, last unit nearly done -> (4 + 0.9)/5 = 0.98.
        Assert.Equal(0.98f, RefineryMath.ProgressFraction(initialAmount: 5, remainingAmount: 1, progress: 9f, refineTime: 10f), 3);
    }

    [Fact]
    public void Progress_Is_Clamped_To_Unit_Interval()
    {
        Assert.Equal(0f, RefineryMath.ProgressFraction(initialAmount: 0, remainingAmount: 0, progress: 0f, refineTime: 10f));
        // Over-shot progress on the current unit doesn't push the fraction past 1.
        Assert.Equal(1f, RefineryMath.ProgressFraction(initialAmount: 2, remainingAmount: 1, progress: 30f, refineTime: 10f));
    }
}
