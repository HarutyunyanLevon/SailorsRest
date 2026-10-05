using System;
using SailorsRest.Rules;
using UnityEngine;

namespace SailorsRest
{
    /// <summary>
    /// The fishing state machine: Aiming → Casting → Searching → Hooking → Reeling → CastOver, and Sunset when
    /// the day's casts are spent. Rules live in SailorsRest.Rules and numbers in <see cref="GameBalance"/>;
    /// this class wires them to input, fish and visuals.
    /// </summary>
    public class FishingController : MonoBehaviour
    {
        public enum Phase { Aiming, Casting, Searching, Hooking, Reeling, CastOver, Sunset }

        [Header("Setup")]
        public GameBalance balance;
        public FishSpawner spawner;
        public FishingInput input;
        public QteRingView qteView;
        public CameraFollow cameraFollow;
        public Transform rodTip;
        public Transform hook;
        public LineRenderer line;

        public Phase State { get; private set; } = Phase.Aiming;
        public DaySession Day { get; private set; }
        public PowerBarModel Power { get; private set; }
        public TensionModel Tension { get; private set; }
        /// <summary>The hooked fish's run / rest cycle. Set while reeling.</summary>
        public FishFight Fight { get; private set; }
        public FishAgent Hooked { get; private set; }
        public float HookDepth => Mathf.Max(0f, GameBalance.WaterlineY - hook.position.y);

        /// <summary>Short messages for the HUD toast: merges, catches, escapes.</summary>
        public event Action<string> Toast;

        static PlayerProgress Progress => GameSession.Progress;

        QteModel qte;
        FishAgent biting;
        FishingLineRope rope;
        float stateTimer, castX;
        Vector3 castStart;
        bool charging;
        readonly System.Random rng = new System.Random();

        void Awake()
        {
            balance = balance.WithGear(Progress);
            Power = new PowerBarModel(balance.powerCyclesPerSecond);
            rope = new FishingLineRope(line, balance);
        }

        void Start()
        {
            Day = new DaySession(balance.castsPerDay, Progress.CastsToday(balance.castsPerDay), Progress.day);
            Tension = new TensionModel(balance.ZoneMin, balance.ZoneMax)
            {
                RiseRate = balance.tensionRiseRate,
                FallRate = balance.tensionFallRate,
                Grace = balance.tensionGrace,
            };
            ResetHook();
            if (Day.IsSunset) Enter(Phase.Sunset);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            stateTimer += dt;
            switch (State)
            {
                case Phase.Aiming: UpdateAiming(dt); break;
                case Phase.Casting: UpdateCasting(); break;
                case Phase.Searching: UpdateSearching(dt); break;
                case Phase.Hooking: UpdateHooking(dt); break;
                case Phase.Reeling: UpdateReeling(dt); break;
                case Phase.CastOver: UpdateCastOver(); break;
                case Phase.Sunset: UpdateSunset(); break;
            }
            DrawLine();
        }

        void Enter(Phase next)
        {
            State = next;
            stateTimer = 0f;
        }

        // ----- Aiming: hold to fill the power bar, release to cast -----
        void UpdateAiming(float dt)
        {
            if (!charging && input.MenuPressed) { GameSession.Go(GameSession.MapScene); return; }
            if (input.PrimaryPressed) { charging = true; Power.Begin(); }
            if (!charging) return;
            if (input.PrimaryHeld) Power.Tick(dt);
            if (!input.PrimaryReleased) return;

            charging = false;
            castStart = rodTip.position;
            castX = Mathf.Min(balance.CastOriginX + Power.Value * balance.MaxCastDistance, balance.FarthestX);
            cameraFollow.target = hook;
            Enter(Phase.Casting);
        }

        // ----- Casting: the bobber flies in an arc and lands -----
        void UpdateCasting()
        {
            float t = Mathf.Clamp01(stateTimer / balance.castFlightSeconds);
            var p = Vector3.Lerp(castStart, new Vector3(castX, GameBalance.WaterlineY, 0f), t);
            p.y += Mathf.Sin(t * Mathf.PI) * balance.castArcHeight;
            hook.position = p;
            if (t >= 1f) Enter(Phase.Searching);
        }

        // ----- Searching: the hook drifts slowly; a fish close enough starts the hooking ring -----
        void UpdateSearching(float dt)
        {
            if (input.CancelPressed) { EndCast(); return; }
            float range = balance.hookHorizontalRange;
            float minX = Mathf.Max(balance.dockEdgeX, castX - range);
            float maxX = Mathf.Min(balance.FarthestX, castX + range);
            MoveHook(dt, balance.hookVerticalSpeed, balance.hookHorizontalSpeed, minX, maxX);

            // O(N) per frame, N = balance.fishCount.
            foreach (var fish in spawner.Fish)
            {
                if (fish.IgnoreUntil > Time.time) continue;
                if (Vector2.Distance(fish.transform.position, hook.position) > balance.biteRadius) continue;
                StartHooking(fish);
                return;
            }
        }

