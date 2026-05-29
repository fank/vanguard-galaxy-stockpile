namespace VGStockpile.Transfers;

internal enum QuantityStepKind { Add, Max }

internal readonly record struct QuantityStepLadder(
    int LeftSmall,
    int LeftLarge,
    QuantityStepKind Center,
    int RightLarge,
    int RightSmall)
{
    public static QuantityStepLadder Build(int small, int large, int shiftMul, bool shiftHeld)
    {
        var s = shiftHeld ? small * shiftMul : small;
        var l = shiftHeld ? large * shiftMul : large;
        return new QuantityStepLadder(s, l, QuantityStepKind.Max, l, s);
    }
}
