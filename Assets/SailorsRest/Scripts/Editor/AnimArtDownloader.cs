using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Networking;

namespace SailorsRest.EditorTools
{
    /// <summary>
    /// Pulls the SpriteCook idle loops listed in Art/SpriteCook/Anim/anim-art.json, by itself after a script reload when
    /// files are missing or out of date, or from "Sailor's Rest → Download Idle Animations".
    /// Each SpriteCook sheet is cropped to the pixels any frame uses and saved as a one-row strip, then sliced into
    /// sprites named name_0..name_N. Frame 0 is lined up with the still sprite it was animated from (same size, same
    /// pivot), so swapping the still for the loop does not make anything jump. Afterwards the sprites are wired into
    /// WorldArt and the species assets, so pressing Play is enough. Links expire about an hour after they were
    /// written; ask Claude to refresh anim-art.json if a download fails.
    /// </summary>
    [InitializeOnLoad]
    public static class AnimArtDownloader
    {
        public const string AnimDir = "Assets/SailorsRest/Art/SpriteCook/Anim";
        const string ManifestPath = AnimDir + "/anim-art.json";
        const string LockPath = "Library/SailorsRest-anim-art.lock";
        const char LockSeparator = '=';

        const byte AlphaThreshold = 8;
        const int CropMargin = 1;
        const int MaxTextureSize = 8192;

#pragma warning disable 0649 // filled by JsonUtility
        [Serializable] class Item
        {
            public string name;
            public string asset_id;
            public string url;
            public int frameWidth;
            public int frameHeight;
            public int frames = 8;
            public float fps = 8f;
            [Tooltip("The still sprite in Art/SpriteCook/World this loop was animated from.")]
            public string still;
            public float pivotX = 0.5f;
            public float pivotY = 0.5f;
        }
        [Serializable] class Manifest { public Item[] items; }
#pragma warning restore 0649

        static readonly Queue<Item> queue = new Queue<Item>();
        static readonly List<Item> saved = new List<Item>();
        static UnityWebRequest request;
        static Item downloading;
        static int failed;

        static AnimArtDownloader() => EditorApplication.delayCall += () => Start(false);

        [MenuItem(PixelArtImport.Menu + "Download Idle Animations", priority = 44)]
        public static void DownloadAll() => Start(true);

        [MenuItem(PixelArtImport.Menu + "Re-slice Idle Animations", priority = 45)]
        public static void ReapplyImport()
        {
            var manifest = LoadManifest();
            if (manifest?.items == null) return;
            foreach (var item in manifest.items) ApplyImport(item);
            AssetDatabase.Refresh();
            SailorsRestSetup.RefreshSprites();
        }

        /// <summary>The frames of an idle loop in play order, or an empty array when it has not been downloaded.</summary>
        public static Sprite[] LoadFrames(string name)
        {
            return AssetDatabase.LoadAllAssetsAtPath($"{AnimDir}/{name}.png")
                .OfType<Sprite>()
                .OrderBy(s => FrameIndex(s.name))
                .ToArray();
        }

        static int FrameIndex(string spriteName)
        {
            int split = spriteName.LastIndexOf('_');
            return split >= 0 && int.TryParse(spriteName.Substring(split + 1), out int i) ? i : 0;
        }

        static Manifest LoadManifest()
        {
            if (!File.Exists(ManifestPath)) return null;
            try { return JsonUtility.FromJson<Manifest>(File.ReadAllText(ManifestPath)); }
            catch (Exception e) { Debug.LogError("Sailor's Rest: bad " + ManifestPath + ": " + e.Message); return null; }
        }

