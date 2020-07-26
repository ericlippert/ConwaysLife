using System;
using System.Collections.Generic;
using System.Linq;

// A simple memoizer. I note that a cache without an expiration policy
// is a memory leak; we will come back to this point later in the series.
sealed class Memoizer<A, R>
{
    private readonly Dictionary<A, R> dict;
#if MEMOIZER_STATS
    private readonly Dictionary<A, int> stats;
#endif
    public Func<A, R> MemoizedFunc { get; }
    public Memoizer(Func<A, R> f)
    {
        dict = new Dictionary<A, R>();
#if MEMOIZER_STATS
        stats = new Dictionary<A, int>();
#endif
        MemoizedFunc = (A a) =>
        {
            if (!dict.TryGetValue(a, out R r))
            {
#if MEMOIZER_STATS
                stats.Add(a, 1);
#endif
                r = f(a);
                dict.Add(a, r);
                return r;
            }
#if MEMOIZER_STATS
            stats[a] += 1;
#endif
            return r;
        };
    }
    public int Count => dict.Count;
#if MEMOIZER_STATS
    public string Report() => 
        string.Join("\n", from v in stats.Values group v by v into g select $"{g.Key},{g.Count()}");
#endif
}