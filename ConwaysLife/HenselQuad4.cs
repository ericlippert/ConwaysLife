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
        // * inactive: the new state is the same as the previous state.
        // * dead: the new state is the same as the previous state, and moreover,
        //   all the cells in the region are dead.

        // We represent these three states in two bits for each of four regions
        // in every quad3:
        //
        // * both bits on is dead
        // * low bit on, high bit off is inactive
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
        // 27: NW all inactive
        // 26: NW W edge inactive
        // 25: NW N edge inactive
        // 24: NW NW corner inactive
        // 23: SW all dead
        // 22: SW W edge dead
        // 21: SW N edge dead
        // 20: SW NW corner dead
        // 19: SW all inactive
        // 18: SW W edge inactive
        // 17: SW N edge inactive
        // 16: SW NW corner inactive
        // 15: NE all dead
        // 14: NE W edge dead
        // 13: NE N edge dead
        // 12: NE NW corner dead
        // 11: NE all inactive
        // 10: NE W edge inactive
        //  9: NE N edge inactive
        //  8: NE NW corner inactive
        //  7: SE all dead
        //  6: SE W edge dead
        //  5: SE N edge dead
        //  4: SE NW corner dead
        //  3: SE all inactive
        //  2: SE W edge inactive
        //  1: SE N edge inactive
        //  0: SE NW corner inactive

        // We similarly track these regions in each odd-cycle quad3:

        // * The entire 8x8 quad3
        // * The 2x8 eastern edge of the quad3
        // * the 8x2 southern edge of the quad3
        // * the 2x2 southeastern corner of the quad3

        private uint oddstate;

        // Bit meanings:
        // 31: NW all dead
        // 30: NW E edge dead
        // 29: NW S edge dead
        // 28: NW SE corner dead
        // 27: NW all inactive
        // 26: NW E edge inactive
        // 25: NW S edge inactive
        // 24: NW SE corner inactive
        // 23: SW all dead
        // 22: SW E edge dead
        // 21: SW S edge dead
        // 20: SW SE corner dead
        // 19: SW all inactive
        // 18: SW E edge inactive
        // 17: SW S edge inactive
        // 16: SW SE corner inactive
        // 15: NE all dead
        // 14: NE E edge dead
        // 13: NE S edge dead
        // 12: NE SE corner dead
        // 11: NE all inactive
        // 10: NE E edge inactive
        //  9: NE S edge inactive
        //  8: NE SE corner inactive
        //  7: SE all dead
        //  6: SE E edge dead
        //  5: SE S edge dead
        //  4: SE SE corner dead
        //  3: SE all inactive
        //  2: SE E edge inactive
        //  1: SE S edge inactive
        //  0: SE SE corner inactive

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

        // Setters

        // Active

        private void SetEvenQuad4AllRegionsActive() => evenstate = 0x00000000;
        private void SetOddQuad4AllRegionsActive() => oddstate = 0x00000000;

        private void SetEvenNWAllRegionsActive() => evenstate &= 0x00ffffff;
        private void SetEvenSWAllRegionsActive() => evenstate &= 0xff00ffff;
        private void SetEvenNEAllRegionsActive() => evenstate &= 0xffff00ff;
        private void SetEvenSEAllRegionsActive() => evenstate &= 0xffffff00;

        private void SetOddNWAllRegionsActive() => oddstate &= 0x00ffffff;
        private void SetOddSWAllRegionsActive() => oddstate &= 0xff00ffff;
        private void SetOddNEAllRegionsActive() => oddstate &= 0xffff00ff;
        private void SetOddSEAllRegionsActive() => oddstate &= 0xffffff00;

        // If we are setting the north, south, east or west edge active
        // then we might as well set the entire quad active at the same time.

        private void SetEvenNWWestEdgeAndQuadActive() => evenstate &= 0x33ffffff;
        private void SetEvenSWWestEdgeAndQuadActive() => evenstate &= 0xff33ffff;
        private void SetEvenNEWestEdgeAndQuadActive() => evenstate &= 0xffff33ff;
        private void SetEvenSEWestEdgeAndQuadActive() => evenstate &= 0xffffff33;

        private void SetOddNWEastEdgeAndQuadActive() => oddstate &= 0x33ffffff;
        private void SetOddSWEastEdgeAndQuadActive() => oddstate &= 0xff33ffff;
        private void SetOddNEEastEdgeAndQuadActive() => oddstate &= 0xffff33ff;
        private void SetOddSEEastEdgeAndQuadActive() => oddstate &= 0xffffff33;

        private void SetEvenNWNorthEdgeAndQuadActive() => evenstate &= 0x55ffffff;
        private void SetEvenSWNorthEdgeAndQuadActive() => evenstate &= 0xff55ffff;
        private void SetEvenNENorthEdgeAndQuadActive() => evenstate &= 0xffff55ff;
        private void SetEvenSENorthEdgeAndQuadActive() => evenstate &= 0xffffff55;

        private void SetOddNWSouthEdgeAndQuadActive() => oddstate &= 0x55ffffff;
        private void SetOddSWSouthEdgeAndQuadActive() => oddstate &= 0xff55ffff;
        private void SetOddNESouthEdgeAndQuadActive() => oddstate &= 0xffff55ff;
        private void SetOddSESouthEdgeAndQuadActive() => oddstate &= 0xffffff55;

        private void SetEvenNWQuad3Active() => evenstate &= 0x77ffffff;
        private void SetEvenSWQuad3Active() => evenstate &= 0xff77ffff;
        private void SetEvenNEQuad3Active() => evenstate &= 0xffff77ff;
        private void SetEvenSEQuad3Active() => evenstate &= 0xffffff77;

        private void SetOddNWQuad3Active() => oddstate &= 0x77ffffff;
        private void SetOddSWQuad3Active() => oddstate &= 0xff77ffff;
        private void SetOddSEQuad3Active() => oddstate &= 0xffffff77;
        private void SetOddNEQuad3Active() => oddstate &= 0xffff77ff;

        // Inactive

        // Calling any of these setters on a region which is dead keeps it dead,
        // which is what we want.

        public void SetEvenQuad4AllRegionsInactive() => evenstate |= 0x0f0f0f0f;
        public void SetOddQuad4AllRegionsInactive() => oddstate |= 0x0f0f0f0f;

        private void SetEvenNWAllRegionsInactive() => evenstate |= 0x0f000000;
        private void SetEvenSWAllRegionsInactive() => evenstate |= 0x000f0000;
        private void SetEvenNEAllRegionsInactive() => evenstate |= 0x00000f00;
        private void SetEvenSEAllRegionsInactive() => evenstate |= 0x0000000f;

        private void SetOddNWAllRegionsInactive() => oddstate |= 0x0f000000;
        private void SetOddSWAllRegionsInactive() => oddstate |= 0x000f0000;
        private void SetOddNEAllRegionsInactive() => oddstate |= 0x00000f00;
        private void SetOddSEAllRegionsInactive() => oddstate |= 0x0000000f;

        // We could also set the corner to be inactive, because if the edge is
        // inactive then the corner is too.  However, on every code path
        // where these are called, the corner bit has already been set.

        private void SetEvenNWWestEdgeInactive() => evenstate |= 0x04000000;
        private void SetEvenSWWestEdgeInactive() => evenstate |= 0x00040000;
        private void SetEvenNEWestEdgeInactive() => evenstate |= 0x00000400;
        private void SetEvenSEWestEdgeInactive() => evenstate |= 0x00000004;

        private void SetOddNWEastEdgeInactive() => oddstate |= 0x04000000;
        private void SetOddSWEastEdgeInactive() => oddstate |= 0x00040000;
        private void SetOddNEEastEdgeInactive() => oddstate |= 0x00000400;
        private void SetOddSEEastEdgeInactive() => oddstate |= 0x00000004;

        private void SetEvenNWNorthEdgeInactive() => evenstate |= 0x02000000;
        private void SetEvenSWNorthEdgeInactive() => evenstate |= 0x00020000;
        private void SetEvenNENorthEdgeInactive() => evenstate |= 0x00000200;
        private void SetEvenSENorthEdgeInactive() => evenstate |= 0x00000002;

        private void SetOddNWSouthEdgeInactive() => oddstate |= 0x02000000;
        private void SetOddSWSouthEdgeInactive() => oddstate |= 0x00020000;
        private void SetOddNESouthEdgeInactive() => oddstate |= 0x00000200;
        private void SetOddSESouthEdgeInactive() => oddstate |= 0x00000002;

        private void SetEvenNWNWCornerInactive() => evenstate |= 0x01000000;
        private void SetEvenSWNWCornerInactive() => evenstate |= 0x00010000;
        private void SetEvenNENWCornerInactive() => evenstate |= 0x00000100;
        private void SetEvenSENWCornerInactive() => evenstate |= 0x00000001;

        private void SetOddNWSECornerInactive() => oddstate |= 0x01000000;
        private void SetOddSWSECornerInactive() => oddstate |= 0x00010000;
        private void SetOddNESECornerInactive() => oddstate |= 0x00000100;
        private void SetOddSESECornerInactive() => oddstate |= 0x00000001;

        // Dead

        public void SetEvenQuad4AllRegionsDead() => evenstate = 0xffffffff;
        public void SetOddQuad4AllRegionsDead() => oddstate = 0xffffffff;

        private void SetEvenNWAllRegionsDead() => evenstate |= 0xff000000;
        private void SetEvenSWAllRegionsDead() => evenstate |= 0x00ff0000;
        private void SetEvenNEAllRegionsDead() => evenstate |= 0x0000ff00;
        private void SetEvenSEAllRegionsDead() => evenstate |= 0x000000ff;

        private void SetOddNWAllRegionsDead() => oddstate |= 0xff000000;
        private void SetOddSWAllRegionsDead() => oddstate |= 0x00ff0000;
        private void SetOddNEAllRegionsDead() => oddstate |= 0x0000ff00;
        private void SetOddSEAllRegionsDead() => oddstate |= 0x000000ff;

        // We could also set the corner to be dead, because if the edge is
        // dead then the corner is too.  However, on every code path
        // where these are called, the corner bit has already been set.

        private void SetEvenNWWestEdgeDead() => evenstate |= 0x44000000;
        private void SetEvenSWWestEdgeDead() => evenstate |= 0x00440000;
        private void SetEvenNEWestEdgeDead() => evenstate |= 0x00004400;
        private void SetEvenSEWestEdgeDead() => evenstate |= 0x00000044;

        private void SetOddNWEastEdgeDead() => oddstate |= 0x44000000;
        private void SetOddSWEastEdgeDead() => oddstate |= 0x00440000;
        private void SetOddNEEastEdgeDead() => oddstate |= 0x00004400;
        private void SetOddSEEastEdgeDead() => oddstate |= 0x00000044;

        private void SetEvenNWNorthEdgeDead() => evenstate |= 0x22000000;
        private void SetEvenSWNorthEdgeDead() => evenstate |= 0x00220000;
        private void SetEvenNENorthEdgeDead() => evenstate |= 0x00002200;
        private void SetEvenSENorthEdgeDead() => evenstate |= 0x00000022;

        private void SetOddNWSouthEdgeDead() => oddstate |= 0x22000000;
        private void SetOddSWSouthEdgeDead() => oddstate |= 0x00220000;
        private void SetOddNESouthEdgeDead() => oddstate |= 0x00002200;
        private void SetOddSESouthEdgeDead() => oddstate |= 0x00000022;

        private void SetEvenNWNWCornerDead() => evenstate |= 0x11000000;
        private void SetEvenSWNWCornerDead() => evenstate |= 0x00110000;
        private void SetEvenNENWCornerDead() => evenstate |= 0x00001100;
        private void SetEvenSENWCornerDead() => evenstate |= 0x00000011;

        private void SetOddNWSECornerDead() => oddstate |= 0x11000000;
        private void SetOddSWSECornerDead() => oddstate |= 0x00110000;
        private void SetOddNESECornerDead() => oddstate |= 0x00001100;
        private void SetOddSESECornerDead() => oddstate |= 0x00000011;

        // If we know a quad3 is inactive, we can also check to see if 
        // some of it can be made dead.

        private void SetEvenNWQuad3FullyInactiveMaybeDead()
        {
            SetEvenNWAllRegionsInactive();
            if (evenNW.NorthwestCornerDead)
                SetEvenNWNWCornerDead();
            if (evenNW.NorthEdgeDead)
                SetEvenNWNorthEdgeDead();
            if (evenNW.WestEdgeDead)
                SetEvenNWWestEdgeDead();
            if (evenNW.AllDead)
                SetEvenNWAllRegionsDead();
        }

        private void SetEvenNEQuad3FullyInactiveMaybeDead()
        {
            SetEvenNEAllRegionsInactive();
            if (evenNE.NorthwestCornerDead)
                SetEvenNENWCornerDead();
            if (evenNE.NorthEdgeDead)
                SetEvenNENorthEdgeDead();
            if (evenNE.WestEdgeDead)
                SetEvenNEWestEdgeDead();
            if (evenNE.AllDead)
                SetEvenNEAllRegionsDead();
        }

        private void SetEvenSWQuad3FullyInactiveMaybeDead()
        {
            SetEvenSWAllRegionsInactive();
            if (evenSW.NorthwestCornerDead)
                SetEvenSWNWCornerDead();
            if (evenSW.NorthEdgeDead)
                SetEvenSWNorthEdgeDead();
            if (evenSW.WestEdgeDead)
                SetEvenSWWestEdgeDead();
            if (evenSW.AllDead)
                SetEvenSWAllRegionsDead();
        }

        private void SetEvenSEQuad3FullyInactiveMaybeDead()
        {
            SetEvenSEAllRegionsInactive();
            if (evenSE.NorthwestCornerDead)
                SetEvenSENWCornerDead();
            if (evenSE.NorthEdgeDead)
                SetEvenSENorthEdgeDead();
            if (evenSE.WestEdgeDead)
                SetEvenSEWestEdgeDead();
            if (evenSE.AllDead)
                SetEvenSEAllRegionsDead();
        }

        private void SetOddNWQuad3FullyInactiveMaybeDead()
        {
            SetOddNWAllRegionsInactive();
            if (oddNW.SoutheastCornerDead)
                SetOddNWSECornerDead();
            if (oddNW.SouthEdgeDead)
                SetOddNWSouthEdgeDead();
            if (oddNW.EastEdgeDead)
                SetOddNWEastEdgeDead();
            if (oddNW.AllDead)
                SetOddNWAllRegionsDead();
        }

        private void SetOddSWQuad3FullyInactiveMaybeDead()
        {
            SetOddSWAllRegionsInactive();
            if (oddSW.SoutheastCornerDead)
                SetOddSWSECornerDead();
            if (oddSW.SouthEdgeDead)
                SetOddSWSouthEdgeDead();
            if (oddSW.EastEdgeDead)
                SetOddSWEastEdgeDead();
            if (oddSW.AllDead)
                SetOddSWAllRegionsDead();
        }

        private void SetOddNEQuad3FullyInactiveMaybeDead()
        {
            SetOddNEAllRegionsInactive();
            if (oddNE.SoutheastCornerDead)
                SetOddNESECornerDead();
            if (oddNE.SouthEdgeDead)
                SetOddNESouthEdgeDead();
            if (oddNE.EastEdgeDead)
                SetOddNEEastEdgeDead();
            if (oddNE.AllDead)
                SetOddNEAllRegionsDead();
        }

        private void SetOddSEQuad3FullyInactiveMaybeDead()
        {
            SetOddSEAllRegionsInactive();
            if (oddSE.SoutheastCornerDead)
                SetOddSESECornerDead();
            if (oddSE.SouthEdgeDead)
                SetOddSESouthEdgeDead();
            if (oddSE.EastEdgeDead)
                SetOddSEEastEdgeDead();
            if (oddSE.AllDead)
                SetOddSEAllRegionsDead();
        }

        // Is the given quad3 possibly active, either because it is active or because
        // a neighboring quad4 has an active adjoining edge?  If yes, return true.
        // If no, then we know that the corresponding next generation will 
        // be inactive, and possibly dead, so we set those bits.

        // Yes, I know I always say that you don't want to make a predicate that causes
        // a side effect, but we'll live with it.

        private bool EvenNWPossiblyActive()
        {
            if (EvenNorthwestOrBorderingActive)
                return true;
            SetOddNWQuad3FullyInactiveMaybeDead();
            return false;
        }

        private bool EvenSWPossiblyActive()
        {
            if (EvenSouthwestOrBorderingActive)
                return true;
            if (S != null && S.EvenNorthEdge10WestActive)
                return true;
            SetOddSWQuad3FullyInactiveMaybeDead();
            return false;
        }

        private bool EvenNEPossiblyActive()
        {
            if (EvenNortheastOrBorderingActive)
                return true;
            if (E != null && E.EvenWestEdge10NorthActive)
                return true;
            SetOddNEQuad3FullyInactiveMaybeDead();
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
            SetOddSEQuad3FullyInactiveMaybeDead();
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
            SetEvenNWQuad3FullyInactiveMaybeDead();
            return false;
        }

        private bool OddNEPossiblyActive()
        {
            if (OddNortheastOrBorderingActive)
                return true;
            if (N != null && N.OddSouthEdge10EastActive)
                return true;
            SetEvenNEQuad3FullyInactiveMaybeDead();
            return false;
        }

        private bool OddSEPossiblyActive()
        {
            if (OddSoutheastOrBorderingActive)
                return true;
            SetEvenSEQuad3FullyInactiveMaybeDead();
            return false;
        }

        private bool OddSWPossiblyActive()
        {
            if (OddSouthwestOrBorderingActive)
                return true;
            if (W != null && W.OddEastEdge10SouthActive)
                return true;
            SetEvenSWQuad3FullyInactiveMaybeDead();
            return false;
        }

        // Update quad3s

        private void UpdateEvenNW(Quad3 newEvenNW)
        {
            Quad3ChangeReport changes = newEvenNW.Compare(evenNW);
            if (changes.NorthwestCornerNoChange)
            {
                if (newEvenNW.NorthwestCornerDead)
                    SetEvenNWNWCornerDead();
                else
                    SetEvenNWNWCornerInactive();

                if (changes.NorthEdgeNoChange)
                {
                    if (newEvenNW.NorthEdgeDead)
                        SetEvenNWNorthEdgeDead();
                    else
                        SetEvenNWNorthEdgeInactive();
                }
                else
                {
                    SetEvenNWNorthEdgeAndQuadActive();
                }

                if (changes.WestEdgeNoChange)
                {
                    if (newEvenNW.WestEdgeDead)
                        SetEvenNWWestEdgeDead();
                    else
                        SetEvenNWWestEdgeInactive();

                    if (changes.NoChange)
                    {
                        if (newEvenNW.AllDead)
                            SetEvenNWAllRegionsDead();
                        else
                            SetEvenNWAllRegionsInactive();
                    }
                    else
                    {
                        SetEvenNWQuad3Active();
                    }
                }
                else
                {
                    SetEvenNWWestEdgeAndQuadActive();
                }
            }
            else
            {
                SetEvenNWAllRegionsActive();
            }
            evenNW = newEvenNW;
        }

        private void UpdateEvenSW(Quad3 newEvenSW)
        {
            Quad3ChangeReport changes = newEvenSW.Compare(evenSW);
            if (changes.NorthwestCornerNoChange)
            {
                if (newEvenSW.NorthwestCornerDead)
                    SetEvenSWNWCornerDead();
                else
                    SetEvenSWNWCornerInactive();

                if (changes.NorthEdgeNoChange)
                {
                    if (newEvenSW.NorthEdgeDead)
                        SetEvenSWNorthEdgeDead();
                    else
                        SetEvenSWNorthEdgeInactive();
                }
                else
                {
                    SetEvenSWNorthEdgeAndQuadActive();
                }

                if (changes.WestEdgeNoChange)
                {
                    if (newEvenSW.WestEdgeDead)
                        SetEvenSWWestEdgeDead();
                    else
                        SetEvenSWWestEdgeInactive();

                    if (changes.NoChange)
                    {
                        if (newEvenSW.AllDead)
                            SetEvenSWAllRegionsDead();
                        else
                            SetEvenSWAllRegionsInactive();
                    }
                    else
                    {
                        SetEvenSWQuad3Active();
                    }
                }
                else
                {
                    SetEvenSWWestEdgeAndQuadActive();
                }
            }
            else
            {
                SetEvenSWAllRegionsActive();
            }

            evenSW = newEvenSW;
        }

        private void UpdateEvenNE(Quad3 newEvenNE)
        {
            Quad3ChangeReport changes = newEvenNE.Compare(evenNE);
            if (changes.NorthwestCornerNoChange)
            {
                if (newEvenNE.NorthwestCornerDead)
                    SetEvenNENWCornerDead();
                else
                    SetEvenNENWCornerInactive();

                if (changes.NorthEdgeNoChange)
                {
                    if (newEvenNE.NorthEdgeDead)
                        SetEvenNENorthEdgeDead();
                    else
                        SetEvenNENorthEdgeInactive();
                }
                else
                {
                    SetEvenNENorthEdgeAndQuadActive();
                }

                if (changes.WestEdgeNoChange)
                {
                    if (newEvenNE.WestEdgeDead)
                        SetEvenNEWestEdgeDead();
                    else
                        SetEvenNEWestEdgeInactive();

                    if (changes.NoChange)
                    {
                        if (newEvenNE.AllDead)
                            SetEvenNEAllRegionsDead();
                        else
                            SetEvenNEAllRegionsInactive();
                    }
                    else
                    {
                        SetEvenNEQuad3Active();
                    }
                }
                else
                {
                    SetEvenNEWestEdgeAndQuadActive();
                }
            }
            else
            {
                SetEvenNEAllRegionsActive();
            }

            evenNE = newEvenNE;
        }

        private void UpdateEvenSE(Quad3 newEvenSE)
        {
            Quad3ChangeReport changes = newEvenSE.Compare(evenSE);
            if (changes.NorthwestCornerNoChange)
            {
                if (newEvenSE.NorthwestCornerDead)
                    SetEvenSENWCornerDead();
                else
                    SetEvenSENWCornerInactive();

                if (changes.NorthEdgeNoChange)
                {
                    if (newEvenSE.NorthEdgeDead)
                        SetEvenSENorthEdgeDead();
                    else
                        SetEvenSENorthEdgeInactive();
                }
                else
                {
                    SetEvenSENorthEdgeAndQuadActive();
                }

                if (changes.WestEdgeNoChange)
                {
                    if (newEvenSE.WestEdgeDead)
                        SetEvenSEWestEdgeDead();
                    else
                        SetEvenSEWestEdgeInactive();

                    if (changes.NoChange)
                    {
                        if (newEvenSE.AllDead)
                            SetEvenSEAllRegionsDead();
                        else
                            SetEvenSEAllRegionsInactive();
                    }
                    else
                    {
                        SetEvenSEQuad3Active();
                    }
                }
                else
                {
                    SetEvenSEWestEdgeAndQuadActive();
                }
            }
            else
            {
                SetEvenSEAllRegionsActive();
            }
            evenSE = newEvenSE;
        }

        private void UpdateOddNW(Quad3 newOddNW)
        {
            Quad3ChangeReport changes = newOddNW.Compare(oddNW);
            if (changes.SoutheastCornerNoChange)
            {
                if (newOddNW.SoutheastCornerDead)
                    SetOddNWSECornerDead();
                else
                    SetOddNWSECornerInactive();

                if (changes.SouthEdgeNoChange)
                {
                    if (newOddNW.SouthEdgeDead)
                        SetOddNWSouthEdgeDead();
                    else
                        SetOddNWSouthEdgeInactive();
                }
                else
                {
                    SetOddNWSouthEdgeAndQuadActive();
                }

                if (changes.EastEdgeNoChange)
                {
                    if (newOddNW.EastEdgeDead)
                        SetOddNWEastEdgeDead();
                    else
                        SetOddNWEastEdgeInactive();

                    if (changes.NoChange)
                    {
                        if (newOddNW.AllDead)
                            SetOddNWAllRegionsDead();
                        else
                            SetOddNWAllRegionsInactive();
                    }
                    else
                    {
                        SetOddNWQuad3Active();
                    }
                }
                else
                {
                    SetOddNWEastEdgeAndQuadActive();
                }
            }
            else
            {
                SetOddNWAllRegionsActive();
            }

            oddNW = newOddNW;
        }

        private void UpdateOddSW(Quad3 newOddSW)
        {
            Quad3ChangeReport changes = newOddSW.Compare(oddSW);
            if (changes.SoutheastCornerNoChange)
            {
                if (newOddSW.SoutheastCornerDead)
                    SetOddSWSECornerDead();
                else
                    SetOddSWSECornerInactive();

                if (changes.SouthEdgeNoChange)
                {
                    if (newOddSW.SouthEdgeDead)
                        SetOddSWSouthEdgeDead();
                    else
                        SetOddSWSouthEdgeInactive();
                }
                else
                {
                    SetOddSWSouthEdgeAndQuadActive();
                }

                if (changes.EastEdgeNoChange)
                {
                    if (newOddSW.EastEdgeDead)
                        SetOddSWEastEdgeDead();
                    else
                        SetOddSWEastEdgeInactive();

                    if (changes.NoChange)
                    {
                        if (newOddSW.AllDead)
                            SetOddSWAllRegionsDead();
                        else
                            SetOddSWAllRegionsInactive();
                    }
                    else
                    {
                        SetOddSWQuad3Active();
                    }
                }
                else
                {
                    SetOddSWEastEdgeAndQuadActive();
                }
            }
            else
            {
                SetOddSWAllRegionsActive();
            }

            oddSW = newOddSW;
        }

        private void UpdateOddNE(Quad3 newOddNE)
        {
            Quad3ChangeReport changes = newOddNE.Compare(oddNE);
            if (changes.SoutheastCornerNoChange)
            {
                if (newOddNE.SoutheastCornerDead)
                    SetOddNESECornerDead();
                else
                    SetOddNESECornerInactive();

                if (changes.SouthEdgeNoChange)
                {
                    if (newOddNE.SouthEdgeDead)
                        SetOddNESouthEdgeDead();
                    else
                        SetOddNESouthEdgeInactive();
                }
                else
                {
                    SetOddNESouthEdgeAndQuadActive();
                }

                if (changes.EastEdgeNoChange)
                {
                    if (newOddNE.EastEdgeDead)
                        SetOddNEEastEdgeDead();
                    else
                        SetOddNEEastEdgeInactive();

                    if (changes.NoChange)
                    {
                        if (newOddNE.AllDead)
                            SetOddNEAllRegionsDead();
                        else
                            SetOddNEAllRegionsInactive();
                    }
                    else
                    {
                        SetOddNEQuad3Active();
                    }
                }
                else
                {
                    SetOddNEEastEdgeAndQuadActive();
                }
            }
            else
            {
                SetOddNEAllRegionsActive();
            }

            oddNE = newOddNE;
        }

        private void UpdateOddSE(Quad3 newOddSE)
        {
            Quad3ChangeReport changes = newOddSE.Compare(oddSE);
            if (changes.SoutheastCornerNoChange)
            {
                if (newOddSE.SoutheastCornerDead)
                    SetOddSESECornerDead();
                else
                    SetOddSESECornerInactive();
                if (changes.SouthEdgeNoChange)
                {
                    if (newOddSE.SouthEdgeDead)
                        SetOddSESouthEdgeDead();
                    else
                        SetOddSESouthEdgeInactive();
                }
                else
                {
                    SetOddSESouthEdgeAndQuadActive();
                }
                if (changes.EastEdgeNoChange)
                {
                    if (newOddSE.EastEdgeDead)
                        SetOddSEEastEdgeDead();
                    else
                        SetOddSEEastEdgeInactive();

                    if (changes.NoChange)
                    {
                        if (newOddSE.AllDead)
                            SetOddSEAllRegionsDead();
                        else
                            SetOddSEAllRegionsInactive();
                    }
                    else
                    {
                        SetOddSEQuad3Active();
                    }
                }
                else
                {
                    SetOddSEEastEdgeAndQuadActive();
                }
            }
            else
            {
                SetOddSEAllRegionsActive();
            }
            oddSE = newOddSE;
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
            UpdateOddNW(newOddNW);
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
            UpdateOddSW(newOddSW);
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
            UpdateOddNE(newOddNE);
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
            UpdateOddSE(newOddSE);
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

            Quad3 new_even_nw = Step9Quad2ToQuad3Odd(
                NW == null ? AllDead : NW.oddSE.SE,
                N == null ? AllDead : N.oddSW.SW,
                N == null ? AllDead : N.oddSW.SE,
                W == null ? AllDead : W.oddNE.NE,
                oddNW.NW,
                oddNW.NE,
                W == null ? AllDead : W.oddNE.SE,
                oddNW.SW,
                oddNW.SE);
            UpdateEvenNW(new_even_nw);
        }

        private void StepOddSW()
        {
            if (!OddSWPossiblyActive())
                return;
            Quad3 new_even_sw = Step9Quad2ToQuad3Odd(
                W == null ? AllDead : W.oddNE.SE,
                oddNW.SW,
                oddNW.SE,
                W == null ? AllDead : W.oddSE.NE,
                oddSW.NW,
                oddSW.NE,
                W == null ? AllDead : W.oddSE.SE,
                oddSW.SW,
                oddSW.SE);
            UpdateEvenSW(new_even_sw);
        }

        private void StepOddNE()
        {
            if (!OddNEPossiblyActive())
                return;
            Quad3 new_even_ne = Step9Quad2ToQuad3Odd(
                N == null ? AllDead : N.oddSW.SE,
                N == null ? AllDead : N.oddSE.SW,
                N == null ? AllDead : N.oddSE.SE,
                oddNW.NE,
                oddNE.NW,
                oddNE.NE,
                oddNW.SE,
                oddNE.SW,
                oddNE.SE);
            UpdateEvenNE(new_even_ne);
        }

        private void StepOddSE()
        {
            if (!OddSEPossiblyActive())
                return;
            Quad3 new_even_se = Step9Quad2ToQuad3Odd(
                oddNW.SE,
                oddNE.SW,
                oddNE.SE,
                oddSW.NE,
                oddSE.NW,
                oddSE.NE,
                oddSW.SE,
                oddSE.SW,
                oddSE.SE);
            UpdateEvenSE(new_even_se);
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
                {
                    evenSW = evenSW.Set(x, y);
                    // TODO: Could be more specific
                    SetEvenSWAllRegionsActive();
                }
                else
                {
                    evenNW = evenNW.Set(x, y - 8);
                    // TODO: Could be more specific
                    SetEvenNWAllRegionsActive();
                }
            }
            else if (y < 8)
            {
                evenSE = evenSE.Set(x - 8, y);
                // TODO: Could be more specific
                SetEvenSEAllRegionsActive();
            }
            else
            {                
                evenNE = evenNE.Set(x - 8, y - 8);
                // TODO: Could be more specific
                SetEvenSEAllRegionsActive();
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
                    // TODO: Could be more specific
                    SetEvenSWAllRegionsActive();
                }
                else
                {
                    evenNW = evenNW.Clear(x, y - 8);
                    // TODO: Could be more specific
                    SetEvenNWAllRegionsActive();
                }
            }
            else if (y < 8)
            {
                evenSE = evenSE.Clear(x - 8, y);
                // TODO: Could be more specific
                SetEvenSEAllRegionsActive();
            }
            else
            {
                evenNE = evenNE.Clear(x - 8, y - 8);
                // TODO: Could be more specific
                SetEvenNEAllRegionsActive();
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
