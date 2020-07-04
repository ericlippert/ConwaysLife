using System;
using System.Collections.Generic;
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


    class Stafford : ILife
    {
        private int height;
        private int width; // Width in triplets
        private Triplet[,] triplets;
        private List<(int, int)> changes;


        // This is an array of delegates keyed on the old and new state;
        // each delegate performs the exact combination of neighbour
        // count increments and decrements.

        private Func<int, int, bool>[] lookup2;


        public Stafford(int size = 8)
        {
            size = Max(size, 8);
            height = (1 << size) + 2;
            width = ((1 << size) + 2) / 3 + 2;

            lookup2 = new Func<int, int, bool>[1 << 6]
            {
                // This table is keyed on the next and current bits. Suppose we have:
                //
                // next state is DDA -- dead, dead, alive
                // current state is DAD -- dead, alive, dead
                //
                // then what has to happen? The left is unchanged, the middle becomes dead
                // and the right becomes alive, so that is helper function "UDA".

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

        public int ChangedTriplets => changes.Count;

        public void Clear()
        {
            triplets = new Triplet[width, height];
            changes = new List<(int, int)>();
        }

        private bool IsValidPoint(long x, long y) =>
            1 <= x && x < (width - 1) * 3 && 1 <= y && y < height - 1;

        // Returns true if a change was made, false if the cell was already alive.
        private bool BecomeAlive(int x, int y)
        {
            int tx = x / 3;
            Triplet t = triplets[tx, y];
            switch (x % 3)
            {
                case 0:
                    if (t.LeftCurrent)
                        return false;
                    // Left is about to be born
                    triplets[tx - 1, y - 1] = triplets[tx - 1, y - 1].UUP();
                    triplets[tx, y - 1] = triplets[tx, y - 1].PPU();
                    triplets[tx - 1, y] = triplets[tx - 1, y].UUP();
                    triplets[tx, y] = t.SetLeftCurrent(true);
                    triplets[tx - 1, y + 1] = triplets[tx - 1, y + 1].UUP();
                    triplets[tx, y + 1] = triplets[tx, y + 1].PPU();
                    break;
                case 1:
                    if (t.MiddleCurrent)
                        return false;
                    // Middle is about to be born
                    triplets[tx, y - 1] = triplets[tx, y - 1].PPP();
                    triplets[tx, y] = t.SetMiddleCurrent(true);
                    triplets[tx, y + 1] = triplets[tx, y + 1].PPP();
                    break;
                case 2:
                    if (t.RightCurrent)
                        return false;
                    // Right is about to be born
                    triplets[tx, y - 1] = triplets[tx, y - 1].UPP();
                    triplets[tx + 1, y - 1] = triplets[tx + 1, y - 1].PUU();
                    triplets[tx, y] = t.SetRightCurrent(true);
                    triplets[tx + 1, y] = triplets[tx + 1, y].PUU();
                    triplets[tx, y + 1] = triplets[tx, y + 1].UPP();
                    triplets[tx + 1, y + 1] = triplets[tx + 1, y + 1].PUU();
                    break;
            }
            return true;
        }


        private bool BecomeDead(int x, int y)
        {
            int tx = x / 3;
            Triplet t = triplets[tx, y];

            switch (x % 3)
            {
                case 0:
                    if (!t.LeftCurrent)
                        return false;
                    triplets[tx - 1, y - 1] = triplets[tx - 1, y - 1].UUM();
                    triplets[tx, y - 1] = triplets[tx, y - 1].MMU();
                    triplets[tx - 1, y] = triplets[tx - 1, y].UUM();
                    triplets[tx, y] = t.SetLeftCurrent(false);
                    triplets[tx - 1, y + 1] = triplets[tx - 1, y + 1].UUM();
                    triplets[tx, y + 1] = triplets[tx, y + 1].MMU();
                    break;
                case 1:
                    if (!t.MiddleCurrent)
                        return false;
                    triplets[tx, y - 1] = triplets[tx, y - 1].MMM();
                    triplets[tx, y] = t.SetMiddleCurrent(false);
                    triplets[tx, y + 1] = triplets[tx, y + 1].MMM();
                    break;
                case 2:
                    if (!t.RightCurrent)
                        return false;
                    triplets[tx, y - 1] = triplets[tx, y - 1].UMM();
                    triplets[tx + 1, y - 1] = triplets[tx + 1, y - 1].MUU();
                    triplets[tx, y] = t.SetRightCurrent(false);
                    triplets[tx + 1, y] = triplets[tx + 1, y].MUU();
                    triplets[tx, y + 1] = triplets[tx, y + 1].UMM();
                    triplets[tx + 1, y + 1] = triplets[tx + 1, y + 1].MUU();
                    break;
            }
            return true;
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
                        if (BecomeAlive((int)x, (int)y))
                            changes.Add(((int)x / 3, (int)y));
                    }
                    else
                    {
                        if (BecomeDead((int)x, (int)y))
                            changes.Add(((int)x / 3, (int)y));
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
            // First pass: for the previous changes and all their neighbours, record their
            // new states. If the new state changed, make a note of that.

            // This list might have duplicates.
            var currentChanges = new List<(int, int)>();

            foreach ((int cx, int cy) in changes)
            {
                int minx = Max(cx - 1, 1);
                int maxx = Min(cx + 2, width - 1);
                int miny = Max(cy - 1, 1);
                int maxy = Min(cy + 2, height - 1);
                for (int y = miny; y < maxy; y += 1)
                {
                    for (int tx = minx; tx < maxx; tx += 1)
                    {
                        int key1 = triplets[tx, y].State1;
                        if (TripletLookup.changed[key1])
                        {
                            triplets[tx, y] = TripletLookup.lookup[key1];
                            currentChanges.Add((tx, y));
                        }
                    }
                }
            }

            // We're done with the previous changes list, so throw it away.
            changes.Clear();

            foreach ((int x, int y) in currentChanges)
            {
                int key2 = triplets[x, y].State2;
                Func<int, int, bool> helper = lookup2[key2];
                bool changed = helper(x, y);
                if (changed)
                {
                    triplets[x, y] = triplets[x, y].NextToCurrent();
                    changes.Add((x, y));
                }
            }
        }

        public void Step(int speed)
        {
            for (int i = 0; i < 1L << speed; i += 1)
                Step();
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
            triplets[tx + 1, y] = triplets[tx + 1, y].PUU();
            triplets[tx, y + 1] = triplets[tx, y + 1].UPP();
            triplets[tx + 1, y + 1] = triplets[tx + 1, y + 1].PUU();
            return true;
        }

        private bool UUD(int tx, int y)
        {
            triplets[tx, y - 1] = triplets[tx, y - 1].UMM();
            triplets[tx + 1, y - 1] = triplets[tx + 1, y - 1].MUU();
            triplets[tx + 1, y] = triplets[tx + 1, y].MUU();
            triplets[tx, y + 1] = triplets[tx, y + 1].UMM();
            triplets[tx + 1, y + 1] = triplets[tx + 1, y + 1].MUU();
            return true;
        }

        private bool UAU(int tx, int y)
        {
            triplets[tx, y - 1] = triplets[tx, y - 1].PPP();
            triplets[tx, y + 1] = triplets[tx, y + 1].PPP();
            return true;
        }

        private bool UAA(int tx, int y)
        {
            triplets[tx, y - 1] = triplets[tx, y - 1].PP2P2();
            triplets[tx + 1, y - 1] = triplets[tx + 1, y - 1].PUU();
            triplets[tx + 1, y] = triplets[tx + 1, y].PUU();
            triplets[tx, y + 1] = triplets[tx, y + 1].PP2P2();
            triplets[tx + 1, y + 1] = triplets[tx + 1, y + 1].PUU();
            return true;
        }


        private bool UAD(int tx, int y)
        {
            triplets[tx, y - 1] = triplets[tx, y - 1].PUU();
            triplets[tx + 1, y - 1] = triplets[tx + 1, y - 1].MUU();
            triplets[tx + 1, y] = triplets[tx + 1, y].MUU();
            triplets[tx, y + 1] = triplets[tx, y + 1].PUU();
            triplets[tx + 1, y + 1] = triplets[tx + 1, y + 1].MUU();
            return true;
        }

        private bool UDU(int tx, int y)
        {
            triplets[tx, y - 1] = triplets[tx, y - 1].MMM();
            triplets[tx, y + 1] = triplets[tx, y + 1].MMM();
            return true;
        }

        private bool UDA(int tx, int y)
        {
            triplets[tx, y - 1] = triplets[tx, y - 1].MUU();
            triplets[tx + 1, y - 1] = triplets[tx + 1, y - 1].PUU();
            triplets[tx + 1, y] = triplets[tx + 1, y].PUU();
            triplets[tx, y + 1] = triplets[tx, y + 1].MUU();
            triplets[tx + 1, y + 1] = triplets[tx + 1, y + 1].PUU();
            return true;
        }

        private bool UDD(int tx, int y)
        {
            triplets[tx, y - 1] = triplets[tx, y - 1].MM2M2();
            triplets[tx + 1, y - 1] = triplets[tx + 1, y - 1].MUU();
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
            triplets[tx + 1, y] = triplets[tx + 1, y].MUU();
            triplets[tx - 1, y + 1] = triplets[tx - 1, y + 1].UUM();
            triplets[tx, y + 1] = triplets[tx, y + 1].M2M3M2();
            triplets[tx + 1, y + 1] = triplets[tx + 1, y + 1].MUU();
            return true;
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
