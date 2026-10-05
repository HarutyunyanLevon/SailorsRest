using System;

namespace SailorsRest.Rules
{
    /// <summary>Merging, tier drops and fish prices.</summary>
    public static class MergeRules
    {
        public const int MinTier = 1;
        /// <summary>A tension failure keeps this share of the fish's weight.</summary>
        public const float TierDropWeightKept = 0.5f;
        /// <summary>Each tier above the first adds this much to the price multiplier.</summary>
        public const float PricePerTierStep = 0.5f;
        public const int MinPrice = 1;

        /// <summary>Same species and same tier merge, as long as the result stays within maxTier.</summary>
        public static bool CanMerge(FishData hooked, FishData wild, int maxTier) =>
            hooked.SpeciesId == wild.SpeciesId && hooked.Tier == wild.Tier && hooked.Tier < maxTier;

        /// <summary>Tier goes up by one, weights add up plus a bonus, and a rare variant on either fish carries over.</summary>
        public static FishData Merge(FishData hooked, FishData wild, float weightBonus)
        {
            var variant = hooked.IsRare ? hooked.Variant : wild.Variant;
            float weight = (hooked.WeightKg + wild.WeightKg) * (1f + weightBonus);
            return new FishData(hooked.SpeciesId, hooked.Tier + 1, weight, variant);
        }

        /// <summary>Drops the fish one tier and cuts its weight. Returns false when a lowest-tier fish escapes instead.</summary>
        public static bool TryDropTier(FishData fish, out FishData dropped)
        {
            dropped = fish;
            if (fish.Tier <= MinTier) return false;
            dropped = new FishData(fish.SpeciesId, fish.Tier - 1, fish.WeightKg * TierDropWeightKept, fish.Variant);
            return true;
        }

        public static int Price(FishData fish, float pricePerKg, float rareMultiplier)
        {
            float tierMultiplier = 1f + PricePerTierStep * (fish.Tier - MinTier);
            float rare = fish.IsRare ? rareMultiplier : 1f;
            int value = (int)Math.Round(fish.WeightKg * pricePerKg * tierMultiplier * rare);
            return Math.Max(MinPrice, value);
        }
    }
}
