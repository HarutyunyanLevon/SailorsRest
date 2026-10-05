using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace SailorsRest.EditorTools
{
    /// <summary>
    /// Pulls the world art listed in Art/SpriteCook/World/world-art.json into that folder, by itself after a script
    /// reload when files are missing or out of date, or from "Sailor's Rest → Download World Art".
    /// Transparent sprites are trimmed to their pixels; every image gets pixel-art import settings with a
    /// pixels-per-unit that makes it <c>worldWidth</c> units wide. The links are signed and expire about an hour
    /// after they were written; ask Claude to refresh world-art.json if a download fails.
    /// </summary>
    [InitializeOnLoad]
    public static class WorldArtDownloader
    {
        public const string WorldDir = "Assets/SailorsRest/Art/SpriteCook/World";
        const string ManifestPath = WorldDir + "/world-art.json";
        /// <summary>Which asset id each saved PNG came from, so a regenerated asset replaces the old file.</summary>
        const string LockPath = "Library/SailorsRest-world-art.lock";
        const char LockSeparator = '=';

        /// <summary>Pixels more transparent than this count as empty when trimming.</summary>
        const byte TrimAlphaThreshold = 8;
        /// <summary>Transparent border kept around trimmed sprites, in pixels.</summary>
        const int TrimMargin = 1;

#pragma warning disable 0649 // filled by JsonUtility
        [Serializable] class Item
        {
            public string name;
            public string asset_id;
            public string url;
            public float worldWidth;
            public float pivotX = 0.5f;
            public float pivotY = 0.5f;
            public bool trim = true;
        }
        [Serializable] class Manifest { public Item[] items; }
#pragma warning restore 0649

        static readonly Queue<Item> queue = new Queue<Item>();
        static readonly List<Item> saved = new List<Item>();
        static UnityWebRequest request;
        static Item downloading;
        static int failed;

        static WorldArtDownloader() => EditorApplication.delayCall += () => Start(false);

        [MenuItem(PixelArtImport.Menu + "Download World Art", priority = 42)]
        public static void DownloadAll() => Start(true);

        [MenuItem(PixelArtImport.Menu + "Re-apply World Art Import Settings", priority = 43)]
        public static void ReapplyImport()
        {
            var manifest = LoadManifest();
            if (manifest?.items == null)
            {
                Debug.LogWarning("Sailor's Rest: no world art listed in " + ManifestPath + ".");
                return;
            }
            foreach (var item in manifest.items) ApplyImport(item);
            AssetDatabase.Refresh();
        }

        static Manifest LoadManifest()
        {
            if (!File.Exists(ManifestPath)) return null;
            try { return JsonUtility.FromJson<Manifest>(File.ReadAllText(ManifestPath)); }
            catch (Exception e) { Debug.LogError("Sailor's Rest: bad " + ManifestPath + ": " + e.Message); return null; }
        }

        static string PngPath(Item item) => $"{WorldDir}/{item.name}.png";

        // ----- download queue -----

        static void Start(bool force)
        {
            if (request != null) return;
            var manifest = LoadManifest();
            if (manifest?.items == null) return;
            var locked = ReadLock();
            queue.Clear();
            saved.Clear();
            failed = 0;
            foreach (var item in manifest.items)
            {
                // A PNG with no lock entry (fresh clone, deleted Library) is trusted; only a known older asset id is replaced.
                bool current = File.Exists(PngPath(item)) && (!locked.TryGetValue(item.name, out var id) || id == item.asset_id);
                if (!string.IsNullOrEmpty(item.url) && (force || !current)) queue.Enqueue(item);
            }
            if (queue.Count == 0) return;
            Debug.Log($"Sailor's Rest: downloading {queue.Count} world art images from SpriteCook…");
            EditorApplication.update += Tick;
            Next();
        }

        static void Next()
        {
            if (queue.Count > 0)
            {
                downloading = queue.Dequeue();
                request = UnityWebRequest.Get(downloading.url);
                request.SendWebRequest();
                return;
            }
            EditorApplication.update -= Tick;
            request = null;
            AssetDatabase.Refresh();
            foreach (var item in saved) ApplyImport(item);
            AssetDatabase.Refresh();
            Debug.Log($"Sailor's Rest: world art done, {saved.Count} saved, {failed} failed." +
                      (saved.Count > 0 ? $" Now run {PixelArtImport.Menu}Build All Scenes." : ""));
        }

        static void Tick()
        {
            if (!request.isDone) return;
            if (request.result == UnityWebRequest.Result.Success) Save(downloading, request.downloadHandler.data);
            else
            {
                failed++;
                Debug.LogError($"Sailor's Rest: download of {downloading.name} failed ({request.responseCode} {request.error}). " +
                               "The SpriteCook links may have expired; ask Claude to refresh world-art.json.");
            }
            request.Dispose();
            request = null;
            Next();
        }

        static void Save(Item item, byte[] png)
        {
            try
            {
                Directory.CreateDirectory(WorldDir);
                File.WriteAllBytes(PngPath(item), item.trim ? Trim(png) : png);
                var locked = ReadLock();
                locked[item.name] = item.asset_id;
                WriteLock(locked);
                saved.Add(item);
            }
            catch (Exception e)
            {
                failed++;
                Debug.LogError($"Sailor's Rest: could not save {item.name}: {e.Message}");
            }
        }

        // ----- lock file: name=asset_id per line -----

        static Dictionary<string, string> ReadLock()
        {
            var map = new Dictionary<string, string>();
            if (!File.Exists(LockPath)) return map;
            foreach (var line in File.ReadAllLines(LockPath))
            {
                int split = line.IndexOf(LockSeparator);
                if (split > 0) map[line.Substring(0, split)] = line.Substring(split + 1);
            }
            return map;
        }

        static void WriteLock(Dictionary<string, string> map)
        {
            var lines = new List<string>();
            foreach (var kv in map) lines.Add(kv.Key + LockSeparator + kv.Value);
            File.WriteAllLines(LockPath, lines);
        }

        // ----- images -----

        /// <summary>Crops to the bounding box of visible pixels, keeping a small transparent margin.</summary>
        static byte[] Trim(byte[] png)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(png)) return png;
            var px = tex.GetPixels32();
            int w = tex.width, h = tex.height;
            RectInt bounds = default;
            bool any = false;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (px[y * w + x].a < TrimAlphaThreshold) continue;
                if (!any) { bounds = new RectInt(x, y, 1, 1); any = true; }
                else bounds.SetMinMax(Vector2Int.Min(bounds.min, new Vector2Int(x, y)), Vector2Int.Max(bounds.max, new Vector2Int(x + 1, y + 1)));
            }
            if (!any) return png;
            var min = Vector2Int.Max(Vector2Int.zero, bounds.min - Vector2Int.one * TrimMargin);
            var max = Vector2Int.Min(new Vector2Int(w, h), bounds.max + Vector2Int.one * TrimMargin);
            var size = max - min;
            if (size.x == w && size.y == h) return png;

            var cropped = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false);
            cropped.SetPixels(tex.GetPixels(min.x, min.y, size.x, size.y));
            cropped.Apply();
            var result = cropped.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(cropped);
            return result;
        }

        static void ApplyImport(Item item)
        {
            if (!(AssetImporter.GetAtPath(PngPath(item)) is TextureImporter importer)) return;
            importer.GetSourceTextureWidthAndHeight(out int width, out _);
            PixelArtImport.ApplyTo(importer);
            importer.spritePixelsPerUnit = item.worldWidth > 0f && width > 0 ? width / item.worldWidth : PixelSprites.PPU;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(item.pivotX, item.pivotY);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }
}
