using BepInEx.Logging;
using Source.Player;

namespace VGStockpile.Transfers.Engine;

internal sealed class CreditsMutator : ICreditsMutator
{
    private readonly ManualLogSource _log;

    public CreditsMutator(ManualLogSource log) { _log = log; }

    public int Current
    {
        get
        {
            var player = GamePlayer.current;
            if (player == null) return 0;
            var raw = player.credits;
            return raw > int.MaxValue ? int.MaxValue : (int)raw;
        }
    }

    public bool TryDebit(int amount)
    {
        if (amount < 0) return false;
        var player = GamePlayer.current;
        if (player == null)
        {
            _log.LogWarning($"TryDebit({amount}) failed: GamePlayer.current is null (no save loaded?).");
            return false;
        }
        var current = player.credits;
        if (current < amount)
        {
            _log.LogWarning($"TryDebit({amount}) failed: have {current}.");
            return false;
        }
        // Direct field write. RemoveCredits(float) exists on the publicized
        // stub but appears to be a no-op outside a transaction context
        // (shop / mission completion), so direct write is the reliable path.
        // `credits` is a public long field on GamePlayer.
        player.credits = current - amount;
        return true;
    }
}
