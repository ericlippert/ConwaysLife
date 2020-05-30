using System.Diagnostics;

namespace ConwaysLife
{
    // This is the three-cells-in-one-short structure used by Stafford's algorithm,
    // with helper methods for the bit twiddling operations.

    struct Triplet
    {
        // We represent three adjacent cells using 15 bits of a short.
        // Call the cells Left, Middle and Right.
        private short triplet;
        public Triplet(short triplet)
        {
            this.triplet = triplet;
        }

        public Triplet(int triplet)
        {
            this.triplet = (short)triplet;
        }

        // The original implementation used bit 15 to store whether the cell
        // triplet was on the edge of the finite grid so that it could decide
        // quickly whether to do wrap-around semantics or not. We're not 
        // implementing wrap-around semantics in this series so bit 15 is unused.

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

        // Masks
        private const int lnm = 1 << lnext;
        private const int mnm = 1 << mnext;
        private const int rnm = 1 << rnext;
        private const int lcm = 1 << lcur;
        private const int mcm = 1 << mcur;
        private const int rcm = 1 << rcur;
        private const int lcountm = 7 << lcount;
        private const int mcountm = 7 << mcount;
        private const int rcountm = 7 << rcount;

        // Getters and setters for state

        // Note that I want to treat state as both a bool and an int,
        // so I've got getters for both cases.

        public bool LeftNext => (lnm & triplet) != 0;
        public bool MiddleNext => (mnm & triplet) != 0;
        public bool RightNext => (rnm & triplet) != 0;

        public int LeftNextRaw => (triplet & lnm) >> lnext;
        public int MiddleNextRaw => (triplet & mnm) >> mnext;
        public int RightNextRaw => (triplet & rnm) >> rnext;

        public Triplet SetLeftNext(bool b) => new Triplet(b ? (lnm | triplet) : (~lnm & triplet));
        public Triplet SetMiddleNext(bool b) => new Triplet(b ? (mnm | triplet) : (~mnm & triplet));
        public Triplet SetRightNext(bool b) => new Triplet(b ? (rnm | triplet) : (~rnm & triplet));

        public bool LeftCurrent => (lcm & triplet) != 0;
        public bool MiddleCurrent => (mcm & triplet) != 0;
        public bool RightCurrent => (rcm & triplet) != 0;

        public int LeftCurrentRaw => (triplet & lcm) >> lcur;
        public int MiddleCurrentRaw => (triplet & mcm) >> mcur;
        public int RightCurrentRaw => (triplet & rcm) >> rcur;

        public Triplet SetLeftCurrent(bool b) => new Triplet(b ? (lcm | triplet) : (~lcm & triplet));
        public Triplet SetMiddleCurrent(bool b) => new Triplet(b ? (mcm | triplet) : (~mcm & triplet));
        public Triplet SetRightCurrent(bool b) => new Triplet(b ? (rcm | triplet) : (~rcm & triplet));

        // Getters and setters for the neighbour counts
        
        // I've got getters for both the "raw" 3-bit integer stored in the 
        // triplet and the actual neighbour count it represents.

        public int LeftCountRaw => (lcountm & triplet) >> lcount;
        public int MiddleCountRaw => (mcountm & triplet) >> mcount;
        public int RightCountRaw => (rcountm & triplet) >> rcount;

        public int LeftCount => MiddleCurrentRaw + LeftCountRaw;
        public int MiddleCount => LeftCurrentRaw + RightCurrentRaw + MiddleCountRaw;
        public int RightCount => MiddleCurrentRaw + RightCountRaw;

        // Adding assertions caught a number of bugs as I was implementing the various
        // versions of this algorithm!

        public Triplet SetLeftCountRaw(int c)
        {
            Debug.Assert(0 <= c && c <= 7);
            return new Triplet((c << lcount) | ~lcountm & triplet);
        }

        public Triplet SetMiddleCountRaw(int c)
        {
            Debug.Assert(0 <= c && c <= 6);
            return new Triplet((c << mcount) | ~mcountm & triplet);
        }

        public Triplet SetRightCountRaw(int c)
        {
            Debug.Assert(0 <= c && c <= 7);
            return new Triplet((c << rcount) | ~rcountm & triplet);
        }

        // It is slow and tedious to change counts via the "set raw count"
        // mechanism; instead, let's make fifteen helper methods using the
        // naming convention:
        //
        // U = unchanged
        // P = increment
        // M = decrement

        private const int lcountone = 1 << lcount;
        private const int mcountone = 1 << mcount;
        private const int rcountone = 1 << rcount;

        public Triplet UUP() => new Triplet(rcountone + triplet);
        public Triplet UUM() => new Triplet(-rcountone + triplet);
        public Triplet UPU() => new Triplet(mcountone + triplet);
        public Triplet UPP() => new Triplet(mcountone + rcountone + triplet);
        public Triplet UMU() => new Triplet(-mcountone + triplet);
        public Triplet UMM() => new Triplet(-mcountone - rcountone + triplet);
        public Triplet PUU() => new Triplet(lcountone + triplet);
        public Triplet PUM() => new Triplet(lcountone - rcountone + triplet);
        public Triplet PPU() => new Triplet(lcountone + mcountone + triplet);
        public Triplet PPP() => new Triplet(lcountone + mcountone + rcountone + triplet);
        public Triplet MUU() => new Triplet(-lcountone + triplet);
        public Triplet MUP() => new Triplet(-lcountone + rcountone + triplet);
        public Triplet MMU() => new Triplet(-lcountone - mcountone + triplet);
        public Triplet MMM() => new Triplet(-lcountone - mcountone - rcountone + triplet);
    }
}
