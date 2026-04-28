using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace VGStockpile.Diagnostics;

// One-shot utility that dumps every loaded Sprite as a PNG into
// BepInEx/cache/vgstockpile-icons/<sprite-name>.png. Triggered by setting
// `Diagnostics/DumpIconsOnce = true` in vgstockpile.cfg, then launching
// the game. After it runs the config flag flips back to false.
//
// Sprite textures are usually not CPU-readable. We blit each sprite's
// texture into a temporary readable RenderTexture, then ReadPixels into
// a Texture2D and encode that as PNG.
internal sealed class IconDumper : MonoBehaviour
{
    private ManualLogSource _log = null!;
    private string          _outDir = "";

    public static IconDumper Begin(ManualLogSource log)
    {
        var go = new GameObject("VGStockpile.IconDumper");
        Object.DontDestroyOnLoad(go);
        var d = go.AddComponent<IconDumper>();
        d._log    = log;
        d._outDir = Path.Combine(
            Paths.CachePath, "vgstockpile-icons");
        return d;
    }

    private void Start() => StartCoroutine(DumpAfterDelay());

    // Wait a few seconds so the main scene loads its asset bundles before
    // we sample. Sampling at Awake misses most of the gameplay sprites.
    private IEnumerator DumpAfterDelay()
    {
        yield return new WaitForSecondsRealtime(8f);
        DumpAll();
        Destroy(gameObject);
    }

    private void DumpAll()
    {
        Directory.CreateDirectory(_outDir);
        _log.LogInfo($"IconDumper: writing PNGs to {_outDir}");

        var seen = new HashSet<string>();
        var manifest = new StringBuilder();
        manifest.AppendLine("filename\tsprite_name\ttexture_name\tnative_w\tnative_h\trect_x\trect_y");

        var sprites = Resources.FindObjectsOfTypeAll<Sprite>();
        int written = 0, skipped = 0;
        foreach (var sp in sprites)
        {
            if (sp == null || sp.texture == null) { skipped++; continue; }

            var safeName = SanitizeFilename(string.IsNullOrEmpty(sp.name)
                ? sp.texture.name : sp.name);
            if (string.IsNullOrEmpty(safeName)) { skipped++; continue; }

            // Disambiguate same-name sprites (different sprite atlases often
            // hold sprites with identical names).
            var unique = safeName;
            int i = 1;
            while (!seen.Add(unique))
                unique = $"{safeName}_{i++}";

            try
            {
                var path = Path.Combine(_outDir, unique + ".png");
                if (TryEncode(sp, out var bytes))
                {
                    File.WriteAllBytes(path, bytes);
                    manifest.AppendLine(
                        $"{unique}.png\t{sp.name}\t{sp.texture.name}\t" +
                        $"{(int)sp.rect.width}\t{(int)sp.rect.height}\t" +
                        $"{(int)sp.rect.x}\t{(int)sp.rect.y}");
                    written++;
                }
                else
                {
                    skipped++;
                }
            }
            catch (System.Exception ex)
            {
                _log.LogWarning($"IconDumper: '{sp.name}' failed: {ex.Message}");
                skipped++;
            }
        }

        File.WriteAllText(
            Path.Combine(_outDir, "manifest.tsv"),
            manifest.ToString());
        _log.LogInfo($"IconDumper: wrote {written} PNG(s), skipped {skipped}.");
    }

    private static bool TryEncode(Sprite sp, out byte[] bytes)
    {
        bytes = System.Array.Empty<byte>();
        var src = sp.texture;
        var rect = sp.rect;

        int w = Mathf.Max(1, (int)rect.width);
        int h = Mathf.Max(1, (int)rect.height);

        // Blit src into a same-sized RenderTexture so we have a readable
        // copy. ReadPixels uses the same coordinate system as Sprite.rect
        // (origin bottom-left of the texture), so pass the rect through
        // without any Y inversion.
        var prev = RenderTexture.active;
        var rt = RenderTexture.GetTemporary(src.width, src.height, 0,
            RenderTextureFormat.ARGB32);
        try
        {
            Graphics.Blit(src, rt);
            RenderTexture.active = rt;

            var copy = new Texture2D(w, h, TextureFormat.ARGB32, false);
            copy.ReadPixels(new Rect(rect.x, rect.y, w, h), 0, 0);
            copy.Apply();
            bytes = copy.EncodeToPNG();
            Destroy(copy);
        }
        finally
        {
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
        }
        return bytes != null && bytes.Length > 0;
    }

    private static string SanitizeFilename(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var c in s)
        {
            if (char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.')
                sb.Append(c);
            else if (c == ' ')
                sb.Append('_');
            // else: drop
        }
        var trimmed = sb.ToString().Trim('.', '_');
        return trimmed.Length == 0 ? "" : trimmed;
    }
}
