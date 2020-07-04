﻿using System.Collections.Generic;
using System.Diagnostics;

namespace ConwaysLife.Hensel
{
    sealed class Hensel
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

        private Quad4 GetQuad4(int x, int y) => quad4s[((short)x, (short)y)];
        private void SetQuad4(int x, int y, Quad4 q) => quad4s[((short)x, (short)y)] = q;

        public Hensel()
        {
            Clear();
        }

        private void Clear()
        {
            generation = 0;
            previousCorrect = false;
            active = null;
            stable = null;
            dead = null;
            quad4s = new Dictionary<(short, short), Quad4>();
        }

        private void EnsureActive(Quad4 c, int x, int y)
        {
            if (c != null)
                MakeActive(c);
            else
                AllocateQuad4(x, y);
        }
      
        private void StepEven()
        {
            Quad4 nextLiving;
            for (Quad4 c = active; c != null; c = nextLiving)
            {
                // The cell might go inactive, so we need to cache the next
                // item in the list.
                nextLiving = c.Next;
                StepEvenQuad4(c);
                if (!previousCorrect)
                    c.SetOddQuad4AllRegionsActive();
            }
            generation++;
            previousCorrect = true;
        }

        private void StepOdd()
        {
            Quad4 nextLiving;
            for (Quad4 c = active; c != null; c = nextLiving)
            {
                // The cell might go inactive, so we need to cache the next
                // item in the list.
                nextLiving = c.Next;
                StepOddQuad4(c);
                if (!previousCorrect)
                    c.SetEvenQuad4AllRegionsActive();
            }
        }

        private void StepEvenQuad4(Quad4 c)
        {
            if (RemoveInactiveEvenQuad4(c))
                return;
            c.StepEvenQuad4();
            if (c.OddSouthEdgeActive)
                EnsureActive(c.S, c.X, c.Y + 1);
            if (c.OddEastEdgeActive)
                EnsureActive(c.E, c.X + 1, c.Y);
            if (c.OddSoutheastCornerActive)
                EnsureActive(c.SE, c.X + 1, c.Y + 1);
        }

        private void StepOddQuad4(Quad4 c)
        {
            if (RemoveInactiveOddQuad4(c))
                return;
            c.StepOddQuad4();
            if (c.EvenWestEdgeActive)
                EnsureActive(c.W, c.X - 1, c.Y);
            if (c.EvenNorthEdgeActive)
                EnsureActive(c.N, c.X, c.Y - 1);
            if (c.EvenNorthwestCornerActive)
                EnsureActive(c.NW, c.X - 1, c.Y - 1);
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
            // TODO: Y is getting larger when we go south.
            Quad4 c = new Quad4(x, y);
            c.S = GetQuad4(x, y + 1);
            if (c.S != null)
                c.S.N = c;
            c.E = GetQuad4(x + 1, y);
            if (c.E != null) 
                c.E.W = c;
            c.SE = GetQuad4(x + 1, y + 1);
            if (c.SE != null) 
                c.SE.NW = c;
            c.N = GetQuad4(x, y - 1);
            if (c.N != null)
                c.N.S = c;
            c.W = GetQuad4(x - 1, y);
            if (c.W != null) 
                c.W.E = c;
            c.NW = GetQuad4(x - 1, y - 1);
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

        public void Step()
        {
            // TODO: Can we come up with a better policy than this? Maybe count size of dead list?
            if ((generation & 0x7f) == 0) 
                RemoveDead();

            if ((generation & 0x1) == 0)
                StepEven();
            else
                StepOdd();

            generation++;
            previousCorrect = true;
        }
    }
}
