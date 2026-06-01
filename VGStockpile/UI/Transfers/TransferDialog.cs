using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VGStockpile.Data;
using VGStockpile.Transfers;

namespace VGStockpile.UI.Transfers;

internal enum TransferDirection { Pull, Push }

internal readonly record struct TransferDialogOutcome(bool Success, string? BannerMessage);

internal sealed class TransferDialog : MonoBehaviour
{
    /// <summary>
    /// Number of currently-open transfer dialogs. Used by
    /// <see cref="StationStorageWindow"/> to suppress its own ESC handler
    /// when a dialog is the topmost modal.
    /// </summary>
    public static int OpenCount { get; private set; }

    private void OnEnable()  => OpenCount++;
    private void OnDisable() => OpenCount = System.Math.Max(0, OpenCount - 1);


    private sealed class RowUi
    {
        public string Identifier = "";
        public int Available;
        public TextMeshProUGUI QtyTmp = null!;
        public Transform StripParent = null!;
        public TextMeshProUGUI[] StripLabels = System.Array.Empty<TextMeshProUGUI>();
    }

    private readonly List<RowUi> _rows = new();

    private TransferDirection _direction;
    private string _fromName = "";
    private string _toName = "";
    private TransferConfig _cfg = null!;
    private int _jumpDistance;
    private MaterialCatalog _catalog = null!;
    private Func<IReadOnlyList<TransferManifestLine>, TransferDialogOutcome>? _onConfirmRequest;
    private Action? _onCancel;

    private readonly Dictionary<string, int> _selected = new();
    private readonly Dictionary<string, int> _availability = new();

    private TextMeshProUGUI _totalTmp = null!;
    private TextMeshProUGUI _etaTmp = null!;
    private TextMeshProUGUI _bannerTmp = null!;

    private bool _shiftHeld;

    public static TransferDialog Open(
        Transform parent,
        TransferDirection direction,
        string fromStationName,
        string toStationName,
        IReadOnlyDictionary<string, int> sourceStock,
        TransferConfig cfg,
        int jumpDistance,
        MaterialCatalog catalog,
        Func<IReadOnlyList<TransferManifestLine>, TransferDialogOutcome> onConfirmRequest,
        Action onCancel)
    {
        var go = new GameObject("VGTransferDialog",
            typeof(RectTransform), typeof(Image), typeof(Canvas), typeof(GraphicRaycaster));
        go.transform.SetParent(parent, worldPositionStays: false);

        // Stretch to fill the parent canvas — becomes the dimmed backdrop.
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var backdrop = go.GetComponent<Image>();
        backdrop.color = new Color(0f, 0f, 0f, 0.5f);

        // Click-on-backdrop cancels the dialog.
        var backdropBtn = go.AddComponent<Button>();
        backdropBtn.transition = Selectable.Transition.None;

        var dlg = go.AddComponent<TransferDialog>();
        dlg._direction        = direction;
        dlg._fromName         = fromStationName;
        dlg._toName           = toStationName;
        dlg._cfg              = cfg;
        dlg._jumpDistance     = jumpDistance;
        dlg._catalog          = catalog;
        dlg._onConfirmRequest = onConfirmRequest;
        dlg._onCancel         = onCancel;

        foreach (var kv in sourceStock)
        {
            if (kv.Value <= 0) continue;
            dlg._availability[kv.Key] = kv.Value;
            dlg._selected[kv.Key]     = 0;
        }

        backdropBtn.onClick.AddListener(() => dlg.Cancel());

        dlg.BuildLayout();
        return dlg;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cancel();
            return;
        }

