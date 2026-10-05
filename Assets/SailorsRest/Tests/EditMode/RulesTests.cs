using NUnit.Framework;
using SailorsRest.Rules;

namespace SailorsRest.Tests
{
    public class MergeRulesTests
    {
        [Test]
        public void SameSpeciesSameTier_Merges()
        {
            var a = new FishData("carp", 1, 1f);
            var b = new FishData("carp", 1, 2f);
            Assert.IsTrue(MergeRules.CanMerge(a, b, 3));
            var m = MergeRules.Merge(a, b, 0.1f);
            Assert.AreEqual(2, m.Tier);
            Assert.AreEqual(3.3f, m.WeightKg, 0.001f);
        }

        [Test]
        public void DifferentTierOrSpecies_DoesNotMerge()
        {
            Assert.IsFalse(MergeRules.CanMerge(new FishData("carp", 1, 1f), new FishData("carp", 2, 1f), 3));
            Assert.IsFalse(MergeRules.CanMerge(new FishData("carp", 1, 1f), new FishData("perch", 1, 1f), 3));
        }

        [Test]
        public void MaxTier_DoesNotMerge()
        {
            Assert.IsFalse(MergeRules.CanMerge(new FishData("carp", 3, 1f), new FishData("carp", 3, 1f), 3));
        }

        [Test]
        public void GoldenVariant_CarriesOver()
        {
            var m = MergeRules.Merge(new FishData("carp", 1, 1f), new FishData("carp", 1, 1f, Variant.Golden), 0f);
            Assert.AreEqual(Variant.Golden, m.Variant);
        }

        [Test]
        public void TierDrop_LowersTier_AndTierOneEscapes()
        {
            Assert.IsTrue(MergeRules.TryDropTier(new FishData("carp", 2, 4f), out var dropped));
            Assert.AreEqual(1, dropped.Tier);
            Assert.IsFalse(MergeRules.TryDropTier(new FishData("carp", 1, 1f), out _));
        }
    }

    public class TensionModelTests
    {
        [Test]
        public void StayingInZone_NeverFails()
        {
            var t = new TensionModel(0.3f, 0.7f, 0.5f) { RiseRate = 0f, FallRate = 0f };
            for (int i = 0; i < 100; i++) Assert.IsFalse(t.Tick(0.1f, true));
            Assert.AreEqual(TensionState.Steady, t.State);
        }

        [Test]
        public void HoldingTooLong_FailsAfterGrace()
        {
            var t = new TensionModel(0.3f, 0.7f, 0.5f) { RiseRate = 1f, Grace = 0.5f };
            bool failed = false;
            for (int i = 0; i < 20 && !failed; i++) failed = t.Tick(0.1f, true);
            Assert.IsTrue(failed);
        }

        [Test]
        public void Surge_PushesIntoDanger()
        {
            var t = new TensionModel(0.3f, 0.7f, 0.6f);
            t.AddSurge(0.3f);
            Assert.AreEqual(TensionState.Danger, t.State);
        }

        [Test]
        public void FishPull_RaisesTension_EvenWhenEasingOff()
        {
            var t = new TensionModel(0.3f, 0.7f, 0.5f) { FallRate = 0.5f };
            t.Tick(0.1f, false, 1f);
            Assert.Greater(t.Value, 0.5f);
        }
    }

    public class FishFightTests
    {
        [Test]
        public void FreshBite_StartsWithARun()
        {
            var fight = new FishFight(new System.Random(1), 1f, 2f, 3f, 4f);
            Assert.IsTrue(fight.IsRunning);
            Assert.That(fight.TimeLeft, Is.InRange(1f, 2f));
        }

        [Test]
        public void RunsAndRests_Alternate()
        {
            var fight = new FishFight(new System.Random(1), 1f, 1f, 2f, 2f);
            Assert.IsFalse(fight.Tick(0.5f));
            Assert.IsTrue(fight.IsRunning);
            Assert.IsTrue(fight.Tick(0.5f));
            Assert.AreEqual(FightPhase.Resting, fight.Phase);
            Assert.AreEqual(2f, fight.TimeLeft, 1e-4f);
            Assert.IsTrue(fight.Tick(2f));
            Assert.IsTrue(fight.IsRunning);
        }
    }

