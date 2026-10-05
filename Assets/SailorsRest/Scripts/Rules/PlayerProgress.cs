using System.Collections.Generic;

namespace SailorsRest.Rules
{
    /// <summary>
    /// Everything that survives between scenes and sessions: coins, the day, casts left today,
    /// gear levels and the creel (fish caught but not sold yet). Plain fields so JsonUtility can save it.
    /// </summary>
    [System.Serializable]
    public class PlayerProgress
    {
        /// <summary>castsLeft value for a day that has not been fished yet.</summary>
        public const int FreshDay = DaySession.FullDay;

        public int coins;
        public int day = DaySession.FirstDay;
        public int castsLeft = FreshDay;
        public int rodLevel = GearCatalog.StarterLevel;
        public int hookLevel = GearCatalog.StarterLevel;
        public int lineLevel = GearCatalog.StarterLevel;
        public int reelLevel = GearCatalog.StarterLevel;
        public List<FishData> creel = new List<FishData>();
        public int bestTierEver;
        public int fishCaughtEver;
        public int caughtToday;

        public int GetLevel(GearSlot slot)
        {
            switch (slot)
            {
                case GearSlot.Rod: return rodLevel;
                case GearSlot.Hook: return hookLevel;
                case GearSlot.Line: return lineLevel;
                default: return reelLevel;
            }
        }

        public void SetLevel(GearSlot slot, int level)
        {
            switch (slot)
            {
                case GearSlot.Rod: rodLevel = level; break;
                case GearSlot.Hook: hookLevel = level; break;
                case GearSlot.Line: lineLevel = level; break;
                default: reelLevel = level; break;
            }
        }

        /// <summary>Casts for today, starting a fresh allowance if the day has not been touched.</summary>
        public int CastsToday(int castsPerDay)
        {
            return castsLeft = DaySession.Normalize(castsLeft, castsPerDay);
        }

        public void AddToCreel(FishData fish)
        {
            creel.Add(fish);
            fishCaughtEver++;
            caughtToday++;
            if (fish.Tier > bestTierEver) bestTierEver = fish.Tier;
        }

        /// <summary>Sleep: the day counter moves on and the cast allowance refills next time you fish.</summary>
        public void NextDay()
        {
            day++;
            castsLeft = FreshDay;
            caughtToday = 0;
        }
    }
}
