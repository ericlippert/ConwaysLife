namespace ConwaysLife
{
    // This class manages the various caches used by Gosper's algorithm.
    static class CacheManager
    {
        public static Memoizer<int, Quad> EmptyMemoizer { get; set; }

        public static Memoizer<(Quad, Quad, Quad, Quad), Quad> MakeQuadMemoizer { get; set; }
    }
}
