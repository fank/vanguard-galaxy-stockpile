using System.Collections.Generic;

namespace VGStockpile.Transfers.Engine;

internal interface IMaterialStorageMutator
{
    void Reserve(string sourceGuid, IReadOnlyList<TransferManifestLine> manifest);
    void Deliver(string destGuid, IReadOnlyList<TransferManifestLine> manifest);
    void Return(string sourceGuid, IReadOnlyList<TransferManifestLine> manifest);
}
