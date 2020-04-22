using System;
using static System.Math;

namespace ConwaysLife
{
    // A straightforward naive implementation of Life.
    
    // The "infinite" board is simulated by a 256 x 256
    // array of Booleans; any cells outside the square
    // bounded by (0, 0) and (255, 255) are considered 
    // permanently dead.

    class BoolArrayLife : ILife
    {
        private int height = 256;
        private int width = 256;
        private bool[,] cells;

        public BoolArrayLife()
        {
            Clear();
        }

        public void Clear()
        {
            cells = new bool[width, height];
        }

        private bool IsValidPoint(long x, long y) => 
            0 <= x && x < width && 0 <= y && y < height;

        public bool this[long x, long y]
        {
            get
            {
                if (IsValidPoint(x, y))
                    return cells[x, y];
                return false;
            }
            set
            {
                if (IsValidPoint(x, y))
                    cells[x, y] = value;
            }
        }

        public bool this[LifePoint v]
        {
            get => this[v.X, v.Y];
            set => this[v.X, v.Y] = value;
        }

        private int LivingNeighbors(int x, int y)
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

        public void Step()
        {
            // We cannot mutate the existing array in-place because
            // that changes the counts of living neighbors. Instead
            // we make a new array and figure out which cells are
            // alive in the next iteration.

            bool[,] newCells = new bool[width, height];
            for (int y = 0; y < height; y += 1)
            {
                for (int x = 0; x < width; x += 1)
                {
                    int count = LivingNeighbors(x, y);
                    // If there are three living neighbors, the cell is
                    // always alive in the next iteration. 
                    // If the cell is alive and there are two living neighbors,
                    // it stays alive.
                    // Under all other circumstances, the cell is dead in the 
                    // next round.
                    newCells[x, y] = count == 3 || (cells[x, y] && count == 2);
                }
            }
            cells = newCells;
        }

        public void Draw(LifeRect rect, Action<LifePoint> setPixel)
        {
            long xmin = Max(0, rect.X);
            long xmax = Min(width, rect.X + rect.Width);
            long ymin = Max(0, rect.Y - rect.Height + 1);
            long ymax = Min(height, rect.Y + 1);
            for (long y = ymin; y < ymax; y += 1)
                for (long x = xmin; x < xmax; x += 1)
                    if (cells[x, y])
                        setPixel(new LifePoint(x, y));
        }
    }
}
