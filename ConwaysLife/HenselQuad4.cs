namespace ConwaysLife.Hensel
{
    using System.Diagnostics;
    using static HenselLookup;
    using static Quad2;
    using static Quad4State;

    enum Quad4State
    {
        Active,
        Stable,
        Dead
    }

    sealed class Quad4 
    {
        // A quad4 represents a 16 x 16 grid located at coordinates (x * 16, y * 16).
        //
        // If we have 65536 possible values for x and y, that gives us a grid of a million by a million
        // roughly, which is pretty decent.

        public Quad4(int x, int y)
        {
            X = (short)x;
            Y = (short)y;
        }

        public short X { get; }
        public short Y { get; }

        public Quad4 S { get; set; }
        public Quad4 E { get; set; }
        public Quad4 SE { get; set; }
        public Quad4 N { get; set; }
        public Quad4 W { get; set; }
        public Quad4 NW { get; set; }

        // A quad4 is actually a pair of quad4s, one for the even numbered generations,
        // one for the odd numbered generations.
        //
        // Rather than having another data structure for a single quad4, we'll just put eight
        // quad3s in here, four for the even cycle and four for the odd.

        private Quad3 evenNW;
        private Quad3 evenSW;
        private Quad3 evenNE;
        private Quad3 evenSE;

        private Quad3 oddNW;
        private Quad3 oddSW;
        private Quad3 oddNE;
        private Quad3 oddSE;

        private uint evenstate;

        // This is an array of 32 bits which tracks change information for regions of
        // a quad4, broken down into regions of the individual quad3s. That is, each bit
        // gives the state of a rectangular region of one of the quad3s in the quad4.
        //
        // There are three possible states: 
        //
        // * active: cells have changed recently.
        // * stable: the new state is the same as the previous state.
        // * dead: the new state is the same as the previous state, and moreover,
        //   all the cells in the region are dead.

        // We represent these three states in two bits for each of four regions
        // in every quad3:
        //
        // * both bits on is dead
        // * low bit on, high bit off is stable
        // * both bits off is active

        // The regions we track in each even-cycle quad3 are:
        //
        // * The entire 8x8 quad3
        // * The 2x8 western edge of the quad3
        // * the 8x2 northern edge of the quad3
        // * the 2x2 northwestern corner of the quad3
        //
        // The details of setting these bits are handled by Quad3 and
        // Quad3State. However, when we get the bits, we usually want
        // to get them in combination; it's efficient to do so with a
        // single masking operation across all four Quad3s.
        //
        // Bit meanings are:
        //
        //  7: SE all dead
        //  6: SE W edge dead
        //  5: SE N edge dead
        //  4: SE NW corner dead
        //  3: SE all stable
        //  2: SE W edge stable
        //  1: SE N edge stable
        //  0: SE NW corner stable
        // 
        // Bits 8-15: same, but for NE
        // Bits 16-23: same, but for SW
        // Bits 24-31: same, but for NW
        //
        // We similarly track these regions in each odd-cycle quad3:
        //
        // * The entire 8x8 quad3
        // * The 2x8 eastern edge of the quad3
        // * the 8x2 southern edge of the quad3
        // * the 2x2 southeastern corner of the quad3
        //
        // Bit meanings are:
        //
        //  7: SE all dead
        //  6: SE E edge dead
        //  5: SE S edge dead
        //  4: SE SE corner dead
        //  3: SE all stable
        //  2: SE E edge stable
        //  1: SE S edge stable
        //  0: SE SE corner stable
        // 
        // Bits 8-15: same, but for NE
        // Bits 16-23: same, but for SW
        // Bits 24-31: same, but for NW

        private Quad3State EvenSEState
        {
            get => new Quad3State(evenstate & 0x000000ff);
            set => evenstate = (evenstate & 0xffffff00) | (uint)value;
        }

        private Quad3State EvenNEState
        {
            get => new Quad3State((evenstate & 0x0000ff00) >> 8);
            set => evenstate = (evenstate & 0xffff00ff) | ((uint)value << 8);
        }

        private Quad3State EvenSWState
        {
            get => new Quad3State((evenstate & 0x00ff0000) >> 16);
            set => evenstate = (evenstate & 0xff00ffff) | ((uint)value << 16);
        }

        private Quad3State EvenNWState
        {
            get => new Quad3State((evenstate & 0x00ff0000) >> 24);
            set => evenstate = (evenstate & 0x00ffffff) | ((uint)value << 24);
        }

        private uint oddstate;

        private Quad3State OddSEState
        {
            get => new Quad3State(oddstate & 0x000000ff);
            set => oddstate = (oddstate & 0xffffff00) | (uint)value;
        }

        private Quad3State OddNEState
        {
            get => new Quad3State((oddstate & 0x0000ff00) >> 8);
            set => oddstate = (oddstate & 0xffff00ff) | ((uint)value << 8);
        }

        private Quad3State OddSWState
        {
            get => new Quad3State((oddstate & 0x00ff0000) >> 16);
            set => oddstate = (oddstate & 0xff00ffff) | ((uint)value << 16);
        }

        private Quad3State OddNWState
        {
            get => new Quad3State((oddstate & 0xff000000) >> 24);
            set => oddstate = (oddstate & 0x00ffffff) | ((uint)value << 24);
        }

        // Getters

        public bool EvenNorthwestCornerActive => (evenstate & 0x01000000) != 0x01000000;
        public bool EvenWestEdgeActive => (evenstate & 0x04040000) != 0x04040000;
        public bool EvenNorthEdgeActive => (evenstate & 0x02000200) != 0x02000200;
        private bool EvenSoutheastActive => (evenstate & 0x00000008) != 0x00000008;
        private bool EvenNorthwestOrBorderingActive => (evenstate & 0x08020401) != 0x08020401;
        private bool EvenSouthwestOrBorderingActive => (evenstate & 0x00080004) != 0x00080004;
        private bool EvenNortheastOrBorderingActive => (evenstate & 0x00000802) != 0x00000802;
        private bool EvenNorthEdge8EastActive => (evenstate & 0x00000200) != 0x00000200;
        private bool EvenWestEdge8SouthActive => (evenstate & 0x00040000) != 0x00040000;
        private bool EvenNorthEdge10WestActive => (evenstate & 0x02000100) != 0x02000100;
        private bool EvenWestEdge10NorthActive => (evenstate & 0x04010000) != 0x04010000;

        public bool OddSoutheastCornerActive => (oddstate & 0x00000001) != 0x00000001;
        public bool OddEastEdgeActive => (oddstate & 0x00000404) != 0x00000404;
        public bool OddSouthEdgeActive => (oddstate & 0x00020002) != 0x00020002;
        private bool OddNorthwestActive => (oddstate & 0x08000000) != 0x08000000;
        private bool OddSoutheastOrBorderingActive => (oddstate & 0x01040208) != 0x01040208;
        private bool OddNortheastOrBorderingActive => (oddstate & 0x04000800) != 0x04000800;
        private bool OddSouthwestOrBorderingActive => (oddstate & 0x02080000) != 0x02080000;
        private bool OddSouthEdge8WestActive => (oddstate & 0x00020000) != 0x00020000;
        private bool OddEastEdge8NorthActive => (oddstate & 0x00000400) != 0x00000400;
        private bool OddSouthEdge10EastActive => (oddstate & 0x00010002) != 0x00010002;
        private bool OddEastEdge10SouthActive => (oddstate & 0x00000104) != 0x00000104;        

        // Suppose we are in an even generation K and we wish to know if there is
        // any point in computing the next odd generation K+1 of a particular Quad3,
        // say the NW Quad3. Recall that the odd generation will be one cell to
        // the southeast of the even generation:
        //
        //   +......+..-----+  
        //   .+------+.     |
        //   .|      |.     |
        //   .| o NW |.e NE |
        //   .|      |.     |
        //   .|      |.     |
        //   .|      |.     |
        //   +|      |.-----+
        //   .+------+.     |
        //   ..........     |
        //   | e SW | e SE  |
        //
        // It suffices to know if anything in the 10x10 region which surrounds
        // the odd NW quad3 -- edged with dots -- is active. If none of it is, 
        // then the odd NW quad3 will be stable or dead. Remember, if those even 
        // regions are marked as stable, that's because in generation K-1 we
        // compared generation K-2 to generation K and determined there was no
        // change, so we can have confidence that if nothing in the dotted
        // region changed recently, then the next state of odd NW will be the same
        // also.
        //
        // We check this 10x10 region by checking the even NW entire Quad3,
        // the even NE west edge, the even SW north edge, and the even
        // SE northwest corner for activity; if any were active, then 
        // we must compute the next state of odd NW. If not, we can re-use the
        // current state.

        private bool OddNWPossiblyActive() =>
            EvenNorthwestOrBorderingActive;

        private bool OddSWPossiblyActive() =>
            EvenSouthwestOrBorderingActive ||
            S != null && S.EvenNorthEdge10WestActive;

        private bool OddNEPossiblyActive() =>
            EvenNortheastOrBorderingActive ||
            E != null && E.EvenWestEdge10NorthActive;

        private bool OddSEPossiblyActive() =>
            EvenSoutheastActive ||
            S != null && S.EvenNorthEdge8EastActive ||
            E != null && E.EvenWestEdge8SouthActive ||
            SE != null && SE.EvenNorthwestCornerActive;

        private bool EvenNWPossiblyActive() =>
            OddNorthwestActive ||
            N != null && N.OddSouthEdge8WestActive ||
            W != null && W.OddEastEdge8NorthActive ||
            NW != null && NW.OddSoutheastCornerActive;

        private bool EvenNEPossiblyActive() =>
            OddNortheastOrBorderingActive ||
            N != null && N.OddSouthEdge10EastActive;

        private bool EvenSEPossiblyActive() =>
            OddSoutheastOrBorderingActive;

        private bool EvenSWPossiblyActive() =>
            OddSouthwestOrBorderingActive ||
            W != null && W.OddEastEdge10SouthActive;

        // Stepping

        private void StepEvenNW()
        {
            if (OddNWPossiblyActive())
            {
                Quad3 newOddNW = Step9Quad2ToQuad3Even(
                    evenNW.NW,
                    evenNW.NE,
                    evenNE.NW,
                    evenNW.SW,
                    evenNW.SE,
                    evenNE.SW,
                    evenSW.NW,
                    evenSW.NE,
                    evenSE.NW);
                OddNWState = oddNW.UpdateOddQuad3State(newOddNW, OddNWState);
                oddNW = newOddNW;
            }
            else
            {
                OddNWState = oddNW.MakeOddStableOrDead(OddNWState);
            }
        }

        private void StepEvenSW()
        {
            if (OddSWPossiblyActive())
            {
                Quad3 newOddSW = Step9Quad2ToQuad3Even(
                    evenSW.NW,
                    evenSW.NE,
                    evenSE.NW,
                    evenSW.SW,
                    evenSW.SE,
                    evenSE.SW,
                    S == null ? AllDead : S.evenNW.NW,
                    S == null ? AllDead : S.evenNW.NE,
                    S == null ? AllDead : S.evenNE.NW);
                OddSWState = oddSW.UpdateOddQuad3State(newOddSW, OddSWState);
                oddSW = newOddSW;
            }
            else
                OddSWState = oddSW.MakeOddStableOrDead(OddSWState);
        }

        private void StepEvenNE()
        {
            if (OddNEPossiblyActive())
            {
                Quad3 newOddNE = Step9Quad2ToQuad3Even(
                    evenNE.NW,
                    evenNE.NE,
                    E == null ? AllDead : E.evenNW.NW,
                    evenNE.SW,
                    evenNE.SE,
                    E == null ? AllDead : E.evenNW.SW,
                    evenSE.NW,
                    evenSE.NE,
                    E == null ? AllDead : E.evenSW.NW);
                OddNEState = oddNE.UpdateOddQuad3State(newOddNE, OddNEState);
                oddNE = newOddNE;
            }
            else
                OddNEState = oddNE.MakeOddStableOrDead(OddNEState);
        }

        private void StepEvenSE()
        {
            if (OddSEPossiblyActive())
            {
                Quad3 newOddSE = Step9Quad2ToQuad3Even(
                    evenSE.NW,
                    evenSE.NE,
                    E == null ? AllDead : E.evenSW.NW,
                    evenSE.SW,
                    evenSE.SE,
                    E == null ? AllDead : E.evenSW.SW,
                    S == null ? AllDead : S.evenNE.NW,
                    S == null ? AllDead : S.evenNE.NE,
                    SE == null ? AllDead : SE.evenNW.NW);
                OddSEState = oddSE.UpdateOddQuad3State(newOddSE, OddSEState);
                oddSE = newOddSE;
            }
            else
                OddSEState = oddSE.MakeOddStableOrDead(OddSEState);
        }

        public void StepEven()
        {
            StepEvenNW();
            StepEvenSW();
            StepEvenNE();
            StepEvenSE();
        }

        private void StepOddNW()
        {
            if (EvenNWPossiblyActive())
            {
                Quad3 newEvenNW = Step9Quad2ToQuad3Odd(
                    NW == null ? AllDead : NW.oddSE.SE,
                    N == null ? AllDead : N.oddSW.SW,
                    N == null ? AllDead : N.oddSW.SE,
                    W == null ? AllDead : W.oddNE.NE,
                    oddNW.NW,
                    oddNW.NE,
                    W == null ? AllDead : W.oddNE.SE,
                    oddNW.SW,
                    oddNW.SE);
                EvenNWState = evenNW.UpdateEvenQuad3State(newEvenNW, EvenNWState);
                evenNW = newEvenNW;
            }
            else
                EvenNWState = evenNW.MakeEvenStableOrDead(EvenNWState);
        }

        private void StepOddSW()
        {
            if (EvenSWPossiblyActive())
            {
                Quad3 newEvenSW = Step9Quad2ToQuad3Odd(
                    W == null ? AllDead : W.oddNE.SE,
                    oddNW.SW,
                    oddNW.SE,
                    W == null ? AllDead : W.oddSE.NE,
                    oddSW.NW,
                    oddSW.NE,
                    W == null ? AllDead : W.oddSE.SE,
                    oddSW.SW,
                    oddSW.SE);
                EvenSWState = evenSW.UpdateEvenQuad3State(newEvenSW, EvenSWState);
                evenSW = newEvenSW;
            }
            else
                EvenSWState = evenSW.MakeEvenStableOrDead(EvenSWState);
        }

        private void StepOddNE()
        {
            if (EvenNEPossiblyActive())
            {
                Quad3 newEvenNE = Step9Quad2ToQuad3Odd(
                    N == null ? AllDead : N.oddSW.SE,
                    N == null ? AllDead : N.oddSE.SW,
                    N == null ? AllDead : N.oddSE.SE,
                    oddNW.NE,
                    oddNE.NW,
                    oddNE.NE,
                    oddNW.SE,
                    oddNE.SW,
                    oddNE.SE);
                EvenNEState = evenNE.UpdateEvenQuad3State(newEvenNE, EvenNEState);
                evenNE = newEvenNE;
            }
            else
                EvenNEState = evenNE.MakeEvenStableOrDead(EvenNEState);
        }

        private void StepOddSE()
        {
            if (EvenSEPossiblyActive())
            {
                Quad3 newEvenSE = Step9Quad2ToQuad3Odd(
                    oddNW.SE,
                    oddNE.SW,
                    oddNE.SE,
                    oddSW.NE,
                    oddSE.NW,
                    oddSE.NE,
                    oddSW.SE,
                    oddSE.SW,
                    oddSE.SE);
                EvenSEState = evenSE.UpdateEvenQuad3State(newEvenSE, EvenSEState);
                evenSE = newEvenSE;
            }
            else
                EvenSEState = evenSE.MakeEvenStableOrDead(EvenSEState);
        }

        public void StepOdd()
        {
            StepOddNW();
            StepOddSW();
            StepOddNE();
            StepOddSE();
        }

        // Reading and writing individual cells

        public bool GetEven(int x, int y)
        {
            Debug.Assert(0 <= x && x < 16);
            Debug.Assert(0 <= y && y < 16);
            if (x < 8)
            {
                if (y < 8)
                    return evenSW.Get(x, y);
                return evenNW.Get(x, y - 8);
            }
            else if (y < 8)
                return evenSE.Get(x - 8, y);
            else
                return evenNE.Get(x - 8, y - 8);
        }

        public void SetEven(int x, int y)
        {
            Debug.Assert(0 <= x && x < 16);
            Debug.Assert(0 <= y && y < 16);
            if (x < 8)
            {
                if (y < 8)
                {
                    evenSW = evenSW.Set(x, y);
                }
                else
                {
                    evenNW = evenNW.Set(x, y - 8);
                }
            }
            else if (y < 8)
            {
                evenSE = evenSE.Set(x - 8, y);
            }
            else
            {                
                evenNE = evenNE.Set(x - 8, y - 8);
            }
        }

        public void ClearEven(int x, int y)
        {
            Debug.Assert(0 <= x && x < 16);
            Debug.Assert(0 <= y && y < 16);
            if (x < 8)
            {
                if (y < 8)
                {
                    evenSW = evenSW.Clear(x, y);
                }
                else
                {
                    evenNW = evenNW.Clear(x, y - 8);
                }
            }
            else if (y < 8)
            {
                evenSE = evenSE.Clear(x - 8, y);
            }
            else
            {
                evenNE = evenNE.Clear(x - 8, y - 8);
            }
        }

        public bool GetOdd(int x, int y)
        {
            Debug.Assert(0 <= x && x < 16);
            Debug.Assert(0 <= y && y < 16);
            if (x < 8)
            {
                if (y < 8)
                    return oddSW.Get(x, y);
                return oddNW.Get(x, y - 8);
            }
            else if (y < 8)
                return oddSE.Get(x - 8, y);
            else
                return oddNE.Get(x - 8, y - 8);
        }

        public void SetOdd(int x, int y)
        {
            Debug.Assert(0 <= x && x < 16);
            Debug.Assert(0 <= y && y < 16);
            if (x < 8)
            {
                if (y < 8)
                    oddSW = oddSW.Set(x, y);
                else
                    oddNW = oddNW.Set(x, y - 8);
            }
            else if (y < 8)
                oddSE = oddSE.Set(x - 8, y);
            else
                oddNE = oddNE.Set(x - 8, y - 8);
        }

        public void ClearOdd(int x, int y)
        {
            Debug.Assert(0 <= x && x < 16);
            Debug.Assert(0 <= y && y < 16);
            if (x < 8)
            {
                if (y < 8)
                    oddSW = oddSW.Clear(x, y);
                else
                    oddNW = oddNW.Clear(x, y - 8);
            }
            else if (y < 8)
                oddSE = oddSE.Clear(x - 8, y);
            else
                oddNE = oddNE.Clear(x - 8, y - 8);
        }

        public override string ToString()
        {
            string s = $"{X}, {Y}\nEven\n";
            for (int y = 15; y >= 0; y -= 1)
            {
                for (int x = 0; x < 16; x += 1)
                    s += this.GetEven(x, y) ? 'O' : '.';
                s += "\n";
            }
            s += "\nOdd\n";
            for (int y = 15; y >= 0; y -= 1)
            {
                for (int x = 0; x < 16; x += 1)
                    s += this.GetOdd(x, y) ? 'O' : '.';
                s += "\n";
            }

            return s;
        }

    }
}
