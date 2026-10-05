using System;

namespace SailorsRest.Rules
{
    /// <summary>How the market values fish: coins per kg by species id, and the bonus for rare variants.</summary>
    public readonly struct Pricing
    {
        readonly Func<string, float> pricePerKg;
        readonly float rareMultiplier;

        public Pricing(Func<string, float> pricePerKg, float rareMultiplier)
        {
            this.pricePerKg = pricePerKg;
            this.rareMultiplier = rareMultiplier;
        }

        public int Of(FishData fish) => MergeRules.Price(fish, pricePerKg(fish.SpeciesId), rareMultiplier);
    }

    /// <summary>Selling the creel at the fish market.</summary>
    public static class MarketRules
    {
        public static int CreelValue(PlayerProgress p, Pricing pricing)
        {
            int total = 0;
            foreach (var fish in p.creel) total += pricing.Of(fish);
            return total;
        }

        /// <summary>Sells one fish by index. Returns the coins earned, or 0 if the index is bad.</summary>
        public static int SellAt(PlayerProgress p, int index, Pricing pricing)
        {
            if (index < 0 || index >= p.creel.Count) return 0;
            int value = pricing.Of(p.creel[index]);
            p.creel.RemoveAt(index);
            p.coins += value;
            return value;
        }

        public static int SellAll(PlayerProgress p, Pricing pricing)
        {
            int total = CreelValue(p, pricing);
            p.creel.Clear();
            p.coins += total;
            return total;
        }
    }
}
