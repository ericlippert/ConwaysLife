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

        // Stepping

        private void StepEvenNW()
        {
            oddNW = Step9Quad2ToQuad3Even(
                evenNW.NW,
                evenNW.NE,
                evenNE.NW,
                evenNW.SW,
                evenNW.SE,
                evenNE.SW,
                evenSW.NW,
                evenSW.NE,
                evenSE.NW);
        }

        private void StepEvenSW()
        {
            oddSW = Step9Quad2ToQuad3Even(
                evenSW.NW,
                evenSW.NE,
                evenSE.NW,
                evenSW.SW,
                evenSW.SE,
                evenSE.SW,
                S == null ? AllDead : S.evenNW.NW,
                S == null ? AllDead : S.evenNW.NE,
                S == null ? AllDead : S.evenNE.NW);
        }

        private void StepEvenNE()
        {
            oddNE = Step9Quad2ToQuad3Even(
                evenNE.NW,
                evenNE.NE,
                E == null ? AllDead : E.evenNW.NW,
                evenNE.SW,
                evenNE.SE,
                E == null ? AllDead : E.evenNW.SW,
                evenSE.NW,
                evenSE.NE,
                E == null ? AllDead : E.evenSW.NW);
        }

        private void StepEvenSE()
        {
            oddSE = Step9Quad2ToQuad3Even(
                evenSE.NW,
                evenSE.NE,
                E == null ? AllDead : E.evenSW.NW,
                evenSE.SW,
                evenSE.SE,
                E == null ? AllDead : E.evenSW.SW,
                S == null ? AllDead : S.evenNE.NW,
                S == null ? AllDead : S.evenNE.NE,
                SE == null ? AllDead : SE.evenNW.NW);
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
            evenNW = Step9Quad2ToQuad3Odd(
                NW == null ? AllDead : NW.oddSE.SE,
                N == null ? AllDead : N.oddSW.SW,
                N == null ? AllDead : N.oddSW.SE,
                W == null ? AllDead : W.oddNE.NE,
                oddNW.NW,
                oddNW.NE,
                W == null ? AllDead : W.oddNE.SE,
                oddNW.SW,
                oddNW.SE);
        }

        private void StepOddSW()
        {
            evenSW = Step9Quad2ToQuad3Odd(
                W == null ? AllDead : W.oddNE.SE,
                oddNW.SW,
                oddNW.SE,
                W == null ? AllDead : W.oddSE.NE,
                oddSW.NW,
                oddSW.NE,
                W == null ? AllDead : W.oddSE.SE,
                oddSW.SW,
                oddSW.SE);
        }

        private void StepOddNE()
        {
            evenNE = Step9Quad2ToQuad3Odd(
                N == null ? AllDead : N.oddSW.SE,
                N == null ? AllDead : N.oddSE.SW,
                N == null ? AllDead : N.oddSE.SE,
                oddNW.NE,
                oddNE.NW,
                oddNE.NE,
                oddNW.SE,
                oddNE.SW,
                oddNE.SE);
        }

        private void StepOddSE()
        {
            evenSE = Step9Quad2ToQuad3Odd(
                oddNW.SE,
                oddNE.SW,
                oddNE.SE,
                oddSW.NE,
                oddSE.NW,
                oddSE.NE,
                oddSW.SE,
                oddSE.SW,
                oddSE.SE);
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
