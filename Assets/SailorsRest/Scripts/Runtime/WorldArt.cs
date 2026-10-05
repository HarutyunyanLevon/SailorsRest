using SailorsRest.Rules;
using UnityEngine;

namespace SailorsRest
{
    /// <summary>
    /// SpriteCook world art (everything that is not HUD chrome) and the layout numbers that depend on it.
    /// Sprites are filled by "Sailor's Rest → Build All Scenes" from Art/SpriteCook/World. Offsets are in
    /// world units from the dock edge at the waterline, (dockEdgeX, 0).
    /// </summary>
    [CreateAssetMenu(menuName = "Sailor's Rest/World Art", fileName = "WorldArt")]
    public class WorldArt : ScriptableObject
    {
        [Header("Shore Pond")]
        [SpriteFile("sky_backdrop")] public Sprite sky;                 // pivot bottom-centre
        [SpriteFile("underwater_backdrop")] public Sprite underwater;   // pivot top-centre
        [SpriteFile("dock")] public Sprite dock;                        // pivot bottom-right
        [SpriteFile("fisherman")] public Sprite fisherman;              // pivot bottom-centre
        [SpriteFile("bobber")] public Sprite bobber;
        [SpriteFile("weeds")] public Sprite weeds;                      // pivot bottom-centre

        [Header("Backdrops")]
        [Range(0f, 1f)] public float skyParallax = 0.9f;
        [Range(0f, 1f)] public float underwaterParallax = 0.8f;
        [Tooltip("The sky art has a flat band under its treeline; this tucks it under the water.")]
        public float skyBottomY = -4.1f;
        public float underwaterTopY = -0.35f;

        [Header("Waterline")]
        public float surfaceBandHeight = 0.44f;
        [Range(0f, 1f)] public float surfaceBandAlpha = 0.85f;
        [Tooltip("How far the surface band runs past the dock and past the lake's end.")]
        public float surfaceBandOverhang = 16f;

        [Header("Dock, fisherman and rod")]
        public Vector2 dockOffset = new Vector2(-0.4f, 0f);
        public Vector2 fishermanOffset = new Vector2(-1.4f, -0.05f);
        [Tooltip("Where the fisherman's hands hold the rod.")]
        public Vector2 handsOffset = new Vector2(-0.6f, 1.4f);
        public Vector2 rodTipOffset = new Vector2(0.3f, 3.2f);
        public float rodThickness = 0.12f;

        [Header("Lake-bed weeds")]
        public int weedClumps = 9;
        [Tooltip("The first clump grows this far past the dock edge.")]
        public float firstWeedGap = 3f;
        public float weedSpacing = 6.4f;
        [Tooltip("Random shift applied to each clump so the row does not look ruled.")]
        public float weedJitter = 1f;
        [Tooltip("Clumps sit this far below the lake bed so their roots are hidden.")]
        public float weedSink = 0.4f;

        [Header("Market")]
        [SpriteFile("market_backdrop")] public Sprite marketBackdrop;
        [SpriteFile("fishmonger")] public Sprite fishmonger;

        [Header("Map")]
        [SpriteFile("map_backdrop")] public Sprite mapBackdrop;
        [SpriteFile("map_pond")] public Sprite mapPond;
        [SpriteFile("map_market")] public Sprite mapMarket;

        [Header("Idle animations (SpriteCook, from Art/SpriteCook/Anim; empty = the still sprite is used)")]
        [SpriteFile("fisherman_idle")] public Sprite[] fishermanIdle;
        [SpriteFile("dock_idle")] public Sprite[] dockIdle;
        [SpriteFile("weeds_idle")] public Sprite[] weedsIdle;
        [SpriteFile("fishmonger_idle")] public Sprite[] fishmongerIdle;
        [SpriteFile("map_pond_idle")] public Sprite[] mapPondIdle;
        [SpriteFile("map_market_idle")] public Sprite[] mapMarketIdle;
        public float idleFps = 8f;

        [Header("Code idles (free, no art)")]
        [Tooltip("Bobber swing while it hangs or drifts, degrees.")]
        public float bobberSwayDegrees = 7f;
        public int skyMotes = 18;
        public int bubbles = 14;
        public int surfaceSparkles = 16;
        public int lightRays = 7;
        [Tooltip("How much the waterline band's alpha dips as it shimmers.")]
        [Range(0f, 0.5f)] public float surfaceShimmer = 0.12f;
        public int marketMotes = 22;
        public int mapSparkles = 10;
        public int gearIconTwinkles = 2;

        [Header("Gear icons")]
        [SpriteFile("icon_rod")] public Sprite iconRod;
        [SpriteFile("icon_hook")] public Sprite iconHook;
        [SpriteFile("icon_line")] public Sprite iconLine;
        [SpriteFile("icon_reel")] public Sprite iconReel;

        public Sprite GearIcon(GearSlot slot)
        {
            switch (slot)
            {
                case GearSlot.Rod: return iconRod;
                case GearSlot.Hook: return iconHook;
                case GearSlot.Line: return iconLine;
                default: return iconReel;
            }
        }
    }
}
