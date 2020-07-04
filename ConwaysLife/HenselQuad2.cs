using System.Diagnostics;

/*
A quadN is a square of cells with side two-to-the-N.

We represent a quad0 by a single bit, but this algorithm rarely looks 
at quad0s.

We represent a quad1 by a four-bit unsigned integer.  Zero is the low bit:

NW   NE
  3 2
  1 0
SW   SE

We represent a Quad2 by a ushort containing four Quad1s; again,
zero is the low bit of the ushort:

(0, 3)          (3, 3)
    NW Q1   NE Q1   
      f e | 7 6
      d c | 5 4
      ----+----
      b a | 3 2
      9 8 | 1 0
    SW Q1   SE Q1 
(0, 0)          (3, 0)
*/

namespace ConwaysLife.Hensel
{
    struct Quad2
    {
        private static readonly uint[] masks = new uint[]
        {
            1 << 0x9, 1 << 0x8, 1 << 0x1, 1 << 0x0,
            1 << 0xb, 1 << 0xa, 1 << 0x3, 1 << 0x2,
            1 << 0xd, 1 << 0xc, 1 << 0x5, 1 << 0x4,
            1 << 0xf, 1 << 0xe, 1 << 0x7, 1 << 0x6
        };

        public const uint NWMask = 0xf000;
        public const uint SWMask = 0x0f00;
        public const uint NEMask = 0x00f0;
        public const uint SEMask = 0x000f;
        const uint EastEdgeMask = NEMask | SEMask;
        const uint WestEdgeMask = NWMask | SWMask;
        const uint SouthEdgeMask = SWMask | SEMask;
        const uint NorthEdgeMask = NWMask | NEMask;

        private readonly ushort cells;

        public static Quad2 AllDead = new Quad2(0);

        public Quad2(ushort cells)
        {
            this.cells = cells;
        }

        public bool Dead => cells == 0;

        // Given a quad2 to my right, produce a quad2 that is in
        // the middle but mirrored in the vertical axis.
        public Quad2 HorizontalMiddleMirrored(Quad2 right) =>
            new Quad2((ushort)((cells & EastEdgeMask) | (right.cells & WestEdgeMask)));

        // Given a quad2 below me, produce a quad2 that is in the middle
        // but flipped in the horizontal axis.

        public Quad2 VerticalMiddleFlipped(Quad2 bottom) =>
            new Quad2((ushort)((cells & SouthEdgeMask) | (bottom.cells & NorthEdgeMask)));

        // Make a quad2 where all but the selected cells are dead.
        public Quad2 NorthEdge => new Quad2((ushort)(cells & NorthEdgeMask));
        public Quad2 SouthEdge => new Quad2((ushort)(cells & SouthEdgeMask));
        public Quad2 EastEdge => new Quad2((ushort)(cells & EastEdgeMask));
        public Quad2 WestEdge => new Quad2((ushort)(cells & WestEdgeMask));
        public Quad2 NW => new Quad2((ushort)(cells & NWMask));
        public Quad2 NE => new Quad2((ushort)(cells & NEMask));
        public Quad2 SW => new Quad2((ushort)(cells & SWMask));
        public Quad2 SE => new Quad2((ushort)(cells & SEMask));
        public static Quad2 operator |(Quad2 x, Quad2 y) => new Quad2((ushort)(x.cells | y.cells));
        public static Quad2 operator ^(Quad2 x, Quad2 y) => new Quad2((ushort)(x.cells ^ y.cells));
        public static explicit operator ushort(Quad2 x) => x.cells;

        public bool Get(int x, int y)
        {
            Debug.Assert(0 <= x && x < 4);
            Debug.Assert(0 <= y && y < 4);
            return (cells & masks[x + y * 4]) != 0;
        }

        public Quad2 Set(int x, int y)
        {
            Debug.Assert(0 <= x && x < 4);
            Debug.Assert(0 <= y && y < 4);
            return new Quad2((ushort)(cells | masks[x + y * 4]));
        }

        public Quad2 Clear(int x, int y)
        {
            Debug.Assert(0 <= x && x < 4);
            Debug.Assert(0 <= y && y < 4);
            return new Quad2((ushort)(cells & ~masks[x + y * 4])); 
        }

        public override string ToString()
        {
            string s = "";
            for (int y = 3; y >= 0; y -= 1)
            {
                for (int x = 0; x < 4; x += 1)
                    s += this.Get(x, y) ? 'O' : '.';
                s += "\n";
            }
            return s;
        }

    }
}
