using System;
using System.Collections.Generic;

namespace ConwaysLife
{
    // A simple sparse array with change list implementation.
    //
    // We keep three sets of points:
    //
    // * all currently living cells -- if the point is in the set it is alive, otherwise, dead.
    // * all recently born cells
    // * all recently died cells
    //
    // The recent births and deaths act as the change list for the next tick, when we 
    // accumulate a new recent births and deaths collection.

    class SparseArray : ILife, IReport
    {
        private HashSet<(long, long)> living;
        private HashSet<(long, long)> recentBirths;
        private HashSet<(long, long)> recentDeaths;
        private int generation;

        public SparseArray()
        {
            Clear();
        }

        public void Clear()
        {
            living = new HashSet<(long, long)>();
            recentBirths = new HashSet<(long, long)>();
            recentDeaths = new HashSet<(long, long)>();
            generation = 0;
        }

        public bool this[long x, long y]
        {
            get => living.Contains((x, y));
            set
            {
                if (value)
                {
                    if (living.Add((x, y)))
                        recentBirths.Add((x, y));
                }
                else
                {
                    if (living.Remove((x, y)))
                        recentDeaths.Add((x, y));
                }
            }
        }

        public bool this[LifePoint v]
        {
            get => this[v.X, v.Y];
            set => this[v.X, v.Y] = value;
        }

        private void CheckCellAndNeighbours(long x, long y)
        {
            for (int iy = -1; iy < 2; iy += 1)
            {
                long cy = y + iy;
                for (int ix = -1; ix < 2; ix += 1)
                {
                    long cx = x + ix;
                    bool state = this[cx, cy];
                    int nw = this[cx - 1, cy - 1] ? 1 : 0;
                    int n = this[cx, cy - 1] ? 1 : 0;
                    int ne = this[cx + 1, cy - 1] ? 1 : 0;
                    int w = this[cx - 1, cy] ? 1 : 0;
                    int e = this[cx + 1, cy] ? 1 : 0;
                    int sw = this[cx - 1, cy + 1] ? 1 : 0;
                    int s = this[cx, cy + 1] ? 1 : 0;
                    int se = this[cx + 1, cy + 1] ? 1 : 0;
                    int count = nw + n + ne + w + e + sw + s + se;
                    if (state & count != 2 & count != 3)
                        recentDeaths.Add((cx, cy));
                    else if (!state & count == 3)
                        recentBirths.Add((cx, cy));
                }
            }
        }

        public void Step()
        {
            // For each *previously* changed cell and all the neighbors of 
            // those cells, compute whether the cell will change now
            // or not.  If not, we can ignore it. If it does change,
            // add it to the "recent births" or "recent deaths" sets.

            var previousBirths = recentBirths;
            recentBirths = new HashSet<(long, long)>();
            var previousDeaths = recentDeaths;
            recentDeaths = new HashSet<(long, long)>();

            foreach ((long x, long y) in previousBirths)
                CheckCellAndNeighbours(x, y);
            foreach ((long x, long y) in previousDeaths)
                CheckCellAndNeighbours(x, y);

            living.UnionWith(recentBirths);
            living.ExceptWith(recentDeaths);
            generation += 1;
        }

        public void Step(int speed)
        {
            for (int i = 0; i < 1L << speed; i += 1)
                Step();
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

        public string Report() =>
            $"gen {generation}\n{living.Count} alive\n{recentBirths.Count + recentDeaths.Count} change\n";
    }
}
