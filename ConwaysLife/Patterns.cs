namespace ConwaysLife
{
    static class Patterns
    {
        // The easiest periodic pattern.
        public static void AddBlinker(this ILife life, LifePoint corner)
        {
            life[corner.X, corner.Y] = true;
            life[corner.X, corner.Y - 1] = true;
            life[corner.X, corner.Y - 2] = true;
        }

        // This small initial pattern runs for over 5200 ticks before
        // reaching a stable state of still lifes, blinkers and
        // gliders.
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
