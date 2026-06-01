using System;
using System.Collections.Generic;
using System.Linq;
using VGStockpile.Data;

namespace VGStockpile.UI.Transfers;

internal readonly record struct TransferRowGroup(
    MaterialCategory Category,
    IReadOnlyList<string> MaterialIds);

/// <summary>
/// Pure ordering/grouping logic for the transfer dialog. Groups material ids
/// by <see cref="MaterialCategory"/> in the same order as the main view's
/// filter strip, and orders rows within each group by the same vanilla key
/// the stockpile grid uses (gameplay type, then name).
/// </summary>
internal static class TransferRowGrouping
{
    public static IReadOnlyList<TransferRowGroup> Build(
        IEnumerable<string> materialIds, IMaterialCatalog catalog)
    {
        return materialIds
            .GroupBy(id => catalog.Category(id))
            .OrderBy(g => MaterialCategoryDisplay.Order(g.Key))
            .Select(g => new TransferRowGroup(
                g.Key,
                g.OrderBy(id => catalog.GameplayTypeOrder(id))
                 .ThenBy(id => catalog.SortName(id), StringComparer.OrdinalIgnoreCase)
                 .ToArray()))
            .ToArray();
    }
}
