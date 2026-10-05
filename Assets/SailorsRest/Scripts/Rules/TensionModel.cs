namespace SailorsRest.Rules
{
    public enum TensionState { Slack, Steady, Danger }

    /// <summary>
    /// Line tension during the reel-in, 0..1. Holding raises it, releasing lowers it, fish surges spike it.
    /// Staying outside the safe zone longer than the grace time is a failure.
    /// </summary>
    public class TensionModel
    {
        public const float DefaultRiseRate = 0.55f;
        public const float DefaultFallRate = 0.7f;
        public const float DefaultGrace = 0.9f;
        public const float Middle = 0.5f;

        public float Value { get; private set; }
        public float ZoneMin { get; private set; }
        public float ZoneMax { get; private set; }
        public float RiseRate = DefaultRiseRate;
        public float FallRate = DefaultFallRate;
        public float Grace = DefaultGrace;
        public float OutsideTime { get; private set; }

        public TensionModel(float zoneMin, float zoneMax, float start = Middle)
        {
            SetZone(zoneMin, zoneMax);
            Value = Clamp01(start);
        }

        public void SetZone(float zoneMin, float zoneMax)
        {
            ZoneMin = Clamp01(zoneMin);
            ZoneMax = Clamp01(zoneMax < zoneMin ? zoneMin : zoneMax);
        }

        public TensionState State =>
            Value < ZoneMin ? TensionState.Slack : Value > ZoneMax ? TensionState.Danger : TensionState.Steady;

        public void AddSurge(float amount) => Value = Clamp01(Value + amount);

        /// <summary>Advances the model. Returns true when the line fails this tick.</summary>
        public bool Tick(float dt, bool reeling)
        {
            Value = Clamp01(Value + (reeling ? RiseRate : -FallRate) * dt);
            if (State == TensionState.Steady)
            {
                OutsideTime = 0f;
                return false;
            }
            OutsideTime += dt;
            if (OutsideTime < Grace) return false;
            OutsideTime = 0f;
            return true;
        }

        /// <summary>Back to the middle of the meter with a full grace period.</summary>
        public void Reset()
        {
            Value = Middle;
            OutsideTime = 0f;
        }

        static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
