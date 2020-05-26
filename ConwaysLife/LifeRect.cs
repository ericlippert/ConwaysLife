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

        // It's interview question time. Given two rectangles, do they overlap?
        //
        // Two rectangles can only overlap if the line segments that are their vertical
        // sides overlap AND the line segments that are their horizontal sides overlap,
        // so we can reduce it to a smaller problem:

        public bool Overlaps(LifeRect r)
        {
            return Overlaps(this.X, this.Width, r.X, r.Width) &&
                Overlaps(this.Y - this.Height, this.Height, r.Y - r.Height, r.Height);
        }

        // Does the line segment which begins at p1 of length l1
        // overlap the line segment which begins at p2 of length l2?
        private static bool Overlaps(long p1, long l1, long p2, long l2)
        {
            // If p1 is to the right of the end of the second segment, then no.
            if (p1 >= p2 + l2) return false;
            // p1 is to the left of the end of the second segment.
            // If p1 is to the right of the start, then the segments overlap.
            if (p1 >= p2) return true;
            // p1 is to the left of the start. If the end of the first line segment
            // is to the right of the start of the second, then they overlap.
            if (p1 + l1 > p2) return true;
            // They do not overlap.
            return false;
        }

    }
}
