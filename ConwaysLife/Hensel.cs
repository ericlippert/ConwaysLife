using System;
using System.Collections.Generic;
using System.Diagnostics;
using static System.Math;

namespace ConwaysLife.Hensel
{
    using static QuadState;

    sealed class QuickLife : ILife, IReport
    {
        // A C# implementation of Alan Hensel's QuickLife algorithm,
        // based on his 1996 source code at:
        // http://www.ibiblio.org/lifepatterns/src.41d/LifeGen.java
        // http://www.ibiblio.org/lifepatterns/src.41d/LifeCell.java

        // The high level data structure is: 
        //
        // * A quad4 is a *pair* of 16x16 grids: one for even-numbered
        //   generations, one for odd-numbered generations
        // * The Life grid is a sparse array of quad4s, indexed by an (x, y)
        //   pair of shorts.
        //
        // That gives us a maximum board size of 16 x 16 x 65536 x 65536 = 
        // a square one million cells on a side, or a quad20 in our jargon.
        //
        // A quad4 is always on exactly one of three lists:
        // * Active: this quad4 changed recently
        // * Stable: this quad4 has living cells but has not changed recently
        // * Dead: this quad4 has no living cells and has not changed recently
        //
        // The state transitions are:
        //
        // * While stepping we only walk the active list; the stable and dead lists
        //   by definition are not changing.
        // 
        // * When stepping from odd to even generation, an active quad4 detects
        //   whether there was no change from the previous even generation and
        //   makes a note of that fact.
        //  
        // * Similarly when stepping from even to odd generation.
        //
        // * If both the even and odd grids report that they have not recently changed,
        //   and all cells in both grids are dead -- which must be the case if one
        //   of them is dead and neither of them have changed -- then the quad4 is
        //   removed from the active list and put on the dead list.
        //
        // * If both even and odd grids are not recently changed but some cells are alive
        //   then the quad4 is removed from the active list and put on the stable list.
        //
        // * If an active quad4 has activity on an edge or corner adjoining a quad4
        //   on the dead or stable list, it rejoins the active list.
        //
        // * Quad4s that stay on the dead list are eventually deallocated.
        //

        private readonly DoubleLinkList<Quad4> active = new DoubleLinkList<Quad4>();
        private readonly DoubleLinkList<Quad4> stable = new DoubleLinkList<Quad4>();
        private readonly DoubleLinkList<Quad4> dead = new DoubleLinkList<Quad4>();
        private Dictionary<(short, short), Quad4> quad4s;
        private int generation;

        public QuickLife()
        {
            Clear();
        }

        public void Clear()
        {
            generation = 0;
            active.Clear();
            stable.Clear();
            dead.Clear();
            quad4s = new Dictionary<(short, short), Quad4>();
        }

        private Quad4 GetQuad4(int x, int y)
        {
            quad4s.TryGetValue(((short)x, (short)y), out var q);
            return q;
        }

        private void SetQuad4(int x, int y, Quad4 q) => quad4s[((short)x, (short)y)] = q;

        private Quad4 EnsureActive(Quad4 c, int x, int y)
        {
            if (c == null)
                return AllocateQuad4(x, y);
            MakeActive(c);
            return c;
        }
      
        private void StepEven()
        {
            foreach (Quad4 c in active)
            {
                if (!RemoveInactiveEvenQuad4(c))
                {
                    c.StepEvenQuad4();
                    MakeOddNeighborsActive(c);
                }
            }
        }

        private void StepOdd()
        {
            foreach (Quad4 c in active)
            {
                if (!RemoveInactiveOddQuad4(c))
                { 
                    c.StepOddQuad4();
                    MakeEvenNeighborsActive(c);
                }
            }
        }

        private void MakeOddNeighborsActive(Quad4 c)
        {
            if (c.OddSouthEdgeActive)
                EnsureActive(c.S, c.X, c.Y - 1);
            if (c.OddEastEdgeActive)
                EnsureActive(c.E, c.X + 1, c.Y);
            if (c.OddSoutheastCornerActive)
                EnsureActive(c.SE, c.X + 1, c.Y - 1);            
        }

