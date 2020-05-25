﻿    using System;
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

        // TODO: Memoize this
        public static Quad Make(Quad nw, Quad ne, Quad se, Quad sw)
        {
            Debug.Assert(nw != null);
            Debug.Assert(ne != null);
            Debug.Assert(se != null);
            Debug.Assert(sw != null);
            Debug.Assert(nw.Level == ne.Level);
            Debug.Assert(ne.Level == se.Level);
            Debug.Assert(se.Level == sw.Level);
            return new Quad(nw, ne, se, sw);
        }

        // TODO: Memoize this
        public static Quad Empty(int level)
        {
            Debug.Assert(level >= 0);
            if (level == 0)
                return Dead;
            var q = Empty(level - 1);
            return Make(q, q, q, q);
        }
    }
}
