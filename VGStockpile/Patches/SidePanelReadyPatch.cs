using Behaviour.UI.Side_Menu;
using HarmonyLib;
using UnityEngine;

namespace VGStockpile.Patches;

// Postfix on SidePanel.Start() — runs once per gameplay-scene load, after
// SidePanel.Awake has set the singleton and the parent Canvas hierarchy is
// fully built. This is a hard, deterministic signal: no polling, no
// timeout, no race against asset bundle streaming.
//
// AttachIcon is idempotent (no-op if _icon already exists), so re-firing
// across save reloads only triggers a fresh attach when the previous
// canvas was destroyed.
[HarmonyPatch(typeof(SidePanel), nameof(SidePanel.Start))]
internal static class SidePanelReadyPatch
{
    [HarmonyPostfix]
    public static void Postfix(SidePanel __instance)
    {
        if (Plugin.Instance is null) return;
        if (Plugin.Instance.IconAttached) return;

        var canvas = __instance.GetComponentInParent<Canvas>();
        if (canvas is null)
        {
            Plugin.Log.LogWarning(
                "SidePanelReadyPatch: SidePanel has no parent Canvas; " +
                "cannot attach VGStockpile icon.");
            return;
        }

        Plugin.Instance.AttachIcon(canvas.rootCanvas);
    }
}
