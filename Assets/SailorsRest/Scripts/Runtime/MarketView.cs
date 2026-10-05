using SailorsRest.Rules;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SailorsRest
{
    /// <summary>
    /// Harbour Market: sell the creel on the left, upgrade tackle on the right, the fishmonger in the middle.
    /// Works with mouse, keyboard and gamepad; B / Esc goes back to the map.
    /// </summary>
    public class MarketView : MonoBehaviour
    {
        public GameBalance balance;
        public UiSkin skin;
        public WorldArt art;

        static class Layout
        {
            public const int VisibleFish = 6;

            public static readonly Vector2 Fishmonger = new Vector2(380, 380);
            public const float FishmongerRaise = 120f;
            public static readonly Vector2 Speech = new Vector2(440, 150);      // fits between the two columns
            public static readonly Vector2 SpeechText = new Vector2(380, 110);
            public const float SpeechRaise = 520f;

            public const float ColumnDrop = 20f;          // the side columns sit a little below the middle
            public const float HeaderDrop = 56f;
            public const float ListDrop = 96f;
            public static readonly Vector2 Header = new Vector2(560, 44);

            public static readonly Vector2 CreelPanel = new Vector2(660, 800);
            public static readonly Vector2 CreelList = new Vector2(580, 560);
            public static readonly Vector2 FishRow = new Vector2(580, 84);
            public const float FishRowStep = 90f;
            public static readonly Vector2 FishIcon = new Vector2(96, 60);
            public const float RowIconInset = 18f;
            public const float RowTextInset = 128f;
            public static readonly Vector2 FishName = new Vector2(240, 34);
            public const float FishNameRaise = 12f;
            public static readonly Vector2 FishWeight = new Vector2(240, 28);
            public const float FishWeightDrop = 18f;
            public static readonly Vector2 SellButton = new Vector2(170, 64);
            public const float RowButtonInset = 14f;
            public static readonly Vector2 EmptyNote = new Vector2(520, 200);
            public static readonly Vector2 EmptyNoteAnchor = new Vector2(0.5f, 0.6f);
            public static readonly Vector2 MoreNote = new Vector2(520, 34);
            public const float MoreNoteGap = 24f;
            public static readonly Vector2 Total = new Vector2(300, 44);
            public static readonly Vector2 TotalPos = new Vector2(48, 82);
            public static readonly Vector2 SellAllButton = new Vector2(250, 84);
            public static readonly Vector2 SellAllPos = new Vector2(-40, 44);

            public static readonly Vector2 GearPanel = new Vector2(700, 800);
            public static readonly Vector2 GearList = new Vector2(620, 660);
            public static readonly Vector2 GearRow = new Vector2(620, 148);
            public const float GearRowStep = 160f;
            public static readonly Vector2 GearIcon = new Vector2(100, 100);
            public const float GearIconInset = 22f;
            public const float GearTextInset = 140f;
            public static readonly Vector2 GearName = new Vector2(280, 36);
            public const float GearNameDrop = 36f;
            public static readonly Vector2 GearEffect = new Vector2(280, 28);
            public const float GearEffectDrop = 4f;
            public static readonly Vector2 Pip = new Vector2(28, 28);
            public const float PipStep = 34f;
            public const float PipRaise = 22f;
            public static readonly Vector2 BuyButton = new Vector2(190, 76);
            public const float BuyButtonInset = 16f;

            public static readonly Vector2 BackButton = new Vector2(380, 88);
        }

        UiKit ui;
        RectTransform creelList, gearList;
        Text coinsText, creelHeader, totalText, speech;
        Button sellAll, back;
        InputAction backAction;

        // After a sale or purchase, keyboard/gamepad focus stays on the row just used, or falls back to Back,
        // so pressing A again can never buy or sell something on a different row by surprise.
        Selectable focus;
        int focusFishRow;
        GearSlot? focusGearRow;

        static PlayerProgress Progress => GameSession.Progress;

        void Awake()
        {
            backAction = new InputAction("Back", InputActionType.Button);
            backAction.AddBinding(FishingInput.EscapeKey);
            backAction.AddBinding(FishingInput.PadBack);
            backAction.Enable();
        }

        void OnDestroy()
        {
            backAction?.Dispose();
            GameSession.Changed -= Rebuild;
        }

        void Start()
        {
            UiKit.EnsureEventSystem();
            ui = new UiKit(skin);
            var root = UiKit.Canvas(transform, "Market Canvas");
            var backdrop = ui.Backdrop(root, art.marketBackdrop);
            UiIdle.Motes(backdrop.rectTransform, art.marketMotes, new Color(1f, 0.88f, 0.6f));   // dust in the golden-hour light
            ui.ScreenTitle(root, "Harbour Market");
            coinsText = ui.CoinChip(root);

            var fishmonger = ui.Icon(root, Anchor.Bottom, Vector2.up * Layout.FishmongerRaise, Layout.Fishmonger, art.fishmonger);
            if (SpriteFlipbook.Play(fishmonger, art.fishmongerIdle, art.idleFps) == null)
                IdleMotion.Breathe(fishmonger.gameObject, 0.015f, 3f);   // no SpriteCook loop yet: breathe in code
            var bubble = ui.Panel(root, Anchor.Bottom, Vector2.up * Layout.SpeechRaise, Layout.Speech);
            speech = ui.Label(bubble, Anchor.Centre, Vector2.zero, Layout.SpeechText, UiStyle.SmallSize, TextAnchor.MiddleCenter, Palette.Ink,
                Progress.creel.Count > 0 ? "Fresh from Shore Pond? Let's see what you've got!" : "Nothing to sell? The tackle's always worth a look.");

            var creelPanel = Column(root, Anchor.Left, Layout.CreelPanel, out creelHeader, out creelList, Layout.CreelList);
            totalText = ui.Label(creelPanel, Anchor.BottomLeft, Layout.TotalPos, Layout.Total, UiStyle.MediumSize, TextAnchor.MiddleLeft, Palette.Ink);
            sellAll = ui.Button(creelPanel, Anchor.BottomRight, Layout.SellAllPos, Layout.SellAllButton, "Sell all", SellAll, UiStyle.LargeBodySize);

            Column(root, Anchor.Right, Layout.GearPanel, out var gearHeader, out gearList, Layout.GearList);
            gearHeader.text = "Tackle shop";

            back = ui.Button(root, Anchor.Bottom, UiKit.Inset(Anchor.Bottom), Layout.BackButton, "Back to map",
                () => GameSession.Go(GameSession.MapScene), UiStyle.LargeBodySize);

            GameSession.Changed += Rebuild;
            Rebuild();
        }

        /// <summary>A tall paper column at the screen's side with a header and an empty list area.</summary>
        RectTransform Column(RectTransform root, Vector2 side, Vector2 size, out Text header, out RectTransform list, Vector2 listSize)
        {
            var panel = ui.Panel(root, side, UiKit.Inset(side) + Vector2.down * Layout.ColumnDrop, size);
            header = ui.Label(panel, Anchor.Top, Vector2.down * Layout.HeaderDrop, Layout.Header, UiStyle.HeadingSize, TextAnchor.MiddleCenter, Palette.Ink);
            list = ui.Group(panel, Anchor.Top, Vector2.down * Layout.ListDrop, listSize, "List");
            return panel;
        }

        void Update()
        {
            if (backAction.WasPressedThisFrame()) GameSession.Go(GameSession.MapScene);
            UiKit.FocusIfLost(focus);
        }

        void Rebuild()
        {
            focus = back;
            coinsText.text = UiKit.Coins(Progress.coins);
            BuildCreel();
            BuildGear();
        }

        // ----- creel -----

        void BuildCreel()
        {
            UiKit.Clear(creelList);
            var creel = Progress.creel;
            creelHeader.text = $"Your creel ({creel.Count})";
            totalText.text = creel.Count > 0 ? $"Worth {MarketRules.CreelValue(Progress, balance.Pricing):N0} coins" : "";
            sellAll.interactable = creel.Count > 0;

            if (creel.Count == 0)
            {
                ui.Label(creelList, Layout.EmptyNoteAnchor, Vector2.zero, Layout.EmptyNote, UiStyle.MediumSize, TextAnchor.MiddleCenter, Palette.InkMuted,
                    "Your creel is empty.\nLand some fish at Shore Pond, then come back to sell them.");
                return;
            }

            int shown = Mathf.Min(Layout.VisibleFish, creel.Count);
            for (int i = 0; i < shown; i++) FishRow(i, creel[i], shown);
            if (creel.Count > shown)
                ui.Label(creelList, Anchor.Top, Vector2.down * (shown * Layout.FishRowStep + Layout.MoreNoteGap), Layout.MoreNote,
                    UiStyle.SmallSize, TextAnchor.MiddleCenter, Palette.InkMuted, $"+ {creel.Count - shown} more in the creel");
        }

        void FishRow(int index, FishData fish, int shown)
        {
            var species = balance.Find(fish.SpeciesId);
            var row = ui.Slot(creelList, Anchor.Top, Vector2.down * (index * Layout.FishRowStep), Layout.FishRow);
            ui.Icon(row, Anchor.Left, new Vector2(Layout.RowIconInset, 0f), Layout.FishIcon, species.sprite).color = Palette.Tint(fish.Variant);
            ui.Label(row, Anchor.Left, new Vector2(Layout.RowTextInset, Layout.FishNameRaise), Layout.FishName,
                UiStyle.BodySize, TextAnchor.MiddleLeft, Palette.OnWood, species.NameOf(fish));
            ui.Label(row, Anchor.Left, new Vector2(Layout.RowTextInset, -Layout.FishWeightDrop), Layout.FishWeight,
                UiStyle.CaptionSize, TextAnchor.MiddleLeft, Palette.PaperShade, FishSpecies.TierAndWeight(fish));
            var sell = ui.Button(row, Anchor.Right, Vector2.left * Layout.RowButtonInset, Layout.SellButton,
                $"Sell {balance.Pricing.Of(fish)}", () => SellOne(index), UiStyle.SmallSize);
            if (focusGearRow == null && index == Mathf.Min(focusFishRow, shown - 1)) focus = sell;
        }

        void SellOne(int index)
        {
            if (index < 0 || index >= Progress.creel.Count) return;
            var fish = Progress.creel[index];
            string name = balance.Find(fish.SpeciesId).displayName;
            int coins = MarketRules.SellAt(Progress, index, balance.Pricing);
            focusFishRow = index;
            focusGearRow = null;
            speech.text = fish.Tier >= balance.maxTier
                ? $"A tier {fish.Tier} {name}! That's a beauty. {coins} coins."
                : $"{coins} coins for the {name}. Pleasure doing business.";
            Commit();
        }

        void SellAll()
        {
            int count = Progress.creel.Count;
            int coins = MarketRules.SellAll(Progress, balance.Pricing);
            focusGearRow = null;
            if (count > 0) speech.text = $"{count} fish for {coins:N0} coins. Spend it on better tackle!";
            Commit();
        }

        // ----- gear -----

        void BuildGear()
        {
            UiKit.Clear(gearList);
            for (int i = 0; i < GearCatalog.All.Length; i++) GearRow(i, GearCatalog.All[i]);
        }

        void GearRow(int index, GearSlot slot)
        {
            int level = Progress.GetLevel(slot);
            int price = GearCatalog.NextPrice(slot, level);
            var row = ui.Slot(gearList, Anchor.Top, Vector2.down * (index * Layout.GearRowStep), Layout.GearRow);
            var icon = ui.Icon(row, Anchor.Left, new Vector2(Layout.GearIconInset, 0f), Layout.GearIcon, art.GearIcon(slot));
            IdleMotion.Bob(icon.gameObject, new Vector2(0f, UiIdle.ArtPixel), 2.6f, UiIdle.ArtPixel);
            UiIdle.Twinkle(icon.rectTransform, art.gearIconTwinkles, Palette.OnWood);
            ui.Label(row, Anchor.TopLeft, new Vector2(Layout.GearTextInset, -Layout.GearNameDrop), Layout.GearName,
                UiStyle.MediumSize, TextAnchor.MiddleLeft, Palette.OnWood, GearCatalog.ItemName(slot, level));
            ui.Label(row, Anchor.Left, new Vector2(Layout.GearTextInset, -Layout.GearEffectDrop), Layout.GearEffect,
                UiStyle.CaptionSize, TextAnchor.MiddleLeft, Palette.PaperShade, $"{GearCatalog.Title(slot)} · {GearCatalog.Effect(slot)}");
            for (int p = 0; p < GearCatalog.MaxLevel; p++)
                ui.Icon(row, Anchor.BottomLeft, new Vector2(Layout.GearTextInset + p * Layout.PipStep, Layout.PipRaise), Layout.Pip,
                    p < level ? skin.pipFilled : skin.pipEmpty);

            var buy = ui.Button(row, Anchor.Right, Vector2.left * Layout.BuyButtonInset, Layout.BuyButton,
                price == GearCatalog.NoPrice ? "Maxed" : $"Upgrade {price}", () => Buy(slot), UiStyle.SmallSize);
            buy.interactable = GearCatalog.CanBuy(Progress, slot);
            if (buy.interactable && focusGearRow == slot) focus = buy;
        }

        void Buy(GearSlot slot)
        {
            if (!GearCatalog.TryBuy(Progress, slot)) return;
            focusGearRow = slot;
            speech.text = $"The {GearCatalog.ItemName(slot, Progress.GetLevel(slot))}. {GearCatalog.Effect(slot)}, you'll see.";
            Commit();
        }

        static void Commit()
        {
            GameSession.Save();
            GameSession.NotifyChanged();
        }
    }
}
