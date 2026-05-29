using System.IO;

namespace VGStockpile.Transfers.Persistence;

internal static class SavePathResolver
{
    public const string SidecarSuffix = ".vgstockpile-transfers.json";

    /// <summary>
    /// Returns the sidecar path next to a given save file. If the save path
    /// has an extension, replace it; otherwise append the suffix.
    /// </summary>
    public static string Sidecar(string saveFilePath)
    {
        var dir = Path.GetDirectoryName(saveFilePath) ?? "";
        var name = Path.GetFileNameWithoutExtension(saveFilePath);
        return Path.Combine(dir, name + SidecarSuffix);
    }
}
