using System.Diagnostics;

namespace ConwaysLife
{
    // An n-quad is an immutable square grid 2-to-the-n on a side, 
    // so 4-to-the-n cells total.
    //
    // There are two 0-quads, which we'll call "Alive" and "Dead".
    //
    // An n-quad for n > 0 consists of four (n-1)-quads labeled NW, NE, SE, SW, for
    // the northwest, northeast, southeast and southwest quadrants of the grid.
    //
    // All of this code is a private implementation detail rather than public surface
    // area that must be robust against misuse, so invariant violations will result
    // in assertions, not exceptions; any assertion is a bug.
    //
    // There is in principle no reason why we cannot make arbitrarily large quads,
    // but in order to keep all the math in the range of a long, I'm going to
    // restrict quads to level 60 or less. 

    // Is this an onerous restriction? I know it seems like 4 to the 60
    // addressible cells ought to be enough for anyone, but there are good reasons
    // why you want to allow larger quads that we will go into later in this series.
    // Were we doing a more powerful implementation of this algorithm then we'd
    // do the math in big integers instead and allow arbitrarily large grids.

    sealed class Quad
    {
        static Quad()
        {
            CacheManager.EmptyMemoizer = new Memoizer<int, Quad>(UnmemoizedEmpty);
            CacheManager.MakeQuadMemoizer = new Memoizer<(Quad, Quad, Quad, Quad), Quad>(UnmemoizedMake);
        }

        public static readonly Quad Dead = new Quad();
        public static readonly Quad Alive = new Quad();

        public Quad NW { get; }
        public Quad NE { get; }
        public Quad SE { get; }
        public Quad SW { get; }

        // We could compute level recursively of course, but this is frequently 
        // accessed so we'll cache it in the object and use an extra field.
        public int Level { get; }

        public long Width => 1L << Level;

        private Quad()
        {
            Level = 0;
        }

        private Quad(Quad nw, Quad ne, Quad se, Quad sw)
        {
            NW = nw;
            NE = ne;
            SE = se;
            SW = sw;
            Level = nw.Level + 1;
        }

        private static Quad UnmemoizedMake((Quad nw, Quad ne, Quad se, Quad sw) args)
        {
            Debug.Assert(args.nw != null);
            Debug.Assert(args.ne != null);
            Debug.Assert(args.se != null);
            Debug.Assert(args.sw != null);
            Debug.Assert(args.nw.Level == args.ne.Level);
            Debug.Assert(args.ne.Level == args.se.Level);
            Debug.Assert(args.se.Level == args.sw.Level);
            return new Quad(args.nw, args.ne, args.se, args.sw);
        }

        private static Quad Make(Quad nw, Quad ne, Quad se, Quad sw) =>
            CacheManager.MakeQuadMemoizer.MemoizedFunc((nw, ne, se, sw));

        private static Quad UnmemoizedEmpty(int level)
        {
            Debug.Assert(level >= 0);
            if (level == 0)
                return Dead;
            var q = Empty(level - 1);
            return Make(q, q, q, q);
        }

        public static Quad Empty(int level) => 
            CacheManager.EmptyMemoizer.MemoizedFunc(level);

        public bool IsEmpty => this == Empty(this.Level);

        // Since we have memoized all quads, it is impossible to know given just a quad
        // what its coordinates are; multiple quads cannot both be referentially 
        // identical and also know their distinct locations. 
        //
        // Therefore, to answer the question "what is the value of the cell at x, y?" 
        // we have to also know which portion of the grid this quad represents.

