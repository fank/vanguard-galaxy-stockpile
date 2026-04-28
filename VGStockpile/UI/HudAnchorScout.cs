using System.Collections.Generic;
using BepInEx.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VGStockpile.UI;

// Polls each frame for a usable HUD anchor canvas. Vanilla's HudManager.Awake
// fires before its child canvas is built, and the side menu (which contains
// "Cargo", "Armory", "Materials" tab labels) appears asynchronously. So we
// scan all loaded GameObjects for one of those known label strings, walk up
// to the root Canvas, and call back once when we find it. After the callback
// fires (or after `MaxAttempts` is reached) the component disables itself.
internal sealed class HudAnchorScout : MonoBehaviour
{
    // Strings we expect to see in the side-menu tab labels (matched
    // case-insensitively against TMP_Text and UnityEngine.UI.Text).
    private static readonly string[] AnchorLabels = { "Cargo", "Armory", "Materials" };

    // Poll roughly twice per second; tab labels usually appear within 1-2s
    // of the main scene loading.
    private const float  PollInterval = 0.5f;
    private const int    MaxAttempts  = 60;   // 30s budget, then give up.

    private System.Action<Canvas>? _onFound;
    private ManualLogSource?       _log;
    private bool                   _verbose;
    private float                  _nextPollTime;
    private int                    _attempts;

    public static HudAnchorScout Begin(
        System.Action<Canvas> onFound,
        ManualLogSource log,
        bool verbose)
    {
        var go = new GameObject("VGStockpile.HudAnchorScout");
        Object.DontDestroyOnLoad(go);
        var s = go.AddComponent<HudAnchorScout>();
        s._onFound = onFound;
        s._log     = log;
        s._verbose = verbose;
        s._nextPollTime = 0f;
        return s;
    }

    private void Update()
    {
        if (Time.unscaledTime < _nextPollTime) return;
        _nextPollTime = Time.unscaledTime + PollInterval;
        _attempts++;

        var anchor = FindAnchorCanvas();
        if (anchor != null)
        {
            _log?.LogInfo($"HudAnchorScout: anchor canvas '{anchor.name}' found after {_attempts} attempt(s).");
            _onFound?.Invoke(anchor);
            Destroy(gameObject);
            return;
        }

        if (_attempts >= MaxAttempts)
        {
            _log?.LogWarning(
                $"HudAnchorScout: gave up after {MaxAttempts} attempts (~{MaxAttempts * PollInterval:F0}s). " +
                "Icon will not be attached this session. " +
                "Inspect BepInEx log for canvas survey at verbose=true.");
            DumpCanvasInventory();
            Destroy(gameObject);
            return;
        }

        if (_verbose && (_attempts % 4 == 0))
        {
            _log?.LogDebug($"HudAnchorScout: still searching (attempt {_attempts}).");
            DumpCanvasInventory();
        }
    }

    private Canvas? FindAnchorCanvas()
    {
        // Pass 1: TMP_Text labels.
        foreach (var t in Resources.FindObjectsOfTypeAll<TMP_Text>())
        {
            if (t == null || string.IsNullOrEmpty(t.text)) continue;
            if (!IsAnchorLabel(t.text)) continue;
            if (!t.gameObject.activeInHierarchy) continue;

            var canvas = t.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                if (_verbose)
                    _log?.LogDebug(
                        $"HudAnchorScout: TMP_Text '{t.text.Trim()}' on '{t.gameObject.name}' → " +
                        $"canvas '{canvas.rootCanvas.name}'.");
                return canvas.rootCanvas;
            }
        }

        // Pass 2: classic Text labels (some vanilla UI still uses these).
        foreach (var t in Resources.FindObjectsOfTypeAll<Text>())
        {
            if (t == null || string.IsNullOrEmpty(t.text)) continue;
            if (!IsAnchorLabel(t.text)) continue;
            if (!t.gameObject.activeInHierarchy) continue;

            var canvas = t.GetComponentInParent<Canvas>();
            if (canvas != null) return canvas.rootCanvas;
        }

        return null;
    }

    private static bool IsAnchorLabel(string text)
    {
        var trimmed = text.Trim();
        foreach (var a in AnchorLabels)
        {
            if (string.Equals(trimmed, a, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private void DumpCanvasInventory()
    {
        if (_log is null) return;
        var canvases = Resources.FindObjectsOfTypeAll<Canvas>();
        var seen = new HashSet<string>();
        var summary = new List<string>();
        foreach (var c in canvases)
        {
            if (c == null) continue;
            var key = c.rootCanvas != null ? c.rootCanvas.name : c.name;
            if (!seen.Add(key)) continue;
            var active = c.rootCanvas != null && c.rootCanvas.gameObject.activeInHierarchy;
            summary.Add($"{key}{(active ? "" : "(inactive)")}");
        }
        _log.LogDebug($"HudAnchorScout: canvases in scene ({summary.Count}): {string.Join(", ", summary)}");
    }
}
