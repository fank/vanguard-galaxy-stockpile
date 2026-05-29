using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VGStockpile.Transfers;

namespace VGStockpile.UI.Transfers;

internal sealed class TransferRowButtons : MonoBehaviour
{
    public Button PullButton { get; private set; } = null!;
    public Button PushButton { get; private set; } = null!;

    public static TransferRowButtons Create(
        Transform parent, TransferConfig cfg,
        Func<StationContext> getCtx,
        Action onPullClick,
        Action onPushClick)
    {
        var go = new GameObject("VGTransferRowButtons",
            typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        go.transform.SetParent(parent, worldPositionStays: false);

        var lg = go.GetComponent<HorizontalLayoutGroup>();
        lg.spacing              = 2f;
        lg.childForceExpandWidth  = false;
        lg.childForceExpandHeight = true;
        lg.childAlignment       = TextAnchor.MiddleLeft;

        var le = go.GetComponent<LayoutElement>();
        le.preferredWidth = 90f;
        le.minWidth       = 90f;

        var comp = go.AddComponent<TransferRowButtons>();
        comp.PullButton = MakeButton(go.transform, "Pull", onPullClick);
        comp.PushButton = MakeButton(go.transform, "Push", onPushClick);
        comp.RefreshState(cfg, getCtx());
        return comp;
    }

    private static Button MakeButton(Transform parent, string label, Action onClick)
    {
        var go = new GameObject($"VG{label}Btn",
            typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, worldPositionStays: false);

        var img = go.GetComponent<Image>();
        img.color = new Color(0.18f, 0.22f, 0.28f, 0.85f);

        var le = go.GetComponent<LayoutElement>();
        le.preferredWidth  = 42f;
        le.preferredHeight = 22f;

        var btn = go.GetComponent<Button>();
        btn.onClick.AddListener(() => onClick());

        var labelGo = UiText.Label("Label", go.transform, label, 12f,
            alignment: TextAlignmentOptions.Center);
        var tmp = UiText.Component(labelGo);
        tmp.alignment = TextAlignmentOptions.Center;

        var lrt = (RectTransform)labelGo.transform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        return btn;
    }

    public void RefreshState(TransferConfig cfg, StationContext ctx)
    {
        PullButton.interactable = EligibilityRules.CanPullFrom(ctx);
        PushButton.gameObject.SetActive(cfg.PushEnabled);
        PushButton.interactable = cfg.PushEnabled && EligibilityRules.CanPushTo(ctx, cfg);
    }
}
