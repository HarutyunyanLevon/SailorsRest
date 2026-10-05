namespace SailorsRest.Rules
{
    /// <summary>One in-game day at the water: a fixed number of casts.</summary>
    public class DaySession
    {
        public const int FirstDay = 1;
        /// <summary>Pass as castsLeft to start a full day.</summary>
        public const int FullDay = -1;

        public int Day { get; }
        public int CastsPerDay { get; }
        public int CastsLeft { get; private set; }

        /// <param name="castsLeft">Casts still unused today when resuming a day; out of range means a full day.</param>
        public DaySession(int castsPerDay, int castsLeft = FullDay, int day = FirstDay)
        {
            CastsPerDay = castsPerDay;
            CastsLeft = Normalize(castsLeft, castsPerDay);
            Day = day < FirstDay ? FirstDay : day;
        }

        /// <summary>Casts left today; anything out of range (such as <see cref="FullDay"/>) means a full day.</summary>
        public static int Normalize(int castsLeft, int castsPerDay) =>
            castsLeft < 0 || castsLeft > castsPerDay ? castsPerDay : castsLeft;

        public bool IsSunset => CastsLeft <= 0;

        public void UseCast()
        {
            if (CastsLeft > 0) CastsLeft--;
        }
    }
}
