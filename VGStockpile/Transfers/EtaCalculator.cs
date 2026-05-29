namespace VGStockpile.Transfers;

internal static class EtaCalculator
{
    public static float ComputeSeconds(int jumpDistance, TransferConfig cfg)
    {
        var raw = cfg.EtaBaseSeconds + cfg.EtaPerJumpSeconds * jumpDistance;
        if (raw < cfg.EtaMinSeconds) return cfg.EtaMinSeconds;
        if (raw > cfg.EtaMaxSeconds) return cfg.EtaMaxSeconds;
        return raw;
    }
}
