using UnityEngine;

namespace VGStockpile.UI;

internal static class Notifications
{
    public static void Toast(string message)
    {
        // Fallback: log to BepInEx + Unity console. Replace with a vanilla
        // NotificationManager / Toast API if one is identified later.
        Plugin.Log.LogInfo($"[Toast] {message}");
        Debug.Log($"VGStockpile: {message}");
    }
}
