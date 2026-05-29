using VGStockpile.Transfers;
using Xunit;

namespace VGStockpile.Tests.Transfers;

public class EligibilityRulesTests
{
    private static StationContext Ctx(
        bool visited = true, bool stock = true, bool refinery = true,
        bool peaceful = true, bool current = false, bool playerDocked = true) =>
        new("g", visited, stock, refinery, peaceful, current, playerDocked);

    private static readonly TransferConfig CfgStrict = TransferConfig.Defaults();

    [Fact]
    public void CanPullFrom_RequiresVisitedStockNotCurrent()
    {
        Assert.True (EligibilityRules.CanPullFrom(Ctx()));
        Assert.False(EligibilityRules.CanPullFrom(Ctx(visited: false)));
        Assert.False(EligibilityRules.CanPullFrom(Ctx(stock:   false)));
        Assert.False(EligibilityRules.CanPullFrom(Ctx(current: true)));
    }

    [Fact]
    public void CanPullFrom_IgnoresPeacefulAndRefinery()
    {
        Assert.True(EligibilityRules.CanPullFrom(Ctx(peaceful: false, refinery: false)));
    }

    [Fact]
    public void CanPushTo_StrictConfig_RequiresAll()
    {
        Assert.True (EligibilityRules.CanPushTo(Ctx(),                CfgStrict));
        Assert.False(EligibilityRules.CanPushTo(Ctx(visited: false),  CfgStrict));
        Assert.False(EligibilityRules.CanPushTo(Ctx(refinery: false), CfgStrict));
        Assert.False(EligibilityRules.CanPushTo(Ctx(peaceful: false), CfgStrict));
        Assert.False(EligibilityRules.CanPushTo(Ctx(current: true),   CfgStrict));
    }

    [Fact]
    public void CanPushTo_RefineryFlagOff_AllowsNonRefinery()
    {
        var cfg = CfgStrict with { PushRequiresRefinery = false };
        Assert.True(EligibilityRules.CanPushTo(Ctx(refinery: false), cfg));
    }

    [Fact]
    public void CanPushTo_PeacefulFlagOff_AllowsHostile()
    {
        var cfg = CfgStrict with { PushRequiresPeaceful = false };
        Assert.True(EligibilityRules.CanPushTo(Ctx(peaceful: false), cfg));
    }

    [Fact]
    public void CanPushTo_BothFlagsOff_StillRequiresVisitedAndNotCurrent()
    {
        var cfg = CfgStrict with { PushRequiresPeaceful = false, PushRequiresRefinery = false };
        Assert.False(EligibilityRules.CanPushTo(Ctx(visited: false), cfg));
        Assert.False(EligibilityRules.CanPushTo(Ctx(current: true),  cfg));
    }

    [Fact]
    public void CanPullFrom_OpenSpace_RejectsAllStations()
    {
        // Player undocked: even a perfectly eligible station can't be pulled
        // because there's no station to deliver to.
        Assert.False(EligibilityRules.CanPullFrom(Ctx(playerDocked: false)));
    }

    [Fact]
    public void CanPushTo_OpenSpace_RejectsAllStations()
    {
        Assert.False(EligibilityRules.CanPushTo(Ctx(playerDocked: false), CfgStrict));
    }
}
