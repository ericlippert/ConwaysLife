using System;
using static ConwaysLife.Quad;
using static System.Linq.Enumerable;

namespace ConwaysLife
{
    // An initial implementation of Gosper's algorithm that just draws a quad.

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
