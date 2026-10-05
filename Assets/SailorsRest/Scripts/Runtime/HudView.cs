using System.Collections.Generic;
using SailorsRest.Rules;
using UnityEngine;
using UnityEngine.UI;
using Phase = SailorsRest.FishingController.Phase;

namespace SailorsRest
{
    /// <summary>
    /// The fishing HUD, laid out like the landscape SpriteCook concept: casts top-left, coins and day top-right,
    /// power gauge left edge, depth gauge right edge, hooked fish bottom-left, line tension bottom-centre,
    /// input prompts bottom-right, toasts top-centre and the sunset card in the middle.
    /// </summary>
    public class HudView : MonoBehaviour
    {
        public FishingController game;
        public FishingInput input;
        public UiSkin skin;

        static class Layout
        {
            public const float ToastSeconds = 2.5f;
            public const int LowCastsWarning = 2;

            public static readonly Vector2 CastsPanel = new Vector2(500, 88);
            public static readonly Vector2 Sun = new Vector2(34, 34);
            public const float SunFirstX = 40f, SunStep = 38f;
            public static readonly Vector2 CastsCount = new Vector2(90, 44);
            public const float CastsCountInset = 44f;

            // Gauges: a frame with a cap above and below the inner strip, a title above the frame.
            public const float GaugeLength = 330f;
            public const float GaugeCap = 12f;
            public static readonly Vector2 GaugeFrame = new Vector2(62, GaugeLength + 2 * GaugeCap);
            public static readonly Vector2 GaugeInner = new Vector2(30, GaugeLength);
            public static readonly Vector2 GaugeTitle = new Vector2(110, 30);
            public const float GaugeTitleRaise = 34f;
            public const float GaugeLift = 20f;          // the gauges sit slightly above the screen's middle
            public const float GaugeFrameBottom = 40f;
            public static readonly Vector2 PowerArea = new Vector2(130, 480);
            public static readonly Vector2 DepthArea = new Vector2(210, 480);
            public const float DepthFrameLeft = 40f;
            public static readonly Vector2 DepthLimit = new Vector2(50, 6);
            public static readonly Vector2 DepthHook = new Vector2(40, 32);
            public static readonly Vector2 DepthReadout = new Vector2(90, 40);
            public const float DepthStepsPerMetre = 10f;      // the readout shows tenths of a metre
            public const float DepthReadoutInset = 40f;

            public static readonly Vector2 FishCard = new Vector2(520, 128);
            public static readonly Vector2 FishIcon = new Vector2(124, 80);
            public const float FishIconInset = 18f;
            public static readonly Vector2 FishName = new Vector2(320, 40);
            public static readonly Vector2 FishNamePos = new Vector2(160, -42);
            public static readonly Vector2 FishInfo = new Vector2(230, 34);
            public static readonly Vector2 FishInfoPos = new Vector2(262, 42);
            public static readonly Vector2 Pip = new Vector2(22, 22);
            public static readonly Vector2 FirstPipPos = new Vector2(174, 42);
            public const float PipStep = 28f;

            public const float TrackWidth = 560f;
            public static readonly Vector2 TensionPanel = new Vector2(TrackWidth + 100, 190);
            public static readonly Vector2 TensionTitle = new Vector2(300, 28);
            public const float TensionTitleDrop = 50f;
            public static readonly Vector2 TensionTrack = new Vector2(TrackWidth + 24, 52);
            public const float TensionBedHeight = 26f;
            public static readonly Vector2 TensionMarker = new Vector2(22, 48);
            public static readonly Vector2 TensionState = new Vector2(400, 30);
            public const float TensionStateRaise = 50f;

            public static readonly Vector2 PromptPanel = new Vector2(540, 190);
            public const float PromptRowInset = 40f, PromptRowOffset = 32f;
            public static readonly Vector2 PromptRow = new Vector2(460, 52);
            public static readonly Vector2 PromptIcon = new Vector2(44, 48);
            public static readonly Vector2 PromptGlyph = new Vector2(48, 40);
            public static readonly Vector2 PromptText = new Vector2(380, 44);
            public const float PromptTextInset = 60f;

