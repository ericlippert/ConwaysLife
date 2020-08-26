using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

// A simple memoizer. I note that a cache without an expiration policy
// is a memory leak; we will come back to this point later in the series.
sealed class Memoizer<A, R>
{
    [Conditional("MEMOIZER_STATS")]
    private void RecordHit(A a)
    {
        if (hits == null)
            hits = new Dictionary<A, int>();
        if (hits.TryGetValue(a, out int c))
            hits[a] = c + 1;
        else
            hits.Add(a, 1);
    }

    private Dictionary<A, R> dict;
    private Dictionary<A, int> hits;
    private readonly Func<A, R> f;

    public R MemoizedFunc(A a)
    {
        RecordHit(a);
        if (!dict.TryGetValue(a, out R r))
        {
            r = f(a);
            dict.Add(a, r);
        }
        return r;
    }
    
    public Memoizer(Func<A, R> f)
    {
        this.dict = new Dictionary<A, R>();
        this.f = f;        
    }

    public void Clear(Dictionary<A, R> newDict = null)
    {
        dict = newDict ?? new Dictionary<A, R>();
        hits = null;
    }
    
    public int Count => dict.Count;
    
    public string Report() => 
        hits == null ? "" :
        string.Join("\n", from v in hits.Values group v by v into g select $"{g.Key},{g.Count()}");
}