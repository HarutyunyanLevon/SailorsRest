namespace SailorsRest.Rules
{
    public enum FightPhase { Running, Resting }

    /// <summary>
    /// A hooked fish's rhythm: it alternates runs (it fights and swims away) with rests (it tires and can be
    /// reeled in). Each phase lasts a random time in its range. A fresh bite always starts with a run.
    /// </summary>
    public class FishFight
    {
        public FightPhase Phase { get; private set; }
        public float TimeLeft { get; private set; }
        public bool IsRunning => Phase == FightPhase.Running;

        readonly System.Random rng;
        readonly float runMin, runMax, restMin, restMax;

        public FishFight(System.Random rng, float runMin, float runMax, float restMin, float restMax)
        {
            this.rng = rng;
            this.runMin = runMin;
            this.runMax = runMax;
            this.restMin = restMin;
            this.restMax = restMax;
            Start(FightPhase.Running);
        }

        /// <summary>Advances the cycle. Returns true on the tick the fish switches between running and resting.</summary>
        public bool Tick(float dt)
        {
            TimeLeft -= dt;
            if (TimeLeft > 0f) return false;
            Start(IsRunning ? FightPhase.Resting : FightPhase.Running);
            return true;
        }

        void Start(FightPhase phase)
        {
            Phase = phase;
            TimeLeft = phase == FightPhase.Running ? Pick(runMin, runMax) : Pick(restMin, restMax);
        }

        float Pick(float min, float max) => min + (float)rng.NextDouble() * (max - min);
    }
}
