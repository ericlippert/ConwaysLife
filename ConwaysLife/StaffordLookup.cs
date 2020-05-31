namespace ConwaysLife
{
    static class TripletLookup
    {
        public static Triplet[] lookup;
        public static bool[] changed;

        static TripletLookup()
        {
            // Some of these are impossible, but who cares?
            lookup = new Triplet[1 << 12];
            changed = new bool[1 << 12];

            for (int left = 0; left < 2; left += 1)
                for (int middle = 0; middle < 2; middle += 1)
                    for (int right = 0; right < 2; right += 1)
                        for (int lc = 0; lc < 8; lc += 1)
                            for (int mc = 0; mc < 7; mc += 1)
                                for (int rc = 0; rc < 8; rc += 1)
                                {
                                    Triplet t = new Triplet()
                                        .SetLeftCurrent(left == 1)
                                        .SetMiddleCurrent(middle == 1)
                                        .SetRightCurrent(right == 1)
                                        .SetLeftCountRaw(lc)
                                        .SetMiddleCountRaw(mc)
                                        .SetRightCountRaw(rc)
                                        .SetLeftNext((lc + middle == 3) | (left == 1) & (lc + middle == 2))
                                        .SetMiddleNext((left + mc + right == 3) | (middle == 1) & (left + mc + right == 2))
                                        .SetRightNext((middle + rc == 3) | (right == 1) & (middle + rc == 2));
                                    lookup[t.LookupKey1] = t;
                                    changed[t.LookupKey1] = t.Changed;
                                }

        }
    }
}
