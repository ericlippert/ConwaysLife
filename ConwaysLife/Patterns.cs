using System;
using System.Linq;

namespace ConwaysLife
{
    static class Patterns
    {
        public const string Acorn = @"
!Name: Acorn
! 
.O..... 
...O... 
OO..OOO 
";

        public const string Blinker = @"
!Name: Blinker
!
OOO";

        public const string Glider = @"
!Name: Glider 
! 
.O. 
..O 
OOO ";

        public const string GliderGun = @"
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
............OO......................";

        public const string LightweightSpaceship = @"
!Name: LWSS
!Author: John Conway
!The smallest known orthogonally moving spaceship, and the second most common spaceship(after the glider).
!http://www.conwaylife.com/wiki/index.php? title = Lightweight_spaceship
.O..O
O....
O...O
OOOO";

        public const string MiddleweightSpaceship = @"
!Name: MWSS
!Author: John Conway
!The third most common spaceship (after the glider and lightweight spaceship).
!http://www.conwaylife.com/wiki/index.php?title=Middleweight_spaceship
...O..
.O...O
O.....
O....O
OOOOO.";

        public const string HeavyweightSpaceship = @"
!Name: HWSS
!Author: John Conway
!The fourth most common spaceship (after the glider, lightweight spaceship and middleweight spaceship).
!http://www.conwaylife.com/wiki/index.php?title=Heavyweight_spaceship
...OO..
.O....O
O......
O.....O
OOOOOO.";


        public const string Puffer1 = @"
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
..O...OO...........OO...O..";

        public const string Puffer2 = @"
!Name: Puffer 2
!Author: Bill Gosper
!The second puffer to be found.It uses two lightweight spaceships to escort a B-heptomino.
!http://www.conwaylife.com/wiki/index.php? title = Puffer_2
.OOO...........OOO
O..O..........O..O
...O....OOO......O
...O....O..O.....O
..O....O........O.";

        public static void AddPlaintext(this ILife life, LifePoint corner, string s)
        {
            // The plaintext Life pattern format is described here: https://www.conwaylife.com/wiki/Plaintext
            var lines = s.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).Where(line => !line.StartsWith("!"));
            
            long y = corner.Y;
            foreach(var line in lines)
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

        public static void AddBlinker(this ILife life, LifePoint corner)
        {
            AddPlaintext(life, corner, Blinker);
        }

        public static void AddAcorn(this ILife life, LifePoint corner)
        {
            AddPlaintext(life, corner, Acorn);
        }

        public static void AddGliderGun(this ILife life, LifePoint corner)
        {
            AddPlaintext(life, corner, GliderGun);
        }
    }
}
