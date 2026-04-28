using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VGStockpile.Data;

namespace VGStockpile.UI;

internal sealed class StationStorageWindow : MonoBehaviour
{
    private RectTransform _root        = null!;
    private RectTransform _gridContent = null!;
    private Toggle        _hideOres    = null!;
    private GameObject    _emptyState  = null!;

    private StorageGridBuilder         _builder         = null!;
    private System.Func<bool>          _hideOresDefault = null!;
    private System.Action<StationStorageSnapshot> _onLabelClick = null!;

    private IReadOnlyList<StationStorageSnapshot> _currentSnapshots =
        System.Array.Empty<StationStorageSnapshot>();

    public static StationStorageWindow Create(
        Canvas hudCanvas,
        StorageGridBuilder builder,
        System.Func<bool> hideOresDefault,
        System.Action<StationStorageSnapshot> onLabelClick)
    {
        var go = new GameObject(
            "VGStockpile.Window",
            typeof(RectTransform), typeof(CanvasGroup), typeof(Image),
            typeof(StationStorageWindow));
        go.transform.SetParent(hudCanvas.transform, worldPositionStays: false);

        var w = go.GetComponent<StationStorageWindow>();
        w._root            = (RectTransform)go.transform;
        w._builder         = builder;
        w._hideOresDefault = hideOresDefault;
        w._onLabelClick    = onLabelClick;
        w.BuildLayout();
        w.Hide();
        return w;
    }

    public void Show(IReadOnlyList<StationStorageSnapshot> snapshots)
    {
        _currentSnapshots = snapshots;
        if (_hideOres != null) _hideOres.isOn = _hideOresDefault();
        gameObject.SetActive(true);
        Render();
    }

    public void Hide() => gameObject.SetActive(false);

    public void Toggle(IReadOnlyList<StationStorageSnapshot> snapshots)
    {
        if (gameObject.activeSelf) Hide();
        else Show(snapshots);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Hide();
    }

    private void BuildLayout()
    {
        _root.anchorMin = new Vector2(0.15f, 0.10f);
        _root.anchorMax = new Vector2(0.85f, 0.90f);
        _root.offsetMin = Vector2.zero;
        _root.offsetMax = Vector2.zero;

        var bg = GetComponent<Image>();
        bg.color = new Color(0.06f, 0.08f, 0.11f, 0.92f);

        BuildHeader();
        BuildGrid();
        BuildEmptyState();
    }

