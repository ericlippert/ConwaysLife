using System;
using System.Collections.Generic;
using static System.Math;

namespace ConwaysLife
{
    // Stafford's algorithm with the "set the next state bits" implemented as a
    // lookup table, but with the second pass still normal.

    static class TripletLookup
    {
        public static Triplet[] lookup;

        static TripletLookup()
        {
            // Some of these are impossible, but who cares?
            lookup = new Triplet[1 << 12];

            for (int left = 0; left < 2; left += 1)
                for (int middle = 0; middle < 2; middle += 1)
                    for (int right = 0; right < 2; right += 1)
                        for (int lc = 0; lc < 8; lc += 1)
                            for (int mc = 0; mc < 7; mc += 1)
                                for (int rc = 0; rc < 8; rc += 1)
                                {
                                    Triplet t = new Triplet()
                                        .SetLeftCurrent(left == 1)
                                        .SetMiddleCurrent(middle == 1)
                                        .SetRightCurrent(right == 1)
                                        .SetLeftCountRaw(lc)
                                        .SetMiddleCountRaw(mc)
                                        .SetRightCountRaw(rc)
                                        .SetLeftNext((lc + middle == 3) | (left == 1) & (lc + middle == 2))
                                        .SetMiddleNext((left + mc + right == 3) | (middle == 1) & (left + mc + right == 2))
                                        .SetRightNext((middle + rc == 3) | (right == 1) & (middle + rc == 2));
                                    lookup[t.State1] = t;
                                }

        }
    }

    class StaffordLookup : ILife
    {
        // We're going to keep the top and bottom edge triplets dead,
        // so this gives us 256 live rows.
        private int height = 258;

        // This is the width in triplets. That gives us 264 cells, but 
        // we'll keep the left and right triplets dead, so that's 258
        // live columns of cells.

        private int width = 88;
        private Triplet[,] triplets;
        private List<(int, int)> changes;

        public StaffordLookup()
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

        private void BecomeAlive(int x, int y)
        {
            int tx = x / 3;
            Triplet t = triplets[tx, y];
            // TODO: Fix these to do it all in one operation
            switch (x % 3)
            {
                case 0:
                    if (t.LeftCurrent)
                        return;
                    // Left is about to be born
                    t = t.SetLeftCurrent(true);
                    triplets[tx, y - 1] = triplets[tx, y - 1].UPU().PUU();
                    triplets[tx, y + 1] = triplets[tx, y + 1].UPU().PUU();
                    triplets[tx - 1, y - 1] = triplets[tx - 1, y - 1].UUP();
                    triplets[tx - 1, y] = triplets[tx - 1, y].UUP();
                    triplets[tx - 1, y + 1] = triplets[tx - 1, y + 1].UUP();
                    break;
                case 1:
                    if (t.MiddleCurrent)
                        return;
                    // Middle is about to be born
                    t = t.SetMiddleCurrent(true);
                    triplets[tx, y - 1] = triplets[tx, y - 1].UPU().PUU().UUP();
                    triplets[tx, y + 1] = triplets[tx, y + 1].UPU().PUU().UUP();
                    break;
                case 2:
                    if (t.RightCurrent)
                        return;
                    // Right is about to be born
                    t = t.SetRightCurrent(true);
                    triplets[tx, y - 1] = triplets[tx, y - 1].UPU().UUP();
                    triplets[tx, y + 1] = triplets[tx, y + 1].UPU().UUP();
                    triplets[tx + 1, y - 1] = triplets[tx + 1, y - 1].PUU();
                    triplets[tx + 1, y] = triplets[tx + 1, y].PUU();
                    triplets[tx + 1, y + 1] = triplets[tx + 1, y + 1].PUU();
                    break;
            }
            triplets[tx, y] = t;
        }

        private void BecomeDead(int x, int y)
        {
            int tx = x / 3;
            Triplet t = triplets[tx, y];
            // TODO: Fix these to do it all in one operation
            switch (x % 3)
            {
                case 0:
                    if (!t.LeftCurrent)
                        return;
                    t = t.SetLeftCurrent(false);
                    triplets[tx, y - 1] = triplets[tx, y - 1].UMU().MUU();
                    triplets[tx, y + 1] = triplets[tx, y + 1].UMU().MUU();
                    triplets[tx - 1, y - 1] = triplets[tx - 1, y - 1].UUM();
                    triplets[tx - 1, y] = triplets[tx - 1, y].UUM();
                    triplets[tx - 1, y + 1] = triplets[tx - 1, y + 1].UUM();
                    break;
                case 1:
                    if (!t.MiddleCurrent)
                        return;
                    t = t.SetMiddleCurrent(false);
                    triplets[tx, y - 1] = triplets[tx, y - 1].UMU().MUU().UUM();
                    triplets[tx, y + 1] = triplets[tx, y + 1].UMU().MUU().UUM();
                    break;
                case 2:
                    if (!t.RightCurrent)
                        return;
                    t = t.SetRightCurrent(false);
                    triplets[tx, y - 1] = triplets[tx, y - 1].UMU().UUM();
                    triplets[tx, y + 1] = triplets[tx, y + 1].UMU().UUM();
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
                    {
                        if (!this[x, y])
                        {
                            BecomeAlive((int)x, (int)y);
                            changes.Add(((int)x / 3, (int)y));
                        }
                    }
                    else
                    {
                        if (this[x, y])
                        {
                            BecomeDead((int)x, (int)y);
                            changes.Add(((int)x / 3, (int)y));
                        }
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
            var previousChanges = changes;
            changes = new List<(int, int)>();

            // TODO: Fix up change list algorithm here

            foreach ((int cx, int cy) in previousChanges)
            {
                int minx = Max(cx - 1, 1);
                int maxx = Min(cx + 2, width - 1);
                int miny = Max(cy - 1, 1);
                int maxy = Min(cy + 2, height - 1);
                for (int y = miny; y < maxy; y += 1)
                {
                    for (int tx = minx; tx < maxx; tx += 1)
                    {
                        triplets[tx, y] = TripletLookup.lookup[triplets[tx, y].State1];
                    }
                }
            }

            foreach ((int cx, int cy) in previousChanges)
            {
                int minx = Max(cx - 1, 1);
                int maxx = Min(cx + 2, width - 1);
                int miny = Max(cy - 1, 1);
                int maxy = Min(cy + 2, height - 1);
                for (int y = miny; y < maxy; y += 1)
                {
                    for (int tx = minx; tx < maxx; tx += 1)
                    {
                        bool changed = false;

                        Triplet t = triplets[tx, y];
                        if (t.LeftCurrent & !t.LeftNext)
                        {
                            BecomeDead(tx * 3, y);
                            changed = true;
                        }
                        else if (!t.LeftCurrent & t.LeftNext)
                        {
                            BecomeAlive(tx * 3, y);
                            changed = true;
                        }

                        if (t.MiddleCurrent & !t.MiddleNext)
                        {
                            BecomeDead(tx * 3 + 1, y);
                            changed = true;
                        }
                        else if (!t.MiddleCurrent & t.MiddleNext)
                        {
                            BecomeAlive(tx * 3 + 1, y);
                            changed = true;
                        }

                        if (t.RightCurrent & !t.RightNext)
                        {
                            BecomeDead(tx * 3 + 2, y);
                            changed = true;
                        }
                        else if (!t.RightCurrent & t.RightNext)
                        {
                            BecomeAlive(tx * 3 + 2, y);
                            changed = true;
                        }
                        if (changed)
                            changes.Add((tx, y));
                    }
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