        static string PngPath(Item item) => $"{AnimDir}/{item.name}.png";
        static string StillPath(Item item) => $"{WorldArtDownloader.WorldDir}/{item.still}.png";

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
                bool current = File.Exists(PngPath(item)) && (!locked.TryGetValue(item.name, out var id) || id == item.asset_id);
                if (!string.IsNullOrEmpty(item.url) && (force || !current)) queue.Enqueue(item);
            }
            if (queue.Count == 0) return;
            Debug.Log($"Sailor's Rest: downloading {queue.Count} idle animations from SpriteCook…");
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
            Debug.Log($"Sailor's Rest: idle animations done, {saved.Count} saved, {failed} failed.");
            if (saved.Count > 0) SailorsRestSetup.RefreshSprites();   // wire the new frames into WorldArt and the species
        }

        static void Tick()
        {
            if (!request.isDone) return;
            if (request.result == UnityWebRequest.Result.Success) Save(downloading, request.downloadHandler.data);
            else
            {
                failed++;
                Debug.LogError($"Sailor's Rest: download of {downloading.name} failed ({request.responseCode} {request.error}). " +
                               "The SpriteCook links may have expired; ask Claude to refresh anim-art.json.");
            }
            request.Dispose();
            request = null;
            Next();
        }

        static void Save(Item item, byte[] sheet)
        {
            try
            {
                var strip = ToStrip(item, sheet);
                if (strip == null)
                {
                    failed++;
                    return;
                }
                Directory.CreateDirectory(AnimDir);
                File.WriteAllBytes(PngPath(item), strip);
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

        // ----- sheet → strip -----

        /// <summary>Reads the frames out of SpriteCook's sheet (row by row, top first), crops all of them to the
        /// pixels any frame uses, and lays them out left to right in one row.</summary>
        static byte[] ToStrip(Item item, byte[] sheetPng)
        {
            var sheet = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!sheet.LoadImage(sheetPng))
            {
                Debug.LogError($"Sailor's Rest: {item.name} is not a PNG sheet.");
                return null;
            }
            int fw = item.frameWidth > 0 ? item.frameWidth : sheet.height;
            int fh = item.frameHeight > 0 ? item.frameHeight : sheet.height;
            int cols = Mathf.Max(1, sheet.width / fw);
            int frames = Mathf.Min(item.frames, cols * Mathf.Max(1, sheet.height / fh));
            if (frames < item.frames)
                Debug.LogWarning($"Sailor's Rest: {item.name} sheet is {sheet.width}x{sheet.height}, only {frames} of {item.frames} frames fit.");

            var px = sheet.GetPixels32();
            RectInt FrameRect(int i) => new RectInt(i % cols * fw, sheet.height - (i / cols + 1) * fh, fw, fh);

            RectInt union = default;
            bool any = false;
            for (int i = 0; i < frames; i++)
            {
                if (!Opaque(px, sheet.width, FrameRect(i), out var b)) continue;
                b.position -= FrameRect(i).position;
                if (!any) { union = b; any = true; }
                else union.SetMinMax(Vector2Int.Min(union.min, b.min), Vector2Int.Max(union.max, b.max));
            }
            if (!any) union = new RectInt(0, 0, fw, fh);
            var min = Vector2Int.Max(Vector2Int.zero, union.min - Vector2Int.one * CropMargin);
            var max = Vector2Int.Min(new Vector2Int(fw, fh), union.max + Vector2Int.one * CropMargin);
            var crop = new RectInt(min, max - min);

            var strip = new Texture2D(crop.width * frames, crop.height, TextureFormat.RGBA32, false);
            var clear = new Color32[strip.width * strip.height];
            strip.SetPixels32(clear);
            for (int i = 0; i < frames; i++)
            {
                var r = FrameRect(i);
                strip.SetPixels(i * crop.width, 0, crop.width, crop.height,
                    sheet.GetPixels(r.x + crop.x, r.y + crop.y, crop.width, crop.height));
            }
            strip.Apply();
            var png = strip.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(sheet);
            UnityEngine.Object.DestroyImmediate(strip);
            return png;
        }

        /// <summary>Bounding box of visible pixels inside <paramref name="area"/>, in texture coordinates.</summary>
        static bool Opaque(Color32[] px, int texWidth, RectInt area, out RectInt bounds)
        {
            bounds = default;
            bool any = false;
            for (int y = area.yMin; y < area.yMax; y++)
            for (int x = area.xMin; x < area.xMax; x++)
            {
                if (px[y * texWidth + x].a < AlphaThreshold) continue;
                if (!any) { bounds = new RectInt(x, y, 1, 1); any = true; }
                else bounds.SetMinMax(Vector2Int.Min(bounds.min, new Vector2Int(x, y)), Vector2Int.Max(bounds.max, new Vector2Int(x + 1, y + 1)));
            }
            return any;
        }

        static bool OpaqueOf(string pngPath, out RectInt bounds, out Vector2Int size)
        {
            bounds = default;
            size = default;
            if (!File.Exists(pngPath)) return false;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            bool ok = tex.LoadImage(File.ReadAllBytes(pngPath)) &&
                      Opaque(tex.GetPixels32(), tex.width, new RectInt(0, 0, tex.width, tex.height), out bounds);
            size = new Vector2Int(tex.width, tex.height);
            UnityEngine.Object.DestroyImmediate(tex);
            return ok;
        }

        // ----- import: slice the strip, match the still -----

        static void ApplyImport(Item item)
        {
            string path = PngPath(item);
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) return;
            if (!OpaqueOf(path, out _, out var stripSize)) return;
            int frames = Mathf.Max(1, item.frames);
            int cw = stripSize.x / frames, ch = stripSize.y;

            // Frame 0 is the still's pose: find its pixels so the loop can sit exactly where the still sat.
            var stripTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            stripTex.LoadImage(File.ReadAllBytes(path));
            if (!Opaque(stripTex.GetPixels32(), stripTex.width, new RectInt(0, 0, cw, ch), out var first)) first = new RectInt(0, 0, cw, ch);
            UnityEngine.Object.DestroyImmediate(stripTex);

            float ppu = PixelSprites.PPU;
            var pivotInBody = new Vector2(item.pivotX, item.pivotY);   // where the pivot sits on the visible body, 0..1
            var still = AssetImporter.GetAtPath(StillPath(item)) as TextureImporter;
            if (still != null && OpaqueOf(StillPath(item), out var stillBody, out var stillSize))
            {
                // Same world width for frame 0 as for the still, whatever scale SpriteCook rendered the loop at.
                ppu = still.spritePixelsPerUnit * first.width / Mathf.Max(1, stillBody.width);
                var stillPivotPx = new Vector2(item.pivotX * stillSize.x, item.pivotY * stillSize.y);
                pivotInBody = (stillPivotPx - (Vector2)stillBody.position) / (Vector2)stillBody.size;
            }
            var pivotPx = (Vector2)first.position + pivotInBody * first.size;
            var pivot = new Vector2(pivotPx.x / cw, pivotPx.y / ch);

            PixelArtImport.ApplyTo(importer);
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = ppu;
            importer.maxTextureSize = MaxTextureSize;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var old = provider.GetSpriteRects().ToDictionary(r => r.name, r => r.spriteID);
            var rects = new SpriteRect[frames];
            for (int i = 0; i < frames; i++)
            {
                string name = $"{item.name}_{i}";
                rects[i] = new SpriteRect
                {
                    name = name,
                    rect = new Rect(i * cw, 0, cw, ch),
                    alignment = SpriteAlignment.Custom,
                    pivot = pivot,
                    spriteID = old.TryGetValue(name, out var id) ? id : GUID.Generate(),   // keep ids so references survive
                };
            }
            provider.SetSpriteRects(rects);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>()
                ?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)).ToList());
            provider.Apply();
            importer.SaveAndReimport();
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
    }
}
