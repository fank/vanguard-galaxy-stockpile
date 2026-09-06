using System;

namespace VGStockpile.Transfers.Persistence;

internal interface ITransferPersistence : IDisposable
{
    bool CanOperate { get; }
}
