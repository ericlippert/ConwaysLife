using System.Diagnostics;

namespace ConwaysLife.Hensel
{
    struct Quad3
    {
        public Quad3(Quad2 nw, Quad2 ne, Quad2 sw, Quad2 se)
        {
            NW = nw;
            NE = ne;
            SW = sw;
            SE = se;
        }

        public Quad2 NW { get; }
        public Quad2 NE { get; }
        public Quad2 SW { get; }
        public Quad2 SE { get; }

        private bool AllDead => (NW | NE | SW | SE).Dead;
        private bool NorthwestCornerDead => NW.NW.Dead;
        private bool SoutheastCornerDead => SE.SE.Dead;

        private bool NorthEdgeDead => (NW | NE).NorthEdge.Dead;
        private bool WestEdgeDead => (SW | NW).WestEdge.Dead;
        private bool SouthEdgeDead => (SW | SE).SouthEdge.Dead;
        private bool EastEdgeDead => (NE | SE).EastEdge.Dead;

        private Quad3ChangeReport Compare(Quad3 q) => new Quad3ChangeReport(this, q);

        public bool Get(int x, int y)
        {
            Debug.Assert(0 <= x && x < 8);
            Debug.Assert(0 <= y && y < 8);
            if (x < 4)
            {
                if (y < 4)
                    return SW.Get(x, y);
                return NW.Get(x, y - 4);
            }
            else if (y < 4)
                return SE.Get(x - 4, y);
            else
                return NE.Get(x - 4, y - 4);
        }

        public Quad3 Set(int x, int y)
        {
            Debug.Assert(0 <= x && x < 8);
            Debug.Assert(0 <= y && y < 8);
            if (x < 4)
            {
                if (y < 4)
                    return new Quad3(NW, NE, SW.Set(x, y), SE);
                return new Quad3(NW.Set(x, y - 4), NE, SW, SE);
            }
            else if (y < 4)
                return new Quad3(NW, NE, SW, SE.Set(x - 4, y));
            else
                return new Quad3(NW, NE.Set(x - 4, y - 4), SW, SE);
        }

        public Quad3 Clear(int x, int y)
        {
            Debug.Assert(0 <= x && x < 8);
            Debug.Assert(0 <= y && y < 8);
            if (x < 4)
            {
                if (y < 4)
                    return new Quad3(NW, NE, SW.Clear(x, y), SE);
                return new Quad3(NW.Clear(x, y - 4), NE, SW, SE);
            }
            else if (y < 4)
                return new Quad3(NW, NE, SW, SE.Clear(x - 4, y));
            else
                return new Quad3(NW, NE.Clear(x - 4, y - 4), SW, SE);
        }

        public override string ToString()
        {
            string s = "";
            for (int y = 7; y >= 0; y -= 1)
            {
                for (int x = 0; x < 8; x += 1)
                    s += this.Get(x, y) ? 'O' : '.';
                s += "\n";
            }
            return s;
        }

        public Quad3State UpdateEvenQuad3State(Quad3 newQ3, Quad3State s)
        {
            Quad3ChangeReport changes = newQ3.Compare(this);
            if (changes.NorthwestCornerNoChange)
            {
                if (newQ3.NorthwestCornerDead)
                    s = s.SetCornerDead();
                else
                    s = s.SetCornerStable();

                if (changes.NorthEdgeNoChange)
                {
                    if (newQ3.NorthEdgeDead)
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
                    if (newQ3.WestEdgeDead)
                        s = s.SetVerticalEdgeDead();
                    else
                        s = s.SetVerticalEdgeStable();

                    if (changes.NoChange)
                    {
                        if (newQ3.AllDead)
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
            return s;
        }

        public Quad3State MakeEvenStableOrDead(Quad3State s)
        {
            s = s.SetAllRegionsStable();
            if (NorthwestCornerDead)
                s = s.SetCornerDead();
            if (NorthEdgeDead)
                s = s.SetHorizontalEdgeDead();
            if (WestEdgeDead)
                s = s.SetVerticalEdgeDead();
            if (AllDead)
                s = s.SetAllRegionsDead();
            return s;
        }

        public Quad3State UpdateOddQuad3State(Quad3 newQ3, Quad3State s)
        {
            Quad3ChangeReport changes = newQ3.Compare(this);
            if (changes.SoutheastCornerNoChange)
            {
                if (newQ3.SoutheastCornerDead)
                    s = s.SetCornerDead();
                else
                    s = s.SetCornerStable();

                if (changes.SouthEdgeNoChange)
                {
                    if (newQ3.SouthEdgeDead)
                        s = s.SetHorizontalEdgeDead();
                    else
                        s = s.SetHorizontalEdgeStable();
                }
                else
                {
                    s = s.SetHorizontalEdgeAndQuadActive();
                }

                if (changes.EastEdgeNoChange)
                {
                    if (newQ3.EastEdgeDead)
                        s = s.SetVerticalEdgeDead();
                    else
                        s = s.SetVerticalEdgeStable();

                    if (changes.NoChange)
                    {
                        if (newQ3.AllDead)
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
            return s;
        }

        public Quad3State MakeOddStableOrDead(Quad3State s)
        {
            s = s.SetAllRegionsStable();
            if (SoutheastCornerDead)
                s = s.SetCornerDead();
            if (SouthEdgeDead)
                s = s.SetHorizontalEdgeDead();
            if (EastEdgeDead)
                s = s.SetVerticalEdgeDead();
            if (AllDead)
                s = s.SetAllRegionsDead();
            return s;
        }

        private struct Quad3ChangeReport
        {
            public Quad3ChangeReport(Quad3 x, Quad3 y)
            {
                q3 = new Quad3(x.NW ^ y.NW, x.NE ^ y.NE, x.SW ^ y.SW, x.SE ^ y.SE);
            }

            private readonly Quad3 q3;

            public bool NoChange => q3.AllDead;
            public bool NorthwestCornerNoChange => q3.NorthwestCornerDead;
            public bool SoutheastCornerNoChange => q3.SoutheastCornerDead;
            public bool NorthEdgeNoChange => q3.NorthEdgeDead;
            public bool WestEdgeNoChange => q3.WestEdgeDead;
            public bool SouthEdgeNoChange => q3.SouthEdgeDead;
            public bool EastEdgeNoChange => q3.EastEdgeDead;
        }
    }
}
