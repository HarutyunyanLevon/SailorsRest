using UnityEngine;
using UnityEngine.InputSystem;

namespace SailorsRest
{
    /// <summary>
    /// Mouse, keyboard and gamepad through one set of actions. Tracks which device was used last
    /// so the HUD can show the matching prompts.
    /// </summary>
    public class FishingInput : MonoBehaviour
    {
        public enum Device { MouseKeyboard, Gamepad }

        // Back / cancel bindings shared with the menu screens.
        public const string EscapeKey = "<Keyboard>/escape";
        public const string PadBack = "<Gamepad>/buttonEast";

        /// <summary>Stick deflection (squared) that counts as the player using it.</summary>
        const float StickDeadzoneSqr = 0.04f;
        /// <summary>Mouse travel (squared, in pixels) that counts as the player using it.</summary>
        const float MouseMoveSqr = 4f;

        public Device LastDevice { get; private set; } = Device.MouseKeyboard;
        public bool PrimaryHeld => primary.IsPressed();
        public bool PrimaryPressed => primary.WasPressedThisFrame();
        public bool PrimaryReleased => primary.WasReleasedThisFrame();
        public bool CancelPressed => cancel.WasPressedThisFrame();
        public Vector2 Stick => steer.ReadValue<Vector2>();
        public bool MenuPressed => menu.WasPressedThisFrame();

        InputAction primary, cancel, steer, menu;
        Vector2 lastMouse;

        void Awake()
        {
            primary = new InputAction("Primary", InputActionType.Button);
            primary.AddBinding("<Mouse>/leftButton");
            primary.AddBinding("<Gamepad>/buttonSouth");
            primary.AddBinding("<Keyboard>/space");

            cancel = new InputAction("Cancel", InputActionType.Button);
            cancel.AddBinding("<Mouse>/rightButton");
            cancel.AddBinding(PadBack);
            cancel.AddBinding("<Keyboard>/r");

            steer = new InputAction("Steer", InputActionType.Value);
            steer.AddBinding("<Gamepad>/leftStick");
            steer.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");

            menu = new InputAction("Menu", InputActionType.Button);
            menu.AddBinding(EscapeKey);
            menu.AddBinding("<Keyboard>/m");
            menu.AddBinding("<Gamepad>/start");
        }

        void OnEnable() { primary.Enable(); cancel.Enable(); steer.Enable(); menu.Enable(); }
        void OnDisable() { primary.Disable(); cancel.Disable(); steer.Disable(); menu.Disable(); }
        void OnDestroy() { primary.Dispose(); cancel.Dispose(); steer.Dispose(); menu.Dispose(); }

        void Update()
        {
            var pad = Gamepad.current;
            if (pad != null && (pad.leftStick.ReadValue().sqrMagnitude > StickDeadzoneSqr
                                || pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame))
                LastDevice = Device.Gamepad;

            var mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 pos = mouse.position.ReadValue();
                if ((pos - lastMouse).sqrMagnitude > MouseMoveSqr || mouse.leftButton.wasPressedThisFrame)
                    LastDevice = Device.MouseKeyboard;
                lastMouse = pos;
            }
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                LastDevice = Device.MouseKeyboard;
        }

        /// <summary>
        /// Where the player is pointing in the world, if they steer with the mouse.
        /// Returns false when a stick or keys are in use; then read <see cref="Stick"/> instead.
        /// </summary>
        public bool TryGetPointerWorld(Camera cam, out Vector2 world)
        {
            world = default;
            if (LastDevice != Device.MouseKeyboard || Mouse.current == null || cam == null) return false;
            if (Stick.sqrMagnitude > StickDeadzoneSqr) return false; // WASD overrides the mouse
            Vector3 p = Mouse.current.position.ReadValue();
            p.z = -cam.transform.position.z;
            world = cam.ScreenToWorldPoint(p);
            return true;
        }

        public string Prompt(string mouseKey, string padKey, string action) =>
            $"[{(LastDevice == Device.Gamepad ? padKey : mouseKey)}] {action}";
    }
}
