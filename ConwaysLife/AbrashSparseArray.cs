using System;
using System.Diagnostics;
using static System.Math;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ConwaysLife
{
    class SparseCellArray
    {
        private Dictionary<(long, long), Cell> dict = new Dictionary<(long, long), Cell>();

        public Cell this[long x, long y]
        {
            get
            {
                Cell cell;
                if (dict.TryGetValue((x, y), out cell))
                    return cell;
                return new Cell();
            }
            set
            {
                if (value.IsZero)
                    dict.Remove((x, y));
                else
                    dict[(x, y)] = value;
            }
        }


    }

    class AbrashSparseArray : ILife
    {
        private SparseCellArray cells;

        private HashSet<(long, long)> recentBirths;
        private HashSet<(long, long)> recentDeaths;

        public AbrashSparseArray()
        {
            Clear();
        }

        public void Clear()
        {
            cells = new SparseCellArray();
            recentBirths = new HashSet<(long, long)>();
            recentDeaths = new HashSet<(long, long)>();
        }

        private void BecomeAlive(long x, long y)
        {
            Debug.Assert(!cells[x, y].State);

            cells[x - 1, y - 1] = cells[x - 1, y - 1].Increment();
            cells[x - 1, y] = cells[x - 1, y].Increment();
            cells[x - 1, y + 1] = cells[x - 1, y + 1].Increment();
            cells[x, y - 1] = cells[x, y - 1].Increment();
            cells[x, y] = cells[x, y].MakeAlive();
            cells[x, y + 1] = cells[x, y + 1].Increment();
            cells[x + 1, y - 1] = cells[x + 1, y - 1].Increment();
            cells[x + 1, y] = cells[x + 1, y].Increment();
            cells[x + 1, y + 1] = cells[x + 1, y + 1].Increment();
        }

        private void BecomeDead(long x, long y)
        {
            Debug.Assert(cells[x, y].State);

            cells[x - 1, y - 1] = cells[x - 1, y - 1].Decrement();
            cells[x - 1, y] = cells[x - 1, y].Decrement();
            cells[x - 1, y + 1] = cells[x - 1, y + 1].Decrement();
            cells[x, y - 1] = cells[x, y - 1].Decrement();
            cells[x, y] = cells[x, y].MakeDead();
            cells[x, y + 1] = cells[x, y + 1].Decrement();
            cells[x + 1, y - 1] = cells[x + 1, y - 1].Decrement();
            cells[x + 1, y] = cells[x + 1, y].Decrement();
            cells[x + 1, y + 1] = cells[x + 1, y + 1].Decrement();
        }

        public bool this[long x, long y]
        {
            get => cells[x, y].State;
            set
            {
                if (value & !cells[x, y].State)
                {
                    BecomeAlive(x, y);
                    recentBirths.Add((x, y));
                }
                else if (!value & cells[x, y].State)
                {
                    BecomeDead(x, y);
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
                    Cell cell = cells[cx, cy];
                    bool state = cell.State;
                    int count = cell.Count;
                    if (state & count != 2 & count != 3)
                        recentDeaths.Add((cx, cy));
                    else if (!state & count == 3)
                        recentBirths.Add((cx, cy));
                }
            }
        }

        public void Step()
        {
            var previousBirths = recentBirths;
            recentBirths = new HashSet<(long, long)>();
            var previousDeaths = recentDeaths;
            recentDeaths = new HashSet<(long, long)>();

            foreach ((long x, long y) in previousBirths)
                CheckCellAndNeighbours(x, y);
            foreach ((long x, long y) in previousDeaths)
                CheckCellAndNeighbours(x, y);

            foreach ((long x, long y) in recentBirths)
                BecomeAlive(x, y);
            foreach ((long x, long y) in recentDeaths)
                BecomeDead(x, y);
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
    }
}
