using static System.Math;
using System;

namespace ConwaysLife
{
    // An implementation of Life using the triplet data structure from Stafford's algorithm,
    // but with no change list or lookup table optimizations; I wrote this just to make
    // sure that I got the data structure updates correct before attempting to optimize them.

    class StaffordUnoptimized : ILife
    {
        // We're going to keep the top and bottom edge triplets dead,
        // so this gives us 256 live rows.
        private int height = 258;

        // This is the width in triplets. That gives us 264 cells, but 
        // we'll keep the left and right triplets dead, so that's 258
        // live columns of cells.

        private int width = 88;
        private Triplet[,] triplets;

        public StaffordUnoptimized()
        {
            Clear();
        }

        public void Clear()
        {
            triplets = new Triplet[width, height];
        }

        private bool IsValidPoint(long x, long y) =>
            1 <= x && x < (width - 1) * 3 && 1 <= y && y < height - 1;

        private void BecomeAlive(int x, int y)
        {
            int tx = x / 3;
            Triplet t = triplets[tx, y];

            switch (x % 3)
            {
                case 0:
                    if (t.LeftCurrent)
                        return;
                    // Left is about to be born
                    t = t.AUU();
                    triplets[tx, y - 1] = triplets[tx, y - 1].PPU();
                    triplets[tx, y + 1] = triplets[tx, y + 1].PPU();
                    triplets[tx - 1, y - 1] = triplets[tx - 1, y - 1].UUP();
                    triplets[tx - 1, y] = triplets[tx - 1, y].UUP();
                    triplets[tx - 1, y + 1] = triplets[tx - 1, y + 1].UUP();
                    break;
                case 1:
                    if (t.MiddleCurrent)
                        return;
                    // Middle is about to be born
                    t = t.UAU();
                    triplets[tx, y - 1] = triplets[tx, y - 1].PPP();
                    triplets[tx, y + 1] = triplets[tx, y + 1].PPP();
                    break;
                case 2:
                    if (t.RightCurrent)
                        return;
                    // Right is about to be born
                    t = t.UUA();
                    triplets[tx, y - 1] = triplets[tx, y - 1].UPP();
                    triplets[tx, y + 1] = triplets[tx, y + 1].UPP();
                    triplets[tx + 1, y - 1] = triplets[tx + 1, y - 1].PUU();
                    triplets[tx + 1, y] = triplets[tx + 1, y].PUU();
                    triplets[tx + 1, y + 1] = triplets[tx + 1, y + 1].PUU();
                    break;
            }
            // TODO: This could be in place. Save mutating t again.
            // TODO: Also re-order the above.
            triplets[tx, y] = t;
        }

        private void BecomeDead(int x, int y)
        {
            int tx = x / 3;
            Triplet t = triplets[tx, y];

            switch (x % 3)
            {
                case 0:
                    if (!t.LeftCurrent)
                        return;
                    t = t.DUU();
                    triplets[tx, y - 1] = triplets[tx, y - 1].MMU();
                    triplets[tx, y + 1] = triplets[tx, y + 1].MMU();
                    triplets[tx - 1, y - 1] = triplets[tx - 1, y - 1].UUM();
                    triplets[tx - 1, y] = triplets[tx - 1, y].UUM();
                    triplets[tx - 1, y + 1] = triplets[tx - 1, y + 1].UUM();
                    break;
                case 1:
                    if (!t.MiddleCurrent)
                        return;
                    t = t.UDU();
                    triplets[tx, y - 1] = triplets[tx, y - 1].MMM();
                    triplets[tx, y + 1] = triplets[tx, y + 1].MMM();
                    break;
                case 2:
                    if (!t.RightCurrent)
                        return;
                    t = t.UUD();
                    triplets[tx, y - 1] = triplets[tx, y - 1].UMM();
                    triplets[tx, y + 1] = triplets[tx, y + 1].UMM();
                    triplets[tx + 1, y - 1] = triplets[tx + 1, y - 1].MUU();
                    triplets[tx + 1, y] = triplets[tx + 1, y].MUU();
                    triplets[tx + 1, y + 1] = triplets[tx + 1, y + 1].MUU();
                    break;
            }
            triplets[tx, y] = t;
        }

        public bool this[long x, long y]
        {
            get
            {
                if (IsValidPoint(x, y))
                {
                    Triplet t = triplets[x / 3, y];
                    switch (x % 3)
                    {
                        case 0: return t.LeftCurrent;
                        case 1: return t.MiddleCurrent;
                        default: return t.RightCurrent;
                    }
                }
                return false;
            }
            set
            {
                if (IsValidPoint(x, y))
                {
                    if (value)
                        BecomeAlive((int)x, (int)y);
                    else
                        BecomeDead((int)x, (int)y);
                }
            }
        }

        public bool this[LifePoint v]
        {
            get => this[v.X, v.Y];
            set => this[v.X, v.Y] = value;
        }

        public void Step()
        {
            // * Compute the next state from the current state and neighbor count.

            for (int y = 1; y < height - 1; y += 1)
            {
                for (int tx = 1; tx < width - 1; tx += 1)
                {
                    Triplet t = triplets[tx, y];
                    int lc = t.LeftCount;
                    int mc = t.MiddleCount;
                    int rc = t.RightCount;
                    t = t.SetLeftNext(lc == 3 | t.LeftCurrent & lc == 2);
                    t = t.SetMiddleNext(mc == 3 | t.MiddleCurrent & mc == 2);
                    t = t.SetRightNext(rc == 3 | t.RightCurrent & rc == 2);
                    triplets[tx, y] = t;
                }
            }

            // * Copy the next cell state to the current cell state.
            // * Update the neighbor counts of the neighboring triplets if necessary
            //   to match the new current state.

            for (int y = 1; y < height - 1; y += 1)
            {
                for (int tx = 1; tx < width - 1; tx += 1)
                {
                    Triplet t = triplets[tx, y];
                    if (t.LeftCurrent & !t.LeftNext)
                        BecomeDead(tx * 3, y);
                    else if (!t.LeftCurrent & t.LeftNext)
                        BecomeAlive(tx * 3, y);

                    if (t.MiddleCurrent & !t.MiddleNext)
                        BecomeDead(tx * 3 + 1, y);
                    else if (!t.MiddleCurrent & t.MiddleNext)
                        BecomeAlive(tx * 3 + 1, y);

                    if (t.RightCurrent & !t.RightNext)
                        BecomeDead(tx * 3 + 2, y);
                    else if (!t.RightCurrent & t.RightNext)
                        BecomeAlive(tx * 3 + 2, y);
                }
            }
        }

        public void Draw(LifeRect rect, Action<LifePoint> setPixel)
        {
            long xmin = Max(0, rect.X);
            long xmax = Min(width * 3, rect.X + rect.Width);
            long ymin = Max(0, rect.Y - rect.Height + 1);
            long ymax = Min(height, rect.Y + 1);
            for (long y = ymin; y < ymax; y += 1)
                for (long x = xmin; x < xmax; x += 1)
                    if (this[x, y])
                        setPixel(new LifePoint(x, y));
        }
    }
}