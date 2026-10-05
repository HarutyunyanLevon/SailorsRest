using UnityEngine;
using UnityEngine.UI;

namespace SailorsRest
{
    /// <summary>
    /// The world map, the game's hub. Shore Pond (fishing) and the Harbour Market are open; two more
    /// spots are shown locked so the map reads as a place that grows.
    /// </summary>
    public class MapView : MonoBehaviour
    {
        public GameBalance balance;
        public UiSkin skin;
        public WorldArt art;

        static class Layout
        {
            // Where each spot sits on the map picture, 0..1 across and up.
            public static readonly Vector2 PondAt = new Vector2(0.34f, 0.46f);
            public static readonly Vector2 MarketAt = new Vector2(0.70f, 0.52f);
            public static readonly Vector2 PineCreekAt = new Vector2(0.17f, 0.74f);
            public static readonly Vector2 QuarryLakeAt = new Vector2(0.52f, 0.78f);

            public static readonly Vector2 Marker = new Vector2(220, 220);
            public static readonly Vector2 MarkerPlate = new Vector2(300, 64);
            public const float MarkerPlateDrop = 50f;
            public static readonly Vector2 Locked = new Vector2(110, 90);
            public static readonly Vector2 LockedPlate = new Vector2(270, 50);
            public const float LockedPlateDrop = 34f;
            public const float LockedPlateAlpha = 0.8f;
            public static readonly Vector2 PlatePadding = new Vector2(30, 16);

            public static readonly Vector2 InfoPanel = new Vector2(620, 170);
            public const float InfoInset = 44f;
            public static readonly Vector2 InfoTitle = new Vector2(540, 40);
            public const float InfoTitleDrop = 48f;
            public static readonly Vector2 InfoBody = new Vector2(540, 70);
            public const float InfoBodyRaise = 64f;

            public static readonly Vector2 QuitButton = new Vector2(200, 76);
        }

        UiKit ui;
        Text infoTitle, infoBody;
        Button pond;

        void Start()
        {
            UiKit.EnsureEventSystem();
            ui = new UiKit(skin);
            var progress = GameSession.Progress;
            var root = UiKit.Canvas(transform, "Map Canvas");
            var map = ui.Backdrop(root, art.mapBackdrop).rectTransform;
            // The sea is the right-hand third of the map picture: let light glint on it.
            var sea = ui.Group(map, new Vector2(0.84f, 0.5f), Vector2.zero, new Vector2(map.rect.width * 0.28f, map.rect.height * 0.8f), "Sea");
            sea.pivot = Anchor.Centre;
            UiIdle.Twinkle(sea, art.mapSparkles, Palette.OnWood, 1f);

            ui.ScreenTitle(root, "Sailor's Rest");
            ui.CoinChip(root).text = UiKit.Coins(progress.coins);
            ui.DayChip(root).text = UiKit.DayAndCreel(progress.day, progress.creel.Count);

            int castsLeft = progress.CastsToday(balance.castsPerDay);
            pond = Marker(map, Layout.PondAt, art.mapPond, "Shore Pond",
                $"Perch, carp and catfish.\n{castsLeft}/{balance.castsPerDay} casts left today.", GameSession.PondScene);
            SpriteFlipbook.Play(pond.targetGraphic, art.mapPondIdle, art.idleFps);
            var market = Marker(map, Layout.MarketAt, art.mapMarket, "Harbour Market",
                $"Sell your creel ({progress.creel.Count} fish) and upgrade your tackle.", GameSession.MarketScene);
            SpriteFlipbook.Play(market.targetGraphic, art.mapMarketIdle, art.idleFps);
            Locked(map, Layout.PineCreekAt, "Pine Creek");
            Locked(map, Layout.QuarryLakeAt, "Old Quarry Lake");

            var info = ui.Panel(root, Anchor.BottomLeft, UiKit.Inset(Anchor.BottomLeft), Layout.InfoPanel);
            infoTitle = ui.Label(info, Anchor.TopLeft, new Vector2(Layout.InfoInset, -Layout.InfoTitleDrop), Layout.InfoTitle,
                UiStyle.LargeBodySize, TextAnchor.MiddleLeft, Palette.Ink);
            infoBody = ui.Label(info, Anchor.BottomLeft, new Vector2(Layout.InfoInset, Layout.InfoBodyRaise), Layout.InfoBody,
                UiStyle.NoteSize, TextAnchor.MiddleLeft, Palette.InkMuted);
            Show("Shore Pond", "Pick a spot. Fish at Shore Pond, then sell at the Harbour Market.");

            // Quitting is a button only: Esc is "back" on every other screen, so a double press must not close the game.
            ui.Button(root, Anchor.BottomRight, UiKit.Inset(Anchor.BottomRight), Layout.QuitButton, "Quit", Quit);
        }

        void Update() => UiKit.FocusIfLost(pond);

        static void Quit()
        {
            GameSession.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void Show(string title, string body)
        {
            infoTitle.text = title;
            infoBody.text = body;
        }

        /// <summary>A map spot you can travel to: a picture that grows when focused, with its name on a plate below.</summary>
        Button Marker(RectTransform map, Vector2 at, Sprite icon, string name, string description, string scene)
        {
            var img = ui.Icon(map, at, Vector2.zero, Layout.Marker, icon);
            img.rectTransform.pivot = Anchor.Centre;
            img.raycastTarget = true;
            var button = img.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            button.onClick.AddListener(() => GameSession.Go(scene));
            img.gameObject.AddComponent<PopOnSelect>().onFocus = () => Show(name, description);
            NamePlate(img.rectTransform, Layout.MarkerPlate, Layout.MarkerPlateDrop, name, UiStyle.BodySize, Palette.Ink);
            return button;
        }

        void Locked(RectTransform map, Vector2 at, string name)
        {
            var lockIcon = ui.Icon(map, at, Vector2.zero, Layout.Locked, skin.slotLocked).rectTransform;
            lockIcon.pivot = Anchor.Centre;
            IdleMotion.Bob(lockIcon.gameObject, new Vector2(0f, UiIdle.ArtPixel), 3.2f, UiIdle.ArtPixel);
            var plate = NamePlate(lockIcon, Layout.LockedPlate, Layout.LockedPlateDrop, name + " · soon", UiStyle.CaptionSize, Palette.InkMuted);
            var faded = plate.GetComponent<Image>().color;
            faded.a = Layout.LockedPlateAlpha;
            plate.GetComponent<Image>().color = faded;
        }

        RectTransform NamePlate(RectTransform under, Vector2 size, float drop, string text, int fontSize, Color color)
        {
            var plate = ui.Plate(under, Anchor.Bottom, Vector2.down * drop, size, false);
            plate.pivot = Anchor.Centre;
            ui.Label(plate, Anchor.Centre, Vector2.zero, size - Layout.PlatePadding, fontSize, TextAnchor.MiddleCenter, color, text);
            return plate;
        }
    }
}
