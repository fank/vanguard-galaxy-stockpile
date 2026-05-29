using System;
using HarmonyLib;
using Source.Util;

namespace VGStockpile.Patches;

[HarmonyPatch(typeof(SaveGame), nameof(SaveGame.Store))]
internal static class SaveWritePatch
{
    [HarmonyPostfix]
    private static void Postfix(string saveName)
    {
        if (Plugin.Instance is null) return;

        try
        {
            var savePath = System.IO.Path.Combine(SaveGame.SavesPath, saveName + ".save");
            Plugin.Instance.OnSaveWritten(savePath);
        }
        catch (Exception e)
        {
            Plugin.Log.LogError($"SaveWritePatch: failed to handle save-written event: {e}");
        }
    }
}
