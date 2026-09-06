using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VGModAPI;
using VGStockpile.Transfers;
using VGStockpile.Transfers.Engine;
using VGStockpile.Transfers.Persistence;
using Xunit;

namespace VGStockpile.Tests.Transfers.Engine;

public sealed class CoordinatedTransfersTests
{
    [Fact]
    public void CaptureAndRestorePreserveQueueWithoutSecondDebit()
    {
        var api = new Api(); var materials = new Materials(); var credits = new Credits();
        var engine = new TransferEngine(new TransferQueue(0), materials, credits, TransferConfig.Defaults());
        using var controller = new CoordinatedTransfers(api, api, engine, false, _ => { }, () => { }, _ => { });
        Assert.False(controller.CanOperate);
        api.Restore(null);
        Assert.True(engine.RequestTransfer("src", "dst", new[] { new TransferManifestLine("iron", 10) }, 0).IsSuccess);
        var money = credits.Current;
        api.Handle.MutationAllowed = false;
        Assert.Empty(engine.Pending);
        var payload = api.Provider!.Capture();
        Assert.True(api.Provider.Validate(payload));
        Assert.Single(TransferPayloadCodec.Decode(payload).Items);
        api.Restore(payload);
        Assert.Single(engine.Pending); Assert.Equal(90, materials.Source); Assert.Equal(money, credits.Current);
        controller.Dispose();
        Assert.Empty(engine.Tick(float.MaxValue)); Assert.False(controller.CanOperate);
    }

    [Fact]
    public void DisabledTransfersRetainOpaqueQueueAndDeferWarningThroughCallback()
    {
        var api = new Api(); int pending = 0, resets = 0;
        using var controller = new CoordinatedTransfers(api, api, null, false, count => pending = count, () => resets++, _ => { });
        var payload = TransferPayloadCodec.Encode(State());
        api.Restore(payload);
        Assert.Equal(1, pending); Assert.Equal(payload, api.Provider!.Capture()); Assert.True(resets >= 2);
        api.Emit(new LifecycleEvent(LifecycleEventKind.SessionInvalidated, api.CurrentSession));
        Assert.Empty(TransferPayloadCodec.Decode(api.Provider.Capture()).Items);
    }