        // Let's start by establishing a convention. Given an n-quad that represents
        // the "center" of the infinite plane, let's say that the lower left corner
        // is (-Width/2, -Width/2).  For instance, suppose we have a 2-quad that is
        // "centered" on the origin. We assign coordinates to it as follows:
        //
        // (-2,  1) | (-1,  1) || (0,  1) | (1,  1)
        // --------------------||------------------
        // (-2,  0) | (-1,  0) || (0,  0) | (1,  0)
        // ========================================
        // (-2, -1) | (-1, -1) || (0, -1) | (1, -1)
        // --------------------||------------------
        // (-2, -2) | (-1, -2) || (0, -2) | (1, -2)
        //
        // Note that the double lines demarcate 1-quads and the single lines 
        // demarcate 0-quads.
        //
        // We will need to answer the question "is this point inside this quad?"
        // If we know the lower-left corner then the answer is straightforward:

        private bool Contains(LifePoint lowerLeft, LifePoint p) => 
            lowerLeft.X <= p.X && p.X < lowerLeft.X + Width &&
            lowerLeft.Y <= p.Y && p.Y < lowerLeft.Y + Width;

        // Given no context, we can assume that the quad we have is centered
        // on the origin:

        public bool Contains(LifePoint p) =>
            Contains(new LifePoint(-Width / 2, -Width / 2), p);

        // To fetch the level-0 quad at a given point, we similarly need to know
        // what the lower left corner is:

        private Quad Get(LifePoint lowerLeft, LifePoint p)
        {
            if (!Contains(lowerLeft, p))
                return Dead;

            if (Level == 0)
                return this;

            // The point is inside somewhere, and we're not level zero.
            // Figure out which quad it's at.

            long w = Width / 2;

            if (p.X >= lowerLeft.X + w)
            {
                if (p.Y >= lowerLeft.Y + w)
                    return NE.Get(new LifePoint(lowerLeft.X + w, lowerLeft.Y + w), p);
                else
                    return SE.Get(new LifePoint(lowerLeft.X + w, lowerLeft.Y), p);
            } 
            else if (p.Y >= lowerLeft.Y + w)
                return NW.Get(new LifePoint(lowerLeft.X, lowerLeft.Y + w), p);
            else
                return SW.Get(lowerLeft, p);
        }

        // And similarly, given no context we assume that we are centered on the origin.
        //
        // Note that I'm creating "Get" and "Set" methods here rather than adding a
        // user-defined indexer because the semantics of indexing are mutation, and I want
        // to emphasize here that we are immutable.

        public Quad Get(LifePoint p) => 
            Get(new LifePoint(-Width / 2, -Width / 2), p);

        public Quad Get(long x, long y) =>
            Get(new LifePoint(x, y));

        // The setter does not mutate anything, since this is an immutable data structure; rather,
        // it returns either the same object if there was no change, or a new object with the change.
        // Since the vast majority of the references in the new object are the same as the old,
        // we typically have only rebuilt the "spine".

        // Given that the lower left corner of this quad is as stated, set the point at p within
        // this quad to value q.
        private Quad Set(LifePoint lowerLeft, LifePoint p, Quad q)
        {
            Debug.Assert(q != null);
            Debug.Assert(q.Level == 0);

            // If the point isn't in this quad then this quad is unchanged.
            if (!Contains(lowerLeft, p))
                return this;

            // If this quad is level zero then changing it is simply returning
            // the new quad.
            if (Level == 0)
                return q;

            long w = Width / 2;

            // The point is inside somewhere, and we're not level zero.
            // Since Make is memoized, if this turns out to be a no-op, no big deal.
            // We rebuild, but we end up with exactly the same reference as we started with.
            return Make(
                NW.Set(new LifePoint(lowerLeft.X, lowerLeft.Y + w), p, q),
                NE.Set(new LifePoint(lowerLeft.X + w, lowerLeft.Y + w), p, q),
                SE.Set(new LifePoint(lowerLeft.X + w, lowerLeft.Y), p, q),
                SW.Set(lowerLeft, p, q)) ;
        }

        public Quad Set(LifePoint p, Quad q) =>
            Set(new LifePoint(-Width / 2, -Width / 2), p, q);
        
        public Quad Set(long x, long y, Quad q) =>
            Set(new LifePoint(-Width / 2, -Width / 2), new LifePoint(x, y), q);

    }
}
