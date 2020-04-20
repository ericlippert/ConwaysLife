using System;

namespace ConwaysLife
{
    // This interface represents a mutable game of Life.
    interface ILife
    {
        // Reset the board to "every cell is dead".
        void Clear();
        // Get the state of a given cell; true is "alive", false is "dead".
        bool this[long x, long y] { get; set; }
        bool this[LifePoint v] { get; set; }
        // Call setPixel back on every living cell in the given rectangle.
        void Draw(LifeRect rect, Action<LifePoint> setPixel);
        void Step();
    }
}
