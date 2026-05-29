using TMPro;
using UnityEngine;

namespace VGStockpile.UI;

internal static class UiText
{
    /// <summary>
    /// Creates a TextMeshProUGUI label using the game's default TMP font.
    /// Mirrors StationStorageWindow.MakeLabel so all VGStockpile UI is visually consistent.
    /// </summary>
    public static GameObject Label(
        string name, Transform parent, string text, float size,
        FontStyles style = FontStyles.Normal,
        TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, worldPositionStays: false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text          = text;
        t.fontSize      = size;
        t.fontStyle     = style;
        t.alignment     = alignment;
        t.color         = Color.white;
        t.raycastTarget = false;
        return go;
    }

    public static TextMeshProUGUI Component(GameObject go) => go.GetComponent<TextMeshProUGUI>();

    /// <summary>
    /// Strips a leading '@', then inserts spaces before CamelCase transitions and digit-runs.
    /// Example: "@OreCommon32" → "Ore Common 32".
    /// </summary>
    public static string HumanizeIdentifier(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return raw;
        if (raw[0] == '@') raw = raw.Substring(1);
        var sb = new System.Text.StringBuilder(raw.Length + 8);
        for (var i = 0; i < raw.Length; i++)
        {
            var c = raw[i];
            if (i > 0)
            {
                var prev = raw[i - 1];
                if ((char.IsUpper(c) && !char.IsUpper(prev)) ||
                    (char.IsDigit(c) && !char.IsDigit(prev) && prev != ' '))
                    sb.Append(' ');
            }
            sb.Append(c);
        }
        return sb.ToString();
    }
}
