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
}
