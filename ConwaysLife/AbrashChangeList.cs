using System;
using System.Diagnostics;
using static System.Math;
using System.Collections.Generic;

namespace ConwaysLife
{
    class AbrashChangeList : ILife
    {
        // This is the same as Abrash's algorithm where we track the current
        // neighbour counts, but we add some new state: a list of every cell
        // that changed in the previous tick. The only cells which can change
        // in the next tick are those that either changed, or were a neighbour
        // of a cell that changed.
        //
        // Of course, we now need to ensure that we compute a new, ideally
        // deduplicated change list on every tick!

        private const int height = 258;
        private const int width = 258;
        private Cell[,] cells;

        private List<(int, int)> changes;

        public AbrashChangeList()
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

            // This method is idempotent; if the cell is already alive it does nothing.

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

            // This method is idempotent; if the cell is already dead it does nothing.

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
            Cell[,] clone = (Cell[,])cells.Clone();
            var previousChanges = changes;

            // Start a new change list.
            changes = new List<(int, int)>();

            // The only cells which will change on this tick are 
            // cells which changed on the previous tick, or neighbour
            // of cells which changed on the previous tick. Since we
            // have a complete list of recent changes, just run down it.
            foreach ((int cx, int cy) in previousChanges)
            {
                int minx = Max(cx - 1, 1);
                int maxx = Min(cx + 2, width - 1);
                int miny = Max(cy - 1, 1);
                int maxy = Min(cy + 2, height - 1);
                for (int y = miny; y < maxy; y += 1)
                {
                    for (int x = minx; x < maxx; x += 1)
                    {
                        Cell cell = clone[x, y];
                        int count = cell.Count;

                        // We might end up calling BecomeAlive or BecomeDead
                        // on the same cells multiple times, but that's OK.
                        // The new change list will be deduplicated because 
                        // repeated operations are ignored.
                        if (cell.State)
                        {
                            if (count != 2 && count != 3)
                                BecomeDead(x, y);
                        }
                        else if (count == 3)
                            BecomeAlive(x, y);
                    }
                }
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
