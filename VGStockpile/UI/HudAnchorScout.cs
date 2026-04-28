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
    private static readonly string[] AnchorLabels = { "Cargo", "Armory", "Materials" };

    private const float PollInterval = 0.5f;
    private const int   MaxAttempts  = 60;   // 30s budget, then give up.

    private System.Action<Canvas>? _onFound;
    private ManualLogSource?       _log;
    private float                  _nextPollTime;
    private int                    _attempts;

    public static HudAnchorScout Begin(System.Action<Canvas> onFound, ManualLogSource log)
    {
        var go = new GameObject("VGStockpile.HudAnchorScout");
        Object.DontDestroyOnLoad(go);
        var s = go.AddComponent<HudAnchorScout>();
        s._onFound = onFound;
        s._log     = log;
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
                "Icon will not be attached this session.");
            Destroy(gameObject);
        }
    }

    private static Canvas? FindAnchorCanvas()
    {
        // Pass 1: TMP_Text labels.
        foreach (var t in Resources.FindObjectsOfTypeAll<TMP_Text>())
        {
            if (t == null || string.IsNullOrEmpty(t.text)) continue;
            if (!IsAnchorLabel(t.text)) continue;
            if (!t.gameObject.activeInHierarchy) continue;

            var canvas = t.GetComponentInParent<Canvas>();
            if (canvas != null) return canvas.rootCanvas;
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
}
