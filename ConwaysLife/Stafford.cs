using System;
using System.Collections.Generic;
using System.Diagnostics;
using static System.Math;

namespace ConwaysLife
{
    // An adaptation of David Stafford's QLIFE algorithm to C#.
    // This was the winning entry in Michael Abrash's optimization
    // contest first proposed in the August 1992 issue of PC Techniques 
    // magazine described here: 
    //
    // http://www.jagregory.com/abrash-black-book/#chapter-17-the-game-of-life
    //
    // Stafford's original implementation is a short C program which when executed
    // produces as its output a rather longer x86 assembly language program that
    // implements the algorithm described here in hand-optimized form.
    //
    // That's an impressive achievement but I am more interested in the algorithm
    // that is implemented than in how it was compressed into a small generator
    // program for the purposes of the contest. The algorithm is a little tricky
    // to understand; Abrash explains it in the January 1994 issue of PC Techniques
    // magazine, which can be found here:
    //
    // http://www.jagregory.com/abrash-black-book/#chapter-18-its-a-plain-wonderful-life
    //
    // Abrash mentions in the original article (which I have a photocopy of from the
    // original magazine that has been in my filing cabinet lo these many decades)
    // that he will discuss an "equally remarkable" algorithm implemented by 
    // Peter Klerings in a future issue. Based on the revised text in the link 
    // above, it looks like that article was never written. 
    

    class StaffordUnoptimized : ILife
    {
        // We're going to keep the top and bottom edge triplets dead,
        // so this gives us 256 live rows.
        private int height = 258;
        
        // This is the width in triplets. That gives us 264 cells, but 
        // we'll keep the left and right triplets dead, so that's 258
        // live columns of cells.

        private int width = 88; 
        private Triplet[,] triplets;

        public StaffordUnoptimized()
        {
            Clear();
        }

        public void Clear()
        {
            triplets = new Triplet[width, height];
        }

        private bool IsValidPoint(long x, long y) =>
            1 <= x && x < (width - 1) * 3 && 1 <= y && y < height - 1;

        private void BecomeAlive(int x, int y)
        {
            int tx = x / 3;
            Triplet t = triplets[tx, y];

            switch(x % 3)
            {
                case 0:
                    if (t.LeftCurrent)
                        return;
                    // Left is about to be born
                    t = t.AUU();
                    triplets[tx, y - 1] = triplets[tx, y - 1].PPU();
                    triplets[tx, y + 1] = triplets[tx, y + 1].PPU();
                    triplets[tx - 1, y - 1] = triplets[tx - 1, y - 1].UUP();
                    triplets[tx - 1, y] = triplets[tx - 1, y].UUP();
                    triplets[tx - 1, y + 1] = triplets[tx - 1, y + 1].UUP();
                    break;
                case 1:
                    if (t.MiddleCurrent)
                        return;
                    // Middle is about to be born
                    t = t.UAU();
                    triplets[tx, y - 1] = triplets[tx, y - 1].PPP();
                    triplets[tx, y + 1] = triplets[tx, y + 1].PPP();
                    break;
                case 2:
                    if (t.RightCurrent)
                        return;
                    // Right is about to be born
                    t = t.UUA();
                    triplets[tx, y - 1] = triplets[tx, y - 1].UPP();
                    triplets[tx, y + 1] = triplets[tx, y + 1].UPP();
                    triplets[tx + 1, y - 1] = triplets[tx + 1, y - 1].PUU();
                    triplets[tx + 1, y] = triplets[tx + 1, y].PUU();
                    triplets[tx + 1, y + 1] = triplets[tx + 1, y + 1].PUU();
                    break;
            }
            // TODO: This could be in place. Save mutating t again.
            // TODO: Also re-order the above.
            triplets[tx, y] = t;
        }

        private void BecomeDead(int x, int y)
        {
            int tx = x / 3;
            Triplet t = triplets[tx, y];

            switch (x % 3)
            {
                case 0:
                    if (!t.LeftCurrent)
                        return;
                    t = t.DUU();
                    triplets[tx, y - 1] = triplets[tx, y - 1].MMU();
                    triplets[tx, y + 1] = triplets[tx, y + 1].MMU();
                    triplets[tx - 1, y - 1] = triplets[tx - 1, y - 1].UUM();
                    triplets[tx - 1, y] = triplets[tx - 1, y].UUM();
                    triplets[tx - 1, y + 1] = triplets[tx - 1, y + 1].UUM();
                    break;
                case 1:
                    if (!t.MiddleCurrent)
                        return;
                    t = t.UDU();
                    triplets[tx, y - 1] = triplets[tx, y - 1].MMM();
                    triplets[tx, y + 1] = triplets[tx, y + 1].MMM();
                    break;
                case 2:
                    if (!t.RightCurrent)
                        return;
                    t = t.UUD();
                    triplets[tx, y - 1] = triplets[tx, y - 1].UMM();
                    triplets[tx, y + 1] = triplets[tx, y + 1].UMM();
                    triplets[tx + 1, y - 1] = triplets[tx + 1, y - 1].MUU();
                    triplets[tx + 1, y] = triplets[tx + 1, y].MUU();
                    triplets[tx + 1, y + 1] = triplets[tx + 1, y + 1].MUU();
                    break;
            }
            triplets[tx, y] = t;
        }

        public bool this[long x, long y]
        {
            get
            {
                if (IsValidPoint(x, y))
                {
                    Triplet t = triplets[x / 3, y];
                    switch (x % 3)
                    {
                        case 0: return t.LeftCurrent;
                        case 1: return t.MiddleCurrent;
                        default: return t.RightCurrent;
                    }
                }
                return false;
            }
            set
            {
                if (IsValidPoint(x, y))
                {
                    if (value)
                        BecomeAlive((int)x, (int)y);
                    else
                        BecomeDead((int)x, (int)y);
                }
            }
        }

        public bool this[LifePoint v]
        {
            get => this[v.X, v.Y];
            set => this[v.X, v.Y] = value;
        }

