using UnityEngine;
using UnityEngine.EventSystems;

namespace SailorsRest
{
    /// <summary>Grows a map marker a little while it is hovered or focused by keyboard/gamepad.</summary>
    public class PopOnSelect : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        const float RestScale = 1f;

        public System.Action onFocus;
        public float focusScale = 1.12f;
        [Tooltip("Scale change per second.")]
        public float growSpeed = 2f;
        float target = RestScale;

        public void OnSelect(BaseEventData e) => Focus();
        public void OnPointerEnter(PointerEventData e) => Focus();
        public void OnDeselect(BaseEventData e) => target = RestScale;
        public void OnPointerExit(PointerEventData e) => target = RestScale;

        void Focus()
        {
            target = focusScale;
            onFocus?.Invoke();
        }

        void Update()
        {
            float s = Mathf.MoveTowards(transform.localScale.x, target, Time.unscaledDeltaTime * growSpeed);
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
