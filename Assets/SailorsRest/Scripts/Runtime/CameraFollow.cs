using UnityEngine;

namespace SailorsRest
{
    /// <summary>Eases toward a target (or a rest point), clamped so the view never leaves the lake.</summary>
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector2 restPosition = new Vector2(13f, -3f);
        [Tooltip("Keeps the target off-centre, toward open water and the sky.")]
        public Vector2 lookAhead = new Vector2(4f, 2f);
        public Vector2 minPosition = new Vector2(13f, -12f);
        public Vector2 maxPosition = new Vector2(48f, -3f);
        public float smoothTime = 0.35f;
        Vector3 velocity;

        void LateUpdate()
        {
            Vector2 goal = target != null ? (Vector2)target.position + lookAhead : restPosition;
            goal = Vector2.Max(minPosition, Vector2.Min(maxPosition, goal));
            var to = new Vector3(goal.x, goal.y, transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, to, ref velocity, smoothTime);
        }
    }
}
