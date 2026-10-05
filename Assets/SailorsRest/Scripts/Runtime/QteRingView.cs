using System.Collections.Generic;
using SailorsRest.Rules;
using UnityEngine;

namespace SailorsRest
{
    /// <summary>Draws the hooking ring above a fish: a generated pixel ring and a needle that turns clockwise.</summary>
    public class QteRingView : MonoBehaviour
    {
        /// <summary>Pulls the ring towards the camera so it never hides behind the fish.</summary>
        static readonly Vector3 TowardCamera = new Vector3(0f, 0f, -0.1f);

        SpriteRenderer ring, needle;
        QteModel model;

        void Awake()
        {
            ring = Child("Ring", SortOrder.Ring);
            needle = Child("Needle", SortOrder.RingNeedle);
            needle.sprite = PixelSprites.Needle;
            gameObject.SetActive(false);
        }

        SpriteRenderer Child(string name, int order)
        {
            var sr = new GameObject(name).AddComponent<SpriteRenderer>();
            sr.transform.SetParent(transform, false);
            sr.sortingOrder = order;
            return sr;
        }

        public void Show(QteModel qte, Vector3 worldPos)
        {
            model = qte;
            transform.position = worldPos + TowardCamera;
            SetRing(qte.Arcs, false);
            needle.color = Color.white;
            gameObject.SetActive(true);
            LateUpdate();
        }

        public void ShowResult(bool hit)
        {
            if (model == null) return;
            SetRing(model.Arcs, hit);
            needle.color = hit ? Color.white : Palette.Danger;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            model = null;
        }

        /// <summary>Each ring is drawn for its own arcs, so the previous texture is freed rather than left to pile up.</summary>
        void SetRing(IReadOnlyList<QteModel.Arc> arcs, bool hit)
        {
            FreeRing();
            ring.sprite = PixelSprites.Ring(arcs, hit);
        }

        void OnDestroy() => FreeRing();

        void FreeRing()
        {
            if (ring == null || ring.sprite == null) return;
            Destroy(ring.sprite.texture);
            Destroy(ring.sprite);
        }

        // The needle points up at 0° and turns clockwise, so it rotates by the negative angle.
        void LateUpdate()
        {
            if (model != null) needle.transform.localRotation = Quaternion.Euler(0f, 0f, -model.ArrowDeg);
        }
    }
}