            public static readonly Vector2 Toast = new Vector2(720, 92);
            public const float ToastDrop = 28f;
            public static readonly Vector2 ToastText = new Vector2(620, 50);

            public static readonly Vector2 SunsetCard = new Vector2(760, 320);
            public static readonly Vector2 SunsetHeader = new Vector2(360, 70);
            public const float SunsetHeaderRaise = 10f;
            public static readonly Vector2 SunsetTitle = new Vector2(320, 50);
            public static readonly Vector2 SunsetText = new Vector2(640, 200);
            public const float SunsetTextDrop = 16f;
        }

        enum Glyph { None, MouseLeft, MouseRight, Mouse, PadA, PadB, Stick, KeyEsc, PadMenu }

        /// <summary>One prompt row: the glyph for each device and what the button does.</summary>
        readonly struct Hint
        {
            public readonly Glyph Mouse, Pad;
            public readonly string Text;
            public Hint(Glyph mouse, Glyph pad, string text) { Mouse = mouse; Pad = pad; Text = text; }
        }

        static readonly Hint NoHint = new Hint(Glyph.None, Glyph.None, "");

        static readonly Dictionary<Phase, Hint[]> Hints = new Dictionary<Phase, Hint[]>
        {
            [Phase.Aiming] = new[]
            {
                new Hint(Glyph.MouseLeft, Glyph.PadA, "Hold to charge, release to cast"),
                new Hint(Glyph.KeyEsc, Glyph.PadMenu, "Back to the map"),
            },
            [Phase.Searching] = new[]
            {
                new Hint(Glyph.Mouse, Glyph.Stick, "Move the hook"),
                new Hint(Glyph.MouseRight, Glyph.PadB, "Reel back"),
            },
            [Phase.Hooking] = new[] { new Hint(Glyph.MouseLeft, Glyph.PadA, "Hook it on green") },
            [Phase.Reeling] = new[]
            {
                new Hint(Glyph.MouseLeft, Glyph.PadA, "Hold to reel"),
                new Hint(Glyph.Mouse, Glyph.Stick, "Steer into matching fish"),
            },
        };

        sealed class PromptRow
        {
            public RectTransform root;
            public Image icon;
            public Text glyph, label;
        }

        UiKit ui;
        Text castsText, coinsText, dayText, depthText, fishName, fishInfo, tensionState, toastText, sunsetText;
        RectTransform powerGauge, depthLimit, depthHook, fishCard, tensionPanel, tensionZone, tensionMarker, toast, sunsetCard;
        Image powerFill, fishIcon;
        Image[] suns, pips;
        PromptRow[] prompts;
        RectTransform promptPanel;
        float toastTimer;

        // Last values written to the texts, so Update only formats a string when something changed.
        const int Unset = -1;
        int shownCasts = Unset, shownCoins = Unset, shownDay = Unset, shownCreel = Unset, shownDepthTenths = Unset;
        FishAgent shownFish;
        FishData shownFishData;
        bool sunsetShown;
        FishingInput.Device sunsetDevice;
        string[] depthLabels;

        void Start()
        {
            ui = new UiKit(skin);
            var root = UiKit.Canvas(transform, "HUD Canvas");
            BuildCasts(root);
            coinsText = ui.CoinChip(root);
            dayText = ui.DayChip(root);
            BuildPower(root);
            BuildDepth(root);
            BuildFishCard(root);
            BuildTension(root);
            BuildPrompts(root);
            BuildToast(root);
            BuildSunset(root);
            game.Toast += ShowToast;
        }

        void OnDestroy()
        {
            if (game != null) game.Toast -= ShowToast;
        }

        // ----- build -----

