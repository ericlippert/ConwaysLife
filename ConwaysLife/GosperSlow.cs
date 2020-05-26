using System;
using static ConwaysLife.Quad;

namespace ConwaysLife
{
    // Implementation of Gosper's algorithm without "hyper speed".
    sealed class GosperSlow : ILife
    {
        // To start with, we will just create a 9-quad and use that for testing.
        Quad cells;

        public GosperSlow()
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
            set => cells = cells.Set(p, value ? Alive : Dead);
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
        }

        public void Step(int speed)
        {
        }
    }
}
