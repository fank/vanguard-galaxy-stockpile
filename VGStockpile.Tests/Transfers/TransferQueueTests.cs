using System.Collections.Generic;
using System.Linq;
using VGStockpile.Transfers;
using Xunit;

namespace VGStockpile.Tests.Transfers;

public class TransferQueueTests
{
    private static TransferRequest Req(string id, float remaining = 60f) =>
        new(id, "src", "dst", new List<TransferManifestLine>(), 0, 0, remaining, remaining,
            TransferStatus.Pending);

    [Fact]
    public void Add_BelowCap_Stores()
    {
        var q = new TransferQueue(maxConcurrent: 2);
        q.Add(Req("a"));
        Assert.Equal(1, q.PendingCount);
        Assert.True(q.CanAccept);
    }

    [Fact]
    public void Add_AtCap_Throws()
    {
        var q = new TransferQueue(maxConcurrent: 1);
        q.Add(Req("a"));
        Assert.Throws<TransferQueueFullException>(() => q.Add(Req("b")));
    }

    [Fact]
    public void Add_UnlimitedCap_NeverFull()
    {
        var q = new TransferQueue(maxConcurrent: 0);
        for (var i = 0; i < 100; i++) q.Add(Req($"r{i}"));
        Assert.True(q.CanAccept);
    }

    [Fact]
    public void TickSeconds_DecrementsRemaining()
    {
        var q = new TransferQueue(0);
        q.Add(Req("a", remaining: 30f));
        var completed = q.TickSeconds(10f);
        Assert.Empty(completed);
        Assert.Equal(20f, q.Items.Single().RemainingSeconds);
    }

    [Fact]
    public void TickSeconds_AtZero_Completes()
    {
        var q = new TransferQueue(0);
        q.Add(Req("a", remaining: 5f));
        var completed = q.TickSeconds(10f);
        Assert.Single(completed);
        Assert.Equal("a", completed[0].Id);
        Assert.Equal(TransferStatus.Completed, completed[0].Status);
    }

    [Fact]
    public void TickSeconds_PastZero_DoesNotDoubleComplete()
    {
        var q = new TransferQueue(0);
        q.Add(Req("a", remaining: 5f));
        q.TickSeconds(10f);
        var second = q.TickSeconds(10f);
        Assert.Empty(second);
    }

    [Fact]
    public void TickSeconds_ZeroDt_IsIdempotent()
    {
        var q = new TransferQueue(0);
        q.Add(Req("a", remaining: 30f));
        Assert.Empty(q.TickSeconds(0f));
        Assert.Equal(30f, q.Items.Single().RemainingSeconds);
    }

    [Fact]
    public void Cancel_PendingNonZero_RemovesAndReturnsTrue()
    {
        var q = new TransferQueue(0);
        q.Add(Req("a", remaining: 30f));
        Assert.True(q.Cancel("a"));
        Assert.Equal(0, q.PendingCount);
    }

    [Fact]
    public void Cancel_AtZero_Rejected()
    {
        var q = new TransferQueue(0);
        q.Add(Req("a", remaining: 5f));
        q.TickSeconds(10f);
        Assert.False(q.Cancel("a"));
    }

    [Fact]
    public void Cancel_UnknownId_ReturnsFalse() =>
        Assert.False(new TransferQueue(0).Cancel("missing"));
}
