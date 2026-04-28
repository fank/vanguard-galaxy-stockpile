using System.Collections.Generic;
using System.Linq;
using VGStockpile.Data;

namespace VGStockpile.UI;

internal sealed class StorageGridBuilder
{
    private readonly IMaterialCatalog _catalog;

    public StorageGridBuilder(IMaterialCatalog catalog) { _catalog = catalog; }

    public GridResult Build(
        IReadOnlyList<StationStorageSnapshot> snapshots,
        bool hideOres)
    {
        var visibleIds = snapshots
            .SelectMany(s => s.Items.Keys)
            .Distinct()
            .Where(id => !hideOres || _catalog.Category(id) != MaterialCategory.Ore)
            .OrderBy(id => _catalog.DisplayName(id), System.StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var displayNames = visibleIds
            .Select(id => _catalog.DisplayName(id))
            .ToArray();

        var rows = snapshots
            .Select(s =>
            {
                var cells = visibleIds
                    .Select(id => s.Items.TryGetValue(id, out var q)
                        ? CompactNumber.Format(q)
                        : "")
                    .ToArray();
                var visibleTotal = visibleIds.Sum(id =>
                    s.Items.TryGetValue(id, out var q) ? q : 0);
                return (Row: new GridRow(s, cells), Total: visibleTotal);
            })
            .OrderByDescending(t => t.Total)
            .ThenBy(t => t.Row.Snapshot.StationName, System.StringComparer.OrdinalIgnoreCase)
            .Select(t => t.Row)
            .ToArray();

        return new GridResult(visibleIds, displayNames, rows);
    }
}
