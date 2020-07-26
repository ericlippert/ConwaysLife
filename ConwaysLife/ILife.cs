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
        void Step(); // Step(0)
        // Speed is the log 2 of the number of ticks to compute; 0 means 
        // one tick, 1 means two ticks, 2 means four ticks, 3 means 
        // eight ticks, and so on.
        void Step(int speed);
    }

    interface IDrawScale
    {
        void Draw(LifeRect rect, Action<LifePoint> setPixel, int scale);
        int MaxScale { get; }
    }

    interface IReport
    {
        string Report();
    }

    interface ILog
    {
        string Log();
    }
}