        void StartHooking(FishAgent fish)
        {
            CancelInvoke(nameof(HideRing));
            biting = fish;
            fish.Frozen = true;
            var species = fish.Species;
            int tier = fish.Data.Tier;
            bool rare = fish.Data.IsRare;
            float zone = species.ZoneFor(tier) * (rare ? balance.rareZoneScale : 1f);
            float speed = species.SpeedFor(tier) * (rare ? balance.rareSpeedScale : 1f) * balance.qteSpeedScale;
            qte = QteModel.Create(rng, zone, species.ArcsFor(tier), speed);
            qteView.Show(qte, fish.transform.position + Vector3.up * balance.ringHeightAboveFish);
            Enter(Phase.Hooking);
        }

        // ----- Hooking: press while the needle is on green -----
        void UpdateHooking(float dt)
        {
            qte.Tick(dt);
            if (input.CancelPressed) { MissHook(); return; }
            if (!input.PrimaryPressed) return;
            if (!qte.IsOnGreen()) { MissHook(); return; }

            ShowRingResult(true);
            BeginReel(biting);
        }

        void MissHook()
        {
            ShowRingResult(false);
            Release(biting);
            biting = null;
            Toast?.Invoke("Missed — it swam off");
            Enter(Phase.Searching);
        }

        void ShowRingResult(bool hit)
        {
            qteView.ShowResult(hit);
            Invoke(nameof(HideRing), balance.ringResultSeconds);
        }

        void HideRing() => qteView.Hide();

        // ----- Reeling: the fish runs (swims away, pulls the line tight) and rests (comes in while you reel on green) -----
        void BeginReel(FishAgent fish)
        {
            Hooked = fish;
            biting = null;
            Tension.SetZone(balance.ZoneMin, balance.ZoneMax);
            Tension.Reset();
            var style = fish.Species.fight;
            Fight = new FishFight(rng, style.runSeconds.x, style.runSeconds.y, style.restSeconds.x, style.restSeconds.y);
            OnFightPhaseChanged();
            Enter(Phase.Reeling);
        }

        void UpdateReeling(float dt)
        {
            bool reeling = input.PrimaryHeld;
            var species = Hooked.Species;
            int tier = Hooked.Data.Tier;

            if (Fight.Tick(dt)) OnFightPhaseChanged();
            float fishPull = Fight.IsRunning ? species.RunTensionFor(tier) : 0f;
            if (Tension.Tick(dt, reeling, fishPull))
            {
                OnLineFailed();
                if (Hooked == null) return;
            }

            float minX = balance.dockEdgeX - balance.underDockReach;
            MoveHook(dt, balance.fishVerticalSpeed, 0f, minX, balance.FarthestX);
            var p = hook.position;
            p.x = Mathf.Clamp(p.x + FishSpeedX(reeling, species, tier) * dt, minX, balance.FarthestX);
            if (Fight.IsRunning) p.y = Mathf.Clamp(p.y + RunSpeedY(species.fight) * dt, MinHookY, MaxHookY);
            hook.position = p;
            Hooked.transform.position = p;

            TryMerge();
            if (p.x <= balance.dockEdgeX + balance.landingDistance) Land();
        }

        /// <summary>A run starts with a jolt on the line and the fish turning away; a rest turns it back toward the dock.</summary>
        void OnFightPhaseChanged()
        {
            if (Fight.IsRunning) Tension.AddSurge(Hooked.Species.SurgeFor(Hooked.Data.Tier));
            Hooked.Face(Fight.IsRunning ? FishAgent.Right : FishAgent.Left);
        }

        /// <summary>
        /// Metres per second along the lake (+ = away from the dock). A running fish always gains distance; a resting
        /// one comes in only while you reel with the tension on green, and drifts off when you let go.
        /// </summary>
        float FishSpeedX(bool reeling, FishSpecies species, int tier)
        {
            if (Fight.IsRunning) return species.RunSpeedFor(tier);
            if (!reeling) return balance.fishDriftPerTier * tier;
            return Tension.State == TensionState.Steady ? -balance.ReelSpeed : 0f;
        }

        /// <summary>Metres per second up or down during a run (+ = toward the surface): the species' dive plus its zigzag.</summary>
        float RunSpeedY(FightStyle style) => -style.dive + style.weave * Mathf.Sin(stateTimer * style.weaveFrequency);

