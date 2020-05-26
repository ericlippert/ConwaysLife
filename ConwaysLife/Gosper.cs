using System;
using System.Diagnostics;
using static ConwaysLife.Quad;

namespace ConwaysLife
{
    // Implementation of Gosper's algorithm
    sealed class Gosper : ILife
    {
        static Gosper()
        {
            CacheManager.StepSpeedMemoizer = new Memoizer<(Quad, int), Quad>(UnmemoizedStep);
        }

        Quad cells;

        public Gosper()
        {
            Clear();
        }

        public void Clear()
        {
            cells = Empty(9);
        }

        public bool this[LifePoint p]
        {
            get => cells.Get(p) != Dead;
            set
            {
                while (!cells.Contains(p))
                    cells = cells.Embiggen();
                cells = cells.Set(p, value ? Alive : Dead);
            }
        }


        public bool this[long x, long y]
        {
            get => this[new LifePoint(x, y)];
            set => this[new LifePoint(x, y)] = value;
        }

        public void Draw(LifeRect rect, Action<LifePoint> setPixel)
        {
            cells.Draw(rect, setPixel);
        }

        public void Step()
        {
            Step(0);
        }

        // Step forward 2 to the n ticks.
        public void Step(int speed)
        {
            const int MaxSpeed = MaxLevel - 2;
            Debug.Assert(speed >= 0);
            Debug.Assert(speed <= MaxSpeed);

            Quad current = cells;
            if (!current.HasAllEmptyEdges)
                current = current.Embiggen().Embiggen();
            else if (!current.Center.HasAllEmptyEdges)
                current = current.Embiggen();
            while (current.Level < speed + 2)
                current = current.Embiggen();

            Quad next = Step(current, speed);
            // Remember, this is now one level smaller than current.
            // We might as well bump it up; we're just going to check
            // its edges for emptiness on the next tick again.
            cells = next.Embiggen();
        }

        // One more time, the life rule. Given a level-zero quad
        // and the count of its living neighbours, it stays the
        // same if the count is two, it stays or becomes alive if the 
        // count is three, and it stays or becomes dead otherwise:
        // 
        private static Quad Rule(Quad q, int count)
        {
            if (count == 2) return q;
            if (count == 3) return Alive;
            return Dead;
        }

        // The basic idea of Gosper's algorithm is that if we have an n-quad,
        // then we have enough information to compute the next state of the 
        // (n-1) quad that is its *center*.
        //
        // We can do this recursively but of course we need a base case. 
        // A 0-quad and a 1-quad do not have enough cells, so our
        // smallest possible base case is a 2-quad.

        private static Quad StepBaseCase(Quad q)
        {
            // We have a 2-quad, which is a 4x4 grid; let's number the cells
            // in it as follows:
            //
            // 00 01 02 03
            // 10 11 12 13
            // 20 21 22 23
            // 30 31 32 33

            // We wish to compute the 1-quad that is the next state of the
            // four middle cells, 11, 12, 21 and 22.

            // First get the state of all 16 cells as an integer:

            Debug.Assert(q.Level == 2);
            int b00 = (q.NW.NW == Dead) ? 0 : 1;
            int b01 = (q.NW.NE == Dead) ? 0 : 1;
            int b02 = (q.NE.NW == Dead) ? 0 : 1;
            int b03 = (q.NE.NE == Dead) ? 0 : 1;
            int b10 = (q.NW.SW == Dead) ? 0 : 1;
            int b11 = (q.NW.SE == Dead) ? 0 : 1;
            int b12 = (q.NE.SW == Dead) ? 0 : 1;
            int b13 = (q.NE.SE == Dead) ? 0 : 1;
            int b20 = (q.SW.NW == Dead) ? 0 : 1;
            int b21 = (q.SW.NE == Dead) ? 0 : 1;
            int b22 = (q.SE.NW == Dead) ? 0 : 1;
            int b23 = (q.SE.NE == Dead) ? 0 : 1;
            int b30 = (q.SW.SW == Dead) ? 0 : 1;
            int b31 = (q.SW.SE == Dead) ? 0 : 1;
            int b32 = (q.SE.SW == Dead) ? 0 : 1;
            int b33 = (q.SE.SE == Dead) ? 0 : 1;

            // The neighbours of cell 11 are cells 00, 01, 02, 10, ...
            // Add them up.

            int n11 = b00 + b01 + b02 + b10 + b12 + b20 + b21 + b22;
            int n12 = b01 + b02 + b03 + b11 + b13 + b21 + b22 + b23;
            int n21 = b11 + b12 + b13 + b21 + b23 + b31 + b32 + b33;
            int n22 = b10 + b11 + b12 + b20 + b22 + b30 + b31 + b32;
            return Make(
                Rule(q.NW.SE, n11),
                Rule(q.NE.SW, n12),
                Rule(q.SE.NW, n21),
                Rule(q.SW.NE, n22));
        }

