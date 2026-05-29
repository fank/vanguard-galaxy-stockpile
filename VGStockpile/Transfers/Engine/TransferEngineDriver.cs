using System;
using UnityEngine;

namespace VGStockpile.Transfers.Engine;

internal sealed class TransferEngineDriver : MonoBehaviour
{
    private TransferEngine _engine = null!;
    private Action<TransferRequest>? _onCompleted;

    public static TransferEngineDriver Attach(
        GameObject host, TransferEngine engine,
        Action<TransferRequest>? onCompleted = null)
    {
        var d = host.AddComponent<TransferEngineDriver>();
        d._engine      = engine;
        d._onCompleted = onCompleted;
        return d;
    }

    private void Update()
    {
        if (!ShouldTick()) return;
        var completed = _engine.Tick(Time.deltaTime);
        for (var i = 0; i < completed.Count; i++)
            _onCompleted?.Invoke(completed[i]);
    }

    private static bool ShouldTick()
    {
        // Per Phase 0 findings: GamePlayer.current must be non-null (active gameplay)
        // AND game must not be paused. Time.timeScale alone is unreliable.
        return Source.Player.GamePlayer.current is not null
            && !Behaviour.GameManager.isPaused;
    }
}