    public class QteModelTests
    {
        [Test]
        public void ArcContains_WrapsAround360()
        {
            var arc = new QteModel.Arc(350f, 20f);
            Assert.IsTrue(arc.Contains(355f));
            Assert.IsTrue(arc.Contains(5f));
            Assert.IsFalse(arc.Contains(20f));
        }

        [Test]
        public void CreatedZones_DoNotStartUnderTheArrow()
        {
            var q = QteModel.Create(new System.Random(1), 40f, 2, 180f);
            Assert.IsFalse(q.IsOnGreen());
        }
    }

    public class DaySessionTests
    {
        [Test]
        public void CastsRunOut_AtSunset()
        {
            var d = new DaySession(2);
            d.UseCast();
            Assert.IsFalse(d.IsSunset);
            d.UseCast();
            Assert.IsTrue(d.IsSunset);
        }
    }

    public class GearAndMarketTests
    {
        static readonly Pricing Prices = new Pricing(id => id == "carp" ? 30f : 10f, 3f);

        [Test]
        public void BuyingGear_SpendsCoins_AndRaisesLevel()
        {
            var p = new PlayerProgress { coins = 100 };
            int price = GearCatalog.NextPrice(GearSlot.Rod, 1);
            Assert.IsTrue(GearCatalog.TryBuy(p, GearSlot.Rod));
            Assert.AreEqual(2, p.rodLevel);
            Assert.AreEqual(100 - price, p.coins);
        }

        [Test]
        public void CannotBuy_WhenPoor_OrMaxed()
        {
            var poor = new PlayerProgress { coins = 1 };
            Assert.IsFalse(GearCatalog.TryBuy(poor, GearSlot.Hook));
            Assert.AreEqual(1, poor.hookLevel);

            var rich = new PlayerProgress { coins = 99999, reelLevel = GearCatalog.MaxLevel };
            Assert.AreEqual(GearCatalog.NoPrice, GearCatalog.NextPrice(GearSlot.Reel, GearCatalog.MaxLevel));
            Assert.IsFalse(GearCatalog.TryBuy(rich, GearSlot.Reel));
            Assert.AreEqual(99999, rich.coins);
        }

        [Test]
        public void SellAll_EmptiesCreel_AndPaysItsValue()
        {
            var p = new PlayerProgress();
            p.AddToCreel(new FishData("carp", 1, 1f));
            p.AddToCreel(new FishData("perch", 2, 2f));
            int value = MarketRules.CreelValue(p, Prices);
            Assert.AreEqual(value, MarketRules.SellAll(p, Prices));
            Assert.AreEqual(0, p.creel.Count);
            Assert.AreEqual(value, p.coins);
            Assert.AreEqual(2, p.fishCaughtEver);
            Assert.AreEqual(2, p.bestTierEver);
        }

        [Test]
        public void SellAt_RemovesOneFish()
        {
            var p = new PlayerProgress();
            p.AddToCreel(new FishData("carp", 1, 1f));
            p.AddToCreel(new FishData("carp", 1, 2f));
            int coins = MarketRules.SellAt(p, 1, Prices);
            Assert.AreEqual(60, coins);
            Assert.AreEqual(1, p.creel.Count);
            Assert.AreEqual(0, MarketRules.SellAt(p, 5, Prices));
        }

        [Test]
        public void CastsCarryOver_UntilNextDay()
        {
            var p = new PlayerProgress();
            Assert.AreEqual(8, p.CastsToday(8));
            p.castsLeft = 3;
            Assert.AreEqual(3, p.CastsToday(8));
            p.NextDay();
            Assert.AreEqual(2, p.day);
            Assert.AreEqual(8, p.CastsToday(8));
        }

        [Test]
        public void CaughtToday_CountsLandings_AndResetsOvernight()
        {
            var p = new PlayerProgress();
            p.AddToCreel(new FishData("carp", 1, 1f));
            p.AddToCreel(new FishData("carp", 2, 1f));
            MarketRules.SellAll(p, Prices);
            Assert.AreEqual(2, p.caughtToday);
            p.NextDay();
            Assert.AreEqual(0, p.caughtToday);
        }

        [Test]
        public void DaySession_ResumesCastsLeft()
        {
            var d = new DaySession(8, 2, 4);
            Assert.AreEqual(2, d.CastsLeft);
            Assert.AreEqual(4, d.Day);
            d.UseCast(); d.UseCast();
            Assert.IsTrue(d.IsSunset);
        }
    }
}
