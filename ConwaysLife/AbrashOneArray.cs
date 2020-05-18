using System;
using System.Diagnostics;
using static System.Math;
using System.Collections.Generic;

namespace ConwaysLife
{
    class AbrashOneArray : ILife
    {
        private const int height = 258;
        private const int width = 258;
        private Cell[,] cells;

        private List<(int, int)> changes;

        public AbrashOneArray()
        {
            Clear();
        }

        public void Clear()
        {
            cells = new Cell[width, height];
            changes = new List<(int, int)>();
        }

        // We ensure that the four borders of the array of cells are
        // always dead.
        private bool IsValidPoint(long x, long y) =>
            0 < x && x < width - 1 && 0 < y && y < height - 1;

        private void BecomeAlive(int x, int y)
        {
            // Make a cell alive; if it is not already, update neighbour counts
            // and remember the change.
            Debug.Assert(IsValidPoint(x, y));
            if (cells[x, y].State)
                return;

            cells[x - 1, y - 1] = cells[x - 1, y - 1].Increment();
            cells[x - 1, y] = cells[x - 1, y].Increment();
            cells[x - 1, y + 1] = cells[x - 1, y + 1].Increment();
            cells[x, y - 1] = cells[x, y - 1].Increment();
            cells[x, y] = cells[x, y].MakeAlive();
            cells[x, y + 1] = cells[x, y + 1].Increment();
            cells[x + 1, y - 1] = cells[x + 1, y - 1].Increment();
            cells[x + 1, y] = cells[x + 1, y].Increment();
            cells[x + 1, y + 1] = cells[x + 1, y + 1].Increment();
            changes.Add((x, y));
        }

        private void BecomeDead(int x, int y)
        {
            // Make a cell dead; if it is not already, update neighbour counts,
            // and remember the change.
            Debug.Assert(IsValidPoint(x, y));
            if (!cells[x, y].State)
                return;

            cells[x - 1, y - 1] = cells[x - 1, y - 1].Decrement();
            cells[x - 1, y] = cells[x - 1, y].Decrement();
            cells[x - 1, y + 1] = cells[x - 1, y + 1].Decrement();
            cells[x, y - 1] = cells[x, y - 1].Decrement();
            cells[x, y] = cells[x, y].MakeDead();
            cells[x, y + 1] = cells[x, y + 1].Decrement();
            cells[x + 1, y - 1] = cells[x + 1, y - 1].Decrement();
            cells[x + 1, y] = cells[x + 1, y].Decrement();
            cells[x + 1, y + 1] = cells[x + 1, y + 1].Decrement();
            changes.Add((x, y));
        }

        public bool this[long x, long y]
        {
            get
            {
                if (IsValidPoint(x, y))
                    return cells[x, y].State;
                return false;
            }
            set
            {
                if (!IsValidPoint(x, y))
                    return;
                if (value)
                    BecomeAlive((int)x, (int)y);
                else
                    BecomeDead((int)x, (int)y);
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

            // First pass:
            // 
            // For each *previously* changed cell and all the neighbors of 
            // those cells, compute whether the cell will change now
            // or not.  If not, we can ignore it. If it does change,
            // add it to the *current* changes list, and set the next
            // bit indicating what the new value will be.

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
                        Cell cell = cells[x, y];

                        int count = cell.Count;
                        bool state = cell.State;
                        bool newState = count == 3 | count == 2 & state;
                        if (state & !newState)
                        {
                            currentChanges.Add((x, y));
                            cells[x, y] = cell.NextDead();
                        }
                        else if (!state & newState)
                        {
                            currentChanges.Add((x, y));
                            cells[x, y] = cell.NextAlive();
                        }
                    }
                }

            }

            // We're done with the previous changes list, so throw it away.
            changes.Clear();

            // Second pass:
            //
            // For all the cells that were marked as needing changes, change them.
            // Since BecomeAlive and BecomeDead are idempotent, we will ensure
            // that the change list is deduplicated.
           
            foreach ((int x, int y) in currentChanges)
            {
                if (cells[x, y].Next)
                    BecomeAlive(x, y);
                else
                    BecomeDead(x, y);
            }
        }

        public void Draw(LifeRect rect, Action<LifePoint> setPixel)
        {
            long xmin = Max(1, rect.X);
            long xmax = Min(width - 1, rect.X + rect.Width);
            long ymin = Max(1, rect.Y - rect.Height + 1);
            long ymax = Min(height - 1, rect.Y + 1);
            for (long y = ymin; y < ymax; y += 1)
                for (long x = xmin; x < xmax; x += 1)
                    if (cells[x, y].State)
                        setPixel(new LifePoint(x, y));
        }
    }
}
