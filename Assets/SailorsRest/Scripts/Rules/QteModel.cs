using System.Collections.Generic;

namespace SailorsRest.Rules
{
    /// <summary>The hooking ring: a needle sweeps clockwise; a press while it is inside a green arc hooks the fish.</summary>
    public class QteModel
    {
        public const float FullTurn = 360f;
        /// <summary>No arc starts this close after the needle's start (and each slice's start), so there is time to react.</summary>
        public const float ArcLeadInDeg = 40f;
        /// <summary>Gap kept between an arc's end and the next slice.</summary>
        public const float ArcTrailGapDeg = 10f;

        public readonly struct Arc
        {
            public readonly float Start;
            public readonly float Width;
            public Arc(float start, float width) { Start = Normalize(start); Width = width; }
            public bool Contains(float deg) => Normalize(deg - Start) <= Width;
        }

        public float ArrowDeg { get; private set; }
        public float SpeedDegPerSec { get; }
        public IReadOnlyList<Arc> Arcs => arcs;
        readonly List<Arc> arcs = new List<Arc>();

        /// <summary>The needle starts at 12 o'clock.</summary>
        public QteModel(float speedDegPerSec, IEnumerable<Arc> zones)
        {
            SpeedDegPerSec = speedDegPerSec;
            arcs.AddRange(zones);
        }

        /// <summary>Spreads the arcs over equal slices of the ring, each placed at random inside its slice.</summary>
        public static QteModel Create(System.Random rng, float zoneWidthDeg, int zoneCount, float speedDegPerSec)
        {
            var zones = new List<Arc>();
            float slice = FullTurn / zoneCount;
            for (int i = 0; i < zoneCount; i++)
            {
                float minStart = i * slice + ArcLeadInDeg;
                float maxStart = System.Math.Max(minStart, (i + 1) * slice - zoneWidthDeg - ArcTrailGapDeg);
                zones.Add(new Arc(minStart + (float)rng.NextDouble() * (maxStart - minStart), zoneWidthDeg));
            }
            return new QteModel(speedDegPerSec, zones);
        }

        public void Tick(float dt) => ArrowDeg = Normalize(ArrowDeg + SpeedDegPerSec * dt);

        public bool IsOnGreen()
        {
            foreach (var a in arcs)
                if (a.Contains(ArrowDeg)) return true;
            return false;
        }

        public static float Normalize(float deg)
        {
            deg %= FullTurn;
            return deg < 0f ? deg + FullTurn : deg;
        }
    }
}