        var nowHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (nowHeld == _shiftHeld) return;
        _shiftHeld = nowHeld;
        foreach (var r in _rows) RefreshStripLabels(r);
    }

    // -------------------------------------------------------------------------
    // Layout
    // -------------------------------------------------------------------------

    // Section heights (px) — fixed regardless of banner visibility so the
    // body never resizes between empty and error states.
    private const float HeaderH = 40f;
    private const float BannerH = 18f;
    private const float FooterH = 44f;

    private void BuildLayout()
    {
        // Fixed-size centered panel — 720 × 540 px. Anchored positioning per
        // section (NO LayoutGroup on the panel — VLG + childForceExpand was
        // stretching the footer to fill the body).
        var panel = new GameObject("Panel",
            typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, worldPositionStays: false);

        var prt = (RectTransform)panel.transform;
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot     = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(720f, 540f);

        panel.GetComponent<Image>().color = new Color(0.10f, 0.13f, 0.17f, 0.97f);

        // Block backdrop clicks from passing through the panel.
        panel.AddComponent<Button>().transition = Selectable.Transition.None;

        BuildHeader(panel.transform);
        BuildBanner(panel.transform);
        BuildBody(panel.transform);
        BuildFooter(panel.transform);
        UpdateTotals();
    }

    /// <summary>
    /// Anchors a child to a band of the panel and sets explicit pixel offsets
    /// from the top. <paramref name="topOffset"/> is the distance from the
    /// top of the panel to the band's top edge; <paramref name="height"/>
    /// is the band's height. Stretches horizontally to the panel width.
    /// </summary>
    private static void AnchorTopBand(RectTransform rt, float topOffset, float height)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, height);
        rt.anchoredPosition = new Vector2(0f, -topOffset);
    }

    private static void AnchorBottomBand(RectTransform rt, float height)
    {
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot     = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(0f, height);
        rt.anchoredPosition = Vector2.zero;
    }

    private static void AnchorMiddleFill(RectTransform rt, float topInset, float bottomInset)
    {
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(0f, bottomInset);
        rt.offsetMax = new Vector2(0f, -topInset);
    }

    private void BuildHeader(Transform parent)
    {
        var bg = new GameObject("Header", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(parent, worldPositionStays: false);
        bg.GetComponent<Image>().color = new Color(0.07f, 0.10f, 0.14f, 1f);
        AnchorTopBand((RectTransform)bg.transform, topOffset: 0f, height: HeaderH);

        // Always source -> target, regardless of direction. _fromName is
        // the source and _toName is the destination per Plugin.OpenTransferDialog.
        var headerText = $"{_fromName}  ->  {_toName}";
        var hgo = UiText.Label("Label", bg.transform, headerText, 16f,
            FontStyles.Bold, TextAlignmentOptions.Midline);
        var lrt = (RectTransform)hgo.transform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(16f, 0f);
        lrt.offsetMax = new Vector2(-16f, 0f);
    }

    private void BuildBanner(Transform parent)
    {
        var bgo = UiText.Label("Banner", parent, "", 12f,
            FontStyles.Normal, TextAlignmentOptions.Center);

        _bannerTmp = UiText.Component(bgo);
        _bannerTmp.color = new Color(1f, 0.6f, 0.4f);

        AnchorTopBand((RectTransform)bgo.transform, topOffset: HeaderH, height: BannerH);

        // Text empty until ShowBanner; reserve the height regardless so the
        // body never resizes when toggling the banner on/off.
        _bannerTmp.text = "";
    }

    private void BuildBody(Transform parent)
    {
        // Scroll viewport container — anchored to fill the middle of the
        // panel (below header + banner, above footer).
        var bodyGo = new GameObject("Body",
            typeof(RectTransform), typeof(Image));
        bodyGo.transform.SetParent(parent, worldPositionStays: false);

        bodyGo.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.10f, 0.6f);

        AnchorMiddleFill((RectTransform)bodyGo.transform,
            topInset:    HeaderH + BannerH,
            bottomInset: FooterH);

        // ScrollRect lives on the body go.
        var scroll = bodyGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical   = true;

        // Viewport child.
        var viewport = new GameObject("Viewport",
            typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(bodyGo.transform, worldPositionStays: false);
        viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f); // Mask requires an Image
        var vprt = (RectTransform)viewport.transform;
        vprt.anchorMin = Vector2.zero;
        vprt.anchorMax = Vector2.one;
        vprt.offsetMin = new Vector2(8f, 8f);
        vprt.offsetMax = new Vector2(-12f, -8f); // leave 4px gap + 8px scrollbar

        // Content container inside viewport.
        var content = new GameObject("Content",
            typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, worldPositionStays: false);

        var vlg = content.GetComponent<VerticalLayoutGroup>();
        vlg.spacing              = 4f;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(0, 0, 4, 4);

        var csf = content.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var crt = (RectTransform)content.transform;
        crt.anchorMin = new Vector2(0f, 1f);
        crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot     = new Vector2(0.5f, 1f);
        crt.offsetMin = Vector2.zero;
        crt.offsetMax = Vector2.zero;

        scroll.viewport = vprt;
        scroll.content  = crt;

        // Vertical scrollbar — slim, subtle. The handle reflects the
        // viewport/content ratio; with AutoHide it only shows when needed.
        var sbGo = new GameObject("Scrollbar",
            typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        sbGo.transform.SetParent(bodyGo.transform, worldPositionStays: false);
        sbGo.GetComponent<Image>().color = new Color(0.08f, 0.10f, 0.12f, 0.6f);

        var sbrt = (RectTransform)sbGo.transform;
        sbrt.anchorMin = new Vector2(1f, 0f);
        sbrt.anchorMax = new Vector2(1f, 1f);
        sbrt.pivot     = new Vector2(1f, 0.5f);
        sbrt.offsetMin = new Vector2(-8f, 8f);
        sbrt.offsetMax = new Vector2(0f, -8f);

        // Sliding area (parent of the handle). Without this, the handle's
        // size doesn't reflect the visible/total ratio — it fills the track.
        var sbSliding = new GameObject("SlidingArea", typeof(RectTransform));
        sbSliding.transform.SetParent(sbGo.transform, worldPositionStays: false);
        var sart = (RectTransform)sbSliding.transform;
        sart.anchorMin = Vector2.zero;
        sart.anchorMax = Vector2.one;
        sart.offsetMin = new Vector2(1f, 1f);
        sart.offsetMax = new Vector2(-1f, -1f);

        var sbHandle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        sbHandle.transform.SetParent(sbSliding.transform, worldPositionStays: false);
        sbHandle.GetComponent<Image>().color = new Color(0.35f, 0.40f, 0.46f, 0.85f);
        var shrt = (RectTransform)sbHandle.transform;
        shrt.anchorMin = Vector2.zero;
        shrt.anchorMax = Vector2.one;
        shrt.offsetMin = Vector2.zero;
        shrt.offsetMax = Vector2.zero;

        var sbComp = sbGo.GetComponent<Scrollbar>();
        sbComp.direction     = Scrollbar.Direction.BottomToTop;
        sbComp.targetGraphic = sbHandle.GetComponent<Image>();
        sbComp.handleRect    = (RectTransform)sbHandle.transform;

        scroll.verticalScrollbar = sbComp;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        // Populate rows.
        if (_availability.Count == 0)
        {
            var emptyLabel = UiText.Label("Empty", crt,
                _direction == TransferDirection.Pull
                    ? $"{_fromName} has no transferable materials."
                    : $"{_toName} has no transferable materials.",
                14f, alignment: TextAlignmentOptions.Center);
            emptyLabel.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            var ele = emptyLabel.AddComponent<LayoutElement>();
            ele.preferredHeight = 30f;
            return;
        }

        // Group rows by material category (Ores, Crystals, …) in the same
        // order as the main view's filter strip, with a section header before
        // each group.
        foreach (var group in TransferRowGrouping.Build(_availability.Keys, _catalog))
        {
            BuildCategoryHeader(crt, group.Category);
            foreach (var id in group.MaterialIds)
                BuildRow(crt, id, _availability[id]);
        }
    }

    private void BuildCategoryHeader(Transform parent, MaterialCategory category)
    {
        var headerGo = new GameObject($"Header_{category}",
            typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup),
            typeof(LayoutElement));
        headerGo.transform.SetParent(parent, worldPositionStays: false);

        headerGo.GetComponent<Image>().color = new Color(0.12f, 0.16f, 0.21f, 0.9f);

        var hlg = headerGo.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing              = 6f;
        hlg.padding              = new RectOffset(6, 6, 0, 0);
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment       = TextAnchor.MiddleLeft;

        var le = headerGo.GetComponent<LayoutElement>();
        le.preferredHeight = 22f;

        var iconSprite = MaterialCategoryDisplay.Icon(category);
        if (iconSprite != null)
        {
            var iconGo = new GameObject("Icon",
                typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            iconGo.transform.SetParent(headerGo.transform, worldPositionStays: false);
            var iconLe = iconGo.GetComponent<LayoutElement>();
            iconLe.preferredWidth  = 18f;
            iconLe.preferredHeight = 18f;
            iconLe.flexibleWidth   = 0f;
            var iconImg = iconGo.GetComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.raycastTarget  = false;
            iconImg.sprite         = iconSprite;
            iconImg.color          = Color.white;
        }

        var labelGo = UiText.Label("Label", headerGo.transform,
            MaterialCategoryDisplay.Label(category), 12f, FontStyles.Bold);
        var labelLe = labelGo.AddComponent<LayoutElement>();
        labelLe.flexibleWidth = 1f;
        UiText.Component(labelGo).color = new Color(0.78f, 0.85f, 0.92f);
    }

    private void BuildRow(Transform parent, string identifier, int available)
    {
        var rowGo = new GameObject($"Row_{identifier}",
            typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        rowGo.transform.SetParent(parent, worldPositionStays: false);

        var hlg = rowGo.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing              = 8f;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment       = TextAnchor.MiddleLeft;

        var le = rowGo.GetComponent<LayoutElement>();
        le.preferredHeight = 28f;

        // Material icon (24×24) — matches the column-header icons in the
        // Stockpile grid (StationStorageWindow.BuildHeaderRow).
        var iconGo = new GameObject("Icon",
            typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        iconGo.transform.SetParent(rowGo.transform, worldPositionStays: false);
        var iconLe = iconGo.GetComponent<LayoutElement>();
        iconLe.preferredWidth  = 24f;
        iconLe.preferredHeight = 24f;
        iconLe.flexibleWidth   = 0f;
        var iconImg = iconGo.GetComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget  = false;
        var iconSprite = _catalog.Icon(identifier);
        if (iconSprite != null) { iconImg.sprite = iconSprite; iconImg.color = Color.white; }
        else                    { iconImg.color  = new Color(0.3f, 0.3f, 0.3f, 0.6f); }

        // Use the same localized display name the main grid shows — the game's
        // InventoryItemType.displayName via the catalog. (An earlier heuristic
        // here mis-classified valid single-word translations as raw identifiers
        // and replaced them with a humanized object name.)
        var displayLabel = _catalog.DisplayName(identifier);

        var nameGo = UiText.Label("Name", rowGo.transform, displayLabel, 13f);
        var nameLe = nameGo.AddComponent<LayoutElement>();
        nameLe.preferredWidth = 180f;
        nameLe.flexibleWidth  = 1f;

        var qtyGo = UiText.Label("Qty", rowGo.transform, $"0 / {available}", 12f,
            alignment: TextAlignmentOptions.MidlineRight);
        var qtyTmp = UiText.Component(qtyGo);
        qtyTmp.alignment = TextAlignmentOptions.MidlineRight;
        var qtyLe = qtyGo.AddComponent<LayoutElement>();
        qtyLe.preferredWidth = 100f;

        // Quantity-button strip.
        var stripGo = new GameObject("QtyStrip",
            typeof(RectTransform), typeof(LayoutElement));
        stripGo.transform.SetParent(rowGo.transform, worldPositionStays: false);
        var stripLe = stripGo.GetComponent<LayoutElement>();
        stripLe.preferredWidth = 232f;

        var rowUi = new RowUi
        {
            Identifier  = identifier,
            Available   = available,
            QtyTmp      = qtyTmp,
            StripParent = stripGo.transform,
        };
        _rows.Add(rowUi);
        BuildQuantityStrip(rowUi);
    }

    private void BuildQuantityStrip(RowUi ui)
    {
        var stripGo = ui.StripParent.gameObject;

        var hlg = stripGo.GetComponent<HorizontalLayoutGroup>()
                  ?? stripGo.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing              = 3f;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment       = TextAnchor.MiddleCenter;

        for (var i = stripGo.transform.childCount - 1; i >= 0; i--)
            Destroy(stripGo.transform.GetChild(i).gameObject);

        var labels = new TextMeshProUGUI[5];

        // Slots: 0 = -small, 1 = -large, 2 = MAX, 3 = +large, 4 = +small
        MakeStripButton(stripGo.transform, "", out labels[0],
            () => ApplyDelta(ui, -CurrentLadder().LeftSmall));
        MakeStripButton(stripGo.transform, "", out labels[1],
            () => ApplyDelta(ui, -CurrentLadder().LeftLarge));
        MakeStripButton(stripGo.transform, "MAX", out labels[2],
            () => SetSelected(ui, ui.Available));
        MakeStripButton(stripGo.transform, "", out labels[3],
            () => ApplyDelta(ui, +CurrentLadder().RightLarge));
        MakeStripButton(stripGo.transform, "", out labels[4],
            () => ApplyDelta(ui, +CurrentLadder().RightSmall));

        ui.StripLabels = labels;
        RefreshStripLabels(ui);
    }

    private static void MakeStripButton(
        Transform parent, string label, out TextMeshProUGUI labelOut, Action onClick)
    {
        var go = new GameObject("StripBtn",
            typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, worldPositionStays: false);

        go.GetComponent<Image>().color = new Color(0.20f, 0.30f, 0.40f, 0.95f);

        var le = go.GetComponent<LayoutElement>();
        le.preferredWidth  = 44f;
        le.preferredHeight = 24f;

        go.GetComponent<Button>().onClick.AddListener(() => onClick());

        var labelGo = UiText.Label("Label", go.transform, label, 11f,
            alignment: TextAlignmentOptions.Center);
        var tmp = UiText.Component(labelGo);
        tmp.alignment = TextAlignmentOptions.Center;

        var lrt = (RectTransform)labelGo.transform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        labelOut = tmp;
    }

    private QuantityStepLadder CurrentLadder() =>
        QuantityStepLadder.Build(
            _cfg.QuantityStepSmall, _cfg.QuantityStepLarge,
            _cfg.ShiftMultiplier, _shiftHeld);

    private void RefreshStripLabels(RowUi ui)
    {
        var l = CurrentLadder();
        if (ui.StripLabels.Length != 5) return;
        ui.StripLabels[0].text = $"-{l.LeftSmall}x";
        ui.StripLabels[1].text = $"-{l.LeftLarge}x";
        ui.StripLabels[2].text = "MAX";
        ui.StripLabels[3].text = $"+{l.RightLarge}x";
        ui.StripLabels[4].text = $"+{l.RightSmall}x";
    }

    private void ApplyDelta(RowUi ui, int delta)
    {
        var current = _selected.TryGetValue(ui.Identifier, out var v) ? v : 0;
        SetSelected(ui, current + delta);
    }

    private void SetSelected(RowUi ui, int target)
    {
        if (target < 0) target = 0;
        if (target > ui.Available) target = ui.Available;
        _selected[ui.Identifier] = target;
        ui.QtyTmp.text = $"{target} / {ui.Available}";
        UpdateTotals();
    }

    private void BuildFooter(Transform parent)
    {
        var footerGo = new GameObject("Footer",
            typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
        footerGo.transform.SetParent(parent, worldPositionStays: false);

        footerGo.GetComponent<Image>().color = new Color(0.07f, 0.10f, 0.14f, 1f);
        AnchorBottomBand((RectTransform)footerGo.transform, height: FooterH);

        var hlg = footerGo.GetComponent<HorizontalLayoutGroup>();
        hlg.padding              = new RectOffset(16, 16, 7, 7);
        hlg.spacing              = 12f;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;
        hlg.childAlignment       = TextAnchor.MiddleLeft;

        var totalGo = UiText.Label("Total", footerGo.transform, "Fee  $0", 14f, FontStyles.Bold);
        var totalLe = totalGo.AddComponent<LayoutElement>();
        totalLe.preferredWidth  = 180f;
        totalLe.preferredHeight = 30f;
        _totalTmp = UiText.Component(totalGo);

        var etaGo = UiText.Label("ETA", footerGo.transform, "ETA  --:--", 14f);
        var etaLe = etaGo.AddComponent<LayoutElement>();
        etaLe.preferredWidth  = 140f;
        etaLe.preferredHeight = 30f;
        _etaTmp = UiText.Component(etaGo);

        // Spacer pushes buttons right.
        var spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
        spacer.transform.SetParent(footerGo.transform, worldPositionStays: false);
        spacer.GetComponent<LayoutElement>().flexibleWidth = 1f;

        MakeButton(footerGo.transform, "Reset", 110f, () =>
        {
            foreach (var k in new List<string>(_selected.Keys)) _selected[k] = 0;
            foreach (var r in _rows) r.QtyTmp.text = $"0 / {r.Available}";
            UpdateTotals();
        });

        MakeButton(footerGo.transform, "Request Transfer", 150f, () =>
        {
            ClearBanner();
            var manifest = new List<TransferManifestLine>();
            foreach (var kv in _selected)
                if (kv.Value > 0) manifest.Add(new TransferManifestLine(kv.Key, kv.Value));

            if (manifest.Count == 0)
            {
                ShowBanner("Select at least one item.");
                return;
            }

            var outcome = _onConfirmRequest?.Invoke(manifest)
                          ?? new TransferDialogOutcome(false, "Engine unavailable.");
            if (outcome.Success) Close();
            else if (!string.IsNullOrEmpty(outcome.BannerMessage)) ShowBanner(outcome.BannerMessage!);
        });
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    public void ShowBanner(string message) => _bannerTmp.text = message;

    public void ClearBanner() => _bannerTmp.text = "";

    public void Close()
    {
        if (this == null) return;
        Destroy(gameObject);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void Cancel()
    {
        _onCancel?.Invoke();
        Close();
    }

    private void UpdateTotals()
    {
        var totalUnits = 0;
        foreach (var kv in _selected) totalUnits += kv.Value;
        var fee    = FeeCalculator.Compute(_jumpDistance, totalUnits, _cfg);
        var eta    = EtaCalculator.ComputeSeconds(_jumpDistance, _cfg);
        var etaInt = (int)System.Math.Ceiling(eta);
        _totalTmp.text = $"Fee  ${fee}";
        _etaTmp.text   = $"ETA  {etaInt / 60:00}:{etaInt % 60:00}";
    }

    private static void MakeButton(Transform parent, string label, float width, Action onClick)
    {
        var go = new GameObject($"VG{label.Replace(" ", "")}Btn",
            typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, worldPositionStays: false);

        go.GetComponent<Image>().color = new Color(0.20f, 0.40f, 0.60f, 0.95f);
        var le = go.GetComponent<LayoutElement>();
        le.preferredWidth  = width;
        le.preferredHeight = 30f;

        go.GetComponent<Button>().onClick.AddListener(() => onClick());

        var labelGo = UiText.Label("Label", go.transform, label, 13f,
            alignment: TextAlignmentOptions.Center);
        var tmp = UiText.Component(labelGo);
        tmp.alignment = TextAlignmentOptions.Center;

        var lrt = (RectTransform)labelGo.transform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
    }
}
