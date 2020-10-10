using System;
using System.Collections.Generic;
using System.Diagnostics;
using static System.Math;

namespace ConwaysLife.Hensel
{
    using static Quad4State;

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
        private bool previousCorrect;

        public QuickLife()
        {
            Clear();
        }

        public void Clear()
        {
            previousCorrect = true;
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

        private Quad4 EnsureActive(Quad4 q, int x, int y)
        {
            if (q == null)
                return AllocateQuad4(x, y);
            MakeActive(q);
            return q;
        }
      
        private void StepEven()
        {
            foreach (Quad4 q in active)
            {
                if (!RemoveStableEvenQuad4(q))
                {
                    q.StepEven();
                    MakeOddNeighborsActive(q);
                }
                q.StayActiveNextStep = false;
                if (!previousCorrect)
                    q.SetOddQuad4AllRegionsActive();
            }
            previousCorrect = true;
        }

        private void StepOdd()
        {
            foreach (Quad4 q in active)
            {
                if (!RemoveStableOddQuad4(q))
                { 
                    q.StepOdd();
                    MakeEvenNeighborsActive(q);
                }
                q.StayActiveNextStep = false;
                if (!previousCorrect)
                    q.SetEvenQuad4AllRegionsActive();
            }
            previousCorrect = true;
        }

        private void MakeOddNeighborsActive(Quad4 q)
        {
            if (q.OddSouthEdgeActive)
                EnsureActive(q.S, q.X, q.Y - 1);
            if (q.OddEastEdgeActive)
                EnsureActive(q.E, q.X + 1, q.Y);
            if (q.OddSoutheastCornerActive)
                EnsureActive(q.SE, q.X + 1, q.Y - 1);            
        }

        private void MakeEvenNeighborsActive(Quad4 q)
        {
            if (q.EvenWestEdgeActive)
                EnsureActive(q.W, q.X - 1, q.Y);
            if (q.EvenNorthEdgeActive)
                EnsureActive(q.N, q.X, q.Y + 1);
            if (q.EvenNorthwestCornerActive)
                EnsureActive(q.NW, q.X - 1, q.Y + 1);
        }

        // Try to remove a stable quad4 on the even cycle.
        // If the quad4 cannot be removed because either the
        // even quad4 is active, or a neighboring edge is active,
        // this returns false; if it successfully make the quad4
        // dead or stable, it returns true and the quad4 should
        // not be processed further.

        private bool RemoveStableEvenQuad4(Quad4 q)
        {
            if (q.EvenQuad4OrNeighborsActive)
            {
                q.EvenState = Active;
                q.OddState = Active;
                return false;
            }

            if (q.EvenQuad4AndNeighborsAreDead)
            { 
                q.EvenState = Dead;
                q.SetOddQuad4AllRegionsDead();
                if (!q.StayActiveNextStep && q.OddState == Dead)
                    MakeDead(q);
            }
            else
            {
                q.EvenState = Stable;
                q.SetOddQuad4AllRegionsStable();
                if (!q.StayActiveNextStep && q.OddState != Active)
                    MakeStable(q);
            }
            return true;
        }

        // Similar to above.
        private bool RemoveStableOddQuad4(Quad4 q)
        {
            if (q.OddQuad4OrNeighborsActive)
            {
                q.EvenState = Active;
                q.OddState = Active;
                return false;
            }

            if (q.OddQuad4AndNeighborsAreDead)
            {
                q.OddState = Dead;
                q.SetEvenQuad4AllRegionsDead();
                if (!q.StayActiveNextStep && q.EvenState == Dead)
                    MakeDead(q);
            }
            else
            {
                q.OddState = Stable;
                q.SetEvenQuad4AllRegionsStable();
                if (!q.StayActiveNextStep && q.EvenState != Active)
                    MakeStable(q);
            }
            return true;
        }

        private void RemoveDead()
        {
            foreach(Quad4 q in dead)
            {
                if (q.S != null) 
                    q.S.N = null;
                if (q.E != null) 
                    q.E.W = null;
                if (q.SE != null) 
                    q.SE.NW = null;
                if (q.N != null) 
                    q.N.S = null;
                if (q.W != null) 
                    q.W.E = null;
                if (q.NW != null) 
                    q.NW.SE = null;
                quad4s.Remove((q.X, q.Y));
            }
            dead.Clear();
        }

        private Quad4 AllocateQuad4(int x, int y)
        {
            Quad4 q = new Quad4(x, y);
            q.S = GetQuad4(x, y - 1);
            if (q.S != null)
                q.S.N = q;
            q.E = GetQuad4(x + 1, y);
            if (q.E != null) 
                q.E.W = q;
            q.SE = GetQuad4(x + 1, y - 1);
            if (q.SE != null) 
                q.SE.NW = q;
            q.N = GetQuad4(x, y + 1);
            if (q.N != null)
                q.N.S = q;
            q.W = GetQuad4(x - 1, y);
            if (q.W != null) 
                q.W.E = q;
            q.NW = GetQuad4(x - 1, y + 1);
            if (q.NW != null) 
                q.NW.SE = q;
            active.Add(q);
            SetQuad4(x, y, q);
            return q;
        }

        private void MakeDead(Quad4 q)
        {
            Debug.Assert(q.State == Active);
            active.Remove(q);
            dead.Add(q);
            q.State = Dead;
        }

        private void MakeStable(Quad4 q)
        {
            Debug.Assert(q.State == Active);
            active.Remove(q);
            stable.Add(q);
            q.State = Stable;
        }

        private void MakeActive(Quad4 q)
        {
            q.StayActiveNextStep = true;
            if (q.State == Active) 
                return;
            else if (q.State == Dead)
                dead.Remove(q);
            else
                stable.Remove(q);
            active.Add(q);
            q.State = Active;
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

                previousCorrect = false;

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