        public void Step()
        {
            // The original explanation of the algorithm in the article
            // presents two problems, one pedagogical and one technical.
            //
            // The pedagogical problem is that the explanation is difficult 
            // to follow because it appears that the order of events in each 
            // iteration of the main update loop is impossible. The order 
            // as presented is:
            //
            // * Copy the next cell state to the current cell state.
            // * Update the neighbor counts of the neighboring triplets if necessary
            //   to match the new current state.
            // * Compute the next state from the current state and neighbor count.
            //
            // It appears we have a chicken-and-egg problem here in that the next
            // state is used before it is computed! How does this make any sense?
            //
            // We *could* resolve the pedagogical problem by explaining why the
            // wrong-looking order is in fact correct, as follows:
            //
            // If you're in an *infinite* loop, it doesn't matter whether this appears
            // a sensible order or not; all that matters is that the loop body
            // precondition is *met* when you *first* enter the loop at the top, and 
            // is *maintained* by the action of the loop body every time it executes.
            //
            // The loop body precondition in this description of the algorithm must
            // be the following:
            //
            // * The current cell state is known.
            // * The neighbor counts are consistent with the current cell states.
            // * The next tick cell state is known.
            //
            // But these conditions are easy to meet on the very first time you 
            // enter the loop; you just make the "current" state "all dead" so
            // the neighbor counts are all zero. The initial state of the board
            // is set to the next state, and we can start the loop at the "copy"
            // step no problem.
            //
            // The algorithm is clearly seen to maintain the body precondition
            // in the action of the loop body; by the time we are done each
            // execution of the body we are in a known good state where the
            // current cell state, current neighbor count, and next cell states
            // are all consistent.
            //
            // The technical problem with my adaptation of this algorithm is that
            // I want to be able to edit the current state, but if the loop body 
            // precondition is that the next state is already known, that presents 
            // a difficulty. You can't just update the current state because that 
            // is about to be wiped out by the update that was computed from the 
            // *pre-edit* current state.
            //
            // We can solve both the pedagogical problem and the technical problem
            // by re-stating the action of the main loop in the more sensible order
            // that we used in our implementation of Abrash's algorithm:
            //
            // * Compute the next state from the current state and neighbor count.
            // * Copy the next cell state to the current cell state.
            // * Update the neighbor counts of the neighboring triplets if necessary
            //   to match the new current state.
            //
            // Now the precondition is only that the current state and the neighbor
            // counts are consistent; this property is easy enough to maintain when
            // editing, as we saw in Abrash's algorithm.
            //
            // Once we have the basic algorithm working, we can start to introduce
            // optimizations, starting with the change list optimization.
            //
            // How does the change list optimization work? It is motivated by 
            // pondering this question:
            //
            // Under what circumstances do we know a cell will not change?
            //
            //   If it did not change between the previous tick and
            //   the current tick, and similarly none of its neighbors
            //   changed, then its state and neighbor count are the same
            //   now as they were in a previous configuration that led to
            //   no change. Therefore it will not change from the current
            //   tick to the next tick either.
            //
            // What then are the cells that *might* change in this tick?
            //
            //   * Cells that changed in the previous tick.
            //   * Neighbors of cells that changed in the previous tick.
            //
            // Therefore: if we have a list of *all* cells which changed
            // in the previous tick, we can restrict our computation of 
            // next state to only those cells and their neighbors.
            //
            // Preconditions:
            // * Current state is known
            // * Neighbor counts are consistent with current state
            // * recent changes list accurately lists every cell
            //   that changed in previous tick.
            //
            // Description of algorithm with change list optimization:
            // 
            // create a "new changes" list
            //
            // for each triplet on the "recent changes" list:
            //   compute and set next tick bits for the triplet and all its neighbors
            //   based on current state and neighbor count.
            //   if the triplet or any of its neighbors will change
            //     add the index of the changed triplet to the "new changes" list.
            //
            // Note that we might end up with duplicates on the "new changes" list,
            // in the common case where two adjacent triplets changed recently and
            // both of them will change again. When we process each we then add each
            // to the "new changes" list, which results in both being added twice. 
            // We'll deduplicate the list in the next step:
            //
            // create a new "recent changes" list
            // for each triplet on the new changes list
            //   copy the "next tick" state bits that were computed in the previous
            //   pass to the "current tick" state bits.
            //   If that changed the triple:
            //     update the neighbor counts for the adjacent cells.
            //     add the triple index to the "recent changes" list.
            //   otherwise:
            //     it was a duplicate that we already processed; ignore it.
            //
            // Once we have the change list optimization working we can identify more
            // optimizations.
            //
            // Consider the first loop; how can we optimize:
            //
            //   compute and set next tick bits for the triplet and all its neighbors
            //   based on current state and neighbor count.
            //
            // ? 
            // 
            // Two observations:
            //
            // * The three next tick bits depend solely on the three current tick bits and 
            //   the nine current neighbor bits. That is, there are 4096 possible 
            //   configurations with 8 possible results. We can just make an array of 4096
            //   bytes that pre-computes the answers. (The original implementation went
            //   even farther than this; it had 65536 bytes and was indexed by the entire
            //   triplet.)
            // * The question of "what neighbors do we need to check?" depends on whether
            //   the triplet is at the edge of the board or not; we have extra work to
            //   do if it is on the edge. But whether a triplet is on the edge does not
            //   change over time, and we have an extra bit in the triplet that is unused.
            //   We can precompute this fact and store it, and then have two code paths.
            //   Moreover, the edge cells are the ones least likely to change anyway.
            //
            //  What about the second loop -- how can we optimize:
            //
            //   copy the "next tick" state bits that were computed in the previous
            //   pass to the "current tick" state bits.
            //   If that changed the triple:
            //     update the neighbor counts for the adjacent cells.
            //     add the triple index to the "recent changes" list.
            //
            // * Once again we are in a situation where there are only a small number
            //   of behaviours that are determined entirely by the three current
            //   state bits (that are about to be overwritten, or already have been),
            //   the three new state bits, and the is-an-edge bit. That is, there are
            //   128 different actions this algorithm could take. Rather than doing 
            //   a bunch of bit twiddling and conditional logic, we could just write
            //   128 different delegates, put them in an array, and call them based
            //   on those 7 bits.
            //
            //   The sixteen bit patterns where the new state bits do not change
            //   because we already did the update are no-op delegates.
            //
            //   Suppose we have 0 for the edge bit, the old state is 011 and the 
            //   new state is 001.  What work do we have to do?  We need to decrement
            //   the left, middle and right neighbor counts for the triplets north
            //   and south the current triplet; we know they exist because the edge
            //   bit is zero. But we do not need to update any neighbor counts for the
            //   triplets to the northwest, northeast, southwest, southeast, east or west.
            //
            //   Suppose we have 0 for the edge bit, the old state is 011 and the
            //   new state is 101. Now what work do we have to do?  The neighbor
            //   count of the left and middle of the triplets north and south have
            //   not changed so we don't need to touch them. The right count north
            //   and south gets decremented. The left neighbor counts on the triplets
            //   to the west, northwest and southwest get incremented.  And so on;
            //   you see how this goes.


            // Let's start with an implementation without all these optimizations
            // and see where we get to.

            // * Compute the next state from the current state and neighbor count.

            for (int y = 1; y < height - 1; y += 1)
            {
                for (int tx = 1; tx < width - 1; tx += 1)
                {
                    // This can be replaced by a table lookup.
                    Triplet t = triplets[tx, y];
                    int lc = t.LeftCount;
                    int mc = t.MiddleCount;
                    int rc = t.RightCount;
                    t = t.SetLeftNext(lc == 3 | t.LeftCurrent & lc == 2);
                    t = t.SetMiddleNext(mc == 3 | t.MiddleCurrent & mc == 2);
                    t = t.SetRightNext(rc == 3 | t.RightCurrent & rc == 2);
                    triplets[tx, y] = t;
                }

            }

            // * Copy the next cell state to the current cell state.
            // * Update the neighbor counts of the neighboring triplets if necessary
            //   to match the new current state.

            for (int y = 1;  y < height - 1; y += 1)
            {
                for (int tx = 1; tx < width - 1; tx += 1)
                {
                    // This can be replaced by jump table logic that looks up the new triple value
                    // and knows which operations to perform on which neighbours.

                    Triplet t = triplets[tx, y];
                    if (t.LeftCurrent & !t.LeftNext)
                        BecomeDead(tx * 3, y);
                    else if (!t.LeftCurrent & t.LeftNext)
                        BecomeAlive(tx * 3, y);

                    if (t.MiddleCurrent & !t.MiddleNext)
                        BecomeDead(tx * 3 + 1, y);
                    else if (!t.MiddleCurrent & t.MiddleNext)
                        BecomeAlive(tx * 3 + 1, y);

                    if (t.RightCurrent & !t.RightNext)
                        BecomeDead(tx * 3 + 2, y);
                    else if (!t.RightCurrent & t.RightNext)
                        BecomeAlive(tx * 3 + 2, y);
                }
            }
        }

