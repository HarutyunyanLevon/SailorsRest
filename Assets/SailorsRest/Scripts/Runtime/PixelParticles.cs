using UnityEngine;
using UnityEngine.UI;

namespace SailorsRest
{
    /// <summary>
    /// Free code idle: a small fixed pool of pixel particles (bubbles, dust motes, fireflies, water sparkles)
    /// drifting inside a rectangle around this object. Works in the world (SpriteRenderers, units = world units)
    /// or on a UI canvas (Images, units = canvas pixels) — it picks by whether this object has a RectTransform.
    /// </summary>
    public class PixelParticles : MonoBehaviour
    {
        public Sprite sprite;
        public int count = 12;
        [Tooltip("Spawn rectangle, local to this object.")]
        public Rect area = new Rect(-5f, -5f, 10f, 10f);
        public Vector2 minVelocity = new Vector2(-0.1f, 0.2f);
        public Vector2 maxVelocity = new Vector2(0.1f, 0.5f);
        [Tooltip("Side-to-side wobble amplitude and frequency (Hz).")]
        public float wobble = 0.1f;
        public float wobbleHz = 0.6f;
        public Vector2 lifeSeconds = new Vector2(3f, 6f);
        public Color color = Color.white;
        [Range(0f, 1f)] public float maxAlpha = 0.7f;
        [Tooltip("0 = fade in/out only. Above 0 = twinkle on and off this many times per second (fireflies, sparkles).")]
        public float blinkHz;
        [Tooltip("Size multiplier for each particle (UI: canvas pixels per sprite pixel).")]
        public Vector2 scale = Vector2.one;
        public int sortingOrder;
        [Tooltip("World only: snap to the pixel grid so particles stay crisp.")]
        public bool pixelSnap = true;

        struct P
        {
            public Transform t;
            public SpriteRenderer sr;
            public Image img;
            public Vector2 start, velocity;
            public float age, life, phase, size;
        }

        P[] ps;
        bool ui;

        void Start()
        {
            ui = transform is RectTransform;
            ps = new P[count];
            for (int i = 0; i < count; i++)
            {
                var go = ui ? new GameObject("p", typeof(RectTransform)) : new GameObject("p");
                go.transform.SetParent(transform, false);
                ps[i].t = go.transform;
                if (ui)
                {
                    var img = go.AddComponent<Image>();
                    img.sprite = sprite;
                    img.raycastTarget = false;
                    ((RectTransform)go.transform).sizeDelta = sprite.rect.size;   // 1 sprite pixel = 1 canvas pixel × scale
                    ps[i].img = img;
                }
                else
                {
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = sprite;
                    sr.sortingOrder = sortingOrder;
                    ps[i].sr = sr;
                }
                Respawn(ref ps[i]);
                ps[i].age = Random.value * ps[i].life;   // pre-warm so the field is full on frame one
            }
        }

        void Respawn(ref P p)
        {
            p.start = new Vector2(Random.Range(area.xMin, area.xMax), Random.Range(area.yMin, area.yMax));
            p.velocity = new Vector2(Random.Range(minVelocity.x, maxVelocity.x), Random.Range(minVelocity.y, maxVelocity.y));
            p.life = Random.Range(lifeSeconds.x, lifeSeconds.y);
            p.phase = Random.value * Mathf.PI * 2f;
            p.size = Random.Range(scale.x, scale.y);
            p.age = 0f;
            p.t.localScale = new Vector3(p.size, p.size, 1f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            float step = 1f / PixelSprites.PPU;
            for (int i = 0; i < ps.Length; i++)
            {
                ref var p = ref ps[i];
                p.age += dt;
                if (p.age >= p.life) Respawn(ref p);

                Vector2 pos = p.start + p.velocity * p.age;
                pos.x += Mathf.Sin(p.age * wobbleHz * Mathf.PI * 2f + p.phase) * wobble;
                if (!ui && pixelSnap) pos = new Vector2(Mathf.Round(pos.x / step) * step, Mathf.Round(pos.y / step) * step);
                p.t.localPosition = pos;

                float u = p.age / p.life;
                float a = Mathf.Min(1f, Mathf.Min(u, 1f - u) * 5f) * maxAlpha;   // quick fade in, quick fade out
                if (blinkHz > 0f) a *= Mathf.Clamp01(Mathf.Sin(p.age * blinkHz * Mathf.PI * 2f + p.phase) * 1.5f + 0.5f);
                var c = color;
                c.a = a;
                if (p.sr != null) p.sr.color = c;
                if (p.img != null) p.img.color = c;
            }
        }
    }
}
