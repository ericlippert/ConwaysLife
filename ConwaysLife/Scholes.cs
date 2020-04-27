using System;
using System.Diagnostics;
using System.Text;
using static System.Math;

namespace ConwaysLife
{
    // An adaptation of John Scholes' APL algorithm to C#.
    //
    // https://aplwiki.com/wiki/John_Scholes%27_Conway%27s_Game_of_Life
    //
    // The original program is a one-liner in APL. The basic idea is to
    // use APL's rich and concise operations on arrays of numbers to
    // compute the living neighbour count as a series of matrix operations.
    //
    // The implementation gets its concision from clever use of APLs 
    // inner and outer product operations, but for our purposes
    // we're interested in understanding the operation of the algorithm
    // rather than the compact form in which the algorithm can be notated.
    //
    // I'll explain how it works by making a mutable 2D matrix of bytes 
    // data type that supports the operations that we need.


    class ByteBlock
    {
        private int Width { get; }
        private int Height { get; }
        private readonly byte[] bytes;

        public ByteBlock(int width, int height, byte[] bytes = null)
        {
            this.Width = width;
            this.Height = height;
            this.bytes = bytes == null ? new byte[width * height] : bytes;
        }

        private bool IsValid(int x, int y) =>
            0 <= x && x < Width && 0 <= y && y < Height;

        public byte this[int x, int y]
        {
            get
            {
                if (!IsValid(x, y))
                    throw new ArgumentOutOfRangeException();
                return bytes[y * Width + x];
            }

            set
            {
                if (!IsValid(x, y))
                    throw new ArgumentOutOfRangeException();
                bytes[y * Width + x] = value;
            }
        }

        // 1 2 3        2 3 0
        // 4 5 6   -->  5 6 0
        // 7 8 9        8 9 0
        public ByteBlock MoveLeft()
        {
            byte[] newBytes = new byte[bytes.Length];
            Array.Copy(bytes, 1, newBytes, 0, bytes.Length - 1);
            for (int i = Width - 1; i < newBytes.Length; i += Width)
                newBytes[i] = 0;
            return new ByteBlock(Width, Height, newBytes);
        }

        // 1 2 3        0 1 2
        // 4 5 6   -->  0 4 5
        // 7 8 9        0 7 8
        public ByteBlock MoveRight()
        {
            byte[] newBytes = new byte[bytes.Length];
            Array.Copy(bytes, 0, newBytes, 1, bytes.Length - 1);
            for (int i = Width; i < newBytes.Length; i += Width)
                newBytes[i] = 0;
            return new ByteBlock(Width, Height, newBytes);
        }

        // 1 2 3        4 5 6
        // 4 5 6   -->  7 8 9
        // 7 8 9        0 0 0
        public ByteBlock MoveUp()
        {
            byte[] newBytes = new byte[bytes.Length];
            Array.Copy(bytes, Width, newBytes, 0, bytes.Length - Width);
            return new ByteBlock(Width, Height, newBytes);
        }

        // 1 2 3        0 0 0
        // 4 5 6   -->  1 2 3
        // 7 8 9        4 5 6
        public ByteBlock MoveDown()
        {
            byte[] newBytes = new byte[bytes.Length];
            Array.Copy(bytes, 0, newBytes, Width, bytes.Length - Width);
            return new ByteBlock(Width, Height, newBytes);
        }


        // 1 2 3               0 0 0
        // 4 5 6  where 4 -->  1 0 0
        // 7 8 9               0 0 0
        public ByteBlock Where(byte b)
        {
            byte[] newBytes = new byte[bytes.Length];
            for (int i = 0; i < bytes.Length; i += 1)
                newBytes[i] = bytes[i] == b ? (byte)1 : (byte)0;
            return new ByteBlock(Width, Height, newBytes);
        }

        // 1 2 3   1 0 1      2 2 4
        // 4 5 6 + 1 1 1 -->  5 6 7
        // 7 8 9   0 1 0      7 9 9
        public ByteBlock Sum(params ByteBlock[] bs)
        {
            if (bs == null)
                throw new ArgumentNullException();

            // Omitted: Verify that every block in bs is the 
            // dimensions as this.

            byte[] newBytes = (byte[])bytes.Clone();
            foreach (var b in bs)
                for (int i = 0; i < newBytes.Length; i += 1)
                    newBytes[i] += b.bytes[i];

            return new ByteBlock(Width, Height, newBytes);
        }

