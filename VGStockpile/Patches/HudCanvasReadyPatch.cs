using Behaviour.UI.HUD;
using HarmonyLib;
using UnityEngine;

namespace VGStockpile.Patches;

// Postfix on HudManager.Awake — at this point the HUD MonoBehaviour is alive
// and (per sibling-plugin convention) its child canvas hierarchy is reachable.
// We walk to the parent Canvas via GetComponentInParent / GetComponentInChildren
// because the HUD canvas isn't a directly-named field.
[HarmonyPatch(typeof(HudManager), nameof(HudManager.Awake))]
internal static class HudCanvasReadyPatch
{
    [HarmonyPostfix]
    public static void Postfix(HudManager __instance)
    {
        if (Plugin.Instance is null) return;
        if (Plugin.Instance.IconAttached) return;

        var canvas = __instance.GetComponentInParent<Canvas>()
                     ?? __instance.GetComponentInChildren<Canvas>();
        if (canvas is null)
        {
            Plugin.Log.LogWarning("HUD canvas not found; cannot attach VGStockpile icon.");
            return;
        }

        Plugin.Instance.AttachIcon(canvas);
    }
}
