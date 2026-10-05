using SailorsRest.Rules;
using UnityEngine;

namespace SailorsRest
{
    /// <summary>Every gameplay number a designer tunes for the Shore Pond, in one asset. World units are metres.</summary>
    [CreateAssetMenu(menuName = "Sailor's Rest/Game Balance", fileName = "GameBalance")]
    public class GameBalance : ScriptableObject
    {
        /// <summary>World y of the lake surface. Depths are measured down from here.</summary>
        public const float WaterlineY = 0f;

        [Header("Day")]
        public int castsPerDay = 8;

        [Header("Lake layout")]
        public float dockEdgeX = 4f;
        public float lakeEndX = 60f;
        public float lakeBottomY = -20f;
        [Tooltip("Casts and spawns stay this far from the lake's far end.")]
        public float lakeEndMargin = 1f;

        // Gear levels come from the save on the runtime copy made by WithGear, so they are not asset data.
        [System.NonSerialized] public int rodLevel = GearCatalog.StarterLevel;
        [System.NonSerialized] public int hookLevel = GearCatalog.StarterLevel;
        [System.NonSerialized] public int lineLevel = GearCatalog.StarterLevel;
        [System.NonSerialized] public int reelLevel = GearCatalog.StarterLevel;

        [Header("Gear effects (base + per level above the starter)")]
        public float castDistanceBase = 14f;
        public float castDistancePerRod = 6f;
        public float hookDepthBase = 6f;
        public float hookDepthPerLevel = 3f;
        public float tensionZoneMin = 0.3f;
        public float tensionZoneMax = 0.72f;
        public float tensionZonePerLine = 0.04f;
        [Tooltip("The safe zone never grows past these edges of the meter.")]
        public Vector2 tensionZoneLimits = new Vector2(0.05f, 0.95f);
        [Tooltip("Metres per second the hooked fish is pulled toward the dock while reeling.")]
        public float reelSpeedBase = 2.2f;
        public float reelSpeedPerLevel = 0.35f;

        [Header("Casting")]
        [Tooltip("How many times per second the power bar fills and empties while held.")]
        public float powerCyclesPerSecond = PowerBarModel.DefaultCyclesPerSecond;
        [Tooltip("The shortest cast lands this far past the dock edge.")]
        public float castMinDistance = 2f;
        public float castFlightSeconds = 0.8f;
        public float castArcHeight = 4f;

        [Header("Hook movement")]
        [Tooltip("Metres per second.")] public float hookVerticalSpeed = 3.5f;
        [Tooltip("Metres per second.")] public float hookHorizontalSpeed = 0.7f;
        public float hookHorizontalRange = 4f;
        [Tooltip("The hook never rises closer than this to the surface.")]
        public float hookMinDepth = 0.4f;
        public float biteRadius = 0.7f;

        [Header("Hooking ring")]
        [Tooltip("Accessibility: slows every hooking ring.")]
        [Range(0.5f, 1f)] public float qteSpeedScale = 1f;
        [Tooltip("Rare fish shrink the green arcs by this factor...")]
        public float rareZoneScale = 0.8f;
        [Tooltip("...and speed the needle up by this factor.")]
        public float rareSpeedScale = 1.15f;
        public float ringHeightAboveFish = 1.6f;
        public float ringResultSeconds = 0.35f;
        [Tooltip("A fish that got away ignores the hook for this long.")]
        public float escapedIgnoreSeconds = 3f;

        [Header("Reeling")]
        [Tooltip("How fast a resting hooked fish drifts away per tier while you are not reeling.")]
        public float fishDriftPerTier = 0.3f;
        [Tooltip("Metres per second the hooked fish can be steered up or down.")]
        public float fishVerticalSpeed = 2.5f;
        public float tensionRiseRate = TensionModel.DefaultRiseRate;
        public float tensionFallRate = TensionModel.DefaultFallRate;
        public float tensionGrace = TensionModel.DefaultGrace;
        public float mergeRadius = 0.75f;
        public float mergeWeightBonus = 0.1f;
        [Tooltip("The fish is landed once it is this close to the dock edge.")]
        public float landingDistance = 0.2f;
        [Tooltip("While reeling, the hook may come this far back under the dock.")]
        public float underDockReach = 1f;

