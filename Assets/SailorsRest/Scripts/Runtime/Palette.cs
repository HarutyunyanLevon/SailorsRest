using SailorsRest.Rules;
using UnityEngine;

namespace SailorsRest
{
    /// <summary>Driftwood design-system colours (Golden Hour theme) used by code-drawn elements and text.</summary>
    public static class Palette
    {
        static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

        public static readonly Color WaterTop = Hex("#e3b27c");
        public static readonly Color WoodDark = Hex("#6b3e22");
        public static readonly Color Edge = Hex("#2e1a10");
        public static readonly Color PaperShade = Hex("#e6cd9c");
        public static readonly Color Ink = Hex("#2e1d12");
        public static readonly Color InkMuted = Hex("#6b4a33");
        public static readonly Color OnWood = Hex("#fff4dc");

        public static readonly Color Accent = Hex("#f2a03d");
        public static readonly Color Success = Hex("#3f6a3b");
        public static readonly Color Danger = Hex("#ad3b25");
        public static readonly Color MeterTrack = Hex("#3d2416");
        public static readonly Color MeterZone = Hex("#86bd68");
        public static readonly Color VariantGolden = Hex("#f7d35a");

        /// <summary>Sprite tint for a fish: golden fish glow, everything else keeps its own colours.</summary>
        public static Color Tint(Variant variant) => variant == Variant.Golden ? VariantGolden : Color.white;
    }
}
