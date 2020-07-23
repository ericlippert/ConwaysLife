using System;
using System.Collections.Generic;
using static System.Math;

namespace ConwaysLife.Hensel
{
    sealed class ProtoQuickLife : ILife, IReport
    {
        private Dictionary<(short, short), Quad4> quad4s;
        private int generation;

        public ProtoQuickLife()
        {
            Clear();
        }

        // Number of 4-quads on a side.
        private const int size = 16;

        public void Clear()
        {
            generation = 0;
            quad4s = new Dictionary<(short, short), Quad4>();
            for (int y = 0; y < size; y += 1)
                for (int x = 0; x < size; x += 1)
                    AllocateQuad4(x, y);
        }

        private Quad4 GetQuad4(int x, int y)
        {
            quad4s.TryGetValue(((short)x, (short)y), out var q);
            return q;
        }

        private void SetQuad4(int x, int y, Quad4 q) => quad4s[((short)x, (short)y)] = q;

        private void StepEven()
        {
            foreach (Quad4 c in quad4s.Values)
                c.StepEven();
        }

        private void StepOdd()
        {
            foreach (Quad4 c in quad4s.Values)
                c.StepOdd();
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
            SetQuad4(x, y, c);
            return c;
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
                if (q == null)
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

                Quad4 q = GetQuad4((int)(x >> 4), (int)(y >> 4));
                if (q == null)
                    return;

                if (IsOdd)
                {
                    if (value)
                        q.SetOdd((int)(x & 0xf), (int)(y & 0xf));
                    else
                        q.ClearOdd((int)(x & 0xf), (int)(y & 0xf));
                }
                else
                {
                    if (value)
                        q.SetEven((int)(x & 0xf), (int)(y & 0xf));
                    else
                        q.ClearEven((int)(x & 0xf), (int)(y & 0xf));
                }
            }
        }

        private bool IsOdd => (generation & 0x1) != 0;

        public void Step()
        {
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
                    if (q == null)
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
            $"gen {generation}";
    }
}
