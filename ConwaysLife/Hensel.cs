using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ConwaysLife.Hensel
{
    sealed class QuickLife : ILife
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

        private Quad4 active;
        private Quad4 stable;
        private Quad4 dead;

        private Dictionary<(short, short), Quad4> quad4s;

        // If we're on the even cycle, do we know that the odd state is correct? 
        // And similarly if we are on the odd cycle.
        public bool previousCorrect;
        public int generation;

        private Quad4 GetQuad4(int x, int y)
        {
            Quad4 q = null;
            quad4s.TryGetValue(((short)x, (short)y), out q);
            return q;
        }

        private void SetQuad4(int x, int y, Quad4 q) => quad4s[((short)x, (short)y)] = q;

        public QuickLife()
        {
            Clear();
        }

        public void Clear()
        {
            generation = 0;
            previousCorrect = false;
            active = null;
            stable = null;
            dead = null;
            quad4s = new Dictionary<(short, short), Quad4>();
        }

        private Quad4 EnsureActive(Quad4 c, int x, int y)
        {
            if (c != null)
            {
                MakeActive(c);
                return c;
            }
            else
                return AllocateQuad4(x, y);
        }
      
        private void StepEven()
        {
            Quad4 nextActive;
            for (Quad4 c = active; c != null; c = nextActive)
            {
                // The cell might go inactive, so we need to cache the next
                // item in the list.
                nextActive = c.Next;
                StepEvenQuad4(c);
                if (!previousCorrect)
                    c.SetOddQuad4AllRegionsActive();
            }
        }

        private void StepOdd()
        {
            Quad4 nextActive;
            for (Quad4 c = active; c != null; c = nextActive)
            {
                // The cell might go inactive, so we need to cache the next
                // item in the list.
                nextActive = c.Next;
                StepOddQuad4(c);
                if (!previousCorrect)
                    c.SetEvenQuad4AllRegionsActive();
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

        private void StepEvenQuad4(Quad4 c)
        {
            if (RemoveInactiveEvenQuad4(c))
                return;
            c.StepEvenQuad4();
            MakeOddNeighborsActive(c);
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

        private void StepOddQuad4(Quad4 c)
        {
            if (RemoveInactiveOddQuad4(c))
                return;
            c.StepOddQuad4();
            MakeEvenNeighborsActive(c);
        }

        // Try to remove an inactive quad4 on the even cycle.
        // If the quad4 cannot be removed because either the
        // even quad4 is active, or a neighboring edge is active,
        // this returns false; if it successfully make the quad4
        // dead or inactive, it returns true and the quad4 should
        // not be processed further.

        private bool RemoveInactiveEvenQuad4(Quad4 c)
        {
            if (!c.EvenQuad4AndNeighborsAreInactive)
                return false;

            if (c.EvenQuad4AndNeighborsAreDead)
            { 
                c.SetEvenReadyForDeadList();
                if (c.BothReadyForDeadList)
                    MakeDead(c);
                c.SetOddQuad4AllRegionsDead();
            }
            else
            {
                c.SetEvenReadyForInactiveList();
                if (c.BothReadyForInactiveList)
                    MakeStable(c);
                c.SetOddQuad4AllRegionsInactive();
            }
            c.ClearStayActive();
            return true;
        }

        // Similar to above.
        private bool RemoveInactiveOddQuad4(Quad4 c)
        {
            if (!c.OddQuad4AndNeighborsAreInactive)
                return false;

            if (c.OddQuad4AndNeighborsAreDead)
            {
                c.SetOddReadyForDeadList();
                if (c.BothReadyForDeadList)
                    MakeDead(c);
                c.SetEvenQuad4AllRegionsDead();
            }
            else
            {
                c.SetOddReadyForInactiveList();
                if (c.BothReadyForInactiveList)
                    MakeStable(c);
                c.SetEvenQuad4AllRegionsInactive();
            }
            c.ClearStayActive();
            return true;
        }

        private void RemoveDead()
        {
            while (dead != null)
            {
                Quad4 current = dead;
                dead = dead.Next;
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
            c.Next = active;
            if (active != null) 
                active.Prev = c;
            active = c;
            SetQuad4(x, y, c);
            return c;
        }

        private void MakeDead(Quad4 c)
        {
            Debug.Assert(c.OnActiveList);
            
            c.SetOnDeadList();

            if (c.Prev == null) 
                active = c.Next;
            else 
                c.Prev.Next = c.Next;
            if (c.Next != null) 
                c.Next.Prev = c.Prev;

            c.Next = dead;
            c.Prev = null;
            if (dead != null) 
                dead.Prev = c;
            dead = c;
        }

        private void MakeStable(Quad4 c)
        {
            Debug.Assert(c.OnActiveList);

            c.SetOnStableList();
            if (c.Prev == null) 
                active = c.Next;
            else 
                c.Prev.Next = c.Next;
            if (c.Next != null) 
                c.Next.Prev = c.Prev;
            c.Next = stable;
            c.Prev = null;
            if (stable != null) 
                stable.Prev = c;
            stable = c;
        }

        private void MakeActive(Quad4 c)
        {
            c.SetStayActive();
            if (c.OnActiveList) 
                return;

            c.BecomeActive();

            if (c.OnDeadList)
            {
                if (c.Prev == null) 
                    dead = c.Next;
                else 
                    c.Prev.Next = c.Next;
                if (c.Next != null) 
                    c.Next.Prev = c.Prev;
            }
            else
            {
                if (c.Prev == null) 
                    stable = c.Next;
                else 
                    c.Prev.Next = c.Next;
                if (c.Next != null) 
                    c.Next.Prev = c.Prev;
            }

            c.Next = active;
            c.Prev = null;
            if (active != null) 
                active.Prev = c;
            active = c;
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

                if (q == null || q.OnDeadList)
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

        // TODO: Can we come up with a better policy than this? Maybe count size of dead list?
        private bool ShouldRemoveDead => (generation & 0x7f) == 0;

        public void Step()
        {
            // TODO: Stable list can have dead quad4s on it; this looks like a bug.
            // TODO: Come back to this after stepping is correct.
            // TODO: Track size of active, stable and dead lists.
            if (ShouldRemoveDead) 
                RemoveDead();

            if (IsOdd)
                StepOdd();
            else
                StepEven();                

            generation++;
            previousCorrect = true;
        }

        public void Draw(LifeRect rect, Action<LifePoint> setPixel)
        {
            // TODO: Do better
            for (long x = rect.X; x < rect.X + rect.Width; x += 1)
                for (long y = rect.Y; y > rect.Y - rect.Height; y -= 1)
                    if (this[x, y])
                        setPixel(new LifePoint(x, y));
        }

        public void Step(int speed)
        {
            for (int i = 0; i < 1L << speed; i += 1)
                Step();
        }
    }
}
