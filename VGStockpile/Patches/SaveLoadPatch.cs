using System;
using HarmonyLib;
using Source.Util;

namespace VGStockpile.Patches;

[HarmonyPatch(typeof(SaveGameFile), nameof(SaveGameFile.LoadSaveGame))]
internal static class SaveLoadPatch
{
    [HarmonyPostfix]
    private static void Postfix(SaveGameFile __instance)
    {
        if (Plugin.Instance is null) return;

        try
        {
            var savePath = __instance.File.FullName;
            Plugin.Instance.OnSaveLoaded(savePath);
        }
        catch (Exception e)
        {
            Plugin.Log.LogError($"SaveLoadPatch: failed to handle save-loaded event: {e}");
        }
    }
}
