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
    
    struct Quad3State
    {
        //  7: all dead
        //  6: W (even) or E (odd) edge dead
        //  5: N (even) or S (odd) edge dead
        //  4: NW (even) or SE (odd) corner dead
        //  3: all stable
        //  2: W (even) or E (odd) edge stable
        //  1: N (even) or S (odd) edge stable
        //  0: NW (even) or SE (odd) corner stable
        readonly private byte b;
        public Quad3State(int b) => this.b = (byte)b;
        public Quad3State(uint b) => this.b = (byte)b;
        public Quad3State SetAllRegionsActive() => new Quad3State(0x00);
        public Quad3State SetVerticalEdgeAndQuadActive() => new Quad3State(b & 0x33);
        public Quad3State SetHorizontalEdgeAndQuadActive() => new Quad3State(b & 0x55);
        public Quad3State SetQuad3Active() => new Quad3State(b & 0x77);
        // Calling any of the stable setters on a region which is dead keeps it dead,
        // which is what we want.
        public Quad3State SetAllRegionsStable() => new Quad3State(b | 0xf);
        // We could also set the corner to be stable, because if the edge is
        // stable then the corner is too.  However, on every code path
        // where these are called, the corner bit has already been set.
        public Quad3State SetVerticalEdgeStable() => new Quad3State(b | 0x04);
        public Quad3State SetHorizontalEdgeStable() => new Quad3State(b | 0x02);
        public Quad3State SetCornerStable() => new Quad3State(b | 0x01);
        public Quad3State SetAllRegionsDead() => new Quad3State(0xff);
        // We could also set the corner to be dead, because if the edge is
        // dead then the corner is too.  However, on every code path
        // where these are called, the corner bit has already been set.
        public Quad3State SetVerticalEdgeDead() => new Quad3State(b | 0x44);
        public Quad3State SetHorizontalEdgeDead() => new Quad3State(b | 0x22);
        public Quad3State SetCornerDead() => new Quad3State(b | 0x11);

        public static explicit operator byte(Quad3State s) => s.b;
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
            set => evenstate &= 0xffffff00 | (byte)value;
        }

        private uint oddstate;

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

        // Stable

        // Calling any of these setters on a region which is dead keeps it dead,
        // which is what we want.

        public void SetEvenQuad4AllRegionsStable() => evenstate |= 0x0f0f0f0f;
        public void SetOddQuad4AllRegionsStable() => oddstate |= 0x0f0f0f0f;

        private void SetEvenNWAllRegionsStable() => evenstate |= 0x0f000000;
        private void SetEvenSWAllRegionsStable() => evenstate |= 0x000f0000;
        private void SetEvenNEAllRegionsStable() => evenstate |= 0x00000f00;
        private void SetEvenSEAllRegionsStable() => evenstate |= 0x0000000f;

        private void SetOddNWAllRegionsStable() => oddstate |= 0x0f000000;
        private void SetOddSWAllRegionsStable() => oddstate |= 0x000f0000;
        private void SetOddNEAllRegionsStable() => oddstate |= 0x00000f00;
        private void SetOddSEAllRegionsStable() => oddstate |= 0x0000000f;

        // We could also set the corner to be stable, because if the edge is
        // stable then the corner is too.  However, on every code path
        // where these are called, the corner bit has already been set.

        private void SetEvenNWWestEdgeStable() => evenstate |= 0x04000000;
        private void SetEvenSWWestEdgeStable() => evenstate |= 0x00040000;
        private void SetEvenNEWestEdgeStable() => evenstate |= 0x00000400;
        private void SetEvenSEWestEdgeStable() => evenstate |= 0x00000004;

        private void SetOddNWEastEdgeStable() => oddstate |= 0x04000000;
        private void SetOddSWEastEdgeStable() => oddstate |= 0x00040000;
        private void SetOddNEEastEdgeStable() => oddstate |= 0x00000400;
        private void SetOddSEEastEdgeStable() => oddstate |= 0x00000004;

        private void SetEvenNWNorthEdgeStable() => evenstate |= 0x02000000;
        private void SetEvenSWNorthEdgeStable() => evenstate |= 0x00020000;
        private void SetEvenNENorthEdgeStable() => evenstate |= 0x00000200;
        private void SetEvenSENorthEdgeStable() => evenstate |= 0x00000002;

        private void SetOddNWSouthEdgeStable() => oddstate |= 0x02000000;
        private void SetOddSWSouthEdgeStable() => oddstate |= 0x00020000;
        private void SetOddNESouthEdgeStable() => oddstate |= 0x00000200;
        private void SetOddSESouthEdgeStable() => oddstate |= 0x00000002;

        private void SetEvenNWNWCornerStable() => evenstate |= 0x01000000;
        private void SetEvenSWNWCornerStable() => evenstate |= 0x00010000;
        private void SetEvenNENWCornerStable() => evenstate |= 0x00000100;
        private void SetEvenSENWCornerStable() => evenstate |= 0x00000001;

        private void SetOddNWSECornerStable() => oddstate |= 0x01000000;
        private void SetOddSWSECornerStable() => oddstate |= 0x00010000;
        private void SetOddNESECornerStable() => oddstate |= 0x00000100;
        private void SetOddSESECornerStable() => oddstate |= 0x00000001;

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

        // If we know a quad3 is stable, we can also check to see if 
        // some of it can be made dead.

        private void SetEvenNWQuad3FullyStableMaybeDead()
        {
            SetEvenNWAllRegionsStable();
            if (evenNW.NorthwestCornerDead)
                SetEvenNWNWCornerDead();
            if (evenNW.NorthEdgeDead)
                SetEvenNWNorthEdgeDead();
            if (evenNW.WestEdgeDead)
                SetEvenNWWestEdgeDead();
            if (evenNW.AllDead)
                SetEvenNWAllRegionsDead();
        }

        private void SetEvenNEQuad3FullyStableMaybeDead()
        {
            SetEvenNEAllRegionsStable();
            if (evenNE.NorthwestCornerDead)
                SetEvenNENWCornerDead();
            if (evenNE.NorthEdgeDead)
                SetEvenNENorthEdgeDead();
            if (evenNE.WestEdgeDead)
                SetEvenNEWestEdgeDead();
            if (evenNE.AllDead)
                SetEvenNEAllRegionsDead();
        }

        private void SetEvenSWQuad3FullyStableMaybeDead()
        {
            SetEvenSWAllRegionsStable();
            if (evenSW.NorthwestCornerDead)
                SetEvenSWNWCornerDead();
            if (evenSW.NorthEdgeDead)
                SetEvenSWNorthEdgeDead();
            if (evenSW.WestEdgeDead)
                SetEvenSWWestEdgeDead();
            if (evenSW.AllDead)
                SetEvenSWAllRegionsDead();
        }

        private void SetEvenSEQuad3FullyStableMaybeDead()
        {
            SetEvenSEAllRegionsStable();
            if (evenSE.NorthwestCornerDead)
                SetEvenSENWCornerDead();
            if (evenSE.NorthEdgeDead)
                SetEvenSENorthEdgeDead();
            if (evenSE.WestEdgeDead)
                SetEvenSEWestEdgeDead();
            if (evenSE.AllDead)
                SetEvenSEAllRegionsDead();
        }

        private void SetOddNWQuad3FullyStableMaybeDead()
        {
            SetOddNWAllRegionsStable();
            if (oddNW.SoutheastCornerDead)
                SetOddNWSECornerDead();
            if (oddNW.SouthEdgeDead)
                SetOddNWSouthEdgeDead();
            if (oddNW.EastEdgeDead)
                SetOddNWEastEdgeDead();
            if (oddNW.AllDead)
                SetOddNWAllRegionsDead();
        }

        private void SetOddSWQuad3FullyStableMaybeDead()
        {
            SetOddSWAllRegionsStable();
            if (oddSW.SoutheastCornerDead)
                SetOddSWSECornerDead();
            if (oddSW.SouthEdgeDead)
                SetOddSWSouthEdgeDead();
            if (oddSW.EastEdgeDead)
                SetOddSWEastEdgeDead();
            if (oddSW.AllDead)
                SetOddSWAllRegionsDead();
        }

        private void SetOddNEQuad3FullyStableMaybeDead()
        {
            SetOddNEAllRegionsStable();
            if (oddNE.SoutheastCornerDead)
                SetOddNESECornerDead();
            if (oddNE.SouthEdgeDead)
                SetOddNESouthEdgeDead();
            if (oddNE.EastEdgeDead)
                SetOddNEEastEdgeDead();
            if (oddNE.AllDead)
                SetOddNEAllRegionsDead();
        }

        private void SetOddSEQuad3FullyStableMaybeDead()
        {
            SetOddSEAllRegionsStable();
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
        // be stable, and possibly dead, so we set those bits.

        // Yes, I know I always say that you don't want to make a predicate that causes
        // a side effect, but we'll live with it.

        private bool EvenNWPossiblyActive()
        {
            if (EvenNorthwestOrBorderingActive)
                return true;
            SetOddNWQuad3FullyStableMaybeDead();
            return false;
        }

        private bool EvenSWPossiblyActive()
        {
            if (EvenSouthwestOrBorderingActive)
                return true;
            if (S != null && S.EvenNorthEdge10WestActive)
                return true;
            SetOddSWQuad3FullyStableMaybeDead();
            return false;
        }

        private bool EvenNEPossiblyActive()
        {
            if (EvenNortheastOrBorderingActive)
                return true;
            if (E != null && E.EvenWestEdge10NorthActive)
                return true;
            SetOddNEQuad3FullyStableMaybeDead();
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
            SetOddSEQuad3FullyStableMaybeDead();
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
            SetEvenNWQuad3FullyStableMaybeDead();
            return false;
        }

        private bool OddNEPossiblyActive()
        {
            if (OddNortheastOrBorderingActive)
                return true;
            if (N != null && N.OddSouthEdge10EastActive)
                return true;
            SetEvenNEQuad3FullyStableMaybeDead();
            return false;
        }

        private bool OddSEPossiblyActive()
        {
            if (OddSoutheastOrBorderingActive)
                return true;
            SetEvenSEQuad3FullyStableMaybeDead();
            return false;
        }

        private bool OddSWPossiblyActive()
        {
            if (OddSouthwestOrBorderingActive)
                return true;
            if (W != null && W.OddEastEdge10SouthActive)
                return true;
            SetEvenSWQuad3FullyStableMaybeDead();
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
                    SetEvenNWNWCornerStable();

                if (changes.NorthEdgeNoChange)
                {
                    if (newEvenNW.NorthEdgeDead)
                        SetEvenNWNorthEdgeDead();
                    else
                        SetEvenNWNorthEdgeStable();
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
                        SetEvenNWWestEdgeStable();

                    if (changes.NoChange)
                    {
                        if (newEvenNW.AllDead)
                            SetEvenNWAllRegionsDead();
                        else
                            SetEvenNWAllRegionsStable();
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
                    SetEvenSWNWCornerStable();

                if (changes.NorthEdgeNoChange)
                {
                    if (newEvenSW.NorthEdgeDead)
                        SetEvenSWNorthEdgeDead();
                    else
                        SetEvenSWNorthEdgeStable();
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
                        SetEvenSWWestEdgeStable();

                    if (changes.NoChange)
                    {
                        if (newEvenSW.AllDead)
                            SetEvenSWAllRegionsDead();
                        else
                            SetEvenSWAllRegionsStable();
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
                    SetEvenNENWCornerStable();

                if (changes.NorthEdgeNoChange)
                {
                    if (newEvenNE.NorthEdgeDead)
                        SetEvenNENorthEdgeDead();
                    else
                        SetEvenNENorthEdgeStable();
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
                        SetEvenNEWestEdgeStable();

                    if (changes.NoChange)
                    {
                        if (newEvenNE.AllDead)
                            SetEvenNEAllRegionsDead();
                        else
                            SetEvenNEAllRegionsStable();
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
            Quad3State s = EvenSEState;
            Quad3ChangeReport changes = newEvenSE.Compare(evenSE);
            if (changes.NorthwestCornerNoChange)
            {
                if (newEvenSE.NorthwestCornerDead)
                    s = s.SetCornerDead();
                else
                    s = s.SetCornerStable();

                if (changes.NorthEdgeNoChange)
                {
                    if (newEvenSE.NorthEdgeDead)
                        s = s.SetHorizontalEdgeDead();
                    else
                        s = s.SetHorizontalEdgeStable();
                }
                else
                {
                    s = s.SetHorizontalEdgeAndQuadActive();
                }

                if (changes.WestEdgeNoChange)
                {
                    if (newEvenSE.WestEdgeDead)
                        s = s.SetVerticalEdgeDead();
                    else
                        s = s.SetVerticalEdgeStable();

                    if (changes.NoChange)
                    {
                        if (newEvenSE.AllDead)
                            s = s.SetAllRegionsDead();
                        else
                            s = s.SetAllRegionsStable();
                    }
                    else
                    {
                        s = s.SetQuad3Active();
                    }
                }
                else
                {
                    s = s.SetVerticalEdgeAndQuadActive();
                }
            }
            else
            {
                s = s.SetAllRegionsActive();
            }
            evenSE = newEvenSE;
            EvenSEState = s;
        }

        private void UpdateOddNW(Quad3 newOddNW)
        {
            Quad3ChangeReport changes = newOddNW.Compare(oddNW);
            if (changes.SoutheastCornerNoChange)
            {
                if (newOddNW.SoutheastCornerDead)
                    SetOddNWSECornerDead();
                else
                    SetOddNWSECornerStable();

                if (changes.SouthEdgeNoChange)
                {
                    if (newOddNW.SouthEdgeDead)
                        SetOddNWSouthEdgeDead();
                    else
                        SetOddNWSouthEdgeStable();
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
                        SetOddNWEastEdgeStable();

                    if (changes.NoChange)
                    {
                        if (newOddNW.AllDead)
                            SetOddNWAllRegionsDead();
                        else
                            SetOddNWAllRegionsStable();
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
                    SetOddSWSECornerStable();

                if (changes.SouthEdgeNoChange)
                {
                    if (newOddSW.SouthEdgeDead)
                        SetOddSWSouthEdgeDead();
                    else
                        SetOddSWSouthEdgeStable();
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
                        SetOddSWEastEdgeStable();

                    if (changes.NoChange)
                    {
                        if (newOddSW.AllDead)
                            SetOddSWAllRegionsDead();
                        else
                            SetOddSWAllRegionsStable();
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
                    SetOddNESECornerStable();

                if (changes.SouthEdgeNoChange)
                {
                    if (newOddNE.SouthEdgeDead)
                        SetOddNESouthEdgeDead();
                    else
                        SetOddNESouthEdgeStable();
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
                        SetOddNEEastEdgeStable();

                    if (changes.NoChange)
                    {
                        if (newOddNE.AllDead)
                            SetOddNEAllRegionsDead();
                        else
                            SetOddNEAllRegionsStable();
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
                    SetOddSESECornerStable();
                if (changes.SouthEdgeNoChange)
                {
                    if (newOddSE.SouthEdgeDead)
                        SetOddSESouthEdgeDead();
                    else
                        SetOddSESouthEdgeStable();
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
                        SetOddSEEastEdgeStable();

                    if (changes.NoChange)
                    {
                        if (newOddSE.AllDead)
                            SetOddSEAllRegionsDead();
                        else
                            SetOddSEAllRegionsStable();
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
            UpdateEvenNW(newEvenNW);
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
            UpdateEvenSW(newEvenSW);
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
            UpdateEvenNE(newEvenNE);
        }

        private void StepOddSE()
        {
            if (!OddSEPossiblyActive())
                return;
            Quad3 newOddSE = Step9Quad2ToQuad3Odd(
                oddNW.SE,
                oddNE.SW,
                oddNE.SE,
                oddSW.NE,
                oddSE.NW,
                oddSE.NE,
                oddSW.SE,
                oddSE.SW,
                oddSE.SE);
            UpdateEvenSE(newOddSE);
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
