namespace ConwaysLife
{
    // This represents a rectangular portion of the infinite grid of Life cells.
    struct LifeRect
    {
        // This is the upper left corner of the rectangle.
        public LifePoint Corner { get; }
        public long X { get => Corner.X; }
        public long Y { get => Corner.Y; }
        public long Width { get; }
        public long Height { get; }
        public LifeRect(LifePoint corner, long width, long height)
        {
            Corner = corner;
            Width = width;
            Height = height;
        }
        public LifeRect(long x, long y, long width, long height) : this(new LifePoint(x, y), width, height)
        {
        }
    }
}