        public void Draw(LifeRect rect, Action<LifePoint> setPixel)
        {
            long xmin = Max(0, rect.X);
            long xmax = Min(width * 3, rect.X + rect.Width);
            long ymin = Max(0, rect.Y - rect.Height + 1);
            long ymax = Min(height, rect.Y + 1);
            for (long y = ymin; y < ymax; y += 1)
                for (long x = xmin; x < xmax; x += 1)
                    if (this[x, y])
                        setPixel(new LifePoint(x, y));
        }
    }

    class StaffordChangeList : ILife
    {
        // We're going to keep the top and bottom edge triplets dead,
        // so this gives us 256 live rows.
        private int height = 258;

        // This is the width in triplets. That gives us 264 cells, but 
        // we'll keep the left and right triplets dead, so that's 258
        // live columns of cells.

        private int width = 88;
        private Triplet[,] triplets;
        private List<(int, int)> changes;

        public StaffordChangeList()
        {
            Clear();
        }

        public void Clear()
        {
            triplets = new Triplet[width, height];
            changes = new List<(int, int)>();
        }

        private bool IsValidPoint(long x, long y) =>
            1 <= x && x < (width - 1) * 3 && 1 <= y && y < height - 1;

        private void BecomeAlive(int x, int y)
        {
            int tx = x / 3;
            Triplet t = triplets[tx, y];

            switch (x % 3)
            {
                case 0:
                    if (t.LeftCurrent)
                        return;
                    // Left is about to be born
                    t = t.SetLeftCurrent(true);
                    triplets[tx, y - 1] = triplets[tx, y - 1].PPU();
                    triplets[tx, y + 1] = triplets[tx, y + 1].PPU();
                    triplets[tx - 1, y - 1] = triplets[tx - 1, y - 1].UUP();
                    triplets[tx - 1, y] = triplets[tx - 1, y].UUP();
                    triplets[tx - 1, y + 1] = triplets[tx - 1, y + 1].UUP();
                    break;
                case 1:
                    if (t.MiddleCurrent)
                        return;
                    // Middle is about to be born
                    t = t.SetMiddleCurrent(true);
                    triplets[tx, y - 1] = triplets[tx, y - 1].PPP();
                    triplets[tx, y + 1] = triplets[tx, y + 1].PPP();
                    break;
                case 2:
                    if (t.RightCurrent)
                        return;
                    // Right is about to be born
                    t = t.SetRightCurrent(true);
                    triplets[tx, y - 1] = triplets[tx, y - 1].UPP();
                    triplets[tx, y + 1] = triplets[tx, y + 1].UPP();
                    triplets[tx + 1, y - 1] = triplets[tx + 1, y - 1].PUU();
                    triplets[tx + 1, y] = triplets[tx + 1, y].PUU();
                    triplets[tx + 1, y + 1] = triplets[tx + 1, y + 1].PUU();
                    break;
            }
            triplets[tx, y] = t;
        }

        private void BecomeDead(int x, int y)
        {
            int tx = x / 3;
            Triplet t = triplets[tx, y];

            switch (x % 3)
            {
                case 0:
                    if (!t.LeftCurrent)
                        return;
                    t = t.SetLeftCurrent(false);
                    triplets[tx, y - 1] = triplets[tx, y - 1].MMU();
                    triplets[tx, y + 1] = triplets[tx, y + 1].MMU();
                    triplets[tx - 1, y - 1] = triplets[tx - 1, y - 1].UUM();
                    triplets[tx - 1, y] = triplets[tx - 1, y].UUM();
                    triplets[tx - 1, y + 1] = triplets[tx - 1, y + 1].UUM();
                    break;
                case 1:
                    if (!t.MiddleCurrent)
                        return;
                    t = t.SetMiddleCurrent(false);
                    triplets[tx, y - 1] = triplets[tx, y - 1].MMM();
                    triplets[tx, y + 1] = triplets[tx, y + 1].MMM();
                    break;
                case 2:
                    if (!t.RightCurrent)
                        return;
                    t = t.SetRightCurrent(false);
                    triplets[tx, y - 1] = triplets[tx, y - 1].UMM();
                    triplets[tx, y + 1] = triplets[tx, y + 1].UMM();
                    triplets[tx + 1, y - 1] = triplets[tx + 1, y - 1].MUU();
                    triplets[tx + 1, y] = triplets[tx + 1, y].MUU();
                    triplets[tx + 1, y + 1] = triplets[tx + 1, y + 1].MUU();
                    break;
            }
            triplets[tx, y] = t;
        }

        public bool this[long x, long y]
        {
            get
            {
                if (IsValidPoint(x, y))
                {
                    Triplet t = triplets[x / 3, y];
                    switch (x % 3)
                    {
                        case 0: return t.LeftCurrent;
                        case 1: return t.MiddleCurrent;
                        default: return t.RightCurrent;
                    }
                }
                return false;
            }
            set
            {
                if (IsValidPoint(x, y))
                {
                    if (value)
                    {
                        if (!this[x, y])
                        {
                            BecomeAlive((int)x, (int)y);
                            changes.Add(((int)x / 3, (int)y));
                        }
                    }
                    else
                    {
                        if (this[x, y])
                        {
                            BecomeDead((int)x, (int)y);
                            changes.Add(((int)x / 3, (int)y));
                        }
                    }
                }
            }
        }

        public bool this[LifePoint v]
        {
            get => this[v.X, v.Y];
            set => this[v.X, v.Y] = value;
        }