        void BuildCasts(RectTransform root)
        {
            var panel = ui.Panel(root, Anchor.TopLeft, UiKit.Inset(Anchor.TopLeft), Layout.CastsPanel);
            suns = new Image[game.balance.castsPerDay];
            for (int i = 0; i < suns.Length; i++)
                suns[i] = ui.Image(panel, Anchor.Left, new Vector2(Layout.SunFirstX + i * Layout.SunStep, 0f), Layout.Sun, skin.sunLeft);
            castsText = ui.Label(panel, Anchor.Right, new Vector2(-Layout.CastsCountInset, 0f), Layout.CastsCount,
                UiStyle.HeadingSize, TextAnchor.MiddleRight, Palette.Ink);
        }

        /// <summary>A titled gauge frame. Returns the inner strip, which fills or scrolls inside the frame.</summary>
        Image Gauge(RectTransform area, Vector2 frameAnchor, Vector2 framePos, string title, Sprite inner)
        {
            var frame = ui.Image(area, frameAnchor, framePos, Layout.GaugeFrame, skin.gaugeFrame, true).rectTransform;
            ui.OverlayLabel(frame, Anchor.Top, new Vector2(0f, Layout.GaugeTitleRaise), Layout.GaugeTitle,
                UiStyle.CaptionSize, TextAnchor.MiddleCenter, title);
            return ui.Image(frame, Anchor.Top, new Vector2(0f, -Layout.GaugeCap), Layout.GaugeInner, inner);
        }

        void BuildPower(RectTransform root)
        {
            powerGauge = ui.Group(root, Anchor.Left, UiKit.Inset(Anchor.Left) + Vector2.up * Layout.GaugeLift, Layout.PowerArea, "Power");
            powerFill = Gauge(powerGauge, Anchor.Bottom, new Vector2(0f, Layout.GaugeFrameBottom), "POWER", skin.powerFill);
            powerFill.type = Image.Type.Filled;
            powerFill.fillMethod = Image.FillMethod.Vertical;
            powerFill.fillOrigin = (int)Image.OriginVertical.Bottom;
        }

        void BuildDepth(RectTransform root)
        {
            var area = ui.Group(root, Anchor.Right, UiKit.Inset(Anchor.Right) + Vector2.up * Layout.GaugeLift, Layout.DepthArea, "Depth");
            var water = Gauge(area, Anchor.BottomLeft, new Vector2(Layout.DepthFrameLeft, Layout.GaugeFrameBottom), "DEPTH", skin.waterFill).rectTransform;
            depthLimit = ui.Block(water, Anchor.Top, Vector2.zero, Layout.DepthLimit, Palette.Danger).rectTransform;
            depthHook = ui.Image(water, Anchor.Top, Vector2.zero, Layout.DepthHook, skin.hookMarker).rectTransform;
            depthLimit.pivot = depthHook.pivot = Anchor.Centre;
            depthText = ui.OverlayLabel(area, Anchor.Right, new Vector2(-Layout.DepthReadoutInset, 0f), Layout.DepthReadout,
                UiStyle.BodySize, TextAnchor.MiddleRight);
        }

        void BuildFishCard(RectTransform root)
        {
            fishCard = ui.Panel(root, Anchor.BottomLeft, UiKit.Inset(Anchor.BottomLeft), Layout.FishCard);
            fishIcon = ui.Icon(fishCard, Anchor.Left, new Vector2(Layout.FishIconInset, 0f), Layout.FishIcon, null);
            fishName = ui.Label(fishCard, Anchor.TopLeft, Layout.FishNamePos, Layout.FishName, UiStyle.LargeBodySize, TextAnchor.MiddleLeft, Palette.Ink);
            fishInfo = ui.Label(fishCard, Anchor.BottomLeft, Layout.FishInfoPos, Layout.FishInfo, UiStyle.SmallSize, TextAnchor.MiddleLeft, Palette.InkMuted);
            pips = new Image[game.balance.maxTier];
            for (int i = 0; i < pips.Length; i++)
            {
                pips[i] = ui.Image(fishCard, Anchor.BottomLeft, Layout.FirstPipPos + Vector2.right * (i * Layout.PipStep), Layout.Pip, skin.pipEmpty);
                pips[i].rectTransform.pivot = Anchor.Centre;
            }
        }

