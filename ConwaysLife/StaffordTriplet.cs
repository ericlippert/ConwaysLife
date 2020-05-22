﻿using System.Diagnostics;

namespace ConwaysLife
{

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

        // Values

        private const int lcountone = 1 << lcount;
        private const int mcountone = 1 << mcount;
        private const int rcountone = 1 << rcount;

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

        // These set the current state bits "in parallel". 
        // U = unchanged
        // A = becomes alive
        // D = becomes dead

        public Triplet UUU() => this;
        // Left unchanged, middle unchanged, right alive.
        public Triplet UUA() => new Triplet((rcm | triplet));
        // Left unchanged, middle unchanged, right dead.
        public Triplet UUD() => new Triplet(~rcm & triplet);

        // And so on; there are only 27 cases so just enumerate them all.

        public Triplet UAU() => new Triplet(mcm | triplet);
        // The compiler will fold the constants, so this is a single or at runtime.
        public Triplet UAA() => new Triplet(mcm | rcm | triplet);
        public Triplet UAD() => new Triplet(mcm | ~rcm & triplet);

        public Triplet UDU() => new Triplet(~mcm & triplet);
        public Triplet UDA() => new Triplet(rcm | ~mcm & triplet);
        public Triplet UDD() => new Triplet(~(mcm | rcm) & triplet);

        public Triplet AUU() => new Triplet(lcm | triplet);
        public Triplet AUA() => new Triplet(lcm | rcm | triplet);
        public Triplet AUD() => new Triplet(lcm | ~rcm & triplet);

        public Triplet AAU() => new Triplet(lcm | mcm | triplet);
        public Triplet AAA() => new Triplet(lcm | mcm | rcm | triplet);
        public Triplet AAD() => new Triplet(lcm | mcm | ~rcm & triplet);

        public Triplet ADU() => new Triplet(lcm | ~mcm & triplet);
        public Triplet ADA() => new Triplet(lcm | rcm | ~mcm & triplet);
        public Triplet ADD() => new Triplet(lcm | ~(mcm | rcm) & triplet);

        public Triplet DUU() => new Triplet(~lcm & triplet);
        public Triplet DUA() => new Triplet(rcm | ~lcm & triplet);
        public Triplet DUD() => new Triplet(~(rcm | lcm) & triplet);

        public Triplet DAU() => new Triplet(mcm | ~lcm & triplet);
        public Triplet DAA() => new Triplet(mcm | rcm | ~lcm & triplet);
        public Triplet DAD() => new Triplet(mcm | ~(rcm | lcm) & triplet);

        public Triplet DDU() => new Triplet(~(lcm | mcm) & triplet);
        public Triplet DDA() => new Triplet(rcm | ~(lcm | mcm) & triplet);
        public Triplet DDD() => new Triplet(~(lcm | mcm | rcm) & triplet);

        public Triplet SetLeftCurrent(bool b) => b ? AUU() : DUU();
        public Triplet SetMiddleCurrent(bool b) => b ? UAU() : UDU();
        public Triplet SetRightCurrent(bool b) => b ? UUA() : UUD();

        public int LeftCountRaw => (lcountm & triplet) >> lcount;
        public int MiddleCountRaw => (mcountm & triplet) >> mcount;
        public int RightCountRaw => (rcountm & triplet) >> rcount;

        public int LeftCount => MiddleCurrentRaw + LeftCountRaw;
        public int MiddleCount => LeftCurrentRaw + RightCurrentRaw + MiddleCountRaw;
        public int RightCount => MiddleCurrentRaw + RightCountRaw;

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

        // Similarly, there are 27 possible combinations of 
        // unchanged/increment/decrement the left/middle/right count, 
        // but we only use 15 of them.
        //
        // U = unchanged
        // P = increment
        // M = decrement
        //
        // Again, if we put all the constant arithmetic on the left
        // then we know the compiler will fold it.

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

        // We also need these:

        public Triplet PP2P2() => new Triplet(lcountone + 2 * mcountone + 2 * rcountone + triplet);
        public Triplet PP2P() => new Triplet(lcountone + 2 * mcountone + rcountone + triplet);
        public Triplet P2P2P() => new Triplet(2 * lcountone + 2 * mcountone + rcountone + triplet);
        public Triplet P2P3P2() => new Triplet(2 * lcountone + 3 * mcountone + 2 * rcountone + triplet);
        public Triplet P2PU() => new Triplet(2 * lcountone + mcountone + triplet);
        public Triplet UPP2() => new Triplet(mcountone + 2 * rcountone + triplet);

        public Triplet MM2M2() => new Triplet(-lcountone - 2 * mcountone - 2 * rcountone + triplet);
        public Triplet MM2M() => new Triplet(-lcountone - 2 * mcountone - 1 * rcountone + triplet);
        public Triplet M2M2M() => new Triplet(-2 * lcountone - 2 * mcountone - rcountone + triplet);
        public Triplet M2M3M2() => new Triplet(-2 * lcountone - 3 * mcountone - 2 * rcountone + triplet);
        public Triplet M2MU() => new Triplet(-2 * lcountone - mcountone + triplet);
        public Triplet UMM2() => new Triplet(-mcountone - 2 * rcountone + triplet);

        public int State1 => triplet & 0x0fff;
        public int State2 => triplet >> 9;
    }
}