        public void Step()
        {
            var previousChanges = changes;
            changes = new List<(int, int)>();

            foreach((int cx, int cy) in previousChanges)
            {
                int minx = Max(cx - 1, 1);
                int maxx = Min(cx + 2, width - 1);
                int miny = Max(cy - 1, 1);
                int maxy = Min(cy + 2, height - 1);
                for (int y = miny; y < maxy; y += 1)
                {
                    for (int tx = minx; tx < maxx; tx += 1)
                    {
                        Triplet t = triplets[tx, y];
                        int lc = t.LeftCount;
                        int mc = t.MiddleCount;
                        int rc = t.RightCount;
                        t = t.SetLeftNext(lc == 3 | t.LeftCurrent & lc == 2);
                        t = t.SetMiddleNext(mc == 3 | t.MiddleCurrent & mc == 2);
                        t = t.SetRightNext(rc == 3 | t.RightCurrent & rc == 2);
                        triplets[tx, y] = t;
                    }
                }
            }

            foreach ((int cx, int cy) in previousChanges)
            {
                int minx = Max(cx - 1, 1);
                int maxx = Min(cx + 2, width - 1);
                int miny = Max(cy - 1, 1);
                int maxy = Min(cy + 2, height - 1);
                for (int y = miny; y < maxy; y += 1)
                {
                    for (int tx = minx; tx < maxx; tx += 1)
                    {
                        bool changed = false;

                        Triplet t = triplets[tx, y];
                        if (t.LeftCurrent & !t.LeftNext)
                        {
                            BecomeDead(tx * 3, y);
                            changed = true;
                        }
                        else if (!t.LeftCurrent & t.LeftNext)
                        {
                            BecomeAlive(tx * 3, y);
                            changed = true;
                        }

                        if (t.MiddleCurrent & !t.MiddleNext)
                        {
                            BecomeDead(tx * 3 + 1, y);
                            changed = true;
                        }
                        else if (!t.MiddleCurrent & t.MiddleNext)
                        {
                            BecomeAlive(tx * 3 + 1, y);
                            changed = true;
                        }

                        if (t.RightCurrent & !t.RightNext)
                        {
                            BecomeDead(tx * 3 + 2, y);
                            changed = true;
                        }
                        else if (!t.RightCurrent & t.RightNext)
                        {
                            BecomeAlive(tx * 3 + 2, y);
                            changed = true;
                        }
                        if (changed)
                            changes.Add((tx, y));
                    }
                }
            }
        }

        public void Draw(LifeRect rect, Action<LifePoint> setPixel)
        {
            long xmin = Max(0, rect.X);
            long xmax = Min(width * 3, rect.X + rect.Width);
            long ymin = Max(0, rect.Y - rect.Height + 1);
            long ymax = Min(height, rect.Y + 1);
            for (long y = ymin; y < ymax; y += 1)
                for (long x = xmin; x < xmax; x += 1)
                    if (this[x, y])
                        setPixel(new LifePoint(x, y));
        }
    }

    static class TripletLookup
    {

        public static Triplet[] lookup;

