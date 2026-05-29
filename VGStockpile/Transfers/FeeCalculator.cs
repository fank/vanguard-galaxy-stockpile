namespace VGStockpile.Transfers;

internal static class FeeCalculator
{
    public static int Compute(int jumpDistance, int totalUnits, TransferConfig cfg) =>
        cfg.FeeBase
        + cfg.FeePerUnit * totalUnits
        + cfg.FeePerJump * jumpDistance
        + (int)(cfg.FeePerUnitPerJump * totalUnits * jumpDistance);
}
