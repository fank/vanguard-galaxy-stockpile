using System.Collections.Generic;
using BepInEx.Logging;
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
    public static Sprite? FindByNameAndRect(string name, int rectX, int rectY)
    {
        return FindByNameAndRect(name, rectX, rectY, log: null);
    }

    public static Sprite? FindByNameAndRect(
        string name, int rectX, int rectY,
        BepInEx.Logging.ManualLogSource? log)
    {
        if (string.IsNullOrEmpty(name)) return null;
        var key = $"{name}@{rectX},{rectY}";
        if (_cache.TryGetValue(key, out var hit)) return hit;

        Sprite? found = null;
        var candidates = new List<Sprite>();
        foreach (var s in Resources.FindObjectsOfTypeAll<Sprite>())
        {
            if (s == null || s.name != name) continue;
            candidates.Add(s);
            // Tolerate fractional rounding by rounding instead of casting.
            if (Mathf.RoundToInt(s.rect.x) == rectX &&
                Mathf.RoundToInt(s.rect.y) == rectY)
            {
                found = s;
                // Keep iterating for diagnostics — but we'd return this one.
            }
        }

        if (log != null)
        {
            if (candidates.Count == 0)
                log.LogWarning($"SpriteLookup: no Sprite named '{name}' loaded.");
            else
            {
                var summary = string.Join(", ", candidates.ConvertAll(c =>
                    $"({c.rect.x:F1},{c.rect.y:F1} {c.rect.width:F0}x{c.rect.height:F0} tex={c.texture?.name})"));
                if (found != null)
                    log.LogInfo(
                        $"SpriteLookup '{name}@{rectX},{rectY}': matched 1 of " +
                        $"{candidates.Count} candidate(s). Candidates: {summary}");
                else
                    log.LogWarning(
                        $"SpriteLookup '{name}@{rectX},{rectY}': no rect match among " +
                        $"{candidates.Count} candidate(s). Candidates: {summary}");
            }
        }

        if (found != null) _cache[key] = found;
        return found;
    }
}
