using System;
using System.Linq;
using static System.Math;

namespace ConwaysLife
{
    interface IPattern
    {
        void Add(ILife life, LifePoint corner);
    }

    sealed class PlaintextPattern : IPattern
    {
        // The plaintext Life pattern format is described here: https://www.conwaylife.com/wiki/Plaintext
        // Briefly:
        // ! indicates a comment
        // O is a living cell
        // . is a dead cell. 
        // Rows are indicated by line breaks.
        private string plaintext;
        public PlaintextPattern(string plaintext)
        {
            this.plaintext = plaintext;
        }

        public void Add(ILife life, LifePoint corner)
        {
            var lines = plaintext.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).Where(line => !line.StartsWith("!"));

            long y = corner.Y;
            foreach (var line in lines)
            {
                long x = corner.X;
                foreach (char c in line)
                {
                    if (c == 'O')
                        life[x, y] = true;
                    x += 1;
                }
                y -= 1;
            }
        }
    }

    sealed class RLEPattern : IPattern
    {
        // The RLE Life pattern format is described here: https://www.conwaylife.com/wiki/Run_Length_Encoded
        // Briefly: 
        // # indicates a comment
        // "x = n, y = n" gives the bounding box of the pattern.
        // A number followed by b or o indicates that number of dead or
        // alive cells, respectively, and the number may be omitted if the number is one.
        // A number followed by $ is a newline, and again, it may be omitted if one.
        // ! is the end of the pattern.

        private string rle;
        public RLEPattern(string rle)
        {
            this.rle = rle;
        }

        public void Add(ILife life, LifePoint corner)
        {
            var lines = rle.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => !line.StartsWith("#") && !line.StartsWith("x"));

            long x = corner.X;
            long y = corner.Y;
            long len = 0;

            foreach (string line in lines)
            {
                foreach (char c in line)
                {
                    switch (c)
                    {
                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                            len = len * 10 + ((long)c - (long)'0');
                            break;
                        case 'b':
                            x += Max(len, 1);
                            len = 0;
                            break;
                        case 'o':
                            len = Max(len, 1);
                            for (long i = 0; i < len; i += 1)
                                life[x + i, y] = true;
                            x += len;
                            len = 0;
                            break;
                        case '$':
                            y -= Max(len, 1);
                            x = corner.X;
                            len = 0;
                            break;
                        case '!':
                            return;
                    }
                }
            }
        }
    }

    static class Patterns
    {
        public static IPattern Acorn = new PlaintextPattern(@"
!Name: Acorn
! 
.O..... 
...O... 
OO..OOO 
");

        public static IPattern Puffer2 = new PlaintextPattern(@"
!Name: Puffer 2
!Author: Bill Gosper
!The second puffer to be found.It uses two lightweight spaceships to escort a B-heptomino.
!http://www.conwaylife.com/wiki/index.php?title=Puffer_2
.OOO...........OOO
O..O..........O..O
...O....OOO......O
...O....O..O.....O
..O....O........O.");

        public static void AddPattern(this ILife life, LifePoint corner, IPattern pattern)
        {
            pattern.Add(life, corner);
        }
    }
}
