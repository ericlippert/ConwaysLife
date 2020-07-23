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

        public bool AllDead => (NW | NE | SW | SE).Dead;

        public bool NorthEdgeDead => (NW | NE).NorthEdge.Dead;
        public bool WestEdgeDead => (SW | NW).WestEdge.Dead;
        public bool SouthEdgeDead => (SW | SE).SouthEdge.Dead;
        public bool EastEdgeDead => (NE | SE).EastEdge.Dead;

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
    }
}
