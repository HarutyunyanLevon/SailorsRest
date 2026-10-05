namespace SailorsRest.Rules
{
    public enum Variant { None = 0, Golden = 1 }

    /// <summary>One fish as the rules see it: no visuals, no MonoBehaviour.</summary>
    [System.Serializable]
    public struct FishData
    {
        public string SpeciesId;
        public int Tier;
        public float WeightKg;
        public Variant Variant;

        public FishData(string speciesId, int tier, float weightKg, Variant variant = Variant.None)
        {
            SpeciesId = speciesId;
            Tier = tier;
            WeightKg = weightKg;
            Variant = variant;
        }

        public bool IsRare => Variant != Variant.None;

        public override string ToString() => $"{SpeciesId} T{Tier} {WeightKg:0.00}kg {Variant}";
    }
}
