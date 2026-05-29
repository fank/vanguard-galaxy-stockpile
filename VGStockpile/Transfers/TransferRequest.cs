using System.Collections.Generic;

namespace VGStockpile.Transfers;

internal sealed record TransferRequest(
    string Id,
    string SourceStationGuid,
    string DestStationGuid,
    IReadOnlyList<TransferManifestLine> Manifest,
    int FeeCredits,
    int JumpDistance,
    float TotalSeconds,
    float RemainingSeconds,
    TransferStatus Status);
