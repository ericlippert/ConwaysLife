using System;
using System.Diagnostics;
using static System.Math;

namespace ConwaysLife
{
    // An adaptation of Michael Abrash's algorithm to C#.
    //
    // http://www.jagregory.com/abrash-black-book/#chapter-17-the-game-of-life
    //
    // The basic idea is: every cell is a byte which maintains its own state
    // plus a count of its number of living neighbors. When the cell changes
    // the neighbor counts of the eight neighbors must be updated, but those
    // changes are relatively rare and it saves us from having to do the eight
    // additions per cell in the naive implementation.
    //
    // Abrash's original implementation assumes "wrap around" edges; I've
    // eliminated that feature and instead am using the "rectangle of death"
    // method.

    struct Cell
    {
        private readonly byte cell;
        public Cell(byte cell)
        {
            this.cell = cell;
        }

        // Bit 4 is the state of the cell; bits 0 through 3 are the 
        // number of living neighbors.
        private const int state = 4;

        public bool State => (cell & (1 << state)) != 0;
        public int Count => cell & ~(1 << state);

        // Dead cell with all dead neighbors.
        public bool AllDead => cell == 0;

        public Cell MakeAlive() => new Cell((byte)(cell | (1 << state)));
        public Cell MakeDead() => new Cell((byte)(cell & ~(1 << state)));

        // We don't have to mask out the state bit to do an increment or
        // decrement!
        public Cell Increment()
        {
            Debug.Assert(Count < 8);
            return new Cell((byte)(cell + 1));
        }
        public Cell Decrement()
        {
            Debug.Assert(Count > 0);
            return new Cell((byte)(cell - 1));
        }
    }

    class Abrash : ILife
    {
        // We're keeping a ring of dead cells around the board; grow the
        // board two in both directions to account for that, so that we 
        // still have 256 x 256 cells computed per tick.
        private const int height = 258;
        private const int width = 258;
        private Cell[,] cells;

        public Abrash()
        {
            Clear();
        }

        public void Clear()
        {
            cells = new Cell[width, height];
        }

        // We ensure that the four borders of the array of cells are
        // always dead.
        private bool IsValidPoint(long x, long y) =>
            0 < x && x < width - 1 && 0 < y && y < height - 1;

        private void BecomeAlive(long x, long y)
        {
            // Make a dead cell come alive.
            Debug.Assert(IsValidPoint(x, y));
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
            // Make a live cell die.
            Debug.Assert(IsValidPoint(x, y));
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
                Cell c = cells[x, y];
                // No change? Bail out.
                if (c.State == value)
                    return;
                if (value)
                    BecomeAlive(x, y);
                else
                    BecomeDead(x, y);
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
            for (int y = 1; y < height - 1; y += 1)
            {
                for (int x = 1; x < width - 1; x += 1)
                {
                    Cell cell = clone[x, y];
                    if (cell.AllDead)
                        continue;

                    int count = cell.Count;
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
