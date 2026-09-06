using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace VGStockpile.Transfers.Persistence;

internal static class TransferPayloadCodec
{
    internal const int MaxBytes = 1024 * 1024;
    private static readonly Encoding Utf8 = new UTF8Encoding(false, true);
    internal static byte[] Encode(TransferSidecar state)
    {
        var bytes = Utf8.GetBytes(JsonConvert.SerializeObject(state, Formatting.Indented));
        if (bytes.Length > MaxBytes) throw new InvalidDataException("Transfer payload exceeds API save-data size limit.");
        return bytes;
    }
    internal static TransferSidecar Decode(byte[] bytes)
    {
        if (bytes.Length > MaxBytes) throw new InvalidDataException("Transfer payload exceeds API save-data size limit.");
        var state = JsonConvert.DeserializeObject<TransferSidecar>(Utf8.GetString(bytes));
        if (state == null || state.Items == null || state.Version < 0 || state.Version > TransferSidecar.CurrentVersion)
            throw new InvalidDataException("Invalid or unsupported transfer schema.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in state.Items)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Id) || !ids.Add(item.Id) || string.IsNullOrWhiteSpace(item.SourceStationGuid)
                || string.IsNullOrWhiteSpace(item.DestStationGuid) || item.Manifest == null || item.FeeCredits < 0 || item.JumpDistance < 0
                || !Enum.IsDefined(typeof(TransferStatus), item.Status) || float.IsNaN(item.TotalSeconds) || float.IsInfinity(item.TotalSeconds)
                || float.IsNaN(item.RemainingSeconds) || float.IsInfinity(item.RemainingSeconds) || item.TotalSeconds < 0 || item.RemainingSeconds < 0)
                throw new InvalidDataException("Invalid transfer record.");
            foreach (var line in item.Manifest)
                if (line == null || string.IsNullOrWhiteSpace(line.ItemIdentifier) || line.Quantity <= 0) throw new InvalidDataException("Invalid transfer manifest.");
        }
        return state;
    }
    internal static bool IsValid(byte[] bytes)
    { try { _ = Decode(bytes); return true; } catch { return false; } }

    internal static byte[]? ReadLegacy(string path)
    {
        try
        {
            using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (file.Length > MaxBytes) throw new InvalidDataException("Legacy transfers exceed API save-data size limit.");
            using var buffer = new MemoryStream(); var chunk = new byte[8192]; int read;
            while ((read = file.Read(chunk, 0, chunk.Length)) > 0)
            {
                if (buffer.Length + read > MaxBytes) throw new InvalidDataException("Legacy transfers grew beyond limit.");
                buffer.Write(chunk, 0, read);
            }
            return buffer.ToArray();
        }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
    }
}
