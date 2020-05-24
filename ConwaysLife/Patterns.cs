using System;
using System.Linq;

namespace ConwaysLife
{

    interface IPattern
    {
        void Add(ILife life, LifePoint corner);
    }

    sealed class PlaintextPattern : IPattern
    {
        // The plaintext Life pattern format is described here: https://www.conwaylife.com/wiki/Plaintext
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
                y += 1;
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

        public static IPattern Blinker = new PlaintextPattern(@"
!Name: Blinker
!
OOO");

        public static IPattern Glider = new PlaintextPattern(@"
!Name: Glider 
! 
.O. 
..O 
OOO ");

        public static IPattern GliderGun = new PlaintextPattern(@"
!Name: Gosper glider gun 
! 
........................O........... 
......................O.O........... 
............OO......OO............OO 
...........O...O....OO............OO 
OO........O.....O...OO.............. 
OO........O...O.OO....O.O........... 
..........O.....O.......O........... 
...........O...O.................... 
............OO......................");

        public static IPattern LightweightSpaceship = new PlaintextPattern(@"
!Name: LWSS
!Author: John Conway
!The smallest known orthogonally moving spaceship, and the second most common spaceship(after the glider).
!http://www.conwaylife.com/wiki/index.php? title = Lightweight_spaceship
.O..O
O....
O...O
OOOO");

        public static IPattern MiddleweightSpaceship = new PlaintextPattern(@"
!Name: MWSS
!Author: John Conway
!The third most common spaceship (after the glider and lightweight spaceship).
!http://www.conwaylife.com/wiki/index.php?title=Middleweight_spaceship
...O..
.O...O
O.....
O....O
OOOOO.");

        public static IPattern HeavyweightSpaceship = new PlaintextPattern(@"
!Name: HWSS
!Author: John Conway
!The fourth most common spaceship (after the glider, lightweight spaceship and middleweight spaceship).
!http://www.conwaylife.com/wiki/index.php?title=Heavyweight_spaceship
...OO..
.O....O
O......
O.....O
OOOOOO.");

        public static IPattern Puffer1 = new PlaintextPattern(@"
!Name: Puffer 1
!Author: Bill Gosper
!An orthogonal, period-128 puffer and the first puffer to be discovered
!http://www.conwaylife.com/wiki/index.php?title=Puffer_1
.OOO......O.....O......OOO.
O..O.....OOO...OOO.....O..O
...O....OO.O...O.OO....O...
...O...................O...
...O..O.............O..O...
...O..OO...........OO..O...
..O...OO...........OO...O..");

        public static IPattern Puffer2 = new PlaintextPattern(@"
!Name: Puffer 2
!Author: Bill Gosper
!The second puffer to be found.It uses two lightweight spaceships to escort a B-heptomino.
!http://www.conwaylife.com/wiki/index.php? title = Puffer_2
.OOO...........OOO
O..O..........O..O
...O....OOO......O
...O....O..O.....O
..O....O........O.");

        public static void AddPattern(this ILife life, LifePoint corner, IPattern pattern)
        {
            pattern.Add(life, corner);
        }

        public static void AddBlinker(this ILife life, LifePoint corner)
        {
            AddPattern(life, corner, Blinker);
        }

        public static void AddAcorn(this ILife life, LifePoint corner)
        {
            AddPattern(life, corner, Acorn);
        }

        public static void AddGliderGun(this ILife life, LifePoint corner)
        {
            AddPattern(life, corner, GliderGun);
        }
    }
}
