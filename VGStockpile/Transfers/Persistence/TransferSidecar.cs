using System.Collections.Generic;

namespace VGStockpile.Transfers.Persistence;

internal sealed record TransferSidecar(int Version, IReadOnlyList<TransferRequest> Items)
{
    public const int CurrentVersion = 1;
    public static TransferSidecar Empty() =>
        new(CurrentVersion, System.Array.Empty<TransferRequest>());
}
