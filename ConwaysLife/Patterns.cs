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
