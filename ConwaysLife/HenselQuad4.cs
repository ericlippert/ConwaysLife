namespace ConwaysLife.Hensel
{
    using System.Diagnostics;
    using static HenselLookup;
    using static Quad2;

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

        // Stepping

        private void StepEvenNW()
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

        private void StepEvenSW()
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

        private void StepEvenNE()
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

        private void StepEvenSE()
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

        public void StepEven()
        {
            StepEvenNW();
            StepEvenSW();
            StepEvenNE();
            StepEvenSE();
        }

        private void StepOddNW()
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

        private void StepOddSW()
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

        private void StepOddNE()
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

        private void StepOddSE()
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

        public void StepOdd()
        {
            StepOddNW();
            StepOddSW();
            StepOddNE();
            StepOddSE();
        }

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
