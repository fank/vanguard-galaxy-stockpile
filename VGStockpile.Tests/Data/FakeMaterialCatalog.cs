using System.Collections.Generic;
using VGStockpile.Data;

namespace VGStockpile.Tests.Data;

internal sealed class FakeMaterialCatalog : IMaterialCatalog
{
    private readonly Dictionary<string, (string Name, MaterialCategory Cat)> _entries = new();

    public FakeMaterialCatalog Add(string id, string name, MaterialCategory cat)
    {
        _entries[id] = (name, cat);
        return this;
    }

    public string DisplayName(string id) =>
        _entries.TryGetValue(id, out var e) ? e.Name : id;

    public MaterialCategory Category(string id) =>
        _entries.TryGetValue(id, out var e) ? e.Cat : MaterialCategory.Unknown;
}
