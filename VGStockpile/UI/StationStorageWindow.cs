using System;
using System.Collections;
using System.Collections.Generic;
using Behaviour.UI.Tooltip;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VGStockpile.Data;

namespace VGStockpile.UI;

internal sealed class StationStorageWindow : MonoBehaviour
{
    private RectTransform _root        = null!;
    private RectTransform _gridContent = null!;
    private RectTransform _filterStrip = null!;
    private GameObject    _emptyState  = null!;

    private StorageGridBuilder              _builder         = null!;
    private MaterialCatalog                 _catalog         = null!;
    private Func<HashSet<MaterialCategory>> _initialActive   = null!;
    private Action<HashSet<MaterialCategory>> _onActiveChanged = null!;
    private Action<StationStorageSnapshot>  _onLabelClick    = null!;
    private Func<bool>                      _verbose         = () => false;
    private Action<string>                  _log             = _ => { };

    private readonly HashSet<MaterialCategory> _active = new();
    private readonly Dictionary<MaterialCategory, Image> _categoryButtons = new();

    private IReadOnlyList<StationStorageSnapshot> _currentSnapshots =
        Array.Empty<StationStorageSnapshot>();

    // Color scheme matches VGHangar's filter buttons.
    private static readonly Color BtnActive   = new(0.30f, 0.40f, 0.50f, 0.85f);
    private static readonly Color BtnInactive = new(0.20f, 0.20f, 0.20f, 0.80f);

    // Visual descriptors for each filter button. Sprite rect coordinates
    // disambiguate runtime name collisions — Resources.FindObjectsOfTypeAll
    // can return multiple Sprite instances sharing a name with different
    // atlas rects (see SpriteLookup). Coords match the values dumped by
    // IconDumper into BepInEx/cache/vgstockpile-icons/manifest.tsv.
    private static readonly (MaterialCategory Cat, string Sprite, int RectX, int RectY, string Label)[] FilterDefs =
    {
        (MaterialCategory.Ore,             "OreIcons_2",       192, 384, "Ores"),
        (MaterialCategory.RefinedCanister, "MaterialIcons_0",    0,  96, "Refined Canisters"),
        (MaterialCategory.RefinedGoods,    "CraftingIcons_0",    0, 384, "Refined Products"),
        (MaterialCategory.Crystal,         "CrystalIcons_5",     0,   0, "Crystals"),
        (MaterialCategory.TradeGoods,      "CraftingIcons_19", 384,  96, "Trade Goods"),
        (MaterialCategory.Salvage,         "SalvageIcons_0",     0,  96, "Salvage"),
    };

    public static StationStorageWindow Create(
        Canvas hudCanvas,
        StorageGridBuilder builder,
        MaterialCatalog catalog,
        Func<HashSet<MaterialCategory>> initialActive,
        Action<HashSet<MaterialCategory>> onActiveChanged,
        Action<StationStorageSnapshot> onLabelClick,
        Func<bool> verbose,
        Action<string> log)
    {
        var go = new GameObject(
            "VGStockpile.Window",
            typeof(RectTransform), typeof(CanvasGroup), typeof(Image),
            typeof(StationStorageWindow));
        go.transform.SetParent(hudCanvas.transform, worldPositionStays: false);

        var w = go.GetComponent<StationStorageWindow>();
        w._root             = (RectTransform)go.transform;
        w._builder          = builder;
        w._catalog          = catalog;
        w._initialActive    = initialActive;
        w._onActiveChanged  = onActiveChanged;
        w._onLabelClick     = onLabelClick;
        w._verbose          = verbose;
        w._log              = log;
        foreach (var c in initialActive()) w._active.Add(c);
        w.BuildLayout();
        w.Hide();
        return w;
    }

    public void Show(IReadOnlyList<StationStorageSnapshot> snapshots)
    {
        _currentSnapshots = snapshots;
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
        rt.sizeDelta = new Vector2(0f, 40f);
        rt.anchoredPosition = Vector2.zero;
        header.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.18f, 1f);

