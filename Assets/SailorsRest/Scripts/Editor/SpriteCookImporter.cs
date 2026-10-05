using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SailorsRest.EditorTools
{
    /// <summary>
    /// After you unzip the SpriteCook UI-kit archive into Art/SpriteCook/Landscape, run
    /// "Sailor's Rest → Apply SpriteCook Import Settings". Every kit image gets pixel-art import settings,
    /// and those listed in kit-landscape.json get their 9-slice border.
    /// </summary>
    public static class SpriteCookImporter
    {
        const string KitDir = "Assets/SailorsRest/Art/SpriteCook/Landscape";
        const string KitFile = "Assets/SailorsRest/Art/SpriteCook/kit-landscape.json";

#pragma warning disable 0649 // filled by JsonUtility
        [Serializable] class NineSlice { public int left, top, right, bottom; }
        [Serializable] class Component { public string name; public string asset_id; public bool scalable; public NineSlice nine_slice; }
        [Serializable] class Kit { public Component[] components; }
#pragma warning restore 0649

        [MenuItem(PixelArtImport.Menu + "Apply SpriteCook Import Settings", priority = 40)]
        public static void Apply()
        {
            if (!File.Exists(KitFile))
            {
                Debug.LogError("Sailor's Rest: " + KitFile + " not found.");
                return;
            }
            var kit = JsonUtility.FromJson<Kit>(File.ReadAllText(KitFile));
            int matched = 0, total = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { KitDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                PixelArtImport.ApplyTo(importer);
                var match = Find(kit, Path.GetFileNameWithoutExtension(path).ToLowerInvariant());
                if (match != null)
                {
                    matched++;
                    var n = match.nine_slice;
                    importer.spriteBorder = match.scalable && n != null ? new Vector4(n.left, n.bottom, n.right, n.top) : Vector4.zero;
                }
                importer.SaveAndReimport();
                total++;
            }
            Debug.Log($"Sailor's Rest: import settings applied to {total} images, {matched} matched a kit component.");
        }

        /// <summary>
        /// The kit component a file is named after. Downloaded files may carry extra prefixes, so names are matched
        /// by containment, and the longest name wins: "button_wide_hover" must not resolve to "button_wide".
        /// </summary>
        static Component Find(Kit kit, string file)
        {
            Component best = null;
            foreach (var c in kit.components)
            {
                if (file.Contains(c.asset_id)) return c;
                if (file.Contains(c.name) && (best == null || c.name.Length > best.name.Length)) best = c;
            }
            return best;
        }
    }
}
