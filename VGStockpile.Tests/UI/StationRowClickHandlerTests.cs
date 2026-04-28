using System;
using System.Collections.Generic;
using VGStockpile.Data;
using VGStockpile.Locate;
using VGStockpile.UI;
using Xunit;

namespace VGStockpile.Tests.UI;

public class StationRowClickHandlerTests
{
    private sealed class FakeLocator : IStationLocator
    {
        public List<StationStorageSnapshot> Calls { get; } = new();
        public Exception? Throw { get; set; }

        public void Locate(StationStorageSnapshot snap)
        {
            Calls.Add(snap);
            if (Throw is not null) throw Throw;
        }
    }

    private static StationStorageSnapshot Snap() => new(
        "s1", "Helios", "Sol", "fac.a",
        new Dictionary<string, int> { ["ti"] = 1 });

    [Fact]
    public void Click_Invokes_Locator_With_Snapshot()
    {
        var loc = new FakeLocator();
        var h = new StationRowClickHandler(
            loc, closeWindow: () => { }, shouldCloseOnLocate: () => false,
            logWarning: _ => { });

        h.Click(Snap());

        Assert.Single(loc.Calls);
        Assert.Equal("Helios", loc.Calls[0].StationName);
    }

    [Fact]
    public void Click_Closes_Window_When_Configured_To()
    {
        var loc = new FakeLocator();
        var closed = false;
        var h = new StationRowClickHandler(
            loc, closeWindow: () => closed = true,
            shouldCloseOnLocate: () => true, logWarning: _ => { });

        h.Click(Snap());

        Assert.True(closed);
    }

    [Fact]
    public void Click_Does_Not_Close_Window_When_Not_Configured()
    {
        var loc = new FakeLocator();
        var closed = false;
        var h = new StationRowClickHandler(
            loc, closeWindow: () => closed = true,
            shouldCloseOnLocate: () => false, logWarning: _ => { });

        h.Click(Snap());

        Assert.False(closed);
    }

    [Fact]
    public void Locator_Failure_Is_Logged_And_Does_Not_Propagate()
    {
        var loc = new FakeLocator { Throw = new InvalidOperationException("map not ready") };
        string? warning = null;
        var closed = false;

        var h = new StationRowClickHandler(
            loc, closeWindow: () => closed = true,
            shouldCloseOnLocate: () => true,
            logWarning: msg => warning = msg);

        h.Click(Snap());

        Assert.NotNull(warning);
        Assert.Contains("Helios", warning);
        Assert.False(closed);
    }
}
