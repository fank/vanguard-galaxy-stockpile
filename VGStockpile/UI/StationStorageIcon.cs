using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VGStockpile.UI;

internal sealed class StationStorageIcon : MonoBehaviour
{
    private Button _button = null!;

    public static StationStorageIcon Create(
        Canvas hudCanvas,
        Action onClick,
        float rightPadding,
        float topPadding)
    {
        var go = new GameObject("VGStockpile.Icon",
            typeof(RectTransform), typeof(Image), typeof(Button),
            typeof(StationStorageIcon));
        go.transform.SetParent(hudCanvas.transform, worldPositionStays: false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(40f, 40f);
        rt.anchoredPosition = new Vector2(-rightPadding, -topPadding);

        var bg = go.GetComponent<Image>();
        bg.color = new Color(0.10f, 0.14f, 0.20f, 0.85f);

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var lrt = (RectTransform)labelGo.transform;
        lrt.SetParent(rt, worldPositionStays: false);
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
        var lbl = labelGo.GetComponent<TextMeshProUGUI>();
        lbl.text = "STK";
        lbl.alignment = TextAlignmentOptions.Center;
        lbl.fontSize  = 14f;
        lbl.fontStyle = FontStyles.Bold;

        var icon = go.GetComponent<StationStorageIcon>();
        icon._button = go.GetComponent<Button>();
        icon._button.onClick.AddListener(() => onClick());
        return icon;
    }
}