        void BuildTension(RectTransform root)
        {
            tensionPanel = ui.Panel(root, Anchor.Bottom, UiKit.Inset(Anchor.Bottom), Layout.TensionPanel);
            ui.Label(tensionPanel, Anchor.Top, new Vector2(0f, -Layout.TensionTitleDrop), Layout.TensionTitle,
                UiStyle.CaptionSize, TextAnchor.MiddleCenter, Palette.InkMuted, "LINE TENSION");
            var track = ui.Image(tensionPanel, Anchor.Centre, Vector2.zero, Layout.TensionTrack, skin.tensionTrack, true).rectTransform;
            var bed = ui.Image(track, Anchor.Centre, Vector2.zero, new Vector2(Layout.TrackWidth, Layout.TensionBedHeight), skin.tensionDanger).rectTransform;
            tensionZone = ui.Image(bed, Anchor.Left, Vector2.zero, new Vector2(0f, Layout.TensionBedHeight), skin.tensionSafe).rectTransform;
            tensionMarker = ui.Image(bed, Anchor.Left, Vector2.zero, Layout.TensionMarker, skin.tensionMarker).rectTransform;
            tensionMarker.pivot = Anchor.Centre;
            tensionState = ui.Label(tensionPanel, Anchor.Bottom, new Vector2(0f, Layout.TensionStateRaise), Layout.TensionState,
                UiStyle.SmallSize, TextAnchor.MiddleCenter, Palette.Ink);
        }

        void BuildPrompts(RectTransform root)
        {
            promptPanel = ui.Panel(root, Anchor.BottomRight, UiKit.Inset(Anchor.BottomRight), Layout.PromptPanel);
            prompts = new[] { PromptRowAt(promptPanel, Layout.PromptRowOffset), PromptRowAt(promptPanel, -Layout.PromptRowOffset) };
        }

        PromptRow PromptRowAt(RectTransform panel, float y)
        {
            var row = new PromptRow { root = ui.Group(panel, Anchor.Left, new Vector2(Layout.PromptRowInset, y), Layout.PromptRow, "Prompt") };
            row.icon = ui.Icon(row.root, Anchor.Left, Vector2.zero, Layout.PromptIcon, null);
            row.glyph = ui.Label(row.icon.rectTransform, Anchor.Centre, Vector2.zero, Layout.PromptGlyph, UiStyle.NoteSize, TextAnchor.MiddleCenter, Palette.OnWood);
            row.label = ui.Label(row.root, Anchor.Left, new Vector2(Layout.PromptTextInset, 0f), Layout.PromptText, UiStyle.BodySize, TextAnchor.MiddleLeft, Palette.Ink);
            return row;
        }

        void BuildToast(RectTransform root)
        {
            toast = ui.Plate(root, Anchor.Top, new Vector2(0f, -Layout.ToastDrop), Layout.Toast, false);
            toastText = ui.Label(toast, Anchor.Centre, Vector2.zero, Layout.ToastText, UiStyle.MediumSize, TextAnchor.MiddleCenter, Palette.Ink);
            toast.gameObject.SetActive(false);
        }

        void BuildSunset(RectTransform root)
        {
            sunsetCard = ui.Panel(root, Anchor.Centre, Vector2.zero, Layout.SunsetCard);
            var header = ui.Plate(sunsetCard, Anchor.Top, new Vector2(0f, Layout.SunsetHeaderRaise), Layout.SunsetHeader, true);
            ui.Label(header, Anchor.Centre, Vector2.zero, Layout.SunsetTitle, UiStyle.HeadingSize, TextAnchor.MiddleCenter, Palette.OnWood, "Sunset");
            sunsetText = ui.Label(sunsetCard, Anchor.Centre, new Vector2(0f, -Layout.SunsetTextDrop), Layout.SunsetText,
                UiStyle.LargeBodySize, TextAnchor.MiddleCenter, Palette.Ink);
        }

