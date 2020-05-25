using System;
using System.Collections.Generic;

// A simple memoizer. I note that a cache without an expiration policy
// is a memory leak; we will come back to this point later in the series.
sealed class Memoizer<A, R>
{
    private readonly Dictionary<A, R> dict;
    public Func<A, R> MemoizedFunc { get; }
    public Memoizer(Func<A, R> f)
    {
        dict = new Dictionary<A, R>();
        MemoizedFunc = (A a) =>
        {
            if (!dict.TryGetValue(a, out R r))
            {
                r = f(a);
                dict.Add(a, r);
            }
            return r;
        };
    }
}