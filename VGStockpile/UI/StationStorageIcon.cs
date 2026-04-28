using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VGStockpile.UI;

internal sealed class StationStorageIcon : MonoBehaviour
{
    private const string SpriteName = "SkillIcons1_103";

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

        // Inner icon.
        var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        var irt = (RectTransform)iconGo.transform;
        irt.SetParent(rt, worldPositionStays: false);
        irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
        irt.offsetMin = new Vector2(4f, 4f);
        irt.offsetMax = new Vector2(-4f, -4f);
        var iconImg = iconGo.GetComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget  = false;
        var sprite = SpriteLookup.FindByName(SpriteName);
        if (sprite != null)
        {
            iconImg.sprite = sprite;
            iconImg.color  = Color.white;
        }
        else
        {
            // Fallback: a small "STK" label so the button is at least visible.
            iconImg.color = new Color(0f, 0f, 0f, 0f);
            var fbGo = new GameObject("Fallback", typeof(RectTransform), typeof(TextMeshProUGUI));
            var fbRt = (RectTransform)fbGo.transform;
            fbRt.SetParent(rt, worldPositionStays: false);
            fbRt.anchorMin = Vector2.zero; fbRt.anchorMax = Vector2.one;
            fbRt.offsetMin = Vector2.zero; fbRt.offsetMax = Vector2.zero;
            var lbl = fbGo.GetComponent<TextMeshProUGUI>();
            lbl.text      = "STK";
            lbl.alignment = TextAlignmentOptions.Center;
            lbl.fontSize  = 14f;
            lbl.fontStyle = FontStyles.Bold;
        }

        var icon = go.GetComponent<StationStorageIcon>();
        var button = go.GetComponent<Button>();
        button.onClick.AddListener(() => onClick());
        return icon;
    }
}