        var title = MakeLabel("Title", header.transform, "Station Stockpiles", 16f, FontStyles.Bold);
        var trt = (RectTransform)title.transform;
        trt.anchorMin = new Vector2(0f, 0f);
        trt.anchorMax = new Vector2(0f, 1f);
        trt.pivot     = new Vector2(0f, 0.5f);
        trt.sizeDelta = new Vector2(220f, 0f);
        trt.anchoredPosition = new Vector2(12f, 0f);

        // Filter strip: row of category-toggle buttons centered to the right
        // of the title.
        var stripGo = new GameObject("FilterStrip",
            typeof(RectTransform), typeof(HorizontalLayoutGroup));
        var strt = (RectTransform)stripGo.transform;
        strt.SetParent(header.transform, worldPositionStays: false);
        strt.anchorMin = new Vector2(1f, 0.5f);
        strt.anchorMax = new Vector2(1f, 0.5f);
        strt.pivot     = new Vector2(1f, 0.5f);
        // Width grows with the number of buttons (6 × 30 + 5 × 6 spacing = 210).
        strt.sizeDelta = new Vector2(260f, 32f);
        strt.anchoredPosition = new Vector2(-64f, 0f);
        var hlg = stripGo.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6f;
        hlg.childAlignment = TextAnchor.MiddleRight;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;
        _filterStrip = strt;
        BuildCategoryButtons();

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
        var clblText = clbl.GetComponent<TextMeshProUGUI>();
        clblText.alignment = TextAlignmentOptions.Center;
        clblText.color     = Color.white;
    }

    private void BuildCategoryButtons()
    {
        foreach (var (cat, sprName, rectX, rectY, label) in FilterDefs)
        {
            var btnGo = new GameObject($"Filter_{cat}",
                typeof(RectTransform), typeof(Image), typeof(Button),
                typeof(LayoutElement));
            btnGo.transform.SetParent(_filterStrip, worldPositionStays: false);
            var le = btnGo.GetComponent<LayoutElement>();
            le.preferredWidth  = 30f;
            le.preferredHeight = 30f;
            le.flexibleWidth   = 0f;

            var bg = btnGo.GetComponent<Image>();
            bg.color = _active.Contains(cat) ? BtnActive : BtnInactive;
            _categoryButtons[cat] = bg;

            var btn = btnGo.GetComponent<Button>();
            var captured = cat;
            btn.onClick.AddListener(() => OnFilterClicked(captured));

            // Inner icon Image — fits inside with a small inset so the
            // background tint is visible as a border.
            var iconGo = new GameObject("Icon",
                typeof(RectTransform), typeof(Image));
            var irt = (RectTransform)iconGo.transform;
            irt.SetParent(btnGo.transform, worldPositionStays: false);
            irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
            irt.offsetMin = new Vector2(3f, 3f);
            irt.offsetMax = new Vector2(-3f, -3f);
            var iconImg = iconGo.GetComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.raycastTarget  = false;
            // Pass plugin log so the rect-match diagnostic fires for filter
            // buttons too. Verbose mode controls whether the parent plugin
            // even hits this path.
            var sprite = SpriteLookup.FindByNameAndRect(sprName, rectX, rectY,
                             VGStockpile.Plugin.Log)
                         ?? SpriteLookup.FindByName(sprName);
            if (sprite != null) { iconImg.sprite = sprite; iconImg.color = Color.white; }
            else                { iconImg.color  = new Color(0.5f, 0.5f, 0.5f, 0.6f); }

            // Plain TooltipSource (not ItemTooltipSource) — vanilla treats
            // these as named hover regions with a title + body.
            var tip = btnGo.AddComponent<TooltipSource>();
            tip.Title    = label;
            tip.BodyText = $"Toggle visibility of {label} in the grid.";
        }
    }

    private void OnFilterClicked(MaterialCategory cat)
    {
        if (_active.Contains(cat)) _active.Remove(cat);
        else                       _active.Add(cat);

        if (_categoryButtons.TryGetValue(cat, out var bg))
            bg.color = _active.Contains(cat) ? BtnActive : BtnInactive;

        _onActiveChanged(new HashSet<MaterialCategory>(_active));
        Render();
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
        srt.offsetMax = new Vector2(-8f, -48f);
        scroll.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.30f);

        var viewport = new GameObject("Viewport",
            typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        var vrt = (RectTransform)viewport.transform;
        vrt.SetParent(scroll.transform, worldPositionStays: false);
        vrt.anchorMin = new Vector2(0f, 0f);
        vrt.anchorMax = new Vector2(1f, 1f);
        vrt.offsetMin = Vector2.zero;
        vrt.offsetMax = Vector2.zero;
        viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);

        var content = new GameObject("Content",
            typeof(RectTransform), typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter));
        var crt = (RectTransform)content.transform;
        crt.SetParent(viewport.transform, worldPositionStays: false);
        crt.anchorMin = new Vector2(0f, 1f);
        crt.anchorMax = new Vector2(0f, 1f);
        crt.pivot     = new Vector2(0f, 1f);
        crt.anchoredPosition = Vector2.zero;

        var fitter = content.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
        var vlg = content.GetComponent<VerticalLayoutGroup>();
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth  = false;
        vlg.spacing = 2f;

        var sr = scroll.GetComponent<ScrollRect>();
        sr.viewport   = vrt;
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
        rt.offsetMax = new Vector2(0f, -48f);
        var lbl = go.GetComponent<TextMeshProUGUI>();
        lbl.alignment = TextAlignmentOptions.Center;
        _emptyState = go;
        _emptyState.SetActive(false);
    }

    private void Render()
    {
        for (int i = _gridContent.childCount - 1; i >= 0; i--)
            Destroy(_gridContent.GetChild(i).gameObject);

        var grid = _builder.Build(_currentSnapshots, _active);

        if (grid.Rows.Count == 0)
        {
            _emptyState.SetActive(true);
            return;
        }
        _emptyState.SetActive(false);

        BuildHeaderRow(grid.ColumnMaterialIds);

        foreach (var row in grid.Rows)
        {
            BuildDataRow(
                label: $"{row.Snapshot.SystemName} - {row.Snapshot.StationName}",
                materialIds: grid.ColumnMaterialIds,
                cells: row.Cells,
                snapshot: row.Snapshot);
        }

        if (_verbose()) StartCoroutine(LogGeometryNextFrame());
    }

    private const float StationLabelWidth = 240f;
    private const float MaterialCellWidth = 56f;

    private void BuildHeaderRow(IReadOnlyList<string> materialIds)
    {
        var rowGo = NewRow(isHeader: true);

        var labelGo = new GameObject("StationHeader",
            typeof(RectTransform), typeof(LayoutElement),
            typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(rowGo.transform, worldPositionStays: false);
        var lle = labelGo.GetComponent<LayoutElement>();
        lle.preferredWidth = StationLabelWidth;
        lle.flexibleWidth  = 0f;
        var lblText = labelGo.GetComponent<TextMeshProUGUI>();
        lblText.text      = "Station";
        lblText.fontSize  = 12f;
        lblText.fontStyle = FontStyles.Bold;
        lblText.alignment = TextAlignmentOptions.Left;

        foreach (var id in materialIds)
        {
            var cellGo = new GameObject("MaterialIconCell",
                typeof(RectTransform), typeof(LayoutElement), typeof(Image));
            cellGo.transform.SetParent(rowGo.transform, worldPositionStays: false);
            var le = cellGo.GetComponent<LayoutElement>();
            le.preferredWidth  = MaterialCellWidth;
            le.preferredHeight = 28f;
            le.flexibleWidth   = 0f;
            var hit = cellGo.GetComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);

            var imgGo = new GameObject("Icon",
                typeof(RectTransform), typeof(Image));
            var irt = (RectTransform)imgGo.transform;
            irt.SetParent(cellGo.transform, worldPositionStays: false);
            irt.anchorMin = new Vector2(1f, 0.5f);
            irt.anchorMax = new Vector2(1f, 0.5f);
            irt.pivot     = new Vector2(1f, 0.5f);
            irt.sizeDelta = new Vector2(24f, 24f);
            irt.anchoredPosition = Vector2.zero;

            var img = imgGo.GetComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget  = false;
            var sprite = _catalog.Icon(id);
            if (sprite != null) { img.sprite = sprite; img.color = Color.white; }
            else                { img.color  = new Color(0.3f, 0.3f, 0.3f, 0.6f); }

            AttachItemTooltip(cellGo, id);
        }
    }

    private void BuildDataRow(
        string label,
        IReadOnlyList<string> materialIds,
        IReadOnlyList<string> cells,
        StationStorageSnapshot snapshot)
    {
        var rowGo = NewRow(isHeader: false);

        var labelGo = new GameObject("Label",
            typeof(RectTransform), typeof(LayoutElement),
            typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(rowGo.transform, worldPositionStays: false);
        var lle = labelGo.GetComponent<LayoutElement>();
        lle.preferredWidth = StationLabelWidth;
        lle.flexibleWidth  = 0f;
        var lblText = labelGo.GetComponent<TextMeshProUGUI>();
        lblText.text      = label;
        lblText.fontSize  = 12f;
        lblText.alignment = TextAlignmentOptions.Left;
        lblText.color     = new Color(0.78f, 0.85f, 1f, 1f);

        var btn = labelGo.AddComponent<Button>();
        var snap = snapshot;
        btn.onClick.AddListener(() => _onLabelClick(snap));

        for (int i = 0; i < materialIds.Count; i++)
        {
            var id  = materialIds[i];
            var qty = cells[i];

            var cellGo = new GameObject("Cell",
                typeof(RectTransform), typeof(LayoutElement),
                typeof(TextMeshProUGUI));
            cellGo.transform.SetParent(rowGo.transform, worldPositionStays: false);
            var ce = cellGo.GetComponent<LayoutElement>();
            ce.preferredWidth = MaterialCellWidth;
            ce.flexibleWidth  = 0f;
            var ctxt = cellGo.GetComponent<TextMeshProUGUI>();
            ctxt.text      = qty;
            ctxt.fontSize  = 12f;
            ctxt.alignment = TextAlignmentOptions.MidlineRight;

            if (!string.IsNullOrEmpty(qty))
                AttachItemTooltip(cellGo, id);
        }
    }

    private GameObject NewRow(bool isHeader)
    {
        var rowGo = new GameObject(isHeader ? "HeaderRow" : "Row",
            typeof(RectTransform), typeof(HorizontalLayoutGroup),
            typeof(Image), typeof(LayoutElement));
        rowGo.transform.SetParent(_gridContent, worldPositionStays: false);
        rowGo.GetComponent<Image>().color = isHeader
            ? new Color(0.18f, 0.20f, 0.25f, 0.95f)
            : new Color(0.10f, 0.12f, 0.15f, 0.85f);
        rowGo.GetComponent<LayoutElement>().minHeight = isHeader ? 32f : 24f;
        var hlg = rowGo.GetComponent<HorizontalLayoutGroup>();
        hlg.childForceExpandHeight = true;
        hlg.spacing = 4f;
        hlg.padding = new RectOffset(4, 4, 2, 2);
        return rowGo;
    }

    private void AttachItemTooltip(GameObject go, string materialTypeId)
    {
        var type = _catalog.GetItemType(materialTypeId);
        if (type is null) return;
        var src = go.AddComponent<ItemTooltipSource>();
        src.SetItem(
            item:        type,
            count:       0,
            allowCompare: false,
            context:     ItemTooltipContext.InInventory);
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

    private IEnumerator LogGeometryNextFrame()
    {
        yield return null;

        string Fmt(string n, RectTransform? rt) =>
            rt == null ? $"{n}: <null>" : $"{n}: {rt.rect.width:F0}x{rt.rect.height:F0}";

        var firstRow = _gridContent != null && _gridContent.childCount > 0
            ? _gridContent.GetChild(0) as RectTransform
            : null;
        var viewport = _gridContent?.parent as RectTransform;

        _log(
            "geometry: " +
            $"{Fmt("root", _root)}, " +
            $"{Fmt("viewport", viewport)}, " +
            $"{Fmt("content", _gridContent)}, " +
            $"{Fmt("row0", firstRow)}, " +
            $"rows={_gridContent?.childCount ?? 0}");
    }
}
