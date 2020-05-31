using System;
using System.Collections.Generic;
using System.Diagnostics;
using static System.Math;

namespace ConwaysLife
{
    // This is a precursor to Stafford's algorithm. So far we have:
    //
    // * Cells stored in triplets in a short
    // * A rectangular array of triplets
    // * Living neighbour count in the triplet
    // * Current/next state in the triplet
    // * Deduplicated list of previous changes

    class StaffordOne : ILife
    {
        // Top and bottom rows are always dead, so 256 cells in
        // effective height.
        private int height = 258;
        // Width in triplets. That gives us 264 cells, but the 
        // leftmost and rightmost triplet in a row is always all dead,
        // so that's only 258 cells wide.
        private int width = 88; 
        private Triplet[,] triplets;
        // Coordinates of triplets, not cells.
        private List<(int, int)> changes;

        public StaffordOne()
        {
            Clear();
        }

        public void Clear()
        {
            triplets = new Triplet[width, height];
            changes = new List<(int, int)>();
        }

        private bool IsValidPoint(long x, long y) =>
            1 <= x && x < (width - 1) * 3 && 1 <= y && y < height - 1;

        // Returns true if a change was made, false if the cell was already alive.
        // In previous versions this also updated the change list, but it does
        // not now.
        private bool BecomeAlive(int x, int y)
        {
            int tx = x / 3;
            Triplet t = triplets[tx, y];
            switch (x % 3)
            {
                case 0:
                    if (t.LeftCurrent)
                        return false;
                    // Left is about to be born
                    triplets[tx - 1, y - 1] = triplets[tx - 1, y - 1].UUP();
                    triplets[tx, y - 1] = triplets[tx, y - 1].PPU();
                    triplets[tx - 1, y] = triplets[tx - 1, y].UUP();
                    triplets[tx, y] = t.SetLeftCurrent(true);
                    triplets[tx - 1, y + 1] = triplets[tx - 1, y + 1].UUP();
                    triplets[tx, y + 1] = triplets[tx, y + 1].PPU();
                    break;
                case 1:
                    if (t.MiddleCurrent)
                        return false;
                    // Middle is about to be born
                    triplets[tx, y - 1] = triplets[tx, y - 1].PPP();
                    triplets[tx, y] = t.SetMiddleCurrent(true);
                    triplets[tx, y + 1] = triplets[tx, y + 1].PPP();
                    break;
                case 2:
                    if (t.RightCurrent)
                        return false;
                    // Right is about to be born
                    triplets[tx, y - 1] = triplets[tx, y - 1].UPP();
                    triplets[tx + 1, y - 1] = triplets[tx + 1, y - 1].PUU();
                    triplets[tx, y] = t.SetRightCurrent(true);
                    triplets[tx + 1, y] = triplets[tx + 1, y].PUU();
                    triplets[tx, y + 1] = triplets[tx, y + 1].UPP();
                    triplets[tx + 1, y + 1] = triplets[tx + 1, y + 1].PUU();
                    break;
            }
            return true;
        }

        private bool BecomeDead(int x, int y)
        {
            int tx = x / 3;
            Triplet t = triplets[tx, y];

            switch (x % 3)
            {
                case 0:
                    if (!t.LeftCurrent)
                        return false;
                    triplets[tx - 1, y - 1] = triplets[tx - 1, y - 1].UUM();
                    triplets[tx, y - 1] = triplets[tx, y - 1].MMU();
                    triplets[tx - 1, y] = triplets[tx - 1, y].UUM();
                    triplets[tx, y] = t.SetLeftCurrent(false);
                    triplets[tx - 1, y + 1] = triplets[tx - 1, y + 1].UUM();
                    triplets[tx, y + 1] = triplets[tx, y + 1].MMU();
                    break;
                case 1:
                    if (!t.MiddleCurrent)
                        return false;
                    triplets[tx, y - 1] = triplets[tx, y - 1].MMM();
                    triplets[tx, y] = t.SetMiddleCurrent(false);
                    triplets[tx, y + 1] = triplets[tx, y + 1].MMM();
                    break;
                case 2:
                    if (!t.RightCurrent)
                        return false;
                    triplets[tx, y - 1] = triplets[tx, y - 1].UMM();
                    triplets[tx + 1, y - 1] = triplets[tx + 1, y - 1].MUU();
                    triplets[tx, y] = t.SetRightCurrent(false);
                    triplets[tx + 1, y] = triplets[tx + 1, y].MUU();
                    triplets[tx, y + 1] = triplets[tx, y + 1].UMM();
                    triplets[tx + 1, y + 1] = triplets[tx + 1, y + 1].MUU();
                    break;
            }
            return true;
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
                    {
                        if (BecomeAlive((int)x, (int)y))
                            changes.Add(((int)x / 3, (int)y));
                    }
                    else
                    {
                        if (BecomeDead((int)x, (int)y))
                            changes.Add(((int)x / 3, (int)y));
                    }
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
            // First pass: for the previous changes and all their neighbours, record their
            // new states. If the new state changed, make a note of that.

            // This list might have duplicates.
            var currentChanges = new List<(int, int)>();

            foreach ((int cx, int cy) in changes)
            {
                int minx = Max(cx - 1, 1);
                int maxx = Min(cx + 2, width - 1);
                int miny = Max(cy - 1, 1);
                int maxy = Min(cy + 2, height - 1);
                for (int y = miny; y < maxy; y += 1)
                {
                    for (int x = minx; x < maxx; x += 1)
                    {
                        Triplet c = triplets[x, y];
                        Triplet t = c;
                        int lc = t.LeftCount;
                        int mc = t.MiddleCount;
                        int rc = t.RightCount;
                        t = t.SetLeftNext(lc == 3 | t.LeftCurrent & lc == 2);
                        t = t.SetMiddleNext(mc == 3 | t.MiddleCurrent & mc == 2);
                        t = t.SetRightNext(rc == 3 | t.RightCurrent & rc == 2);
                        if (t.Changed)
                        {
                            currentChanges.Add((x, y));
                            triplets[x, y] = t;
                        }
                    }
                }
            }

            // We're done with the previous changes list, so throw it away.
            changes.Clear();

            // Second pass: all the triplets that were marked as having changed need
            // to have their neighbour counts updated.

            foreach ((int x, int y) in currentChanges)
            {
                Triplet t = triplets[x, y];

                // If we've already done this one before, no need to do it again.

                if (!t.Changed)
                    continue;

                bool changed = false;
                if (t.LeftNext)
                    changed |= BecomeAlive(x * 3, y);
                else
                    changed |= BecomeDead(x * 3, y);

                if (t.MiddleNext)
                    changed |= BecomeAlive(x * 3 + 1, y);
                else
                    changed |= BecomeDead(x * 3 + 1, y);

                if (t.RightNext)
                    changed |= BecomeAlive(x * 3 + 2, y);
                else
                    changed |= BecomeDead(x * 3 + 2, y);
                Debug.Assert(changed);
                changes.Add((x, y));
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
