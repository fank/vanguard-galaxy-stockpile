using System.Collections.Generic;
using System.IO;
using VGStockpile.Transfers;
using VGStockpile.Transfers.Persistence;
using Xunit;

namespace VGStockpile.Tests.Transfers.Persistence;

public class JsonTransferStoreTests : System.IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(),
        "vgstockpile-tests-" + System.Guid.NewGuid().ToString("N"));

    public JsonTransferStoreTests() { Directory.CreateDirectory(_dir); }
    public void Dispose() { try { Directory.Delete(_dir, recursive: true); } catch { } }

    private string Path1 => Path.Combine(_dir, "save1.vgstockpile-transfers.json");

    [Fact]
    public void Load_MissingFile_ReturnsEmpty()
    {
        var store = new JsonTransferStore(_ => { });
        var loaded = store.Load(Path1);
        Assert.Equal(TransferSidecar.CurrentVersion, loaded.Version);
        Assert.Empty(loaded.Items);
    }

    [Fact]
    public void Load_MalformedJson_ThrowsAndLogsWarning()
    {
        File.WriteAllText(Path1, "{ not valid json");
        string? warned = null;
        var store = new JsonTransferStore(msg => warned = msg);
        Assert.ThrowsAny<Newtonsoft.Json.JsonException>(() => store.Load(Path1));
        Assert.NotNull(warned);
    }

    [Fact]
    public void EmptySnapshotOnlyWritesWhenDestinationAlreadyExists()
    {
        var store = new JsonTransferStore(_ => { });
        store.Save(Path1, TransferSidecar.Empty());
        Assert.False(File.Exists(Path1));
        File.WriteAllText(Path1, "{\"Version\":1,\"Items\":[]}");
        store.Save(Path1, TransferSidecar.Empty());
        Assert.Empty(store.Load(Path1).Items);
    }

    [Theory]
    [InlineData("{ broken")]
    [InlineData("null")]
    [InlineData("{\"Version\":1,\"Items\":null}")]
    public void InvalidExistingDataCannotBeOverwritten(string text)
    {
        File.WriteAllText(Path1, text);
        var store = new JsonTransferStore(_ => { });
        Assert.ThrowsAny<Newtonsoft.Json.JsonException>(() => store.Save(Path1, TransferSidecar.Empty()));
        Assert.Equal(text, File.ReadAllText(Path1));
    }

    [Fact]
    public void Save_ThenLoad_RoundTrips()
    {
        var sidecar = new TransferSidecar(1, new List<TransferRequest>
        {
            new("id1", "src", "dst",
                new List<TransferManifestLine> { new("iron", 200) },
                420, 5, 130f, 60f, TransferStatus.Pending),
        });
        var store = new JsonTransferStore(_ => { });
        store.Save(Path1, sidecar);
        var loaded = store.Load(Path1);
        Assert.Equal(1, loaded.Version);
        Assert.Single(loaded.Items);
        var item = loaded.Items[0];
        Assert.Equal("id1", item.Id);
        Assert.Equal("src", item.SourceStationGuid);
        Assert.Equal("dst", item.DestStationGuid);
        Assert.Equal(420, item.FeeCredits);
        Assert.Equal(5, item.JumpDistance);
        Assert.Equal(130f, item.TotalSeconds);
        Assert.Equal(60f, item.RemainingSeconds);
        Assert.Equal(TransferStatus.Pending, item.Status);
        Assert.Single(item.Manifest);
        Assert.Equal("iron", item.Manifest[0].ItemIdentifier);
        Assert.Equal(200, item.Manifest[0].Quantity);
    }

    [Fact]
    public void Save_RefusesToOverwriteHigherVersion()
    {
        File.WriteAllText(Path1, "{\"Version\":99,\"Items\":[]}");
        var store = new JsonTransferStore(_ => { });
        Assert.Throws<InvalidDataException>(() => store.Save(Path1, TransferSidecar.Empty()));
        Assert.Contains("\"Version\":99", File.ReadAllText(Path1));
    }
}
