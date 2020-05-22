namespace ConwaysLife
{
    static class Patterns
    {
        public const string Acorn = @"
 #
   #
##  ###";

        public const string Blinker = "###";

        public const string Glider = @"
#
 #
##";

        public const string GliderGun = @"
                        #
                      # #
            ##      ##            ##
           #   #    ##            ##
##        #     #   ##
##        #   # ##    # #
          #     #       #
           #   #
            ##";
        

        public static void AddPattern(this ILife life, LifePoint corner, string s)
        {
            long x = corner.X;
            long y = corner.Y;
            foreach(char c in s)
            {
                switch(c)
                {
                    case ' ':
                        x += 1;
                        break;
                    case '\r':
                        break;
                    case '\n':
                        x = corner.X;
                        y += 1;
                        break;
                    default:
                        life[x, y] = true;
                        x += 1;
                        break;
                }
            }
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
