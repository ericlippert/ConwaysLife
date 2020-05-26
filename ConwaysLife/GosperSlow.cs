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
            long xmin = rect.X;
            long xmax = rect.X + rect.Width;
            long ymin = rect.Y - rect.Height + 1;
            long ymax = rect.Y + 1;
            for (long y = ymin; y < ymax; y += 1)
                for (long x = xmin; x < xmax; x += 1)
                    if (this[x, y])
                        setPixel(new LifePoint(x, y));
        }

        public void Step()
        {
        }

        public void Step(int speed)
        {
        }
    }
}