        // ----- update -----

        void ShowToast(string message)
        {
            toastText.text = message;
            toast.gameObject.SetActive(true);
            toastTimer = Layout.ToastSeconds;
        }

        void Update()
        {
            if (game.Day == null || suns == null) return;
            var state = game.State;
            UpdateTopBar();
            UpdatePower(state);
            UpdateDepth();
            UpdateReeling(state);
            UpdatePrompts(state);
            UpdateSunset(state);

            if (toastTimer > 0f && (toastTimer -= Time.deltaTime) <= 0f) toast.gameObject.SetActive(false);
        }

        void UpdateTopBar()
        {
            var day = game.Day;
            var progress = GameSession.Progress;
            if (day.CastsLeft != shownCasts)
            {
                shownCasts = day.CastsLeft;
                for (int i = 0; i < suns.Length; i++) suns[i].sprite = i < shownCasts ? skin.sunLeft : skin.sunSpent;
                castsText.text = $"{shownCasts}/{day.CastsPerDay}";
                castsText.color = shownCasts <= Layout.LowCastsWarning ? Palette.Danger : Palette.Ink;
            }
            if (progress.coins != shownCoins)
            {
                shownCoins = progress.coins;
                coinsText.text = UiKit.Coins(shownCoins);
            }
            if (day.Day != shownDay || progress.creel.Count != shownCreel)
            {
                shownDay = day.Day;
                shownCreel = progress.creel.Count;
                dayText.text = UiKit.DayAndCreel(shownDay, shownCreel);
            }
        }

        void UpdatePower(Phase state)
        {
            powerGauge.gameObject.SetActive(state == Phase.Aiming);
            powerFill.fillAmount = game.Power.Value;
            powerFill.sprite = game.Power.IsMax ? skin.powerFillMax : skin.powerFill;
        }

        void UpdateDepth()
        {
            float lakeDepth = GameBalance.WaterlineY - game.balance.lakeBottomY;
            depthLimit.anchoredPosition = Vector2.down * (Layout.GaugeLength * Mathf.Clamp01(game.balance.MaxHookDepth / lakeDepth));
            depthHook.anchoredPosition = Vector2.down * (Layout.GaugeLength * Mathf.Clamp01(game.HookDepth / lakeDepth));

            // The readout shows tenths of a metre; the labels are made once, so moving the hook allocates nothing.
            if (depthLabels == null) depthLabels = DepthLabels(lakeDepth);
            int tenths = Mathf.Clamp(Mathf.RoundToInt(game.HookDepth * Layout.DepthStepsPerMetre), 0, depthLabels.Length - 1);
            if (tenths == shownDepthTenths) return;
            shownDepthTenths = tenths;
            depthText.text = depthLabels[tenths];
        }

        static string[] DepthLabels(float lakeDepth)
        {
            var labels = new string[Mathf.CeilToInt(lakeDepth * Layout.DepthStepsPerMetre) + 1];
            for (int i = 0; i < labels.Length; i++) labels[i] = $"{i / Layout.DepthStepsPerMetre:0.0} m";
            return labels;
        }

