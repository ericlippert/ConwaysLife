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

    // The explanation of how Stafford's algorithm works is a little tricky, so I've
    // broken it down here into six steps:
    //
    // * Abrash's "store the neighbour counts" algorithm
    // * Abrash's algorithm plus a change list 
    // * Abrash's algorithm with a "next bit" so there is only one array
    //   (This is an O(change) step algorithm)
    // * Stafford's algorithm with a change list and next bits.
    // * Stafford's algorithm with change list, next bits, and lookup table
    //   for the first pass
    // * Stafford's algorithm with change list, next bits, and lookup tables
    //   for both passes.
    //
    // This is the final version with all optimizations.

    sealed class Stafford : ILife
    {
        // Top and bottom rows are always dead, so 256 cells in
        // effective height.
        private int height = 258;
        // Width in triplets. That gives us 264 cells, but the 
        // leftmost and rightmost triplet in a row is always all dead,
        // so that's only 258 cells wide.
        private int width = 88;
        private Triplet[,] triplets;
        // Coordinates of triplets, not cells.
        private List<(int, int)> changes;
        // This is an array of delegates keyed on the old and new state;
        // each delegate performs the exact combination of neighbour
        // count increments and decrements.
        private Func<int, int, bool>[] lookup2;

        public Stafford()
        {
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
                        int key1 = triplets[tx, y].LookupKey1;
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
                int key2 = triplets[x, y].LookupKey2;
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
