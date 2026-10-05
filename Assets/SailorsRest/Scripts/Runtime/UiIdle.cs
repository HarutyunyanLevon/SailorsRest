using UnityEngine;

namespace SailorsRest
{
    /// <summary>Free code idles for UI screens (map, market): drifting motes over a backdrop and sparkles that wink on icons.</summary>
    public static class UiIdle
    {
        /// <summary>Canvas pixels per art pixel: the 480-wide art fills the 1920-wide reference canvas.</summary>
        public const float ArtPixel = 4f;

        /// <summary>Warm dust motes drifting up across <paramref name="over"/>. Add right after the backdrop so panels draw on top.</summary>
        public static PixelParticles Motes(RectTransform over, int count, Color color)
        {
            if (count <= 0) return null;
            var size = over.rect.size;
            var p = Field("Motes", over, IdleSprites.Dot, count, new Rect(-size.x / 2f, -size.y / 2f, size.x, size.y));
            p.minVelocity = new Vector2(-12f, 6f);
            p.maxVelocity = new Vector2(12f, 26f);
            p.wobble = 18f;
            p.wobbleHz = 0.2f;
            p.lifeSeconds = new Vector2(5f, 10f);
            p.color = color;
            p.maxAlpha = 0.6f;
            p.blinkHz = 0.25f;
            p.scale = new Vector2(ArtPixel, ArtPixel);
            return p;
        }

        /// <summary>Little four-point stars that pop on and off over an icon, like light catching metal or water.</summary>
        public static PixelParticles Twinkle(RectTransform icon, int count, Color color, float spread = 0.7f)
        {
            if (count <= 0) return null;
            var size = icon.rect.size * spread;
            var p = Field("Twinkle", icon, IdleSprites.BigSparkle, count, new Rect(-size.x / 2f, -size.y / 2f, size.x, size.y));
            p.minVelocity = p.maxVelocity = Vector2.zero;
            p.wobble = 0f;
            p.lifeSeconds = new Vector2(1.2f, 2.6f);
            p.color = color;
            p.maxAlpha = 0.95f;
            p.blinkHz = 0.6f;
            p.scale = new Vector2(ArtPixel * 0.6f, ArtPixel);
            return p;
        }

        static PixelParticles Field(string name, RectTransform parent, Sprite sprite, int count, Rect area)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;   // centred anchors put the field on the parent's centre whatever its pivot
            rt.sizeDelta = Vector2.zero;
            var p = go.AddComponent<PixelParticles>();
            p.sprite = sprite;
            p.count = count;
            p.area = area;
            return p;
        }
    }
}