        private void MakeEvenNeighborsActive(Quad4 c)
        {
            if (c.EvenWestEdgeActive)
                EnsureActive(c.W, c.X - 1, c.Y);
            if (c.EvenNorthEdgeActive)
                EnsureActive(c.N, c.X, c.Y + 1);
            if (c.EvenNorthwestCornerActive)
                EnsureActive(c.NW, c.X - 1, c.Y + 1);
        }

        // Try to remove an inactive quad4 on the even cycle.
        // If the quad4 cannot be removed because either the
        // even quad4 is active, or a neighboring edge is active,
        // this returns false; if it successfully make the quad4
        // dead or stable, it returns true and the quad4 should
        // not be processed further.

        private bool RemoveInactiveEvenQuad4(Quad4 c)
        {
            if (c.EvenQuad4OrNeighborsActive)
            {
                c.EvenState = Active;
                c.OddState = Active;
                c.StayActiveNextStep = false;
                return false;
            }

            if (c.EvenQuad4AndNeighborsAreDead)
            { 
                c.EvenState = Dead;
                c.SetOddQuad4AllRegionsDead();
                if (!c.StayActiveNextStep && c.OddState == Dead)
                    MakeDead(c);
            }
            else
            {
                c.EvenState = Stable;
                c.SetOddQuad4AllRegionsInactive();
                if (!c.StayActiveNextStep && c.OddState != Active)
                    MakeStable(c);
            }
            c.StayActiveNextStep = false;
            return true;
        }

        // Similar to above.
        private bool RemoveInactiveOddQuad4(Quad4 c)
        {
            if (c.OddQuad4OrNeighborsActive)
            {
                c.EvenState = Active;
                c.OddState = Active;
                c.StayActiveNextStep = false;
                return false;
            }

            if (c.OddQuad4AndNeighborsAreDead)
            {
                c.OddState = Dead;
                c.SetEvenQuad4AllRegionsDead();
                if (!c.StayActiveNextStep && c.EvenState == Dead)
                    MakeDead(c);
            }
            else
            {
                c.OddState = Stable;
                c.SetEvenQuad4AllRegionsInactive();
                if (!c.StayActiveNextStep && c.EvenState != Active)
                    MakeStable(c);
            }
            c.StayActiveNextStep = false;
            return true;
        }

        private void RemoveDead()
        {
            foreach(Quad4 current in dead)
            {
                if (current.S != null) 
                    current.S.N = null;
                if (current.E != null) 
                    current.E.W = null;
                if (current.SE != null) 
                    current.SE.NW = null;
                if (current.N != null) 
                    current.N.S = null;
                if (current.W != null) 
                    current.W.E = null;
                if (current.NW != null) 
                    current.NW.SE = null;
                quad4s.Remove((current.X, current.Y));
            }
            dead.Clear();
        }

        private Quad4 AllocateQuad4(int x, int y)
        {
            Quad4 c = new Quad4(x, y);
            c.S = GetQuad4(x, y - 1);
            if (c.S != null)
                c.S.N = c;
            c.E = GetQuad4(x + 1, y);
            if (c.E != null) 
                c.E.W = c;
            c.SE = GetQuad4(x + 1, y - 1);
            if (c.SE != null) 
                c.SE.NW = c;
            c.N = GetQuad4(x, y + 1);
            if (c.N != null)
                c.N.S = c;
            c.W = GetQuad4(x - 1, y);
            if (c.W != null) 
                c.W.E = c;
            c.NW = GetQuad4(x - 1, y + 1);
            if (c.NW != null) 
                c.NW.SE = c;
            active.Add(c);
            SetQuad4(x, y, c);
            return c;
        }

        private void MakeDead(Quad4 c)
        {
            Debug.Assert(c.State == Active);
            active.Remove(c);
            dead.Add(c);
            c.State = Dead;
        }

