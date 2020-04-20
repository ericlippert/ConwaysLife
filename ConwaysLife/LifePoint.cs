namespace ConwaysLife
{
    // This represents a point in the infinite Life grid, but we'll use 
    // longs for convenience, limiting the size of the grid to a mere 10^38
    // cells. Redoing this project so that all the quantities are BigIntegers
    // is left as an exercise.

    struct LifePoint
    {
        public long X { get; }
        public long Y { get; }
        public LifePoint(long x, long y)
        {
            X = x;
            Y = y;
        }
    }
}
