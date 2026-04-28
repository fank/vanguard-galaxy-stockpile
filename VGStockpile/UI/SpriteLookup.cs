using System.Collections.Generic;
using UnityEngine;

namespace VGStockpile.UI;

// Find a Sprite asset by name from anything currently loaded by Unity.
// Atlas sub-sprites work — Unity exposes them as standalone Sprite instances
// through Resources.FindObjectsOfTypeAll. Lookups are cached per name (the
// scan is O(n) over every loaded sprite).
internal static class SpriteLookup
{
    private static readonly Dictionary<string, Sprite?> _cache = new();

    public static Sprite? FindByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (_cache.TryGetValue(name, out var hit)) return hit;

        Sprite? found = null;
        foreach (var s in Resources.FindObjectsOfTypeAll<Sprite>())
        {
            if (s != null && s.name == name)
            {
                found = s;
                break;
            }
        }
        // Only cache successful resolutions. A null at startup may resolve
        // later once the asset bundle for this sprite has loaded.
        if (found != null) _cache[name] = found;
        return found;
    }
}