        [Header("Fishing line (rope physics)")]
        [Tooltip("Rope points between the rod tip and the hook. More = smoother curve.")]
        public int lineSegments = 24;
        [Tooltip("Extra line on top of the straight distance when it is fully slack, as a fraction: 0.25 = 25% longer.")]
        public float lineSlack = 0.25f;
        [Tooltip("How taut the line is while it is not being reeled (0 = all slack, 1 = straight).")]
        [Range(0f, 1f)] public float lineRestTautness = 0.3f;
        [Tooltip("Metres per second squared pulling the line down in the air.")]
        public float lineAirGravity = 9.8f;
        [Tooltip("Metres per second squared pulling the line down under water: it sinks slowly.")]
        public float lineWaterGravity = 1.2f;
        [Tooltip("Share of its speed a line point keeps each frame in the air.")]
        [Range(0f, 1f)] public float lineAirDamping = 0.98f;
        [Tooltip("Share of its speed a line point keeps each frame under water: water drag.")]
        [Range(0f, 1f)] public float lineWaterDamping = 0.85f;
        [Tooltip("Longest physics step, in seconds, so a frame hitch doesn't fling the line.")]
        public float lineMaxStep = 1f / 30f;

        [Header("End of a cast")]
        [Tooltip("Metres per second the empty line comes back after a cast.")]
        public float reelBackSpeed = 25f;
        public float castOverSeconds = 0.5f;
        [Tooltip("Seconds of ignored input after sunset, so a held button does not skip the screen.")]
        public float sunsetInputDelay = 0.6f;

        [Header("Wild fish")]
        public int fishCount = 22;
        public int maxTier = 3;
        [Tooltip("Sprite scale per tier: index 0 = T1.")]
        public float[] tierScale = { 1f, 1.5f, 2.1f };
        [Tooltip("Metres per second, picked at random per fish.")]
        public Vector2 swimSpeed = new Vector2(0.4f, 0.9f);
        [Tooltip("Metres up and down.")]
        public float bobAmplitude = 0.25f;
        [Tooltip("Radians per second of the bob's sine wave.")]
        public float bobFrequency = 0.8f;
        [Tooltip("Fish spawn at least this far past the dock edge.")]
        public float spawnGapFromDock = 3f;
        [Tooltip("Fish never patrol closer than this to the dock edge.")]
        public float patrolGapFromDock = 2f;
        public Vector2 patrolHalfWidth = new Vector2(3f, 7f);

        [Header("Economy")]
        public float goldenPriceMultiplier = 3f;

        public FishSpecies[] species;

        public float MaxCastDistance => castDistanceBase + castDistancePerRod * AboveStarter(rodLevel);
        public float MaxHookDepth => hookDepthBase + hookDepthPerLevel * AboveStarter(hookLevel);
        public float ZoneMin => Mathf.Max(tensionZoneLimits.x, tensionZoneMin - tensionZonePerLine * AboveStarter(lineLevel));
        public float ZoneMax => Mathf.Min(tensionZoneLimits.y, tensionZoneMax + tensionZonePerLine * AboveStarter(lineLevel));
        public float ReelSpeed => reelSpeedBase + reelSpeedPerLevel * AboveStarter(reelLevel);
        public float CastOriginX => dockEdgeX + castMinDistance;
        public float FarthestX => lakeEndX - lakeEndMargin;
        public Pricing Pricing => new Pricing(PricePerKg, goldenPriceMultiplier);

        public float ScaleFor(int tier) => tierScale[Mathf.Clamp(tier - MergeRules.MinTier, 0, tierScale.Length - 1)];

        /// <summary>A runtime copy with the player's bought gear levels, so the asset on disk never changes.</summary>
        public GameBalance WithGear(PlayerProgress progress)
        {
            var copy = Instantiate(this);
            copy.name = name;
            copy.rodLevel = ClampLevel(progress.rodLevel);
            copy.hookLevel = ClampLevel(progress.hookLevel);
            copy.lineLevel = ClampLevel(progress.lineLevel);
            copy.reelLevel = ClampLevel(progress.reelLevel);
            return copy;
        }

        public FishSpecies Find(string id)
        {
            if (species == null) return null;
            foreach (var s in species) if (s != null && s.id == id) return s;
            return null;
        }

        float PricePerKg(string speciesId)
        {
            var species = Find(speciesId);
            return species != null ? species.pricePerKg : 0f;
        }

        static int AboveStarter(int level) => level - GearCatalog.StarterLevel;
        static int ClampLevel(int level) => Mathf.Clamp(level, GearCatalog.StarterLevel, GearCatalog.MaxLevel);
    }
}
