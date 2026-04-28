using System.Collections.Generic;
using VGStockpile.Data;

namespace VGStockpile.UI;

internal sealed record GridRow(
    StationStorageSnapshot Snapshot,
    IReadOnlyList<string> Cells);
