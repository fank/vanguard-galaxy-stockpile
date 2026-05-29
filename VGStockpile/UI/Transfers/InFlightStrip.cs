using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VGStockpile.Transfers;

namespace VGStockpile.UI.Transfers;

internal sealed class InFlightStrip : MonoBehaviour
{
    private Func<IReadOnlyList<TransferRequest>> _getPending = null!;
    private Func<string, bool> _onCancel = null!;
    private Action<string> _onLocate = null!;
    private Func<string, string> _stationDisplayName = null!;

    public static InFlightStrip Attach(
        Transform parent,
        Func<IReadOnlyList<TransferRequest>> getPending,
        Func<string, bool> onCancel,
        Action<string> onLocateDest,
        Func<string, string> stationDisplayName)
    {
        var go = new GameObject("VGInFlightStrip",
            typeof(RectTransform), typeof(HorizontalLayoutGroup));
        go.transform.SetParent(parent, worldPositionStays: false);

        // Stretch to fill the parent footer — without this the default 100x100
        // sizeDelta crams pills into a vertical sliver.
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var hlg = go.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing              = 6f;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;
        hlg.childAlignment       = TextAnchor.MiddleLeft;
        hlg.padding              = new RectOffset(8, 8, 4, 4);

        var c = go.AddComponent<InFlightStrip>();
        c._getPending         = getPending;
        c._onCancel           = onCancel;
        c._onLocate           = onLocateDest;
        c._stationDisplayName = stationDisplayName;
        return c;
    }

    private void Update() => Refresh();

    private void Refresh()
    {
        for (var i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        var pending = _getPending();
        if (pending.Count == 0) return;

        for (var i = 0; i < pending.Count; i++)
        {
            var r = pending[i];
            if (r.Status != TransferStatus.Pending) continue;
            BuildPill(r);
        }
    }

    private void BuildPill(TransferRequest r)
    {
        var pill = new GameObject($"Pill_{r.Id}",
            typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        pill.transform.SetParent(transform, worldPositionStays: false);

        pill.GetComponent<Image>().color = new Color(0.16f, 0.22f, 0.30f, 0.95f);

        var hlg = pill.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing              = 6f;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment       = TextAnchor.MiddleLeft;
        hlg.padding              = new RectOffset(6, 4, 2, 2);

        var le = pill.GetComponent<LayoutElement>();
        le.preferredWidth  = 240f;
        le.preferredHeight = 22f;

        // Body button: locate on click; sits behind the X button.
        var bodyBtn = pill.AddComponent<Button>();
        bodyBtn.transition = Selectable.Transition.None;
        bodyBtn.onClick.AddListener(() => _onLocate(r.DestStationGuid));

        var pillText =
            $"{Truncate(_stationDisplayName(r.SourceStationGuid), 10)} -> " +
            $"{Truncate(_stationDisplayName(r.DestStationGuid), 10)}  {Format(r.RemainingSeconds)}";

        var labelGo = UiText.Label("Label", pill.transform, pillText, 11f);
        var labelLe = labelGo.AddComponent<LayoutElement>();
        labelLe.flexibleWidth = 1f;

        var canCancel = r.RemainingSeconds > 0f;
        MakeXButton(pill.transform, canCancel, () => _onCancel(r.Id));
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s.Substring(0, max - 2) + "..";

    private static string Format(float seconds)
    {
        var s = (int)System.Math.Ceiling(seconds);
        if (s < 0) s = 0;
        return $"{s / 60:00}:{s % 60:00}";
    }

    private static Button MakeXButton(Transform parent, bool enabled, Action onClick)
    {
        var go = new GameObject("X",
            typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, worldPositionStays: false);
        go.GetComponent<Image>().color =
            enabled
                ? new Color(0.55f, 0.20f, 0.20f, 0.95f)
                : new Color(0.30f, 0.30f, 0.30f, 0.85f);

        var le = go.GetComponent<LayoutElement>();
        le.preferredWidth  = 18f;
        le.preferredHeight = 18f;

        var btn = go.GetComponent<Button>();
        btn.interactable = enabled;
        btn.onClick.AddListener(() => { if (enabled) onClick(); });

        var labelGo = UiText.Label("Label", go.transform, "X", 14f,
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
}