        /// <summary>O(N) per frame while reeling, N = balance.fishCount.</summary>
        void TryMerge()
        {
            float reach = balance.mergeRadius * Hooked.Data.Tier;
            foreach (var wild in spawner.Fish)
            {
                if (wild == Hooked) continue;
                if (Vector2.Distance(wild.transform.position, Hooked.transform.position) > reach) continue;
                if (!MergeRules.CanMerge(Hooked.Data, wild.Data, balance.maxTier)) continue;

                var merged = MergeRules.Merge(Hooked.Data, wild.Data, balance.mergeWeightBonus);
                Hooked.SetData(merged);
                spawner.Remove(wild);
                Toast?.Invoke($"Merged! {Hooked.Species.NameOf(merged)} T{merged.Tier}");
                return;
            }
        }

        void OnLineFailed()
        {
            if (MergeRules.TryDropTier(Hooked.Data, out var dropped))
            {
                Hooked.SetData(dropped);
                Tension.Reset();
                Toast?.Invoke($"Slipped to T{dropped.Tier}");
                return;
            }
            Release(Hooked);
            Hooked = null;
            Toast?.Invoke("It got away");
            EndCast();
        }

        void Land()
        {
            var fish = Hooked.Data;
            var species = Hooked.Species;
            spawner.Remove(Hooked);
            Hooked = null;
            Progress.AddToCreel(fish);
            Toast?.Invoke($"Landed {species.NameOf(fish)} {FishSpecies.TierAndWeight(fish)} · worth {balance.Pricing.Of(fish)}");
            EndCast();
        }

        /// <summary>A fish that got off the hook swims away and ignores it for a while.</summary>
        void Release(FishAgent fish)
        {
            fish.Frozen = false;
            fish.IgnoreUntil = Time.time + balance.escapedIgnoreSeconds;
            fish.Face(FishAgent.Right);
        }

        // ----- End of a cast and of the day -----
        void EndCast()
        {
            if (biting != null) { biting.Frozen = false; biting = null; }
            qteView.Hide();
            Day.UseCast();
            Progress.castsLeft = Day.CastsLeft;
            GameSession.Save();
            spawner.Refill();
            cameraFollow.target = null;
            Enter(Phase.CastOver);
        }

        void UpdateCastOver()
        {
            hook.position = Vector3.MoveTowards(hook.position, rodTip.position, balance.reelBackSpeed * Time.deltaTime);
            if (stateTimer < balance.castOverSeconds) return;
            ResetHook();
            Enter(Day.IsSunset ? Phase.Sunset : Phase.Aiming);
        }

        // Sunset: sell at the market, or walk back to the map. Either way a new day starts.
        void UpdateSunset()
        {
            if (stateTimer < balance.sunsetInputDelay) return;
            if (input.PrimaryPressed) LeaveForNextDay(GameSession.MarketScene);
            else if (input.CancelPressed || input.MenuPressed) LeaveForNextDay(GameSession.MapScene);
        }

        static void LeaveForNextDay(string scene)
        {
            Progress.NextDay();
            GameSession.Go(scene);
        }

        // ----- Helpers -----
        float MinHookY => GameBalance.WaterlineY - balance.MaxHookDepth;
        float MaxHookY => GameBalance.WaterlineY - balance.hookMinDepth;

        void MoveHook(float dt, float vSpeed, float hSpeed, float minX, float maxX)
        {
            Vector2 p = hook.position;
            float minY = MinHookY;
            float maxY = MaxHookY;
            if (input.TryGetPointerWorld(Camera.main, out var pointer))
            {
                p.y = Mathf.MoveTowards(p.y, Mathf.Clamp(pointer.y, minY, maxY), vSpeed * dt);
                p.x = Mathf.MoveTowards(p.x, Mathf.Clamp(pointer.x, minX, maxX), hSpeed * dt);
            }
            else
            {
                var stick = input.Stick;
                p.y = Mathf.Clamp(p.y + stick.y * vSpeed * dt, minY, maxY);
                p.x = Mathf.Clamp(p.x + stick.x * hSpeed * dt, minX, maxX);
            }
            hook.position = p;
        }

        void ResetHook()
        {
            hook.position = rodTip.position;
            Power.Begin();
        }

        void DrawLine()
        {
            if (State == Phase.Aiming || State == Phase.Sunset) { rope.Hide(); return; }
            rope.Simulate(rodTip.position, hook.position, LineTautness(), Time.deltaTime);
        }

        /// <summary>While reeling the line is as taut as the tension; reeling back empty pulls it straight.</summary>
        float LineTautness()
        {
            switch (State)
            {
                case Phase.Reeling: return Tension.Value;
                case Phase.CastOver: return 1f;
                default: return balance.lineRestTautness;
            }
        }
    }
}