        void UpdateReeling(Phase state)
        {
            bool reeling = state == Phase.Reeling && game.Hooked != null;
            tensionPanel.gameObject.SetActive(reeling);
            fishCard.gameObject.SetActive(reeling);
            if (!reeling) return;

            var t = game.Tension;
            tensionZone.anchoredPosition = Vector2.right * (Layout.TrackWidth * t.ZoneMin);
            tensionZone.sizeDelta = new Vector2(Layout.TrackWidth * (t.ZoneMax - t.ZoneMin), Layout.TensionBedHeight);
            tensionMarker.anchoredPosition = Vector2.right * (Layout.TrackWidth * t.Value);
            tensionState.text = t.State == TensionState.Slack ? "Slack — reel in"
                : t.State == TensionState.Danger ? "Snap risk!"
                : game.Fight.IsRunning ? "It's running — ease off" : "Steady — reel in";
            tensionState.color = t.State == TensionState.Steady ? Palette.Success : t.State == TensionState.Danger ? Palette.Danger : Palette.Ink;

            // The card only changes when a new fish is hooked or the hooked one merges or slips a tier.
            var hooked = game.Hooked;
            var fish = hooked.Data;
            if (hooked == shownFish && fish.Tier == shownFishData.Tier && fish.WeightKg == shownFishData.WeightKg
                && fish.Variant == shownFishData.Variant) return;
            shownFish = hooked;
            shownFishData = fish;
            fishName.text = hooked.Species.NameOf(fish);
            fishInfo.text = FishSpecies.TierAndWeight(fish);
            fishIcon.sprite = hooked.Species.sprite;
            fishIcon.color = Palette.Tint(fish.Variant);
            for (int i = 0; i < pips.Length; i++) pips[i].sprite = i < fish.Tier ? skin.pipFilled : skin.pipEmpty;
        }

        void UpdateSunset(Phase state)
        {
            bool sunset = state == Phase.Sunset;
            sunsetCard.gameObject.SetActive(sunset);
            // The card's numbers are fixed once the sun is down; only the button names follow the device.
            if (!sunset || (sunsetShown && input.LastDevice == sunsetDevice)) return;
            sunsetShown = true;
            sunsetDevice = input.LastDevice;
            var progress = GameSession.Progress;
            sunsetText.text = $"Day {game.Day.Day} is over\n{progress.caughtToday} caught today · {progress.creel.Count} in the creel\n\n" +
                              $"{input.Prompt("Click", "A", "Sell at the market")}\n{input.Prompt("Right click", "B", "Back to the map")}";
        }

        // ----- prompts -----

        void UpdatePrompts(Phase state)
        {
            Hints.TryGetValue(state, out var hints);
            promptPanel.gameObject.SetActive(hints != null);
            if (hints == null) return;
            bool pad = input.LastDevice == FishingInput.Device.Gamepad;
            for (int i = 0; i < prompts.Length; i++)
            {
                var hint = i < hints.Length ? hints[i] : NoHint;
                SetPrompt(prompts[i], pad ? hint.Pad : hint.Mouse, hint.Text);
            }
        }

        void SetPrompt(PromptRow row, Glyph glyph, string text)
        {
            row.root.gameObject.SetActive(glyph != Glyph.None);
            if (glyph == Glyph.None) return;
            var (sprite, letter, lightCap) = Look(glyph);
            row.label.text = text;
            row.icon.sprite = sprite;
            row.glyph.text = letter;
            row.glyph.color = lightCap ? Palette.Ink : Palette.OnWood;
        }

        /// <summary>Sprite, overlay letter and whether the cap is light (needs dark text) for each glyph.</summary>
        (Sprite sprite, string letter, bool lightCap) Look(Glyph glyph)
        {
            switch (glyph)
            {
                case Glyph.MouseLeft: return (skin.mouseLeft, "", false);
                case Glyph.MouseRight: return (skin.mouseRight, "", false);
                case Glyph.Mouse: return (skin.mouseIdle, "", false);
                case Glyph.PadA: return (skin.faceButton, "A", true);
                case Glyph.PadB: return (skin.faceButtonDark, "B", false);
                case Glyph.Stick: return (skin.faceButtonDark, "L", false);
                case Glyph.KeyEsc: return (skin.keycap, "Esc", true);
                default: return (skin.faceButtonDark, "≡", false);
            }
        }
    }
}
