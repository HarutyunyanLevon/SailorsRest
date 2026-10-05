using UnityEngine;
using UnityEngine.UI;

namespace SailorsRest
{
    /// <summary>Free code idle: slowly breathes the alpha of the SpriteRenderer or Image on this object between two values.</summary>
    public class AlphaPulse : MonoBehaviour
    {
        [Range(0f, 1f)] public float minAlpha = 0.6f;
        [Range(0f, 1f)] public float maxAlpha = 1f;
        public float seconds = 3f;

        SpriteRenderer sr;
        Image img;
        float phase;

        public static AlphaPulse Add(GameObject go, float min, float max, float seconds)
        {
            var p = go.AddComponent<AlphaPulse>();
            p.minAlpha = min;
            p.maxAlpha = max;
            p.seconds = seconds;
            return p;
        }

        void Start()
        {
            sr = GetComponent<SpriteRenderer>();
            img = GetComponent<Image>();
            phase = Random.value * Mathf.PI * 2f;
        }

        void Update()
        {
            float u = 0.5f + 0.5f * Mathf.Sin(Time.time / Mathf.Max(seconds, 0.01f) * Mathf.PI * 2f + phase);
            float a = Mathf.Lerp(minAlpha, maxAlpha, u);
            if (sr != null) { var c = sr.color; c.a = a; sr.color = c; }
            if (img != null) { var c = img.color; c.a = a; img.color = c; }
        }
    }
}
