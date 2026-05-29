namespace VGStockpile.Transfers.Persistence;

internal interface ITransferStore
{
    TransferSidecar Load(string savePath);
    void Save(string savePath, TransferSidecar sidecar);
}
