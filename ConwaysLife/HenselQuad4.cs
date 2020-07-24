namespace ConwaysLife.Hensel
{
    using System.Diagnostics;
    using static HenselLookup;
    using static Quad2;
    using static QuadState;

    enum QuadState
    {
        Active,
        Stable,
        Dead
    }
    
    sealed class Quad4 : IDoubleLink<Quad4>
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

        // A quad4 is on a bunch of doubly-linked lists:
        //
        // * Exactly one of the dead, stable and active lists; this is the Next and Prev references.
        // * The N/S, E/W and NW/SE references are a doubly-linked list to neighboring quad4s.

        Quad4 IDoubleLink<Quad4>.Next { get; set; }
        Quad4 IDoubleLink<Quad4>.Prev { get; set; }

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

        // Bit meanings:
        // 31: NW all dead
        // 30: NW W edge dead
        // 29: NW N edge dead
        // 28: NW NW corner dead
        // 27: NW all stable
        // 26: NW W edge stable
        // 25: NW N edge stable
        // 24: NW NW corner stable
        // 23: SW all dead
        // 22: SW W edge dead
        // 21: SW N edge dead
        // 20: SW NW corner dead
        // 19: SW all stable
        // 18: SW W edge stable
        // 17: SW N edge stable
        // 16: SW NW corner stable
        // 15: NE all dead
        // 14: NE W edge dead
        // 13: NE N edge dead
        // 12: NE NW corner dead
        // 11: NE all stable
        // 10: NE W edge stable
        //  9: NE N edge stable
        //  8: NE NW corner stable
        //  7: SE all dead
        //  6: SE W edge dead
        //  5: SE N edge dead
        //  4: SE NW corner dead
        //  3: SE all stable
        //  2: SE W edge stable
        //  1: SE N edge stable
        //  0: SE NW corner stable

        // We similarly track these regions in each odd-cycle quad3:

        // * The entire 8x8 quad3
        // * The 2x8 eastern edge of the quad3
        // * the 8x2 southern edge of the quad3
        // * the 2x2 southeastern corner of the quad3

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
            set => oddstate &= (oddstate & 0x00ffffff) | ((uint)value << 24);
        }

        // Bit meanings:
        // 31: NW all dead
        // 30: NW E edge dead
        // 29: NW S edge dead
        // 28: NW SE corner dead
        // 27: NW all stable
        // 26: NW E edge stable
        // 25: NW S edge stable
        // 24: NW SE corner stable
        // 23: SW all dead
        // 22: SW E edge dead
        // 21: SW S edge dead
        // 20: SW SE corner dead
        // 19: SW all stable
        // 18: SW E edge stable
        // 17: SW S edge stable
        // 16: SW SE corner stable
        // 15: NE all dead
        // 14: NE E edge dead
        // 13: NE S edge dead
        // 12: NE SE corner dead
        // 11: NE all stable
        // 10: NE E edge stable
        //  9: NE S edge stable
        //  8: NE SE corner stable
        //  7: SE all dead
        //  6: SE E edge dead
        //  5: SE S edge dead
        //  4: SE SE corner dead
        //  3: SE all stable
        //  2: SE E edge stable
        //  1: SE S edge stable
        //  0: SE SE corner stable

        // Getters

        public bool EvenWestEdgeActive => (evenstate & 0x04040000) != 0x04040000;
        public bool EvenNorthEdgeActive => (evenstate & 0x02000200) != 0x02000200;
        public bool EvenNorthwestCornerActive => (evenstate & 0x01000000) != 0x01000000;

        public bool OddSouthEdgeActive => (oddstate & 0x00020002) != 0x00020002;
        public bool OddEastEdgeActive => (oddstate & 0x00000404) != 0x00000404;
        public bool OddSoutheastCornerActive => (oddstate & 0x00000001) != 0x00000001;

        private bool EvenQuad4Active => (evenstate & 0x08080808) != 0x08080808;
        private bool EvenQuad4Dead => (evenstate & 0x80808080) == 0x80808080;
        private bool EvenNorthEdgeDead => (evenstate & 0x20002000) == 0x20002000;
        private bool EvenWestEdgeDead => (evenstate & 0x40400000) == 0x40400000;
        private bool EvenNorthwestCornerDead => (evenstate & 0x10000000) == 0x10000000;
        private bool EvenNorthwestOrBorderingActive => (evenstate & 0x08020401) != 0x08020401;
        private bool EvenSouthwestOrBorderingActive => (evenstate & 0x00080004) != 0x00080004;

        // Is the 10 x 2 region on the west side of the north edge active? And similarly throughout.
        private bool EvenNorthEdge10WestActive => (evenstate & 0x02000100) != 0x02000100;
        private bool EvenNortheastOrBorderingActive => (evenstate & 0x00000802) != 0x00000802;
        private bool EvenWestEdge10NorthActive => (evenstate & 0x04010000) != 0x04010000;
        private bool EvenSoutheastActive => (evenstate & 0x00000008) != 0x00000008;
        private bool EvenNorthEdge8EastActive => (evenstate & 0x00000200) != 0x00000200;
        private bool EvenWestEdge8SouthActive => (evenstate & 0x00040000) != 0x00040000;

        private bool OddQuad4Active => (oddstate & 0x08080808) != 0x08080808;
        private bool OddQuad4Dead => (oddstate & 0x80808080) == 0x80808080;
        private bool OddSouthEdgeDead => (oddstate & 0x00200020) == 0x00200020;
        private bool OddEastEdgeDead => (oddstate & 0x00004040) == 0x00004040;
        private bool OddSoutheastCornerDead => (oddstate & 0x00000010) == 0x00000010;
        private bool OddNorthwestActive => (oddstate & 0x08000000) != 0x08000000;
        private bool OddSouthEdge8WestActive => (oddstate & 0x00020000) != 0x00020000;
        private bool OddEastEdge8NorthActive => (oddstate & 0x00000400) != 0x00000400;
        private bool OddSouthwestOrBorderingActive => (oddstate & 0x02080000) != 0x02080000;
        private bool OddEastEdge10SouthActive => (oddstate & 0x00000104) != 0x00000104;
        private bool OddNortheastOrBorderingActive => (oddstate & 0x04000800) != 0x04000800;
        private bool OddSouthEdge10EastActive => (oddstate & 0x00010002) != 0x00010002;
        private bool OddSoutheastOrBorderingActive => (oddstate & 0x01040208) != 0x01040208;

        // Is this quad active? Or do any of the neighboring quads have an active shared edge?
        public bool EvenQuad4OrNeighborsActive =>
            EvenQuad4Active ||
            (S != null && S.EvenNorthEdgeActive) ||
            (E != null && E.EvenWestEdgeActive) ||
            (SE != null && SE.EvenNorthwestCornerActive);

        // Similarly for the odd cycle:

        public bool OddQuad4OrNeighborsActive =>
            OddQuad4Active ||
            (N != null && N.OddSouthEdgeActive) ||
            (W != null && W.OddEastEdgeActive) ||
            (NW != null && NW.OddSoutheastCornerActive);

        // And similarly for dead quads; is the entire quad and all its
        // neighbors' shared edges dead?

        public bool EvenQuad4AndNeighborsAreDead =>
            EvenQuad4Dead &&
            (S == null || S.EvenNorthEdgeDead) &&
            (E == null || E.EvenWestEdgeDead) &&
            (SE == null || SE.EvenNorthwestCornerDead);

        public bool OddQuad4AndNeighborsAreDead =>
            OddQuad4Dead &&
            (N == null || N.OddSouthEdgeDead) &&
            (W == null || W.OddEastEdgeDead) &&
            (NW == null || NW.OddSoutheastCornerDead);

        // Setters that affect all four Quad3s.

        private void SetEvenQuad4AllRegionsActive() => evenstate = 0x00000000;
        private void SetOddQuad4AllRegionsActive() => oddstate = 0x00000000;
        public void SetEvenQuad4AllRegionsStable() => evenstate |= 0x0f0f0f0f;
        public void SetOddQuad4AllRegionsStable() => oddstate |= 0x0f0f0f0f;
        public void SetEvenQuad4AllRegionsDead() => evenstate = 0xffffffff;
        public void SetOddQuad4AllRegionsDead() => oddstate = 0xffffffff;

        // Is the given quad3 possibly active, either because it is active or because
        // a neighboring quad4 has an active adjoining edge?  If yes, return true.
        // If no, then we know that the corresponding next generation will 
        // be stable, and possibly dead, so we set those bits.

        // Yes, I know I always say that you don't want to make a predicate that causes
        // a side effect, but we'll live with it.

        private bool EvenNWPossiblyActive()
        {
            if (EvenNorthwestOrBorderingActive)
                return true;
            OddNWState = oddNW.MakeOddStableOrDead(OddNWState);
            return false;
        }

        private bool EvenSWPossiblyActive()
        {
            if (EvenSouthwestOrBorderingActive)
                return true;
            if (S != null && S.EvenNorthEdge10WestActive)
                return true;
            OddSWState = oddSW.MakeOddStableOrDead(OddSWState);
            return false;
        }

        private bool EvenNEPossiblyActive()
        {
            if (EvenNortheastOrBorderingActive)
                return true;
            if (E != null && E.EvenWestEdge10NorthActive)
                return true;
            OddNEState = oddNE.MakeOddStableOrDead(OddNEState);
            return false;
        }

        private bool EvenSEPossiblyActive()
        {
            if (EvenSoutheastActive)
                return true;
            if (S != null && S.EvenNorthEdge8EastActive)
                return true;
            if (E != null && E.EvenWestEdge8SouthActive)
                return true;
            if (SE != null && SE.EvenNorthwestCornerActive)
                return true;
            OddSEState = oddSE.MakeOddStableOrDead(OddSEState);
            return false;
        }

        private bool OddNWPossiblyActive()
        {
            if (OddNorthwestActive)
                return true;
            if (N != null && N.OddSouthEdge8WestActive)
                return true;
            if (W != null && W.OddEastEdge8NorthActive)
                return true;
            if (NW != null && NW.OddSoutheastCornerActive)
                return true;
            EvenNWState = evenNW.MakeEvenStableOrDead(EvenNWState);
            return false;
        }

        private bool OddNEPossiblyActive()
        {
            if (OddNortheastOrBorderingActive)
                return true;
            if (N != null && N.OddSouthEdge10EastActive)
                return true;
            EvenNEState = evenNE.MakeEvenStableOrDead(EvenNEState);
            return false;
        }

        private bool OddSEPossiblyActive()
        {
            if (OddSoutheastOrBorderingActive)
                return true;
            EvenSEState = evenSE.MakeEvenStableOrDead(EvenSEState);
            return false;
        }

        private bool OddSWPossiblyActive()
        {
            if (OddSouthwestOrBorderingActive)
                return true;
            if (W != null && W.OddEastEdge10SouthActive)
                return true;
            EvenSWState = evenSW.MakeEvenStableOrDead(EvenSWState);
            return false;
        }

        // Stepping

        private void StepEvenNW()
        {
            if (!EvenNWPossiblyActive())
                return;
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
            if (!EvenSWPossiblyActive())
                return;
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
            if (!EvenNEPossiblyActive())
                return;
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
            if (!EvenSEPossiblyActive())
                return;
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
            if (!OddNWPossiblyActive())
                return;

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
            if (!OddSWPossiblyActive())
                return;
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
            if (!OddNEPossiblyActive())
                return;
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
            if (!OddSEPossiblyActive())
                return;
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

        public bool StayActiveNextStep { get; set; }

        private QuadState state; 
        public QuadState State { 
            get => state; 
            set
            {
                if (value == Active)
                {
                    EvenState = Active;
                    OddState = Active;
                    SetEvenQuad4AllRegionsActive();
                    SetOddQuad4AllRegionsActive();
                }
                state = value;
            }
        }
        public QuadState EvenState { get; set; }
        public QuadState OddState { get; set; }

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
                    evenSW = evenSW.Set(x, y);
                else
                    evenNW = evenNW.Set(x, y - 8);
            }
            else if (y < 8)
                evenSE = evenSE.Set(x - 8, y);
            else
                evenNE = evenNE.Set(x - 8, y - 8);
            SetEvenQuad4AllRegionsActive();
        }

        public void ClearEven(int x, int y)
        {
            Debug.Assert(0 <= x && x < 16);
            Debug.Assert(0 <= y && y < 16);
            if (x < 8)
            {
                if (y < 8)
                    evenSW = evenSW.Clear(x, y);
                else
                    evenNW = evenNW.Clear(x, y - 8);
            }
            else if (y < 8)
                evenSE = evenSE.Clear(x - 8, y);
            else
                evenNE = evenNE.Clear(x - 8, y - 8);
            SetEvenQuad4AllRegionsActive();
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
            SetOddQuad4AllRegionsActive();
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
            SetOddQuad4AllRegionsActive();
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
