using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SailorsRest
{
    /// <summary>
    /// Builds uGUI elements in the SpriteCook kit style. One instance per screen, bound to the <see cref="UiSkin"/>.
    /// Every element is anchored and pivoted on the same point, so <c>pos</c> is an offset from that corner or edge.
    /// </summary>
    public sealed class UiKit
    {
        const string BuiltinFont = "LegacyRuntime.ttf";
        static Font font;

        public UiSkin Skin { get; }

        public UiKit(UiSkin skin) => Skin = skin;

        static Font Font => font != null ? font : font = Resources.GetBuiltinResource<Font>(BuiltinFont);

        // ----- roots -----

        public static RectTransform Canvas(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = UiStyle.ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            return go.GetComponent<RectTransform>();
        }

        /// <summary>Mouse, keyboard and gamepad navigation for buttons, using the new Input System.</summary>
        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            UnityEngine.Object.DontDestroyOnLoad(go);
        }

        /// <summary>Full-screen picture that covers the screen without stretching.</summary>
        public Image Backdrop(RectTransform root, Sprite sprite)
        {
            var img = Image(root, Anchor.Centre, Vector2.zero, UiStyle.ReferenceResolution, sprite);
            img.name = "Backdrop";
            var fitter = img.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
            return img;
        }

        // ----- primitives -----

        /// <summary>An empty, invisible rectangle for grouping and toggling elements.</summary>
        public RectTransform Group(RectTransform parent, Vector2 anchor, Vector2 pos, Vector2 size, string name = "Group") =>
            Rect(new GameObject(name, typeof(RectTransform)), parent, anchor, pos, size);

        public Image Image(RectTransform parent, Vector2 anchor, Vector2 pos, Vector2 size, Sprite sprite, bool sliced = false)
        {
            var go = new GameObject(sprite != null ? sprite.name : "Image", typeof(RectTransform), typeof(Image));
            Rect(go, parent, anchor, pos, size);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.sprite = sprite;
            if (sliced && sprite != null && sprite.border.sqrMagnitude > 0f)
            {
                img.type = UnityEngine.UI.Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = UiStyle.BorderThinning;
            }
            return img;
        }

        public Image Icon(RectTransform parent, Vector2 anchor, Vector2 pos, Vector2 size, Sprite sprite)
        {
            var img = Image(parent, anchor, pos, size, sprite);
            img.preserveAspect = true;
            return img;
        }

        /// <summary>A flat-coloured rectangle.</summary>
        public Image Block(RectTransform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
        {
            var img = Image(parent, anchor, pos, size, null);
            img.color = color;
            return img;
        }

        public RectTransform Panel(RectTransform parent, Vector2 anchor, Vector2 pos, Vector2 size) =>
            Image(parent, anchor, pos, size, Skin.panelPaper, true).rectTransform;

        public RectTransform Plate(RectTransform parent, Vector2 anchor, Vector2 pos, Vector2 size, bool dark) =>
            Image(parent, anchor, pos, size, dark ? Skin.titlePlateDark : Skin.titlePlateLight, true).rectTransform;

        public RectTransform Slot(RectTransform parent, Vector2 anchor, Vector2 pos, Vector2 size) =>
            Image(parent, anchor, pos, size, Skin.slotNormal, true).rectTransform;

        public Text Label(RectTransform parent, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize, TextAnchor align, Color color, string text = "")
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            var rt = Rect(go, parent, anchor, pos, size);
            rt.pivot = new Vector2(anchor.x, Anchor.Centre.y);
            var t = go.GetComponent<Text>();
            t.font = Font;
            t.fontSize = fontSize;
            t.alignment = align;
            t.color = color;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.text = text;
            return t;
        }

        /// <summary>Light text with a dark outline, readable straight on top of scenery.</summary>
        public Text OverlayLabel(RectTransform parent, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize, TextAnchor align, string text = "")
        {
            var t = Label(parent, anchor, pos, size, fontSize, align, Palette.OnWood, text);
            var outline = t.gameObject.AddComponent<Outline>();
            var c = Palette.Edge;
            c.a = UiStyle.TextOutlineAlpha;
            outline.effectColor = c;
            outline.effectDistance = UiStyle.TextOutlineOffset;
            return t;
        }

        /// <summary>A paper button; hover, focus, press and disabled use the kit's state sprites.</summary>
        public Button Button(RectTransform parent, Vector2 anchor, Vector2 pos, Vector2 size, string text, Action onClick, int fontSize = UiStyle.BodySize)
        {
            var img = Image(parent, anchor, pos, size, Skin.buttonWide, true);
            img.name = "Button " + text;
            img.raycastTarget = true;
            var button = img.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = Skin.buttonWideHover,
                selectedSprite = Skin.buttonWideHover,
                pressedSprite = Skin.buttonWidePressed,
                disabledSprite = Skin.buttonWideDisabled,
            };
            var label = Label(img.rectTransform, Anchor.Centre, UiStyle.ButtonTextNudge, size - UiStyle.ButtonTextPadding, fontSize, TextAnchor.MiddleCenter, Palette.Ink, text);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = UiStyle.MinFitSize;
            label.resizeTextMaxSize = fontSize;
            button.onClick.AddListener(() => onClick());
            return button;
        }

        // ----- pieces shared by the HUD, Map and Market -----

        /// <summary>Dark wooden title plate in the top-left corner.</summary>
        public void ScreenTitle(RectTransform root, string title)
        {
            var plate = Plate(root, Anchor.TopLeft, Inset(Anchor.TopLeft), UiStyle.TitlePlateSize, true);
            var textSize = UiStyle.TitlePlateSize - Vector2.one * UiStyle.TitlePlatePadding;
            Label(plate, Anchor.Centre, Vector2.zero, textSize, UiStyle.TitleSize, TextAnchor.MiddleCenter, Palette.OnWood, title);
        }

        /// <summary>Coin count in the top-right corner. Returns the text to update.</summary>
        public Text CoinChip(RectTransform root) =>
            Chip(root, 0f, UiStyle.ChipSize, Skin.coin, UiStyle.CoinIconSize, UiStyle.ValueSize);

        public static string Coins(int coins) => coins.ToString("N0");

        public static string DayAndCreel(int day, int creelCount) => $"Day {day}\n{creelCount} in creel";

        /// <summary>Day and creel count under the coin chip. Returns the text to update.</summary>
        public Text DayChip(RectTransform root) =>
            Chip(root, UiStyle.ChipSize.y + UiStyle.ChipGap, UiStyle.DayChipSize, Skin.sunLeft, UiStyle.SunIconSize, UiStyle.SmallSize);

        Text Chip(RectTransform root, float drop, Vector2 size, Sprite icon, Vector2 iconSize, int fontSize)
        {
            var chip = Panel(root, Anchor.TopRight, Inset(Anchor.TopRight) + Vector2.down * drop, size);
            Icon(chip, Anchor.Left, new Vector2(UiStyle.ChipIconInset, 0f), iconSize, icon);
            return Label(chip, Anchor.Right, new Vector2(-UiStyle.ChipTextInset, 0f), UiStyle.ChipTextSize, fontSize, TextAnchor.MiddleRight, Palette.Ink);
        }

        // ----- helpers -----

        /// <summary>Offset that keeps an element anchored at a screen edge or corner <see cref="UiStyle.ScreenMargin"/> inside it.</summary>
        public static Vector2 Inset(Vector2 anchor) =>
            new Vector2(InwardSign(anchor.x), InwardSign(anchor.y)) * UiStyle.ScreenMargin;

        /// <summary>+1 from the low edge, -1 from the high edge, 0 from the middle.</summary>
        static float InwardSign(float edge) => edge < Anchor.Centre.x ? 1f : edge > Anchor.Centre.x ? -1f : 0f;

        /// <summary>Gives keyboard/gamepad focus to a button if nothing sensible has it.</summary>
        public static void FocusIfLost(Selectable fallback)
        {
            var es = EventSystem.current;
            if (es == null || fallback == null) return;
            var selected = es.currentSelectedGameObject;
            if (selected == null || !selected.activeInHierarchy) es.SetSelectedGameObject(fallback.gameObject);
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }

        static RectTransform Rect(GameObject go, RectTransform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }
    }
}
