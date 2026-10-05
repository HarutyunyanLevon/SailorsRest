using UnityEngine;

namespace SailorsRest
{
    /// <summary>
    /// Free code idle: a few soft diagonal sunbeams under the water surface that sway and fade in and out.
    /// Place it at the waterline; rays hang down from here across <see cref="width"/>.
    /// </summary>
    public class LightRays : MonoBehaviour
    {
        public int rays = 7;
        public float width = 40f;
        public Vector2 rayWidth = new Vector2(0.6f, 1.6f);
        public Vector2 rayLength = new Vector2(7f, 13f);
        [Tooltip("Lean of the rays, degrees from straight down (sun on the left = positive).")]
        public float lean = 18f;
        public Color color = new Color(1f, 0.93f, 0.7f, 1f);
        public Vector2 alpha = new Vector2(0.03f, 0.12f);
        public int sortingOrder = SortOrder.LightRays;
        [Tooltip("Fixed seed so the rays sit in the same places every visit.")]
        public int seed = 11;

        void Start()
        {
            var rng = new System.Random(seed);
            float Range(Vector2 r) => r.x + (float)rng.NextDouble() * (r.y - r.x);
            for (int i = 0; i < rays; i++)
            {
                float x = -width / 2f + (i + (float)rng.NextDouble()) * width / rays;
                var pivot = new GameObject("Ray").transform;
                pivot.SetParent(transform, false);
                pivot.localPosition = new Vector3(x, 0f, 0f);
                pivot.localRotation = Quaternion.Euler(0f, 0f, lean);

                // The beam hangs below its pivot, so swaying the pivot swings it like light through moving water.
                float length = Range(rayLength), w = Range(rayWidth);
                var beam = new GameObject("Beam").AddComponent<SpriteRenderer>();
                beam.transform.SetParent(pivot, false);
                beam.transform.localPosition = new Vector3(0f, -length / 2f, 0f);
                beam.transform.localScale = new Vector3(w * PixelSprites.PPU, length * PixelSprites.PPU, 1f);
                beam.sprite = PixelSprites.White;
                beam.sortingOrder = sortingOrder;
                beam.color = color;

                IdleMotion.Sway(pivot.gameObject, 2.5f + (float)rng.NextDouble() * 2f, 5f + (float)rng.NextDouble() * 4f);
                AlphaPulse.Add(beam.gameObject, alpha.x, alpha.y, 3f + (float)rng.NextDouble() * 4f);
            }
        }
    }
}