        static TripletLookup()
        {
            // Some of these are impossible, but who cares?
            lookup = new Triplet[1 << 12];

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
                                    lookup[t.State1] = t;
                                }

        }
    }

    class StaffordLookup : ILife
    {
        // We're going to keep the top and bottom edge triplets dead,
        // so this gives us 256 live rows.
        private int height = 258;

        // This is the width in triplets. That gives us 264 cells, but 
        // we'll keep the left and right triplets dead, so that's 258
        // live columns of cells.

        private int width = 88;
        private Triplet[,] triplets;
        private List<(int, int)> changes;



        public StaffordLookup()
        {
            Clear();
        }

        public void Clear()
        {
            triplets = new Triplet[width, height];
            changes = new List<(int, int)>();
        }

        private bool IsValidPoint(long x, long y) =>
            1 <= x && x < (width - 1) * 3 && 1 <= y && y < height - 1;

        private void BecomeAlive(int x, int y)
        {
            int tx = x / 3;
            Triplet t = triplets[tx, y];

            switch (x % 3)
            {
                case 0:
                    if (t.LeftCurrent)
                        return;
                    // Left is about to be born
                    t = t.SetLeftCurrent(true);
                    triplets[tx, y - 1] = triplets[tx, y - 1].UPU().PUU();
                    triplets[tx, y + 1] = triplets[tx, y + 1].UPU().PUU();
                    triplets[tx - 1, y - 1] = triplets[tx - 1, y - 1].UUP();
                    triplets[tx - 1, y] = triplets[tx - 1, y].UUP();
                    triplets[tx - 1, y + 1] = triplets[tx - 1, y + 1].UUP();
                    break;
                case 1:
                    if (t.MiddleCurrent)
                        return;
                    // Middle is about to be born
                    t = t.SetMiddleCurrent(true);
                    triplets[tx, y - 1] = triplets[tx, y - 1].UPU().PUU().UUP();
                    triplets[tx, y + 1] = triplets[tx, y + 1].UPU().PUU().UUP();
                    break;
                case 2:
                    if (t.RightCurrent)
                        return;
                    // Right is about to be born
                    t = t.SetRightCurrent(true);
                    triplets[tx, y - 1] = triplets[tx, y - 1].UPU().UUP();
                    triplets[tx, y + 1] = triplets[tx, y + 1].UPU().UUP();
                    triplets[tx + 1, y - 1] = triplets[tx + 1, y - 1].PUU();
                    triplets[tx + 1, y] = triplets[tx + 1, y].PUU();
                    triplets[tx + 1, y + 1] = triplets[tx + 1, y + 1].PUU();
                    break;
            }
            triplets[tx, y] = t;
        }

        private void BecomeDead(int x, int y)
        {
            int tx = x / 3;
            Triplet t = triplets[tx, y];

            switch (x % 3)
            {
                case 0:
                    if (!t.LeftCurrent)
                        return;
                    t = t.SetLeftCurrent(false);
                    triplets[tx, y - 1] = triplets[tx, y - 1].UMU().MUU();
                    triplets[tx, y + 1] = triplets[tx, y + 1].UMU().MUU();
                    triplets[tx - 1, y - 1] = triplets[tx - 1, y - 1].UUM();
                    triplets[tx - 1, y] = triplets[tx - 1, y].UUM();
                    triplets[tx - 1, y + 1] = triplets[tx - 1, y + 1].UUM();
                    break;
                case 1:
                    if (!t.MiddleCurrent)
                        return;
                    t = t.SetMiddleCurrent(false);
                    triplets[tx, y - 1] = triplets[tx, y - 1].UMU().MUU().UUM();
                    triplets[tx, y + 1] = triplets[tx, y + 1].UMU().MUU().UUM();
                    break;
                case 2:
                    if (!t.RightCurrent)
                        return;
                    t = t.SetRightCurrent(false);
                    triplets[tx, y - 1] = triplets[tx, y - 1].UMU().UUM();
                    triplets[tx, y + 1] = triplets[tx, y + 1].UMU().UUM();
                    triplets[tx + 1, y - 1] = triplets[tx + 1, y - 1].MUU();
                    triplets[tx + 1, y] = triplets[tx + 1, y].MUU();
                    triplets[tx + 1, y + 1] = triplets[tx + 1, y + 1].MUU();
                    break;
            }
            triplets[tx, y] = t;
        }

        public bool this[long x, long y]
        {
            get
            {
                if (IsValidPoint(x, y))
                {
                    Triplet t = triplets[x / 3, y];
                    switch (x % 3)
                    {
                        case 0: return t.LeftCurrent;
                        case 1: return t.MiddleCurrent;
                        default: return t.RightCurrent;
                    }
                }
                return false;
            }
            set
            {
                if (IsValidPoint(x, y))
                {
                    if (value)
                    {
                        if (!this[x, y])
                        {
                            BecomeAlive((int)x, (int)y);
                            changes.Add(((int)x / 3, (int)y));
                        }
                    }
                    else
                    {
                        if (this[x, y])
                        {
                            BecomeDead((int)x, (int)y);
                            changes.Add(((int)x / 3, (int)y));
                        }
                    }
                }
            }
        }

        public bool this[LifePoint v]
        {
            get => this[v.X, v.Y];
            set => this[v.X, v.Y] = value;
        }

        public void Step()
        {
            var previousChanges = changes;
            changes = new List<(int, int)>();

            foreach ((int cx, int cy) in previousChanges)
            {
                int minx = Max(cx - 1, 1);
                int maxx = Min(cx + 2, width - 1);
                int miny = Max(cy - 1, 1);
                int maxy = Min(cy + 2, height - 1);
                for (int y = miny; y < maxy; y += 1)
                {
                    for (int tx = minx; tx < maxx; tx += 1)
                    {
                        triplets[tx, y] = TripletLookup.lookup[triplets[tx, y].State1];
                    }
                }
            }

            foreach ((int cx, int cy) in previousChanges)
            {
                int minx = Max(cx - 1, 1);
                int maxx = Min(cx + 2, width - 1);
                int miny = Max(cy - 1, 1);
                int maxy = Min(cy + 2, height - 1);
                for (int y = miny; y < maxy; y += 1)
                {
                    for (int tx = minx; tx < maxx; tx += 1)
                    {
                        bool changed = false;

                        Triplet t = triplets[tx, y];
                        if (t.LeftCurrent & !t.LeftNext)
                        {
                            BecomeDead(tx * 3, y);
                            changed = true;
                        }
                        else if (!t.LeftCurrent & t.LeftNext)
                        {
                            BecomeAlive(tx * 3, y);
                            changed = true;
                        }

                        if (t.MiddleCurrent & !t.MiddleNext)
                        {
                            BecomeDead(tx * 3 + 1, y);
                            changed = true;
                        }
                        else if (!t.MiddleCurrent & t.MiddleNext)
                        {
                            BecomeAlive(tx * 3 + 1, y);
                            changed = true;
                        }

                        if (t.RightCurrent & !t.RightNext)
                        {
                            BecomeDead(tx * 3 + 2, y);
                            changed = true;
                        }
                        else if (!t.RightCurrent & t.RightNext)
                        {
                            BecomeAlive(tx * 3 + 2, y);
                            changed = true;
                        }
                        if (changed)
                            changes.Add((tx, y));
                    }
                }
            }
        }

        public void Draw(LifeRect rect, Action<LifePoint> setPixel)
        {
            long xmin = Max(0, rect.X);
            long xmax = Min(width * 3, rect.X + rect.Width);
            long ymin = Max(0, rect.Y - rect.Height + 1);
            long ymax = Min(height, rect.Y + 1);
            for (long y = ymin; y < ymax; y += 1)
                for (long x = xmin; x < xmax; x += 1)
                    if (this[x, y])
                        setPixel(new LifePoint(x, y));
        }
    }

    class Stafford : ILife
    {
        // We're going to keep the top and bottom edge triplets dead,
        // so this gives us 256 live rows.
        private int height = 258;

        // This is the width in triplets. That gives us 264 cells, but 
        // we'll keep the left and right triplets dead, so that's 258
        // live columns of cells.

        private int width = 88;
        private Triplet[,] triplets;
        private List<(int, int)> changes;

        private Func<int, int, bool>[] lookup2;


        public Stafford()
        {
            lookup2 = new Func<int, int, bool>[1 << 6]
            {
                /* NXT CUR */
                /* DDD DDD */ UUU, /* DDD DDA */ UUD, /* DDD DAD */ UDU, /* DDD DAA */ UDD,
                /* DDD ADD */ DUU, /* DDD ADA */ DUD, /* DDD AAD */ DDU, /* DDD AAA */ DDD,
                /* DDA DDD */ UUA, /* DDA DDA */ UUU, /* DDA DAD */ UDA, /* DDA DAA */ UDU,
                /* DDA ADD */ DUA, /* DDA ADA */ DUU, /* DDA AAD */ DDA, /* DDA AAA */ DDU,
                /* DAD DDD */ UAU, /* DAD DDA */ UAD, /* DAD DAD */ UUU, /* DAD DAA */ UUD,
                /* DAD ADD */ DAU, /* DAD ADA */ DAD, /* DAD AAD */ DUU, /* DAD AAA */ DUD,
                /* DAA DDD */ UAA, /* DAA DDA */ UAU, /* DAA DAD */ UUA, /* DAA DAA */ UUU,
                /* DAA ADD */ DAA, /* DAA ADA */ DAU, /* DAA AAD */ DUA, /* DAA AAA */ DUU,

                /* ADD DDD */ AUU, /* ADD DDA */ AUD, /* ADD DAD */ ADU, /* ADD DAA */ ADD,
                /* ADD ADD */ UUU, /* ADD ADA */ UUD, /* ADD AAD */ UDU, /* ADD AAA */ UDD,
                /* ADA DDD */ AUA, /* ADA DDA */ AUU, /* ADA DAD */ ADA, /* ADA DAA */ ADU,
                /* ADA ADD */ UUA, /* ADA ADA */ UUU, /* ADA AAD */ UDA, /* ADA AAA */ UDU,
                /* AAD DDD */ AAU, /* AAD DDA */ AAD, /* AAD DAD */ AUU, /* AAD DAA */ AUD,
                /* AAD ADD */ UAU, /* AAD ADA */ UAD, /* AAD AAD */ UUU, /* AAD AAA */ UUD,
                /* AAA DDD */ AAA, /* AAA DDA */ AAU, /* AAA DAD */ AUA, /* AAA DAA */ AUU,
                /* AAA ADD */ UAA, /* AAA ADA */ UAU, /* AAA AAD */ UUA, /* AAA AAA */ UUU
            };
            Clear();
        }

        public void Clear()
        {
            triplets = new Triplet[width, height];
            changes = new List<(int, int)>();
        }

        private bool IsValidPoint(long x, long y) =>
            1 <= x && x < (width - 1) * 3 && 1 <= y && y < height - 1;

        private void BecomeAlive(int x, int y)
        {
            int tx = x / 3;
            Triplet t = triplets[tx, y];

            switch (x % 3)
            {
                case 0:
                    if (t.LeftCurrent)
                        return;
                    // Left is about to be born
                    t = t.AUU();
                    triplets[tx, y - 1] = triplets[tx, y - 1].PPU();
                    triplets[tx, y + 1] = triplets[tx, y + 1].PPU();
                    triplets[tx - 1, y - 1] = triplets[tx - 1, y - 1].UUP();
                    triplets[tx - 1, y] = triplets[tx - 1, y].UUP();
                    triplets[tx - 1, y + 1] = triplets[tx - 1, y + 1].UUP();
                    break;
                case 1:
                    if (t.MiddleCurrent)
                        return;
                    // Middle is about to be born
                    t = t.UAU();
                    triplets[tx, y - 1] = triplets[tx, y - 1].PPP();
                    triplets[tx, y + 1] = triplets[tx, y + 1].PPP();
                    break;
                case 2:
                    if (t.RightCurrent)
                        return;
                    // Right is about to be born
                    t = t.UUA();
                    triplets[tx, y - 1] = triplets[tx, y - 1].UPP();
                    triplets[tx, y + 1] = triplets[tx, y + 1].UPP();
                    triplets[tx + 1, y - 1] = triplets[tx + 1, y - 1].PUU();
                    triplets[tx + 1, y] = triplets[tx + 1, y].PUU();
                    triplets[tx + 1, y + 1] = triplets[tx + 1, y + 1].PUU();
                    break;
            }
            triplets[tx, y] = t;
        }

        private void BecomeDead(int x, int y)
        {
            int tx = x / 3;
            Triplet t = triplets[tx, y];

            switch (x % 3)
            {
                case 0:
                    if (!t.LeftCurrent)
                        return;
                    t = t.DUU();
                    triplets[tx, y - 1] = triplets[tx, y - 1].MMU();
                    triplets[tx, y + 1] = triplets[tx, y + 1].MMU();
                    triplets[tx - 1, y - 1] = triplets[tx - 1, y - 1].UUM();
                    triplets[tx - 1, y] = triplets[tx - 1, y].UUM();
                    triplets[tx - 1, y + 1] = triplets[tx - 1, y + 1].UUM();
                    break;
                case 1:
                    if (!t.MiddleCurrent)
                        return;
                    t = t.UDU();
                    triplets[tx, y - 1] = triplets[tx, y - 1].MMM();
                    triplets[tx, y + 1] = triplets[tx, y + 1].MMM();
                    break;
                case 2:
                    if (!t.RightCurrent)
                        return;
                    t = t.UUD();
                    triplets[tx, y - 1] = triplets[tx, y - 1].UMM();
                    triplets[tx, y + 1] = triplets[tx, y + 1].UMM();
                    triplets[tx + 1, y - 1] = triplets[tx + 1, y - 1].MUU();
                    triplets[tx + 1, y] = triplets[tx + 1, y].MUU();
                    triplets[tx + 1, y + 1] = triplets[tx + 1, y + 1].MUU();
                    break;
            }
            triplets[tx, y] = t;
        }

        public bool this[long x, long y]
        {
            get
            {
                if (IsValidPoint(x, y))
                {
                    Triplet t = triplets[x / 3, y];
                    switch (x % 3)
                    {
                        case 0: return t.LeftCurrent;
                        case 1: return t.MiddleCurrent;
                        default: return t.RightCurrent;
                    }
                }
                return false;
            }
            set
            {
                if (IsValidPoint(x, y))
                {
                    if (value)
                    {
                        if (!this[x, y])
                        {
                            BecomeAlive((int)x, (int)y);
                            changes.Add(((int)x / 3, (int)y));
                        }
                    }
                    else
                    {
                        if (this[x, y])
                        {
                            BecomeDead((int)x, (int)y);
                            changes.Add(((int)x / 3, (int)y));
                        }
                    }
                }
            }
        }

        public bool this[LifePoint v]
        {
            get => this[v.X, v.Y];
            set => this[v.X, v.Y] = value;
        }

        // We are about to update the current state with the next state.
        // This requires incrementing and decrementing a bunch of neighbour
        // counts; each combination of "unchanged / become alive / become dead"
        // is different, and there are 27 such combinations. 
        //
        // Here we have 27 helper methods, each of which deals with one combination.
        // It returns true if there was a change made, so the only one that returns
        // false is the one that does nothing.

        private bool UUU(int tx, int y)
        {
            return false;
        }

        private bool UUA(int tx, int y)
        {
            triplets[tx, y - 1] = triplets[tx, y - 1].UPP();
            triplets[tx + 1, y - 1] = triplets[tx + 1, y - 1].PUU();
            triplets[tx, y] = triplets[tx, y].UUA();
            triplets[tx + 1, y] = triplets[tx + 1, y].PUU();
            triplets[tx, y + 1] = triplets[tx, y + 1].UPP();
            triplets[tx + 1, y + 1] = triplets[tx + 1, y + 1].PUU();
            return true;
        }

        private bool UUD(int tx, int y)
        {
            triplets[tx, y - 1] = triplets[tx, y - 1].UMM();
            triplets[tx + 1, y - 1] = triplets[tx + 1, y - 1].MUU();
            triplets[tx, y] = triplets[tx, y].UUD();
            triplets[tx + 1, y] = triplets[tx + 1, y].MUU();
            triplets[tx, y + 1] = triplets[tx, y + 1].UMM();
            triplets[tx + 1, y + 1] = triplets[tx + 1, y + 1].MUU();
            return true;
        }

        private bool UAU(int tx, int y)
        {
            triplets[tx, y - 1] = triplets[tx, y - 1].PPP();
            triplets[tx, y] = triplets[tx, y].UAU();
            triplets[tx, y + 1] = triplets[tx, y + 1].PPP();
            return true;
        }

        private bool UAA(int tx, int y)
        {
            triplets[tx, y - 1] = triplets[tx, y - 1].PP2P2();
            triplets[tx + 1, y - 1] = triplets[tx + 1, y - 1].PUU();
            triplets[tx, y] = triplets[tx, y].UAA();
            triplets[tx + 1, y] = triplets[tx + 1, y].PUU();
            triplets[tx, y + 1] = triplets[tx, y + 1].PP2P2();
            triplets[tx + 1, y + 1] = triplets[tx + 1, y + 1].PUU();
            return true;
        }

        
        private bool UAD(int tx, int y)
        {
            triplets[tx, y - 1] = triplets[tx, y - 1].PUU();
            triplets[tx + 1, y - 1] = triplets[tx + 1, y - 1].MUU();
            triplets[tx, y] = triplets[tx, y].UAD();
            triplets[tx + 1, y] = triplets[tx + 1, y].MUU();
            triplets[tx, y + 1] = triplets[tx, y + 1].PUU();
            triplets[tx + 1, y + 1] = triplets[tx + 1, y + 1].MUU();
            return true;
        }

        private bool UDU(int tx, int y)
        {
            triplets[tx, y - 1] = triplets[tx, y - 1].MMM();
            triplets[tx, y] = triplets[tx, y].UDU();
            triplets[tx, y + 1] = triplets[tx, y + 1].MMM();
            return true;
        }

        private bool UDA(int tx, int y)
        {
            triplets[tx, y - 1] = triplets[tx, y - 1].MUU();
            triplets[tx + 1, y - 1] = triplets[tx + 1, y - 1].PUU();
            triplets[tx, y] = triplets[tx, y].UDA();
            triplets[tx + 1, y] = triplets[tx + 1, y].PUU();
            triplets[tx, y + 1] = triplets[tx, y + 1].MUU();
            triplets[tx + 1, y + 1] = triplets[tx + 1, y + 1].PUU();
            return true;
        }

        private bool UDD(int tx, int y)
        {
            triplets[tx, y - 1] = triplets[tx, y - 1].MM2M2();
            triplets[tx + 1, y - 1] = triplets[tx + 1, y - 1].MUU();
            triplets[tx, y] = triplets[tx, y].UDD();
            triplets[tx + 1, y] = triplets[tx + 1, y].MUU();
            triplets[tx, y + 1] = triplets[tx, y + 1].MM2M2();
            triplets[tx + 1, y + 1] = triplets[tx + 1, y + 1].MUU();
            return true;
        }

        private bool AUU(int tx, int y)
        {
            triplets[tx - 1, y - 1] = triplets[tx - 1, y - 1].UUP();
            triplets[tx, y - 1] = triplets[tx, y - 1].PPU();
            triplets[tx - 1, y] = triplets[tx - 1, y].UUP();
            triplets[tx, y] = triplets[tx, y].AUU();
            triplets[tx - 1, y + 1] = triplets[tx - 1, y + 1].UUP();
            triplets[tx, y + 1] = triplets[tx, y + 1].PPU();
            return true;
        }

        private bool AUA(int tx, int y)
        {

            triplets[tx - 1, y - 1] = triplets[tx - 1, y - 1].UUP();
            triplets[tx, y - 1] = triplets[tx, y - 1].PP2P();
            triplets[tx + 1, y - 1] = triplets[tx + 1, y - 1].PUU();
            triplets[tx - 1, y] = triplets[tx - 1, y].UUP();
            triplets[tx, y] = triplets[tx, y].AUA();
            triplets[tx + 1, y] = triplets[tx + 1, y].PUU();
            triplets[tx - 1, y + 1] = triplets[tx - 1, y + 1].UUP();
            triplets[tx, y + 1] = triplets[tx, y + 1].PP2P();
            triplets[tx + 1, y + 1] = triplets[tx + 1, y + 1].PUU();
            return true;
        }

        private bool AUD(int tx, int y)
        {
            triplets[tx - 1, y - 1] = triplets[tx - 1, y - 1].UUP();
            triplets[tx, y - 1] = triplets[tx, y - 1].PUM();
            triplets[tx + 1, y - 1] = triplets[tx + 1, y - 1].MUU();
            triplets[tx - 1, y] = triplets[tx - 1, y].UUP();
            triplets[tx, y] = triplets[tx, y].AUD();
            triplets[tx + 1, y] = triplets[tx + 1, y].MUU();
            triplets[tx - 1, y + 1] = triplets[tx - 1, y + 1].UUP();
            triplets[tx, y + 1] = triplets[tx, y + 1].PUM();
            triplets[tx + 1, y + 1] = triplets[tx + 1, y + 1].MUU();
            return true;
        }

        private bool AAU(int tx, int y)
        {
            triplets[tx - 1, y - 1] = triplets[tx - 1, y - 1].UUP();
            triplets[tx, y - 1] = triplets[tx, y - 1].P2P2P();
            triplets[tx - 1, y] = triplets[tx - 1, y].UUP();
            triplets[tx, y] = triplets[tx, y].AAU();
            triplets[tx - 1, y + 1] = triplets[tx - 1, y + 1].UUP();
            triplets[tx, y + 1] = triplets[tx, y + 1].P2P2P();
            return true;
        }

        private bool AAA(int tx, int y)
        {
            triplets[tx - 1, y - 1] = triplets[tx - 1, y - 1].UUP();
            triplets[tx, y - 1] = triplets[tx, y - 1].P2P3P2();
            triplets[tx + 1, y - 1] = triplets[tx + 1, y - 1].PUU();
            triplets[tx - 1, y] = triplets[tx - 1, y].UUP();
            triplets[tx, y] = triplets[tx, y].AAA();
            triplets[tx + 1, y] = triplets[tx + 1, y].PUU();
            triplets[tx - 1, y + 1] = triplets[tx - 1, y + 1].UUP();
            triplets[tx, y + 1] = triplets[tx, y + 1].P2P3P2();
            triplets[tx + 1, y + 1] = triplets[tx + 1, y + 1].PUU();
            return true;
        }

        private bool AAD(int tx, int y)
        {
            triplets[tx - 1, y - 1] = triplets[tx - 1, y - 1].UUP();
            triplets[tx, y - 1] = triplets[tx, y - 1].P2PU();
            triplets[tx + 1, y - 1] = triplets[tx + 1, y - 1].MUU();
            triplets[tx - 1, y] = triplets[tx - 1, y].UUP();
            triplets[tx, y] = triplets[tx, y].AAD();
            triplets[tx + 1, y] = triplets[tx + 1, y].MUU();
            triplets[tx - 1, y + 1] = triplets[tx - 1, y + 1].UUP();
            triplets[tx, y + 1] = triplets[tx, y + 1].P2PU();
            triplets[tx + 1, y + 1] = triplets[tx + 1, y + 1].MUU();
            return true;
        }

        private bool ADU(int tx, int y)
        {
            triplets[tx - 1, y - 1] = triplets[tx - 1, y - 1].UUP();
            triplets[tx, y - 1] = triplets[tx, y - 1].UUM();
            triplets[tx - 1, y] = triplets[tx - 1, y].UUP();
            triplets[tx, y] = triplets[tx, y].ADU();
            triplets[tx - 1, y + 1] = triplets[tx - 1, y + 1].UUP();
            triplets[tx, y + 1] = triplets[tx, y + 1].UUM();
            return true;
        }

        private bool ADA(int tx, int y)
        {
            triplets[tx - 1, y - 1] = triplets[tx - 1, y - 1].UUP();
            triplets[tx, y - 1] = triplets[tx, y - 1].UPU();
            triplets[tx + 1, y - 1] = triplets[tx + 1, y - 1].PUU();
            triplets[tx - 1, y] = triplets[tx - 1, y].UUP();
            triplets[tx, y] = triplets[tx, y].ADA();
            triplets[tx + 1, y] = triplets[tx + 1, y].PUU();
            triplets[tx - 1, y + 1] = triplets[tx - 1, y + 1].UUP();
            triplets[tx, y + 1] = triplets[tx, y + 1].UPU();
            triplets[tx + 1, y + 1] = triplets[tx + 1, y + 1].PUU();
            return true;
        }

        private bool ADD(int tx, int y)
        {
            triplets[tx - 1, y - 1] = triplets[tx - 1, y - 1].UUP();
            triplets[tx, y - 1] = triplets[tx, y - 1].UMM2();
            triplets[tx + 1, y - 1] = triplets[tx + 1, y - 1].MUU();
            triplets[tx - 1, y] = triplets[tx - 1, y].UUP();
            triplets[tx, y] = triplets[tx, y].ADD();
            triplets[tx + 1, y] = triplets[tx + 1, y].MUU();
            triplets[tx - 1, y + 1] = triplets[tx - 1, y + 1].UUP();
            triplets[tx, y + 1] = triplets[tx, y + 1].UMM2();
            triplets[tx + 1, y + 1] = triplets[tx + 1, y + 1].MUU();
            return true;
        }

        private bool DUU(int tx, int y)
        {
            triplets[tx - 1, y - 1] = triplets[tx - 1, y - 1].UUM();
            triplets[tx, y - 1] = triplets[tx, y - 1].MMU();
            triplets[tx - 1, y] = triplets[tx - 1, y].UUM();
            triplets[tx, y] = triplets[tx, y].DUU();
            triplets[tx - 1, y + 1] = triplets[tx - 1, y + 1].UUM();
            triplets[tx, y + 1] = triplets[tx, y + 1].MMU();
            return true;
        }

        private bool DUA(int tx, int y)
        {
            triplets[tx - 1, y - 1] = triplets[tx - 1, y - 1].UUM();
            triplets[tx, y - 1] = triplets[tx, y - 1].MUP();
            triplets[tx + 1, y - 1] = triplets[tx + 1, y - 1].PUU();
            triplets[tx - 1, y] = triplets[tx - 1, y].UUM();
            triplets[tx, y] = triplets[tx, y].DUA();
            triplets[tx + 1, y] = triplets[tx + 1, y].PUU();
            triplets[tx - 1, y + 1] = triplets[tx - 1, y + 1].UUM();
            triplets[tx, y + 1] = triplets[tx, y + 1].MUP();
            triplets[tx + 1, y + 1] = triplets[tx + 1, y + 1].PUU();
            return true;
        }

        private bool DUD(int tx, int y)
        {
            triplets[tx - 1, y - 1] = triplets[tx - 1, y - 1].UUM();
            triplets[tx, y - 1] = triplets[tx, y - 1].MM2M();
            triplets[tx + 1, y - 1] = triplets[tx + 1, y - 1].MUU();
            triplets[tx - 1, y] = triplets[tx - 1, y].UUM();
            triplets[tx, y] = triplets[tx, y].DUD();
            triplets[tx + 1, y] = triplets[tx + 1, y].MUU();
            triplets[tx - 1, y + 1] = triplets[tx - 1, y + 1].UUM();
            triplets[tx, y + 1] = triplets[tx, y + 1].MM2M();
            triplets[tx + 1, y + 1] = triplets[tx + 1, y + 1].MUU();
            return true;
        }

        private bool DAU(int tx, int y)
        {
            triplets[tx - 1, y - 1] = triplets[tx - 1, y - 1].UUM();
            triplets[tx, y - 1] = triplets[tx, y - 1].UUP();
            triplets[tx - 1, y] = triplets[tx - 1, y].UUM();
            triplets[tx, y] = triplets[tx, y].DAU();
            triplets[tx - 1, y + 1] = triplets[tx - 1, y + 1].UUM();
            triplets[tx, y + 1] = triplets[tx, y + 1].UUP();
            return true;
        }

        private bool DAA(int tx, int y)
        {
            triplets[tx - 1, y - 1] = triplets[tx - 1, y - 1].UUM();
            triplets[tx, y - 1] = triplets[tx, y - 1].UPP2();
            triplets[tx + 1, y - 1] = triplets[tx + 1, y - 1].PUU();
            triplets[tx - 1, y] = triplets[tx - 1, y].UUM();
            triplets[tx, y] = triplets[tx, y].DAA();
            triplets[tx + 1, y] = triplets[tx + 1, y].PUU();
            triplets[tx - 1, y + 1] = triplets[tx - 1, y + 1].UUM();
            triplets[tx, y + 1] = triplets[tx, y + 1].UPP2();
            triplets[tx + 1, y + 1] = triplets[tx + 1, y + 1].PUU();
            return true;
        }

        private bool DAD(int tx, int y)
        {
            triplets[tx - 1, y - 1] = triplets[tx - 1, y - 1].UUM();
            triplets[tx, y - 1] = triplets[tx, y - 1].UMU();
            triplets[tx + 1, y - 1] = triplets[tx + 1, y - 1].MUU();
            triplets[tx - 1, y] = triplets[tx - 1, y].UUM();
            triplets[tx, y] = triplets[tx, y].DAD();
            triplets[tx + 1, y] = triplets[tx + 1, y].MUU();
            triplets[tx - 1, y + 1] = triplets[tx - 1, y + 1].UUM();
            triplets[tx, y + 1] = triplets[tx, y + 1].UMU();
            triplets[tx + 1, y + 1] = triplets[tx + 1, y + 1].MUU();
            return true;
        }

        private bool DDU(int tx, int y)
        {
            triplets[tx - 1, y - 1] = triplets[tx - 1, y - 1].UUM();
            triplets[tx, y - 1] = triplets[tx, y - 1].M2M2M();
            triplets[tx - 1, y] = triplets[tx - 1, y].UUM();
            triplets[tx, y] = triplets[tx, y].DDU();
            triplets[tx - 1, y + 1] = triplets[tx - 1, y + 1].UUM();
            triplets[tx, y + 1] = triplets[tx, y + 1].M2M2M();
            return true;
        }

        private bool DDA(int tx, int y)
        {

            triplets[tx - 1, y - 1] = triplets[tx - 1, y - 1].UUM();
            triplets[tx, y - 1] = triplets[tx, y - 1].M2MU();
            triplets[tx + 1, y - 1] = triplets[tx + 1, y - 1].PUU();
            triplets[tx - 1, y] = triplets[tx - 1, y].UUM();
            triplets[tx, y] = triplets[tx, y].DDA();
            triplets[tx + 1, y] = triplets[tx + 1, y].PUU();
            triplets[tx - 1, y + 1] = triplets[tx - 1, y + 1].UUM();
            triplets[tx, y + 1] = triplets[tx, y + 1].M2MU();
            triplets[tx + 1, y + 1] = triplets[tx + 1, y + 1].PUU();
            return true;
        }

        private bool DDD(int tx, int y)
        {
            triplets[tx - 1, y - 1] = triplets[tx - 1, y - 1].UUM();
            triplets[tx, y - 1] = triplets[tx, y - 1].M2M3M2();
            triplets[tx + 1, y - 1] = triplets[tx + 1, y - 1].MUU();
            triplets[tx - 1, y] = triplets[tx - 1, y].UUM();
            triplets[tx, y] = triplets[tx, y].DDD();
            triplets[tx + 1, y] = triplets[tx + 1, y].MUU();
            triplets[tx - 1, y + 1] = triplets[tx - 1, y + 1].UUM();
            triplets[tx, y + 1] = triplets[tx, y + 1].M2M3M2();
            triplets[tx + 1, y + 1] = triplets[tx + 1, y + 1].MUU();
            return true;
        }

        public void Step()
        {
            var previousChanges = changes;
            changes = new List<(int, int)>();

            foreach ((int cx, int cy) in previousChanges)
            {
                int minx = Max(cx - 1, 1);
                int maxx = Min(cx + 2, width - 1);
                int miny = Max(cy - 1, 1);
                int maxy = Min(cy + 2, height - 1);
                for (int y = miny; y < maxy; y += 1)
                {
                    for (int tx = minx; tx < maxx; tx += 1)
                    {
                        triplets[tx, y] = TripletLookup.lookup[triplets[tx, y].State1];
                    }
                }
            }

            foreach ((int cx, int cy) in previousChanges)
            {
                int minx = Max(cx - 1, 1);
                int maxx = Min(cx + 2, width - 1);
                int miny = Max(cy - 1, 1);
                int maxy = Min(cy + 2, height - 1);
                for (int y = miny; y < maxy; y += 1)
                {
                    for (int tx = minx; tx < maxx; tx += 1)
                    {
                        bool changed = lookup2[triplets[tx, y].State2](tx, y);
                        if (changed)
                            changes.Add((tx, y));
                    }
                }
            }
        }

        public void Draw(LifeRect rect, Action<LifePoint> setPixel)
        {
            long xmin = Max(0, rect.X);
            long xmax = Min(width * 3, rect.X + rect.Width);
            long ymin = Max(0, rect.Y - rect.Height + 1);
            long ymax = Min(height, rect.Y + 1);
            for (long y = ymin; y < ymax; y += 1)
                for (long x = xmin; x < xmax; x += 1)
                    if (this[x, y])
                        setPixel(new LifePoint(x, y));
        }
    }
}
