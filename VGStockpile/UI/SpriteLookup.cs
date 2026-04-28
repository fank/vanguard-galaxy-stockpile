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

    // Disambiguating lookup: when multiple sprites share a name, pick the one
    // whose atlas rect matches (rect_x, rect_y) from the IconDumper manifest.
    // Game asset bundles can produce several Sprite instances with identical
    // names but different rects, and Resources.FindObjectsOfTypeAll iteration
    // order isn't stable — so plain name-lookup picks an arbitrary one.
    public static Sprite? FindByNameAndRect(string name, int rectX, int rectY)
    {
        if (string.IsNullOrEmpty(name)) return null;
        var key = $"{name}@{rectX},{rectY}";
        if (_cache.TryGetValue(key, out var hit)) return hit;

        Sprite? found = null;
        foreach (var s in Resources.FindObjectsOfTypeAll<Sprite>())
        {
            if (s == null || s.name != name) continue;
            if ((int)s.rect.x == rectX && (int)s.rect.y == rectY)
            {
                found = s;
                break;
            }
        }
        if (found != null) _cache[key] = found;
        return found;
    }
}
