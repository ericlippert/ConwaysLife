using System;
using System.Collections.Generic;
using static System.Math;

namespace ConwaysLife
{
    // This is a precursor to Stafford's algorithm that implements the change list
    // optimization but not any further optimizations.

    class StaffordChangeList : ILife
    {
        private int height = 258;
        private int width = 88;
        private Triplet[,] triplets;
        private List<(int, int)> changes;

        public StaffordChangeList()
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
            // TODO: Make this idempotent
            // TODO: Add to change list
            int tx = x / 3;
            Triplet t = triplets[tx, y];

            switch (x % 3)
            {
                case 0:
                    if (t.LeftCurrent)
                        return;
                    // Left is about to be born
                    t = t.SetLeftCurrent(true);
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
                    t = t.SetMiddleCurrent(true);
                    triplets[tx, y - 1] = triplets[tx, y - 1].PPP();
                    triplets[tx, y + 1] = triplets[tx, y + 1].PPP();
                    break;
                case 2:
                    if (t.RightCurrent)
                        return;
                    // Right is about to be born
                    t = t.SetRightCurrent(true);
                    triplets[tx, y - 1] = triplets[tx, y - 1].UPP();
                    triplets[tx, y + 1] = triplets[tx, y + 1].UPP();
                    triplets[tx + 1, y - 1] = triplets[tx + 1, y - 1].PUU();
                    triplets[tx + 1, y] = triplets[tx + 1, y].PUU();
                    triplets[tx + 1, y + 1] = triplets[tx + 1, y + 1].PUU();
                    break;
            }
            triplets[tx, y] = t;
        }

        private void BecomeDead(int x, int y)
        {
            // TODO: Make this idempotent
            // TODO: Add to change list
            int tx = x / 3;
            Triplet t = triplets[tx, y];

            switch (x % 3)
            {
                case 0:
                    if (!t.LeftCurrent)
                        return;
                    t = t.SetLeftCurrent(false);
                    triplets[tx, y - 1] = triplets[tx, y - 1].MMU();
                    triplets[tx, y + 1] = triplets[tx, y + 1].MMU();
                    triplets[tx - 1, y - 1] = triplets[tx - 1, y - 1].UUM();
                    triplets[tx - 1, y] = triplets[tx - 1, y].UUM();
                    triplets[tx - 1, y + 1] = triplets[tx - 1, y + 1].UUM();
                    break;
                case 1:
                    if (!t.MiddleCurrent)
                        return;
                    t = t.SetMiddleCurrent(false);
                    triplets[tx, y - 1] = triplets[tx, y - 1].MMM();
                    triplets[tx, y + 1] = triplets[tx, y + 1].MMM();
                    break;
                case 2:
                    if (!t.RightCurrent)
                        return;
                    t = t.SetRightCurrent(false);
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
                        if (c.CurrentState != t.NextState)
                        {
                            currentChanges.Add((x, y));
                            triplets[x, y] = t;
                        }
                    }
                }
            }

            // We're done with the previous changes list, so throw it away.
            changes.Clear();

            foreach ((int x, int y) in currentChanges)
            {
                bool changed = false;

                Triplet t = triplets[x, y];
                if (t.LeftCurrent & !t.LeftNext)
                {
                    BecomeDead(x * 3, y);
                    changed = true;
                }
                else if (!t.LeftCurrent & t.LeftNext)
                {
                    BecomeAlive(x * 3, y);
                    changed = true;
                }

                if (t.MiddleCurrent & !t.MiddleNext)
                {
                    BecomeDead(x * 3 + 1, y);
                    changed = true;
                }
                else if (!t.MiddleCurrent & t.MiddleNext)
                {
                    BecomeAlive(x * 3 + 1, y);
                    changed = true;
                }

                if (t.RightCurrent & !t.RightNext)
                {
                    BecomeDead(x * 3 + 2, y);
                    changed = true;
                }
                else if (!t.RightCurrent & t.RightNext)
                {
                    BecomeAlive(x * 3 + 2, y);
                    changed = true;
                }
                if (changed)
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