        private static Quad UnmemoizedStep((Quad q, int speed) args)
        {
            // This algorithm moves forward 2-to-the-speed ticks on the 
            // center of quad q.
            //
            // There are two possibilities; either we are running at maximum
            // speed, which is speed equal to level - 2, or we are running
            // slower than that.
            //
            // If we are running at maximum speed then when we recurse,
            // we need to reduce speed.
            //
            // If we are running at slower than maximum speed, we can do
            // the recursion at current speed, to get the center bits at the correct
            // number of ticks forwards, and then extract the bits from there.

            Quad q = args.q;
            int speed = args.speed;

            Debug.Assert(q.Level >= 2);
            Debug.Assert(speed >= 0);
            Debug.Assert(speed <= q.Level - 2);

            Quad r;
            if (q.IsEmpty)
                r = Quad.Empty(q.Level - 1);
            else if (speed == 0 && q.Level == 2)
                r = StepBaseCase(q);
            else
            {
                // Do we need to slow down on the recursion?

                int nineSpeed = (speed == q.Level - 2) ? speed - 1 : speed;

                Quad q9nw = Step(q.NW, nineSpeed);
                Quad q9n = Step(q.N, nineSpeed);
                Quad q9ne = Step(q.NE, nineSpeed);
                Quad q9w = Step(q.W, nineSpeed);
                Quad q9c = Step(q.Center, nineSpeed);
                Quad q9e = Step(q.E, nineSpeed);
                Quad q9sw = Step(q.SW, nineSpeed);
                Quad q9s = Step(q.S, nineSpeed);
                Quad q9se = Step(q.SE, nineSpeed);
                Quad q4nw = Make(q9nw, q9n, q9c, q9w);
                Quad q4ne = Make(q9n, q9ne, q9e, q9c);
                Quad q4se = Make(q9c, q9e, q9se, q9s);
                Quad q4sw = Make(q9w, q9c, q9s, q9sw);

                // Do we already have the result we need, or should
                // we run forwards as fast as possible?

                if (speed == q.Level - 2)
                {
                    Quad rnw = Step(q4nw, speed - 1);
                    Quad rne = Step(q4ne, speed - 1);
                    Quad rse = Step(q4se, speed - 1);
                    Quad rsw = Step(q4sw, speed - 1);
                    r = Make(rnw, rne, rse, rsw);
                }
                else
                {
                    Quad rnw = q4nw.Center;
                    Quad rne = q4ne.Center;
                    Quad rse = q4se.Center;
                    Quad rsw = q4sw.Center;
                    r = Make(rnw, rne, rse, rsw);
                }
            }
            Debug.Assert(q.Level == r.Level + 1);
            return r;
        }

        private static Quad Step(Quad q, int speed) => UnmemoizedStep((q, speed));
            // CacheManager.StepSpeedMemoizer.MemoizedFunc((q, speed));
    }
}
