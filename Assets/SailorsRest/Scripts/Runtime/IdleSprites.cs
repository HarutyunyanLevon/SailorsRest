using UnityEngine;

namespace SailorsRest
{
    /// <summary>Tiny code-drawn pixel sprites for the free idle effects: dots, bubbles, sparkles. White, tint at use.</summary>
    public static class IdleSprites
    {
        static Sprite dot, bubble, sparkle, bigSparkle;

        public static Sprite Dot => dot != null ? dot : dot = Make(new[] { "w" });

        public static Sprite Bubble => bubble != null ? bubble : bubble = Make(new[]
        {
            ".ww.",
            "wh.w",
            "w..w",
            ".ww.",
        });

        public static Sprite Sparkle => sparkle != null ? sparkle : sparkle = Make(new[]
        {
            ".w.",
            "www",
            ".w.",
        });

        public static Sprite BigSparkle => bigSparkle != null ? bigSparkle : bigSparkle = Make(new[]
        {
            "..w..",
            "..w..",
            "wwwww",
            "..w..",
            "..w..",
        });

        /// <summary>Rows top to bottom: 'w' white, 'h' highlight (white), '.' clear.</summary>
        static Sprite Make(string[] rows)
        {
            int h = rows.Length, w = rows[0].Length;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                char c = rows[y][x];
                px[(h - 1 - y) * w + x] = c == 'w' ? Color.white : c == 'h' ? new Color(1f, 1f, 1f, 0.9f) : Color.clear;
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PixelSprites.PPU);
        }
    }
}
