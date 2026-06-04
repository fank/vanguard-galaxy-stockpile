using System.Collections.Generic;
using System.Linq;
using VGStockpile.Data;
using VGStockpile.UI.Transfers;
using Xunit;

namespace VGStockpile.Tests.UI;

public class TransferSourceStockTests
{
    private static StationStorageSnapshot Snap(
        string id, params (string mat, int qty)[] items)
        => new(
            StationId:   id,
            StationName: id,
            SystemGuid:  "sys",
            SystemName:  "Sys",
            FactionId:   "fac",
            Items:       items.ToDictionary(i => i.mat, i => i.qty));

    [Fact]
    public void Pull_ReadsLiveSource_NotTheStaleClickedRow()
    {
        // The row snapshot captured at window-open still says 100, but the
        // station has since been drained to 40 by an in-flight transfer. The
        // dialog must offer the live 40, not the stale 100. (This is the bug.)
        var staleRow = Snap("A", ("ore", 100));
        var live     = new[] { Snap("A", ("ore", 40)) };

        var result = TransferSourceStock.Resolve(
            TransferDirection.Pull, staleRow, dockedStationGuid: "B", live);

        Assert.Equal(40, result["ore"]);
    }

    [Fact]
    public void Pull_ReturnsEmpty_WhenSourceDrainedAndAbsentFromLive()
    {
        // A fully drained source is filtered out of the live snapshot list
        // (the reader drops stations with zero stored materials), so the dialog
        // must show nothing — not the stale row's contents.
        var staleRow = Snap("A", ("ore", 100));
        var live     = new[] { Snap("B", ("ore", 7)) };

        var result = TransferSourceStock.Resolve(
            TransferDirection.Pull, staleRow, dockedStationGuid: "B", live);

        Assert.Empty(result);
    }

    [Fact]
    public void Pull_SelectsTheClickedStation_AmongSeveral()
    {
        var clicked = Snap("B");
        var live = new[]
        {
            Snap("A", ("ore", 1)),
            Snap("B", ("ore", 2)),
            Snap("C", ("ore", 3)),
        };

        var result = TransferSourceStock.Resolve(
            TransferDirection.Pull, clicked, dockedStationGuid: "Z", live);

        Assert.Equal(2, result["ore"]);
    }

    [Fact]
    public void Push_ReadsLiveDockedStation_NotTheClickedTargetRow()
    {
        // Push source is the docked station (B), never the clicked target row (A).
        var targetRow = Snap("A", ("ore", 999));
        var live = new[]
        {
            Snap("A", ("ore", 999)),
            Snap("B", ("ore", 50)),
        };

        var result = TransferSourceStock.Resolve(
            TransferDirection.Push, targetRow, dockedStationGuid: "B", live);

        Assert.Equal(50, result["ore"]);
    }

    [Fact]
    public void Push_ReturnsEmpty_WhenNoDockedStation()
    {
        var targetRow = Snap("A", ("ore", 5));

        var result = TransferSourceStock.Resolve(
            TransferDirection.Push, targetRow, dockedStationGuid: null,
            liveSnapshots: new[] { Snap("A", ("ore", 5)) });

        Assert.Empty(result);
    }
}
