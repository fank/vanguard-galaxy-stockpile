using System.Collections.Generic;
using VGStockpile.Data;

namespace VGStockpile.Tests.Data;

internal sealed class FakeMaterialCatalog : IMaterialCatalog
{
    private readonly Dictionary<string, Entry> _entries = new();

    private sealed record Entry(string Name, MaterialCategory Cat, int CatOrder, int GameplayOrder);

    public FakeMaterialCatalog Add(
        string id, string name, MaterialCategory cat,
        int catOrder = 0, int gameplayOrder = 0)
    {
        _entries[id] = new Entry(name, cat, catOrder, gameplayOrder);
        return this;
    }

    public string DisplayName(string id) =>
        _entries.TryGetValue(id, out var e) ? e.Name : id;

    public MaterialCategory Category(string id) =>
        _entries.TryGetValue(id, out var e) ? e.Cat : MaterialCategory.Unknown;

    public int CategoryOrder(string id) =>
        _entries.TryGetValue(id, out var e) ? e.CatOrder : int.MaxValue;

    public int GameplayTypeOrder(string id) =>
        _entries.TryGetValue(id, out var e) ? e.GameplayOrder : int.MaxValue;

    public string SortName(string id) =>
        _entries.TryGetValue(id, out var e) ? e.Name : id;
}