    private void BuildHeader()
    {
        var header = new GameObject("Header",
            typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)header.transform;
        rt.SetParent(_root, worldPositionStays: false);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, 32f);
        rt.anchoredPosition = Vector2.zero;
        header.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.18f, 1f);

        var title = MakeLabel("Title", header.transform, "Station Stockpiles", 16f, FontStyles.Bold);
        var trt = (RectTransform)title.transform;
        trt.anchorMin = new Vector2(0f, 0f);
        trt.anchorMax = new Vector2(0f, 1f);
        trt.pivot     = new Vector2(0f, 0.5f);
        trt.sizeDelta = new Vector2(220f, 0f);
        trt.anchoredPosition = new Vector2(12f, 0f);

        var toggleGo = new GameObject("HideOres",
            typeof(RectTransform), typeof(Toggle), typeof(Image));
        var togRt = (RectTransform)toggleGo.transform;
        togRt.SetParent(header.transform, worldPositionStays: false);
        togRt.anchorMin = new Vector2(1f, 0.5f);
        togRt.anchorMax = new Vector2(1f, 0.5f);
        togRt.pivot     = new Vector2(1f, 0.5f);
        togRt.sizeDelta = new Vector2(140f, 24f);
        togRt.anchoredPosition = new Vector2(-60f, 0f);
        _hideOres = toggleGo.GetComponent<Toggle>();
        _hideOres.onValueChanged.AddListener(_ => Render());
        var togLabel = MakeLabel("Label", toggleGo.transform, "Hide ores", 12f, FontStyles.Normal);
        var tlRt = (RectTransform)togLabel.transform;
        tlRt.anchorMin = Vector2.zero; tlRt.anchorMax = Vector2.one;
        tlRt.offsetMin = Vector2.zero; tlRt.offsetMax = Vector2.zero;

        var closeGo = new GameObject("Close",
            typeof(RectTransform), typeof(Image), typeof(Button));
        var crt = (RectTransform)closeGo.transform;
        crt.SetParent(header.transform, worldPositionStays: false);
        crt.anchorMin = new Vector2(1f, 0.5f);
        crt.anchorMax = new Vector2(1f, 0.5f);
        crt.pivot     = new Vector2(1f, 0.5f);
        crt.sizeDelta = new Vector2(48f, 24f);
        crt.anchoredPosition = new Vector2(-8f, 0f);
        closeGo.GetComponent<Image>().color = new Color(0.30f, 0.10f, 0.10f, 0.85f);
        closeGo.GetComponent<Button>().onClick.AddListener(Hide);
        var clbl = MakeLabel("X", closeGo.transform, "Close", 12f, FontStyles.Bold);
        var clrt = (RectTransform)clbl.transform;
        clrt.anchorMin = Vector2.zero; clrt.anchorMax = Vector2.one;
        clrt.offsetMin = Vector2.zero; clrt.offsetMax = Vector2.zero;
    }

    private void BuildGrid()
    {
        var scroll = new GameObject("Scroll",
            typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        var srt = (RectTransform)scroll.transform;
        srt.SetParent(_root, worldPositionStays: false);
        srt.anchorMin = new Vector2(0f, 0f);
        srt.anchorMax = new Vector2(1f, 1f);
        srt.offsetMin = new Vector2(8f, 8f);
        srt.offsetMax = new Vector2(-8f, -40f);
        scroll.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.30f);

        var content = new GameObject("Content",
            typeof(RectTransform), typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter));
        var crt = (RectTransform)content.transform;
        crt.SetParent(scroll.transform, worldPositionStays: false);
        crt.anchorMin = new Vector2(0f, 1f);
        crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot     = new Vector2(0f, 1f);

        var fitter = content.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var vlg = content.GetComponent<VerticalLayoutGroup>();
        vlg.childForceExpandHeight = false;
        vlg.spacing = 2f;

        var sr = scroll.GetComponent<ScrollRect>();
        sr.content    = crt;
        sr.horizontal = true;
        sr.vertical   = true;

        _gridContent = crt;
    }

    private void BuildEmptyState()
    {
        var go = MakeLabel("Empty", _root, "No stations with stored materials.",
            14f, FontStyles.Italic);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(0f, 0f);
        rt.offsetMax = new Vector2(0f, -40f);
        var lbl = go.GetComponent<TextMeshProUGUI>();
        lbl.alignment = TextAlignmentOptions.Center;
        _emptyState = go;
        _emptyState.SetActive(false);
    }

    private void Render()
    {
        for (int i = _gridContent.childCount - 1; i >= 0; i--)
            Destroy(_gridContent.GetChild(i).gameObject);

        var grid = _builder.Build(_currentSnapshots, _hideOres.isOn);

        if (grid.Rows.Count == 0)
        {
            _emptyState.SetActive(true);
            return;
        }
        _emptyState.SetActive(false);

        BuildRow(isHeader: true,
            label: "Station",
            cells: grid.ColumnDisplayNames,
            snapshot: null);

        foreach (var row in grid.Rows)
        {
            BuildRow(isHeader: false,
                label: $"{row.Snapshot.SystemName} — {row.Snapshot.StationName}",
                cells: row.Cells,
                snapshot: row.Snapshot);
        }
    }

    private void BuildRow(
        bool isHeader, string label, IReadOnlyList<string> cells,
        StationStorageSnapshot? snapshot)
    {
        var rowGo = new GameObject(isHeader ? "HeaderRow" : "Row",
            typeof(RectTransform), typeof(HorizontalLayoutGroup),
            typeof(Image), typeof(LayoutElement));
        var rrt = (RectTransform)rowGo.transform;
        rrt.SetParent(_gridContent, worldPositionStays: false);
        rowGo.GetComponent<Image>().color = isHeader
            ? new Color(0.18f, 0.20f, 0.25f, 0.95f)
            : new Color(0.10f, 0.12f, 0.15f, 0.85f);
        rowGo.GetComponent<LayoutElement>().minHeight = 24f;
        var hlg = rowGo.GetComponent<HorizontalLayoutGroup>();
        hlg.childForceExpandHeight = true;
        hlg.spacing = 4f;
        hlg.padding = new RectOffset(4, 4, 2, 2);

        var labelGo = new GameObject("Label",
            typeof(RectTransform), typeof(LayoutElement),
            typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(rowGo.transform, worldPositionStays: false);
        var lle = labelGo.GetComponent<LayoutElement>();
        lle.preferredWidth = 240f;
        lle.flexibleWidth  = 0f;
        var lblText = labelGo.GetComponent<TextMeshProUGUI>();
        lblText.text = label;
        lblText.fontSize = 12f;
        lblText.fontStyle = isHeader ? FontStyles.Bold : FontStyles.Normal;
        lblText.alignment = TextAlignmentOptions.Left;

        if (!isHeader && snapshot is not null)
        {
            var btn = labelGo.AddComponent<Button>();
            var snap = snapshot;
            btn.onClick.AddListener(() => _onLabelClick(snap));
            lblText.color = new Color(0.78f, 0.85f, 1f, 1f);
        }

        foreach (var cell in cells)
        {
            var cellGo = new GameObject("Cell",
                typeof(RectTransform), typeof(LayoutElement),
                typeof(TextMeshProUGUI));
            cellGo.transform.SetParent(rowGo.transform, worldPositionStays: false);
            var ce = cellGo.GetComponent<LayoutElement>();
            ce.preferredWidth = 80f;
            ce.flexibleWidth  = 0f;
            var ctxt = cellGo.GetComponent<TextMeshProUGUI>();
            ctxt.text = cell;
            ctxt.fontSize = 12f;
            ctxt.fontStyle = isHeader ? FontStyles.Bold : FontStyles.Normal;
            ctxt.alignment = TextAlignmentOptions.MidlineRight;
        }
    }

    private static GameObject MakeLabel(
        string name, Transform parent, string text, float size, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, worldPositionStays: false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text      = text;
        t.fontSize  = size;
        t.fontStyle = style;
        t.alignment = TextAlignmentOptions.MidlineLeft;
        return go;
    }
}
