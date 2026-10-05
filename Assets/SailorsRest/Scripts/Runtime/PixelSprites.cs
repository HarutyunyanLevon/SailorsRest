using System.Collections.Generic;
using SailorsRest.Rules;
using UnityEngine;

namespace SailorsRest
{
    /// <summary>Small pixel sprites generated in code: a white block for flat shapes, and the hooking ring.</summary>
    public static class PixelSprites
    {
        /// <summary>Pixels per world unit for everything generated here.</summary>
        public const int PPU = 16;

        static readonly Vector2 Centre = new Vector2(0.5f, 0.5f);
        static readonly Vector2 BottomCentre = new Vector2(0.5f, 0f);

        // Ring geometry, in pixels.
        const int RingSize = 31;
        const float RingInset = 1.5f;      // gap between the texture edge and the outer rim
        const float RingBand = 3.5f;       // width of the coloured band
        const float RingOutline = 1.2f;    // dark outline on both sides of the band

        // Needle: a dark cap pixel row on top of an accent shaft.
        const int NeedleLength = 13;
        static readonly string NeedleCap = "ooo";
        static readonly string NeedleShaft = ".a.";

        static Sprite white, needle;

        public static Sprite White => white != null ? white : white = FromPixels(new[] { "w" }, _ => Color.white, Centre);

        public static Sprite Needle
        {
            get
            {
                if (needle != null) return needle;
                var rows = new string[NeedleLength];
                for (int i = 0; i < rows.Length; i++) rows[i] = i == 0 ? NeedleCap : NeedleShaft;
                return needle = FromPixels(rows, c => c == 'o' ? Palette.Edge : c == 'a' ? Palette.Accent : Color.clear, BottomCentre);
            }
        }

        /// <summary>The ring with its green arcs; 0° is 12 o'clock, angles grow clockwise like the needle.</summary>
        public static Sprite Ring(IReadOnlyList<QteModel.Arc> arcs, bool hit)
        {
            var tex = NewTexture(RingSize, RingSize);
            float centre = RingSize / 2f;
            float outer = centre - RingInset, inner = outer - RingBand;
            var px = new Color[RingSize * RingSize];
            for (int y = 0; y < RingSize; y++)
            for (int x = 0; x < RingSize; x++)
            {
                float dx = x + 0.5f - centre, dy = y + 0.5f - centre;   // pixel centres
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                Color col = Color.clear;
                bool rim = (d > outer && d <= outer + RingOutline) || (d >= inner - RingOutline && d < inner);
                if (rim) col = Palette.Edge;
                else if (d >= inner && d <= outer)
                {
                    float angle = QteModel.Normalize(Mathf.Atan2(dx, dy) * Mathf.Rad2Deg);
                    col = !OnArc(arcs, angle) ? Palette.MeterTrack : hit ? Palette.Success : Palette.MeterZone;
                }
                px[y * RingSize + x] = col;
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, RingSize, RingSize), Centre, PPU);
        }

        static bool OnArc(IReadOnlyList<QteModel.Arc> arcs, float angle)
        {
            foreach (var arc in arcs)
                if (arc.Contains(angle)) return true;
            return false;
        }

        static Texture2D NewTexture(int w, int h) => new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };

        /// <summary>Rows are written top to bottom.</summary>
        static Sprite FromPixels(string[] rows, System.Func<char, Color> colorOf, Vector2 pivot)
        {
            int h = rows.Length, w = rows[0].Length;
            var tex = NewTexture(w, h);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[(h - 1 - y) * w + x] = colorOf(rows[y][x]);
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), pivot, PPU);
        }
    }
}
