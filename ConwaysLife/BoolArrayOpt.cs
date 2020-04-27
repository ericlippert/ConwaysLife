using System;
using static System.Math;

namespace ConwaysLife
{
    // The naive implementation with a few small tweaks to make it faster.
    //
    // The original implementation took about 3400us per step, or 
    // about 300 steps per second.
    // 
    // This only slightly tweaked implementation takes 800us per step or
    // about 1250 steps per second; a 4x improvement!
    class BoolArrayOpt : ILife
    {
        private int height = 256;
        private int width = 256;

        // Allocating the two arrays once and re-using them instead of 
        // allocating the new array every time,  saves 50us per step.
        // Using a byte array instead of a bool array saves 40us per step;
        // This is quite surprising!
        private byte[,] cells;
        private byte[,] temp;

        public BoolArrayOpt()
        {
            Clear();
        }

        public void Clear()
        {
            cells = new byte[width, height];
            temp = new byte[width, height];
        }

        private bool IsValidPoint(long x, long y) =>
            0 <= x && x < width && 0 <= y && y < height;

        public bool this[long x, long y]
        {
            get
            {
                if (IsValidPoint(x, y))
                    return cells[x, y] != 0;
                return false;
            }
            set
            {
                if (IsValidPoint(x, y))
                    cells[x, y] = value ? (byte)1 : (byte)0;
            }
        }

        public bool this[LifePoint v]
        {
            get => this[v.X, v.Y];
            set => this[v.X, v.Y] = value;
        }

        private int LivingNeighbors(int x, int y)
        {
            // Checking for known-good validity *once* instead of
            // *eight times* per cycle is the majority of the win.
            if (1 <= x && x < width - 1 && 1 <= y && y < height - 1)
            {
                // Using & instead of && is a small regression.

                // Precomputing x-1, y-1, x+1, y+1 and storing them in locals
                // causes a small regression!
                int sum = cells[x - 1, y - 1];
                sum += cells[x - 1, y];
                sum += cells[x - 1, y + 1];
                sum += cells[x, y - 1];
                sum += cells[x, y + 1];
                sum += cells[x + 1, y - 1];
                sum += cells[x + 1, y];
                sum += cells[x + 1, y + 1];
                return sum;
            }
            else
            {
                int sum = this[x - 1, y - 1] ? 1 : 0;
                sum += this[x - 1, y] ? 1 : 0;
                sum += this[x - 1, y + 1] ? 1 : 0;
                sum += this[x, y - 1] ? 1 : 0;
                sum += this[x, y + 1] ? 1 : 0;
                sum += this[x + 1, y - 1] ? 1 : 0;
                sum += this[x + 1, y] ? 1 : 0;
                sum += this[x + 1, y + 1] ? 1 : 0;
                return sum;
            }
        }

        public void Step()
        {
            for (int y = 0; y < height; y += 1)
            {
                for (int x = 0; x < width; x += 1)
                {
                    int count = LivingNeighbors(x, y);
                    temp[x, y] = count == 3 || (cells[x, y] != 0 && count == 2) ? (byte)1 : (byte)0;
                }
            }
            var t = temp;
            temp = cells;
            cells = t;
        }

        public void Draw(LifeRect rect, Action<LifePoint> setPixel)
        {
            long xmin = Max(0, rect.X);
            long xmax = Min(width, rect.X + rect.Width);
            long ymin = Max(0, rect.Y - rect.Height + 1);
            long ymax = Min(height, rect.Y + 1);
            for (long y = ymin; y < ymax; y += 1)
                for (long x = xmin; x < xmax; x += 1)
                    if (cells[x, y] != 0)
                        setPixel(new LifePoint(x, y));
        }
    }
}
