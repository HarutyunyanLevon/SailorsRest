using SailorsRest.Rules;
using UnityEngine;

namespace SailorsRest
{
    /// <summary>
    /// How a hooked fish fights: it alternates runs (swims away, pulls the line tight) with rests (tired, can be
    /// reeled in). Per-tier arrays: index 0 = T1.
    /// </summary>
    [System.Serializable]
    public class FightStyle
    {
        [Tooltip("Seconds each run lasts, picked at random in this range. A fresh bite starts with a run.")]
        public Vector2 runSeconds = new Vector2(1.2f, 2.2f);
        [Tooltip("Seconds it rests between runs. Only a resting fish comes in when you reel on green.")]
        public Vector2 restSeconds = new Vector2(1.5f, 3f);
        [Tooltip("Metres per second it swims away from the dock during a run, per tier.")]
        public float[] runSpeed = { 0.9f, 1.2f, 1.5f };
        [Tooltip("Line tension per second it adds during a run, per tier. Keep it below the tension fall rate so easing off still helps.")]
        public float[] runTension = { 0.3f, 0.35f, 0.4f };
        [Tooltip("Metres per second it pulls down during a run; negative pulls toward the surface.")]
        public float dive;
        [Tooltip("Peak up-and-down speed of its zigzag during a run, metres per second. 0 = swims straight.")]
        public float weave;
        [Tooltip("Radians per second of the zigzag: low = slow rolls, high = head shakes.")]
        public float weaveFrequency = 6f;
    }

    /// <summary>One kind of fish. Per-tier arrays are indexed from T1.</summary>
    [CreateAssetMenu(menuName = "Sailor's Rest/Fish Species", fileName = "Species")]
    public class FishSpecies : ScriptableObject
    {
        public string id = "carp";
        public string displayName = "Carp";
        [Tooltip("Side-view sprite facing right.")]
        public Sprite sprite;
        [Tooltip("SpriteCook swim loop, same facing as the sprite. Filled by the scene builder; empty = the still sprite.")]
        public Sprite[] swimFrames;
        public float swimFps = 8f;

        [Header("Where it lives (metres below the surface)")]
        public float minDepth = 1f;
        public float maxDepth = 6f;
        [Tooltip("Relative spawn chance against other species at this spot.")]
        public float spawnWeight = 1f;

        [Header("Per tier: index 0 = T1, 1 = T2, 2 = T3")]
        [Tooltip("Relative chance a wild fish spawns at each tier. Keep the top tier at 0: it only exists by merging.")]
        public float[] wildTierWeights = { 0.75f, 0.25f, 0f };
        public Vector2[] weightRangeKg = { new Vector2(0.3f, 0.7f), new Vector2(0.9f, 1.6f), new Vector2(2.4f, 4f) };
        [Tooltip("Width of each green arc on the hooking ring, in degrees.")]
        public float[] qteZoneDegrees = { 70f, 45f, 30f };
        [Tooltip("Number of green arcs on the hooking ring.")]
        public int[] qteArcCount = { 2, 1, 1 };
        [Tooltip("Ring needle speed, in degrees per second.")]
        public float[] qteSpeed = { 200f, 260f, 320f };
        [Tooltip("Tension jolt when the fish starts a run during the reel-in.")]
        public float[] surgeStrength = { 0.12f, 0.2f, 0.28f };

        [Header("Fight while reeling")]
        public FightStyle fight = new FightStyle();

        [Header("Economy")]
        public float pricePerKg = 30f;
        [Range(0f, 1f)] public float goldenChance = 0.05f;

        const string GoldenPrefix = "Golden ";

        /// <summary>"Golden Perch" or "Perch".</summary>
        public string NameOf(FishData fish) => fish.Variant == Variant.Golden ? GoldenPrefix + displayName : displayName;

        /// <summary>"T2 · 1.25 kg", the same everywhere a fish is listed.</summary>
        public static string TierAndWeight(FishData fish) => $"T{fish.Tier} · {fish.WeightKg:0.00} kg";

        public float ZoneFor(int tier) => At(qteZoneDegrees, tier);
        public int ArcsFor(int tier) => At(qteArcCount, tier);
        public float SpeedFor(int tier) => At(qteSpeed, tier);
        public float SurgeFor(int tier) => At(surgeStrength, tier);
        public float RunSpeedFor(int tier) => At(fight.runSpeed, tier);
        public float RunTensionFor(int tier) => At(fight.runTension, tier);

        public float RandomWeight(int tier)
        {
            var range = At(weightRangeKg, tier);
            return Random.Range(range.x, range.y);
        }

        static T At<T>(T[] perTier, int tier) => perTier[Mathf.Clamp(tier - MergeRules.MinTier, 0, perTier.Length - 1)];
    }
}
