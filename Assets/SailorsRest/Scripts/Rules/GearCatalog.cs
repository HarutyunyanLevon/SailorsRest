namespace SailorsRest.Rules
{
    public enum GearSlot { Rod = 0, Hook = 1, Line = 2, Reel = 3 }

    /// <summary>Gear upgrade names and prices. Levels run StarterLevel..MaxLevel.</summary>
    public static class GearCatalog
    {
        public const int StarterLevel = 1;
        public const int MaxLevel = 5;
        public const int NoPrice = -1;

        public static readonly GearSlot[] All = { GearSlot.Rod, GearSlot.Hook, GearSlot.Line, GearSlot.Reel };

        sealed class Gear
        {
            public readonly string Title, Effect;
            public readonly string[] Names;   // one per level
            public readonly int[] Prices;     // price to leave each level, [0] = starter → 2

            public Gear(string title, string effect, string[] names, int[] prices)
            {
                Title = title; Effect = effect; Names = names; Prices = prices;
            }
        }

        // Indexed by GearSlot.
        static readonly Gear[] Table =
        {
            new Gear("Rod", "Casts further out",
                new[] { "Willow Twig", "Bamboo Rod", "Ash Rod", "Carbon Rod", "Sailor's Pride" },
                new[] { 60, 160, 360, 720 }),
            new Gear("Hook", "Sinks deeper",
                new[] { "Bent Pin", "Iron Hook", "Barbed Hook", "Weighted Hook", "Deep Sinker" },
                new[] { 50, 140, 320, 650 }),
            new Gear("Line", "Wider safe tension",
                new[] { "Cotton Thread", "Hemp Line", "Mono Line", "Braided Line", "Silk Leader" },
                new[] { 40, 120, 280, 560 }),
            new Gear("Reel", "Reels in faster",
                new[] { "Hand Spool", "Wooden Reel", "Brass Reel", "Geared Reel", "Captain's Reel" },
                new[] { 45, 130, 300, 600 }),
        };

        public static string Title(GearSlot slot) => Table[(int)slot].Title;
        public static string Effect(GearSlot slot) => Table[(int)slot].Effect;
        public static string ItemName(GearSlot slot, int level) => Table[(int)slot].Names[Index(level)];
        public static bool IsMaxed(int level) => level >= MaxLevel;

        /// <summary>Cost of the next level, or <see cref="NoPrice"/> when the slot is maxed out.</summary>
        public static int NextPrice(GearSlot slot, int level) =>
            IsMaxed(level) ? NoPrice : Table[(int)slot].Prices[Index(level)];

        public static bool CanBuy(PlayerProgress p, GearSlot slot)
        {
            int price = NextPrice(slot, p.GetLevel(slot));
            return price != NoPrice && p.coins >= price;
        }

        public static bool TryBuy(PlayerProgress p, GearSlot slot)
        {
            if (!CanBuy(p, slot)) return false;
            int level = p.GetLevel(slot);
            p.coins -= NextPrice(slot, level);
            p.SetLevel(slot, level + 1);
            return true;
        }

        static int Index(int level) =>
            (level < StarterLevel ? StarterLevel : level > MaxLevel ? MaxLevel : level) - StarterLevel;
    }
}
