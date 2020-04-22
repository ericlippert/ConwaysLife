using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConwaysLife
{
    static class Patterns
    {
        public static void AddBlinker(this ILife life, LifePoint corner)
        {
            life[corner.X, corner.Y] = true;
            life[corner.X, corner.Y - 1] = true;
            life[corner.X, corner.Y - 2] = true;
        }

        public static void AddAcorn(this ILife life, LifePoint corner)
        {
            life[corner.X + 1, corner.Y] = true;
            life[corner.X + 3, corner.Y - 1] = true;
            life[corner.X, corner.Y - 2] = true;
            life[corner.X + 1, corner.Y - 2] = true;
            life[corner.X + 4, corner.Y - 2] = true;
            life[corner.X + 5, corner.Y - 2] = true;
            life[corner.X + 6, corner.Y - 2] = true;
        }
    }
}
