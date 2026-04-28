using System.Collections.Generic;

namespace VGStockpile.UI;

internal sealed record GridResult(
    IReadOnlyList<string> ColumnMaterialIds,
    IReadOnlyList<string> ColumnDisplayNames,
    IReadOnlyList<GridRow> Rows);
