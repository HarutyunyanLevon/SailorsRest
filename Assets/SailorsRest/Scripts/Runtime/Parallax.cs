using UnityEngine;

namespace SailorsRest
{
    /// <summary>Moves a background layer with the camera: factor 1 = glued to the camera, 0 = fixed in the world.</summary>
    [DefaultExecutionOrder(AfterCameraFollow)]
    public class Parallax : MonoBehaviour
    {
        /// <summary>Runs after the camera has moved this frame, so layers never lag a frame behind.</summary>
        const int AfterCameraFollow = 100;

        public Transform cam;
        [Range(0f, 1f)] public float factor = 0.8f;
        Vector3 start;
        float camStartX;

        void Start()
        {
            start = transform.position;
            camStartX = cam.position.x;
        }

        void LateUpdate()
        {
            var p = start;
            p.x += (cam.position.x - camStartX) * factor;
            transform.position = p;
        }
    }
}