    [Fact]
    public void LegacyImportIsExplicitPreservedAndKnownPayloadTakesPrecedence()
    {
        var root = Path.Combine(Path.GetTempPath(), "stockpile-coord-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var save = Path.Combine(root, "fixture.save"); var path = SavePathResolver.Sidecar(save);
            var original = TransferPayloadCodec.Encode(State()); File.WriteAllBytes(path, original);
            var api = new Api();
            using (var off = new CoordinatedTransfers(api, api, null, false, _ => { }, () => { }, _ => { }))
            { api.Restore(null, save); Assert.Empty(TransferPayloadCodec.Decode(api.Provider!.Capture()).Items); }
            using var on = new CoordinatedTransfers(api, api, null, true, _ => { }, () => { }, _ => { });
            api.Restore(null, save); Assert.Equal(original, api.Provider!.Capture());
            Assert.Equal(original, File.ReadAllBytes(path)); Assert.Single(Directory.GetFiles(root));
            File.WriteAllText(path, "broken");
            api.Restore(original, save); Assert.Equal(original, api.Provider.Capture());
            Assert.ThrowsAny<Exception>(() => api.Restore(null, save));
            Assert.Equal("broken", File.ReadAllText(path)); Assert.False(on.CanOperate);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void MalformedFutureAndOversizedPayloadsAreRejected()
    {
        Assert.False(TransferPayloadCodec.IsValid(TransferPayloadCodec.Encode(State() with { Version = 999 })));
        var item = State().Items.Single();
        foreach (var bad in new[] { item with { FeeCredits = -1 }, item with { RemainingSeconds = float.NaN }, item with { Manifest = new[] { new TransferManifestLine("iron", -1) } } })
            Assert.False(TransferPayloadCodec.IsValid(TransferPayloadCodec.Encode(new TransferSidecar(1, new[] { bad }))));
        Assert.False(TransferPayloadCodec.IsValid(new byte[TransferPayloadCodec.MaxBytes + 1]));
        Assert.Throws<InvalidDataException>(() => TransferPayloadCodec.Encode(new TransferSidecar(1, new[] { item with { Id = new string('x', TransferPayloadCodec.MaxBytes + 1) } })));
    }

    [Theory]
    [InlineData("0.1.1", false)]
    [InlineData("0.1.2", true)]
    [InlineData("0.2.0", false)]
    public void RequiresNewPersistenceContract(string version, bool expected)
        => Assert.Equal(expected, TransferLifecycle.IsCompatible(new Version(version), new Api()));

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(-1f)]
    [InlineData(float.PositiveInfinity)]
    public void InvalidEtaCannotDebitOrReserve(float eta)
    {
        var api = new Api(); var materials = new Materials(); var credits = new Credits();
        var cfg = TransferConfig.Defaults() with { EtaBaseSeconds = eta, EtaMinSeconds = eta, EtaMaxSeconds = eta };
        var engine = new TransferEngine(new TransferQueue(0), materials, credits, cfg);
        using var controller = new CoordinatedTransfers(api, api, engine, false, _ => { }, () => { }, _ => { });
        api.Restore(null);
        var result = engine.RequestTransfer("src", "dst", new[] { new TransferManifestLine("iron", 10) }, 0);
        Assert.False(result.IsSuccess); Assert.Equal(TransferError.PersistenceUnavailable, result.Error);
        Assert.Equal(10000, credits.Current); Assert.Equal(100, materials.Source); Assert.Empty(engine.Pending);
    }

    private static TransferSidecar State() => new(1, new[] { new TransferRequest("id", "src", "dst", new[] { new TransferManifestLine("iron", 10) }, 10, 0, 60, 50, TransferStatus.Pending) });
    private sealed class Materials : IMaterialStorageMutator
    {
        internal int Source = 100;
        public void Reserve(string s, IReadOnlyList<TransferManifestLine> m) => Source -= m.Sum(x => x.Quantity);
        public void Return(string s, IReadOnlyList<TransferManifestLine> m) => Source += m.Sum(x => x.Quantity);
        public void Deliver(string s, IReadOnlyList<TransferManifestLine> m) { }
    }
    private sealed class Credits : ICreditsMutator
    {
        public int Current { get; private set; } = 10000;
        public bool TryDebit(int amount) { if (Current < amount) return false; Current -= amount; return true; }
    }
    private sealed class Api : IPersistenceApi, ILifecycleApi, ILifecycleDispatchState
    {
        internal PersistenceProvider? Provider;
        internal Handle Handle = new();
        private event Action<LifecycleEvent>? Events;
        public SessionSnapshot? CurrentSession { get; private set; }
        public bool IsDispatchingCallbacks => false;
        public IReadOnlyList<CapabilityStatus> Capabilities { get; } = new[] { new CapabilityStatus("session-lifecycle", true, false, "test"), new CapabilityStatus("save-outcomes", true, false, "test") };
        public IPersistenceRegistration Register(PersistenceProvider provider) { Provider = provider; Handle = new Handle(); return Handle; }
        public IDisposable Subscribe(string owner, Action<LifecycleEvent> callback) { Events += callback; return new Subscription(() => Events -= callback); }
        internal void Restore(byte[]? payload, string? path = null)
        {
            Handle.MutationAllowed = false;
            CurrentSession = new SessionSnapshot(Guid.NewGuid(), SessionPhase.GameplayInitialized, SessionOrigin.SaveLoad, path);
            Provider!.Restore(CurrentSession, payload); Handle.MutationAllowed = true;
        }
        internal void Emit(LifecycleEvent e) { Handle.MutationAllowed = false; Events?.Invoke(e); }
    }
    private sealed class Handle : IPersistenceRegistration
    {
        public bool MutationAllowed { get; set; }
        public string Status => MutationAllowed ? "ready" : "inactive";
        public void Dispose() => MutationAllowed = false;
    }
    private sealed class Subscription : IDisposable
    {
        private readonly Action _dispose;
        internal Subscription(Action dispose) { _dispose = dispose; }
        public void Dispose() => _dispose();
    }
}
