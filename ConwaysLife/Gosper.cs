using System;
using static ConwaysLife.Quad;
using static System.Linq.Enumerable;
using System.Diagnostics;

namespace ConwaysLife
{
    // An initial one-step implementation of Gosper's algorithm.

    sealed class Gosper : ILife, IReport, IDrawScale
    {
        private Quad cells;

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
            Draw(rect, setPixel, 0);
        }

        public void Draw(LifeRect rect, Action<LifePoint> setPixel, int scale)
        {
            cells.Draw(rect, setPixel, scale);
        }

        public int MaxScale => 50;

        // One more time, the life rule. Given a level-zero quad
        // and the count of its living neighbours, it stays the
        // same if the count is two, it stays or becomes alive if the 
        // count is three, and it stays or becomes dead otherwise:
        // 
        private static Quad Rule(Quad q, int count)
        {
            if (count == 2) 
                return q;
            if (count == 3) 
                return Alive;
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

        public void Step()
        {
            // TODO
        }

        // Step forward 2 to the n ticks.
        public void Step(int speed)
        {
            // TODO
        }

        public string Report() => "Non-stepping Gosper's algorithm";
    }
}
