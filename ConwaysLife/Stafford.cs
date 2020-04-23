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
    
    struct Triplet
    {
        // We represent three adjacent cells using 15 bits of a short.
        // Call the cells Left, Middle and Right.
        private short triplet;
        public Triplet(short triplet)
        {
            this.triplet = triplet;
        }

        // Bits 12, 13 and 14 are the state of the cells on the next tick.
        private const int lnext = 14;
        private const int mnext = 13;
        private const int rnext = 12;
        // Bits 9, 10 and 11 are the state of the cells on the current tick.
        private const int lcur = 11;
        private const int mcur = 10;
        private const int rcur = 9;
        // Bits 6, 7 and 8 are the count of living neighbors of the left
        // cell; three bits is not enough to count up to eight living
        // neighbours, but we already know the state of the neighbor
        // to the right; it is the middle cell.
        private const int lcount = 6;
        // Similarly for bits 3, 4 and 5 for the middle cell.
        private const int mcount = 3;
        // And similarly for bits 0, 1 and 2 for the right cell.
        private const int rcount = 0;

        public int LeftNext => (triplet & (1 << lnext)) >> lnext;
        public int MiddleNext => (triplet & (1 << mnext)) >> mnext;
        public int RightNext => (triplet & (1 << rnext)) >> rnext;

        public Triplet SetLeftNext() => new Triplet((short)(triplet | (1 << lnext)));
        public Triplet SetMiddleNext() => new Triplet((short)(triplet | (1 << mnext)));
        public Triplet SetRightNext() => new Triplet((short)(triplet | (1 << rnext)));

        public int LeftCurrent => (triplet & (1 << lcur)) >> lcur;
        public int MiddleCurrent => (triplet & (1 << mcur)) >> mcur;
        public int RightCurrent => (triplet & (1 << rcur)) >> rcur;

        public Triplet SetLeftCurrent() => new Triplet((short)(triplet | (1 << lcur)));
        public Triplet SetMiddleCurrent() => new Triplet((short)(triplet | (1 << mcur)));
        public Triplet SetRightCurrent() => new Triplet((short)(triplet | (1 << rcur)));

        public int LeftCountRaw => (triplet & (7 << lcount)) >> lcount;
        public int MiddleCountRaw => (triplet & (7 << mcount)) >> mcount;
        public int RightCountRaw => (triplet & (7 << rcount)) >> rcount;

        public int LeftCount => MiddleCurrent + LeftCountRaw;
        public int MiddleCount => LeftCurrent + RightCurrent + MiddleCountRaw;
        public int RightCount => MiddleCurrent + RightCountRaw;

        public Triplet SetLeftCountRaw(int c)
        {
            Debug.Assert(0 <= c && c <= 7);
            return new Triplet((short)(triplet & (~7 << lcount) | (c << lcount)));
        }

        public Triplet SetMiddleCountRaw(int c)
        {
            Debug.Assert(0 <= c && c <= 6);
            return new Triplet((short)(triplet & (~7 << mcount) | (c << mcount)));
        }

        public Triplet SetRightCountRaw(int c)
        {
            Debug.Assert(0 <= c && c <= 7);
            return new Triplet((short)(triplet & (~7 << rcount) | (c << rcount)));
        }

        public Triplet IncLeft() => SetLeftCountRaw(LeftCountRaw + 1);
        public Triplet DecLeft() => SetLeftCountRaw(LeftCountRaw - 1);
        public Triplet IncMiddle() => SetMiddleCountRaw(MiddleCountRaw + 1);
        public Triplet DecMiddle() => SetMiddleCountRaw(MiddleCountRaw - 1);
        public Triplet IncRight() => SetRightCountRaw(RightCountRaw + 1);
        public Triplet DecRight() => SetRightCountRaw(RightCountRaw - 1);
    }

    class Stafford : ILife
    {
        private int height = 256;
        private int width = 89; // Width in triplets
        private Triplet[] triplets;

        public Stafford()
        {
            Clear();
        }

        public void Clear()
        {
            triplets = new Triplet[width * height];
        }

        private bool IsValidPoint(long x, long y) =>
            0 <= x && x < width * 3 && 0 <= y && y < height;

        public bool this[long x, long y]
        {
            get
            {
                if (IsValidPoint(x, y))
                {
                    Triplet t = triplets[x / 3 + y * width];
                    switch (x % 3)
                    {
                        case 0: return t.LeftCurrent == 1;
                        case 1: return t.MiddleCurrent == 1;
                        default: return t.RightCurrent == 1;
                    }
                }
                return false;
            }
            set
            {
                throw new NotImplementedException();
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
           

            throw new NotImplementedException();
        }

        public void Draw(LifeRect rect, Action<LifePoint> setPixel)
        {
            long xmin = Max(0, rect.X);
            long xmax = Min(width, rect.X + rect.Width);
            long ymin = Max(0, rect.Y - rect.Height + 1);
            long ymax = Min(height, rect.Y + 1);
            for (long y = ymin; y < ymax; y += 1)
                for (long x = xmin; x < xmax; x += 1)
                    if (this[x, y])
                        setPixel(new LifePoint(x, y));
        }
    }
}
