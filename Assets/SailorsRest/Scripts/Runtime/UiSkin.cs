using UnityEngine;

namespace SailorsRest
{
    /// <summary>
    /// The SpriteCook landscape UI kit as named slots for the HUD, Map and Market. Each slot is filled by
    /// "Sailor's Rest → Build All Scenes" from the file named in its <see cref="SpriteFileAttribute"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Sailor's Rest/UI Skin", fileName = "UiSkin")]
    public class UiSkin : ScriptableObject
    {
        [Header("Panels (9-slice)")]
        [SpriteFile("panel_paper_large")] public Sprite panelPaper;
        [SpriteFile("title_plate_light")] public Sprite titlePlateLight;
        [SpriteFile("title_plate_dark")] public Sprite titlePlateDark;
        [SpriteFile("slot_normal")] public Sprite slotNormal;
        [SpriteFile("slot_locked")] public Sprite slotLocked;

        [Header("Meters")]
        [SpriteFile("depth_gauge_frame")] public Sprite gaugeFrame;
        [SpriteFile("power_bar_fill_amber")] public Sprite powerFill;
        [SpriteFile("power_bar_fill_red")] public Sprite powerFillMax;
        [SpriteFile("depth_gauge_water_fill")] public Sprite waterFill;
        [SpriteFile("depth_hook_marker")] public Sprite hookMarker;
        [SpriteFile("tension_meter_track")] public Sprite tensionTrack;
        [SpriteFile("tension_zone_safe")] public Sprite tensionSafe;
        [SpriteFile("tension_zone_danger")] public Sprite tensionDanger;
        [SpriteFile("tension_marker")] public Sprite tensionMarker;

        [Header("Icons")]
        [SpriteFile("sun_cast_left_a")] public Sprite sunLeft;
        [SpriteFile("sun_cast_spent")] public Sprite sunSpent;
        [SpriteFile("badge_golden_fish")] public Sprite coin;
        [SpriteFile("tier_pip_filled_a")] public Sprite pipFilled;
        [SpriteFile("tier_pip_empty_a")] public Sprite pipEmpty;

        [Header("Input prompts")]
        [SpriteFile("mouse_glyph_left")] public Sprite mouseLeft;
        [SpriteFile("mouse_glyph_right")] public Sprite mouseRight;
        [SpriteFile("mouse_glyph_idle")] public Sprite mouseIdle;
        [SpriteFile("face_button_amber")] public Sprite faceButton;
        [SpriteFile("face_button_dark")] public Sprite faceButtonDark;
        [SpriteFile("keycap_wide")] public Sprite keycap;

        [Header("Buttons")]
        [SpriteFile("button_wide")] public Sprite buttonWide;
        [SpriteFile("button_wide_hover")] public Sprite buttonWideHover;
        [SpriteFile("button_wide_pressed")] public Sprite buttonWidePressed;
        [SpriteFile("button_wide_disabled")] public Sprite buttonWideDisabled;
    }
}
