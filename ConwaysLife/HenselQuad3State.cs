namespace ConwaysLife.Hensel
{
    struct Quad3State
    {
        //  7: all dead
        //  6: W (even) or E (odd) edge dead
        //  5: N (even) or S (odd) edge dead
        //  4: NW (even) or SE (odd) corner dead
        //  3: all stable
        //  2: W (even) or E (odd) edge stable
        //  1: N (even) or S (odd) edge stable
        //  0: NW (even) or SE (odd) corner stable

        // We never store one of these; we just use it as a repository for giving names to
        // bit operations, so there's no reason to force the runtime to shrink it down to
        // a byte constantly.

        readonly private uint b;
        public Quad3State(int b) => this.b = (uint)b;
        public Quad3State(uint b) => this.b = b;
        public Quad3State SetAllRegionsActive() => new Quad3State(0x00);
        public Quad3State SetVerticalEdgeAndQuadActive() => new Quad3State(b & 0x33);
        public Quad3State SetHorizontalEdgeAndQuadActive() => new Quad3State(b & 0x55);
        public Quad3State SetQuad3Active() => new Quad3State(b & 0x77);
        // Calling any of the stable setters on a region which is dead keeps it dead,
        // which is what we want.
        public Quad3State SetAllRegionsStable() => new Quad3State(b | 0xf);
        // We could also set the corner to be stable, because if the edge is
        // stable then the corner is too.  However, on every code path
        // where these are called, the corner bit has already been set.
        public Quad3State SetVerticalEdgeStable() => new Quad3State(b | 0x04);
        public Quad3State SetHorizontalEdgeStable() => new Quad3State(b | 0x02);
        public Quad3State SetCornerStable() => new Quad3State(b | 0x01);
        public Quad3State SetAllRegionsDead() => new Quad3State(0xff);
        // We could also set the corner to be dead, because if the edge is
        // dead then the corner is too.  However, on every code path
        // where these are called, the corner bit has already been set.
        public Quad3State SetVerticalEdgeDead() => new Quad3State(b | 0x44);
        public Quad3State SetHorizontalEdgeDead() => new Quad3State(b | 0x22);
        public Quad3State SetCornerDead() => new Quad3State(b | 0x11);

        public static explicit operator uint(Quad3State s) => s.b;
    }
}