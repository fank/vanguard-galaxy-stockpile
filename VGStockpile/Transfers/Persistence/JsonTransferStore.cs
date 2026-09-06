using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace VGStockpile.Transfers.Persistence;

internal sealed class JsonTransferStore : ITransferStore
{
    private readonly Action<string> _logWarning;
    public JsonTransferStore(Action<string> logWarning) { _logWarning = logWarning; }

    private static TransferSidecar? ReadExisting(string path)
    {
        string json;
        try { json = File.ReadAllText(path); }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
        var state = JsonConvert.DeserializeObject<TransferSidecar>(json);
        if (state == null || state.Items == null || state.Items.Any(item => item == null || item.Manifest == null))
            throw new JsonSerializationException("Invalid transfer sidecar structure.");
        if (state.Version > TransferSidecar.CurrentVersion)
            throw new InvalidDataException($"Unsupported transfer sidecar version {state.Version}; preserving file.");
        return state;
    }

    public TransferSidecar Load(string savePath)
    {
        try { return ReadExisting(savePath) ?? TransferSidecar.Empty(); }
        catch (Exception ex)
        {
            _logWarning($"Failed to read sidecar at {savePath}: {ex.Message}");
            throw;
        }
    }

    public void Save(string savePath, TransferSidecar sidecar)
    {
        // Refuse unreadable/corrupt/future files, including destinations changed since restore.
        var existing = ReadExisting(savePath);
        if (existing == null && sidecar.Items.Count == 0) return;
        var dir = Path.GetDirectoryName(savePath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        var json = JsonConvert.SerializeObject(sidecar, Formatting.Indented);
        var tmp = savePath + ".tmp";
        File.WriteAllText(tmp, json);
        File.Copy(tmp, savePath, overwrite: true);
        File.Delete(tmp);
    }
}