        public static ByteBlock operator |(ByteBlock a, ByteBlock b)
        {
            if (a == null || b == null || a.Width != b.Width || a.Height != b.Height)
                throw new ArgumentException();

            byte[] newBytes = new byte[a.bytes.Length];
            for (int i = 0; i < newBytes.Length; i += 1)
                newBytes[i] = (byte)(a.bytes[i] | b.bytes[i]);

            return new ByteBlock(a.Width, a.Height, newBytes);
        }

        public static ByteBlock operator &(ByteBlock a, ByteBlock b)
        {
            if (a == null || b == null || a.Width != b.Width || a.Height != b.Height)
                throw new ArgumentException();

            byte[] newBytes = new byte[a.bytes.Length];
            for (int i = 0; i < newBytes.Length; i += 1)
                newBytes[i] = (byte)(a.bytes[i] & b.bytes[i]);

            return new ByteBlock(a.Width, a.Height, newBytes);
        }

        public override string ToString()
        {
            var s = new StringBuilder();
            for (int y = Height - 1; y >= 0; y -= 1)
            {
                for (int x = 0; x < Width; x += 1)
                {
                    var b = bytes[x + Width * y];
                    s.Append(b == 0 ? " " : b.ToString());
                    s.Append(" ");
                }
                s.AppendLine();
            }
            return s.ToString();
        }
    }

    class Scholes : ILife
    {
        private const int height = 256;
        private const int width = 256;
        private ByteBlock cells;

        public Scholes()
        {
            Clear();
        }

        public void Clear()
        {
            cells = new ByteBlock(width, height);
        }

        private bool IsValidPoint(long x, long y) =>
            0 < x && x < width - 1 && 0 < y && y < height - 1;

        public bool this[long x, long y]
        {
            get
            {
                if (IsValidPoint(x, y))
                    return cells[(int)x, (int)y] == 1;
                return false;
            }
            set
            {
                if (!IsValidPoint(x, y))
                    return;
                cells[(int)x, (int)y] = value ? (byte)1 : (byte)0;
            }
        }

        public bool this[LifePoint v]
        {
            get => this[v.X, v.Y];
            set => this[v.X, v.Y] = value;
        }

        public void Step()
        {
            // Suppose we have the r-pentomino on a 5x5 board:
            // 00000
            // 00110
            // 01100
            // 00100
            // 00000

            // 00000
            // 01100
            // 11000
            // 01000
            // 00000
            var w = cells.MoveLeft();

            // 00000
            // 00011
            // 00110
            // 00010
            // 00000
            var e = cells.MoveRight();

            // 00110
            // 01100
            // 00100
            // 00000
            // 00000
            var n = cells.MoveUp();

            // 00000
            // 00000
            // 00110
            // 01100
            // 00100
            var s = cells.MoveDown();

            // 01100
            // 11000
            // 01000
            // 00000
            // 00000
            var nw = w.MoveUp();

            // 00011
            // 00110
            // 00010
            // 00000
            // 00000
            var ne = e.MoveUp();

            // 00000
            // 00000
            // 01100
            // 11000
            // 01000
            var sw = w.MoveDown();

            // 00000
            // 00000
            // 00011
            // 00110
            // 00010
            var se = e.MoveDown();

            // 01221
            // 13431
            // 14541
            // 13320
            // 01110
            var sum = cells.Sum(w, e, n, s, nw, ne, sw, se);

            // Under what circumstances can a cell be alive on the next tick?
            //
            // * If it was alive and it had 2 living neighbors then the sum is now 3
            // * If it was alive and it had 3 living neighbors then the sum is now 4
            // * If it was dead and had 3 living neighbors then the sum is now 3.
            //
            // In all other cases, the cell is dead. So what do we need?

            // Sums of three are definitely alive:

            // 00000
            // 01010
            // 00000
            // 01100
            // 00000
            var threes = sum.Where(3);

            // Sums of four are alive if the original cell was alive:

            // 00000
            // 00100
            // 01010
            // 00000
            // 00000
            var fours = sum.Where(4);

            // 00000
            // 00100
            // 01000
            // 00000
            // 00000
            var livingFours = fours & cells;

            // 00000
            // 01110
            // 01000
            // 01100
            // 00000
            cells = threes | livingFours;
        }

        public void Draw(LifeRect rect, Action<LifePoint> setPixel)
        {
            long xmin = Max(0, rect.X);
            long xmax = Min(width, rect.X + rect.Width);
            long ymin = Max(0, rect.Y - rect.Height + 1);
            long ymax = Min(height, rect.Y + 1);
            for (long y = ymin; y < ymax; y += 1)
                for (long x = xmin; x < xmax; x += 1)
                    if (cells[(int)x, (int)y] == 1)
                        setPixel(new LifePoint(x, y));
        }
    }
}
