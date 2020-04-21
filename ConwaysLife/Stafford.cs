using System;
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
        private Triplet[,] triplets;

        public Stafford()
        {
            Clear();
        }

        public void Clear()
        {
            triplets = new Triplet[width, height];
        }

        private bool IsValidPoint(long x, long y) =>
            0 <= x && x < width * 3 && 0 <= y && y < height;

        public bool this[long x, long y]
        {
            get
            {
                if (IsValidPoint(x, y))
                {
                    Triplet t = triplets[x / 3, y];
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