        private void MakeStable(Quad4 c)
        {
            Debug.Assert(c.State == Active);
            active.Remove(c);
            stable.Add(c);
            c.State = Stable;
        }

        private void MakeActive(Quad4 c)
        {
            c.StayActiveNextStep = true;
            if (c.State == Active) 
                return;
            else if (c.State == Dead)
                dead.Remove(c);
            else
                stable.Remove(c);
            active.Add(c);
            c.State = Active;
        }

        const int maximum = short.MaxValue;
        const int minimum = short.MinValue;

        private bool IsValidPoint(long x, long y) => 
            minimum <= (x >> 4) && (x >> 4) < maximum && minimum <= (y >> 4) && (y >> 4) < maximum;

        public bool this[LifePoint v] 
        {
            get => this[v.X, v.Y];
            set => this[v.X, v.Y] = value;
        }
        
        public bool this[long x, long y] 
        { 
            get
            {
                if (IsOdd)
                {
                    x -= 1;
                    y += 1;
                }

                if (!IsValidPoint(x, y))
                    return false;

                Quad4 q = GetQuad4((int)(x >> 4), (int)(y >> 4));
                if (q == null || q.State == Dead)
                    return false;

                if (IsOdd)
                    return q.GetOdd((int)(x & 0xf), (int)(y & 0xf));
                return q.GetEven((int)(x & 0xf), (int)(y & 0xf));
            }
            set
            {
                if (IsOdd)
                {
                    x += 1;
                    y -= 1;
                }

                if (!IsValidPoint(x, y))
                    return;

                Quad4 q = EnsureActive(GetQuad4((int)(x >> 4), (int)(y >> 4)), (int)(x >> 4), (int)(y >> 4));

                if (IsOdd)
                { 
                    if (value)
                        q.SetOdd((int)(x & 0xf), (int)(y & 0xf));
                    else
                        q.ClearOdd((int)(x & 0xf), (int)(y & 0xf));
                    MakeOddNeighborsActive(q);
                }
                else
                {
                    if (value)
                        q.SetEven((int)(x & 0xf), (int)(y & 0xf));
                    else
                        q.ClearEven((int)(x & 0xf), (int)(y & 0xf));
                    MakeEvenNeighborsActive(q);
                }
            }
        }

        private bool IsOdd => (generation & 0x1) != 0;

        private bool ShouldRemoveDead => (generation & 0x7f) == 0 && dead.Count > 100;

        public void Step()
        {
            if (ShouldRemoveDead) 
                RemoveDead();

            if (IsOdd)
                StepOdd();
            else
                StepEven();                

            generation++;
        }

        public void Draw(LifeRect rect, Action<LifePoint> setPixel)
        {
            long xmin = Max(minimum + 1, rect.X >> 4);
            long xmax = Min(maximum - 1, (rect.X + rect.Width) >> 4);
            long ymin = Max(minimum + 1, (rect.Y - rect.Height + 1) >> 4);
            long ymax = Min(maximum - 1, (rect.Y + 1) >> 4);

            long omin = IsOdd ? 1 : 0;
            long omax = omin + 16;

            for (long y = ymin - 1; y <= ymax; y += 1)
            {
                for (long x = xmin - 1; x <= xmax; x += 1)
                {
                    Quad4 q = GetQuad4((int)x, (int)y);
                    if (q == null || q.State == Dead)
                        continue;
                    for (long oy = omin; oy < omax; oy += 1)
                    {
                        long ry = (y << 4) + oy;
                        for (long ox = omin; ox < omax; ox += 1)
                        {
                            long rx = (x << 4) + ox;
                            if (this[rx, ry])
                                setPixel(new LifePoint(rx, ry));
                        }
                    }
                }
            }
        }

        public void Step(int speed)
        {
            for (int i = 0; i < 1L << speed; i += 1)
                Step();
        }

        public string Report() => 
            $"gen {generation}\n{active.Count} active\n{stable.Count} stable\n{dead.Count} dead\n";
    }
}
