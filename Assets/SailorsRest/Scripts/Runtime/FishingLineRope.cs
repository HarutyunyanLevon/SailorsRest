using UnityEngine;

namespace SailorsRest
{
    /// <summary>
    /// The fishing line as a hanging rope: Verlet points between the rod tip and the hook, pulled down by gravity
    /// (gently under water, with drag) and kept from stretching past their length. That length is the straight
    /// distance plus slack, so a taut line is nearly straight and a slack one sags into a curve.
    /// Arrays are made once; simulating allocates nothing.
    /// </summary>
    public class FishingLineRope
    {
        /// <summary>Constraint passes per frame: more = less stretchy.</summary>
        const int SolverPasses = 30;
        const float MinSegment = 1e-4f;

        readonly LineRenderer line;
        readonly GameBalance balance;
        readonly Vector3[] points;
        readonly Vector3[] previous;
        readonly int last;
        bool shown;

        public FishingLineRope(LineRenderer line, GameBalance balance)
        {
            this.line = line;
            this.balance = balance;
            last = Mathf.Max(1, balance.lineSegments);
            points = new Vector3[last + 1];
            previous = new Vector3[last + 1];
            line.positionCount = points.Length;
            Hide();
        }

        public void Hide()
        {
            shown = false;
            line.enabled = false;
        }

        /// <param name="tautness">0 = all the slack the line has, 1 = straight.</param>
        public void Simulate(Vector3 tip, Vector3 end, float tautness, float dt)
        {
            if (!shown) LayStraight(tip, end);
            Integrate(Mathf.Min(dt, balance.lineMaxStep));
            Constrain(tip, end, SegmentLength(tip, end, tautness));
            line.SetPositions(points);
        }

        /// <summary>A newly shown line starts straight and still, so it doesn't whip in from where it was hidden.</summary>
        void LayStraight(Vector3 tip, Vector3 end)
        {
            for (int i = 0; i <= last; i++) points[i] = previous[i] = Vector3.Lerp(tip, end, (float)i / last);
            shown = true;
            line.enabled = true;
        }

        void Integrate(float dt)
        {
            for (int i = 1; i < last; i++)
            {
                var p = points[i];
                bool wet = p.y < GameBalance.WaterlineY;
                var velocity = (p - previous[i]) * (wet ? balance.lineWaterDamping : balance.lineAirDamping);
                float gravity = wet ? balance.lineWaterGravity : balance.lineAirGravity;
                previous[i] = p;
                points[i] = p + velocity + Vector3.down * (gravity * dt * dt);
            }
        }

        float SegmentLength(Vector3 tip, Vector3 end, float tautness)
        {
            float slack = balance.lineSlack * (1f - Mathf.Clamp01(tautness));
            return Vector3.Distance(tip, end) * (1f + slack) / last;
        }

        /// <summary>A rope only resists stretching; a segment shorter than its length is left to sag.</summary>
        void Constrain(Vector3 tip, Vector3 end, float segment)
        {
            points[0] = tip;
            points[last] = end;
            for (int pass = 0; pass < SolverPasses; pass++)
            {
                for (int i = 0; i < last; i++)
                {
                    var delta = points[i + 1] - points[i];
                    float distance = delta.magnitude;
                    if (distance <= segment || distance < MinSegment) continue;
                    var fix = delta * ((distance - segment) / distance);
                    bool startPinned = i == 0;
                    bool endPinned = i + 1 == last;
                    if (startPinned && endPinned) continue;
                    if (startPinned) points[i + 1] -= fix;
                    else if (endPinned) points[i] += fix;
                    else
                    {
                        points[i] += fix * 0.5f;
                        points[i + 1] -= fix * 0.5f;
                    }
                }
            }
        }
    }
}
