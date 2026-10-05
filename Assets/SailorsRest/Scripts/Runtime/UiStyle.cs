using UnityEngine;

namespace SailorsRest
{
    /// <summary>Shared UI metrics. All sizes are in reference-canvas pixels (1920x1080).</summary>
    public static class UiStyle
    {
        /// <summary>
        /// The canvas is never smaller than this in either direction (CanvasScaler "Expand"), so a 16:10 Steam Deck
        /// screen gets extra height instead of squeezing the layout sideways.
        /// </summary>
        public static readonly Vector2 ReferenceResolution = new Vector2(1920, 1080);

        /// <summary>Gap between screen-edge panels and the edge of the screen.</summary>
        public const float ScreenMargin = 24f;

        /// <summary>
        /// How much thinner 9-slice borders draw than the kit's native pixels (2 = half as thick).
        /// Applied to every sliced panel, plate, button and gauge frame.
        /// </summary>
        public const float BorderThinning = 2f;

        // Font sizes.
        public const int TitleSize = 36;
        public const int ValueSize = 34;
        public const int HeadingSize = 32;
        public const int LargeBodySize = 30;
        public const int BodySize = 26;
        public const int MediumSize = 28;
        public const int SmallSize = 24;
        public const int NoteSize = 22;
        public const int CaptionSize = 20;
        public const int MinFitSize = 14;

        // Outline that keeps light text readable over scenery.
        public static readonly Vector2 TextOutlineOffset = new Vector2(2, -2);
        public const float TextOutlineAlpha = 0.85f;

        // Top-right chips shared by the HUD, Map and Market.
        public static readonly Vector2 ChipSize = new Vector2(280, 88);
        public static readonly Vector2 DayChipSize = new Vector2(280, 96);
        public const float ChipGap = 12f;
        public const float ChipIconInset = 36f;
        public const float ChipTextInset = 40f;
        public static readonly Vector2 CoinIconSize = new Vector2(48, 48);
        public static readonly Vector2 SunIconSize = new Vector2(44, 44);
        public static readonly Vector2 ChipTextSize = new Vector2(150, 70);   // stops short of the icon

        // Screen title plate, top-left.
        public static readonly Vector2 TitlePlateSize = new Vector2(440, 84);
        public const float TitlePlatePadding = 40f;

        // Buttons.
        public static readonly Vector2 ButtonTextPadding = new Vector2(30, 10);
        public static readonly Vector2 ButtonTextNudge = new Vector2(0, 2);
    }

    /// <summary>Anchor and pivot points on a RectTransform.</summary>
    public static class Anchor
    {
        public static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        public static readonly Vector2 Top = new Vector2(0.5f, 1f);
        public static readonly Vector2 TopRight = new Vector2(1f, 1f);
        public static readonly Vector2 Left = new Vector2(0f, 0.5f);
        public static readonly Vector2 Centre = new Vector2(0.5f, 0.5f);
        public static readonly Vector2 Right = new Vector2(1f, 0.5f);
        public static readonly Vector2 BottomLeft = new Vector2(0f, 0f);
        public static readonly Vector2 Bottom = new Vector2(0.5f, 0f);
        public static readonly Vector2 BottomRight = new Vector2(1f, 0f);
    }
}
