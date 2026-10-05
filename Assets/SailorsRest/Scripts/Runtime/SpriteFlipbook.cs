using UnityEngine;
using UnityEngine.UI;

namespace SailorsRest
{
    /// <summary>
    /// Loops a SpriteCook idle animation on the SpriteRenderer or UI Image on the same object.
    /// Each copy starts on a random frame so a row of weeds or a school of fish never moves in step.
    /// </summary>
    public class SpriteFlipbook : MonoBehaviour
    {
        public Sprite[] frames;
        public float fps = 8f;
        [Tooltip("Random ± change to the speed, so copies drift out of step over time.")]
        [Range(0f, 0.5f)] public float speedJitter = 0.1f;

        SpriteRenderer sr;
        Image img;
        float time, speed;

        /// <summary>Adds a flipbook to <paramref name="target"/> when there are frames; returns null otherwise.</summary>
        public static SpriteFlipbook Play(Component target, Sprite[] frames, float fps = 8f)
        {
            if (target == null || frames == null || frames.Length < 2) return null;
            var book = target.gameObject.AddComponent<SpriteFlipbook>();
            book.frames = frames;
            book.fps = fps;
            return book;
        }

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            img = GetComponent<Image>();
        }

        void Start()
        {
            speed = 1f + Random.Range(-speedJitter, speedJitter);
            time = Random.value * frames.Length / Mathf.Max(fps, 0.01f);
            Show(0);
        }

        void Update()
        {
            if (frames == null || frames.Length == 0) return;
            time += Time.deltaTime * speed;
            Show(Mathf.FloorToInt(time * fps) % frames.Length);
        }

        void Show(int i)
        {
            var frame = frames[Mathf.Clamp(i, 0, frames.Length - 1)];
            if (sr != null) sr.sprite = frame;
            if (img != null) img.sprite = frame;
        }
    }
}
