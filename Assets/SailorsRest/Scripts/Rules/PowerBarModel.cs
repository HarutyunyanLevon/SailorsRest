namespace SailorsRest.Rules
{
    /// <summary>Cast power: rises and falls while held; the value at release sets the distance.</summary>
    public class PowerBarModel
    {
        public const float DefaultCyclesPerSecond = 0.8f;
        /// <summary>The top of the bar that counts as a full-power cast for the HUD.</summary>
        public const float MaxBand = 0.17f;

        public float Value { get; private set; }
        public float CyclesPerSecond { get; }
        float phase;

        public PowerBarModel(float cyclesPerSecond = DefaultCyclesPerSecond) => CyclesPerSecond = cyclesPerSecond;

        public void Begin() { phase = 0f; Value = 0f; }

        /// <summary>A triangle wave: 0 → 1 over the first half of a cycle, back to 0 over the second.</summary>
        public void Tick(float dt)
        {
            phase = (phase + dt * CyclesPerSecond) % 1f;
            Value = 1f - System.Math.Abs(2f * phase - 1f);
        }

        public bool IsMax => Value >= 1f - MaxBand;
    }
}
