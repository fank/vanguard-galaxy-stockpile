using System;
using System.IO;
using Newtonsoft.Json;

namespace VGStockpile.Transfers.Persistence;

internal sealed class JsonTransferStore : ITransferStore
{
    private readonly Action<string> _logWarning;

    public JsonTransferStore(Action<string> logWarning) { _logWarning = logWarning; }

    public TransferSidecar Load(string savePath)
    {
        if (!File.Exists(savePath)) return TransferSidecar.Empty();
        try
        {
            var json = File.ReadAllText(savePath);
            var loaded = JsonConvert.DeserializeObject<TransferSidecar>(json);
            if (loaded is null)
            {
                _logWarning($"Sidecar at {savePath} deserialized to null; treating as empty.");
                return TransferSidecar.Empty();
            }
            if (loaded.Version > TransferSidecar.CurrentVersion)
            {
                _logWarning($"Sidecar at {savePath} has newer version {loaded.Version} > " +
                            $"{TransferSidecar.CurrentVersion}; treating as empty (will not overwrite).");
                return TransferSidecar.Empty();
            }
            return loaded;
        }
        catch (Exception ex)
        {
            _logWarning($"Failed to read sidecar at {savePath}: {ex.Message}");
            return TransferSidecar.Empty();
        }
    }

    public void Save(string savePath, TransferSidecar sidecar)
    {
        if (File.Exists(savePath))
        {
            try
            {
                var existing = JsonConvert.DeserializeObject<TransferSidecar>(File.ReadAllText(savePath));
                if (existing is { Version: > TransferSidecar.CurrentVersion })
                {
                    _logWarning($"Refusing to overwrite newer sidecar (v{existing.Version}) at {savePath}.");
                    return;
                }
            }
            catch (Exception ex)
            {
                _logWarning($"Could not parse existing sidecar at {savePath} for version check: {ex.Message}. Will overwrite.");
            }
        }

        var dir = Path.GetDirectoryName(savePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

        var json = JsonConvert.SerializeObject(sidecar, Formatting.Indented);
        var tmp = savePath + ".tmp";
        File.WriteAllText(tmp, json);
        File.Copy(tmp, savePath, overwrite: true);
        File.Delete(tmp);
    }
}
