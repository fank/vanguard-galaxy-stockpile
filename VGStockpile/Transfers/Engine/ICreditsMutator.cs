namespace VGStockpile.Transfers.Engine;

internal interface ICreditsMutator
{
    int Current { get; }
    bool TryDebit(int amount);
}
