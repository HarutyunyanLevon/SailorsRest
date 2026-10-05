using UnityEngine;

namespace SailorsRest
{
    /// <summary>
    /// Free code idle: a gentle sway (rotation), bob (local offset) and breathe (scale) on any transform,
    /// world or UI. Only touches what is switched on, so it can share an object that other code moves:
    /// the bobber uses sway only, because the fishing controller owns its position.
    /// </summary>
    public class IdleMotion : MonoBehaviour
    {
        const float FullCircle = Mathf.PI * 2f;

        [Header("Sway (degrees)")]
        public float swayDegrees;
        public float swaySeconds = 2.4f;

        [Header("Bob (local units: world units, or pixels on UI)")]
        public Vector2 bob;
        public float bobSeconds = 2f;

        [Header("Breathe (scale fraction, e.g. 0.03)")]
        public float breathe;
        public float breatheSeconds = 2.8f;

        [Tooltip("Snap the bob offset to this step (world units). 0 = smooth. Use 1/16 for pixel-crisp world sprites.")]
        public float snap;

        Vector3 basePos, baseScale;
        Quaternion baseRot;
        float phase;
        bool moveUsed;

        public static IdleMotion Sway(GameObject go, float degrees, float seconds)
        {
            var m = go.AddComponent<IdleMotion>();
            m.swayDegrees = degrees;
            m.swaySeconds = seconds;
            return m;
        }

        public static IdleMotion Breathe(GameObject go, float amount, float seconds)
        {
            var m = go.AddComponent<IdleMotion>();
            m.breathe = amount;
            m.breatheSeconds = seconds;
            return m;
        }

        public static IdleMotion Bob(GameObject go, Vector2 offset, float seconds, float snap = 0f)
        {
            var m = go.AddComponent<IdleMotion>();
            m.bob = offset;
            m.bobSeconds = seconds;
            m.snap = snap;
            return m;
        }

        void Start()
        {
            basePos = transform.localPosition;
            baseRot = transform.localRotation;
            baseScale = transform.localScale;
            phase = Random.value * FullCircle;
            moveUsed = bob != Vector2.zero;
        }

        void LateUpdate()
        {
            float t = Time.time;
            if (swayDegrees != 0f)
                transform.localRotation = baseRot * Quaternion.Euler(0f, 0f, Wave(t, swaySeconds) * swayDegrees);
            if (moveUsed)
            {
                Vector3 offset = bob * Wave(t, bobSeconds);
                if (snap > 0f) offset = new Vector3(Mathf.Round(offset.x / snap) * snap, Mathf.Round(offset.y / snap) * snap, 0f);
                transform.localPosition = basePos + offset;
            }
            if (breathe != 0f)
            {
                float s = 1f + Wave(t, breatheSeconds) * breathe;
                transform.localScale = new Vector3(baseScale.x * s, baseScale.y * s, baseScale.z);
            }
        }

        float Wave(float t, float seconds) => Mathf.Sin(t / Mathf.Max(seconds, 0.01f) * FullCircle + phase);
    }
}
