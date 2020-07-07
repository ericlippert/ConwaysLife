namespace ConwaysLife.Hensel
{
    static class HenselLookup
    {
        public static Quad2[] oddLookup = new Quad2[65536];
        public static Quad2[] evenLookup = new Quad2[65536];

        private static int Rule(int cell, int count)
        {
            if (count == 2) 
                return cell;
            if (count == 3) 
                return 1;
            return 0;
        }

        // Takes a quad2, returns an int where the low four bits are the 
        // quad1 that is the next state of the four middle cells of the quad2.
        //
        // The returned quad1 bits are:
        //
        // 3 = NW
        // 2 = NE
        // 1 = SW
        // 0 = SE

        private static int StepQuad2(Quad2 q)
        {
            int b00 = q.Get(0, 0) ? 1 : 0;
            int b01 = q.Get(0, 1) ? 1 : 0;
            int b02 = q.Get(0, 2) ? 1 : 0;
            int b03 = q.Get(0, 3) ? 1 : 0;
            int b10 = q.Get(1, 0) ? 1 : 0;
            int b11 = q.Get(1, 1) ? 1 : 0;
            int b12 = q.Get(1, 2) ? 1 : 0;
            int b13 = q.Get(1, 3) ? 1 : 0;
            int b20 = q.Get(2, 0) ? 1 : 0;
            int b21 = q.Get(2, 1) ? 1 : 0;
            int b22 = q.Get(2, 2) ? 1 : 0;
            int b23 = q.Get(2, 3) ? 1 : 0;
            int b30 = q.Get(3, 0) ? 1 : 0;
            int b31 = q.Get(3, 1) ? 1 : 0;
            int b32 = q.Get(3, 2) ? 1 : 0;
            int b33 = q.Get(3, 3) ? 1 : 0;

            int n11 = b00 + b01 + b02 + b10 + b12 + b20 + b21 + b22;
            int n12 = b01 + b02 + b03 + b11 + b13 + b21 + b22 + b23;
            int n21 = b10 + b11 + b12 + b20 + b22 + b30 + b31 + b32;
            int n22 = b11 + b12 + b13 + b21 + b23 + b31 + b32 + b33;
           
            int r11 = Rule(b11, n11);
            int r12 = Rule(b12, n12);
            int r21 = Rule(b21, n21);
            int r22 = Rule(b22, n22);

            return (r21 << 0) | (r11 << 1) | (r22 << 2) | (r12 << 3);
        }

        static HenselLookup()
        {
            // The lookup table has an unusual format; it uses 8x the amount of storage
            // it needs, but this avoids rather a lot of bit shifting and masking in the 
            // inner loop.
            //
            // Let's just consider the "even" lookup table first.
            //
            // Each entry in the lookup table is keyed on the 16 bits in a quad2, and
            // it returns a quad2. Now, you might think that the returned quad2 would
            // be the center four cells stepped one tick ahead, and the outer edge zero,
            // but that's not the case at all.
            //
            // Rather, the northwest quad1 of the returned quad2 is the center of the key
            // quad2 stepped one tick ahead. 
            //
            // What's in the other three quad1s of the output?
            //
            // The northeast quad1 gives the answer to the question "suppose we mirror-
            // reflected the input quad2 **at the level of its component quad1s** and then
            // stepped that one tick ahead".
            //
            // Let's work an example. Suppose the key quad2 is:
            //
            //  .. ..
            //  O. .O
            //  .. .O
            //  .. ..
            //
            // If we stepped the center one ahead, we'd get an empty quad1. So evenLookup[key]'s northwest corner is empty.
            //
            // The mirror reflection of that quad2 is:
            //  .. ..
            //  .O O.
            //  .O ..
            //  .. ..
            //
            // Remember, we're not reflecting the cells, we're reflecting the quad1s, swapping the east 2x4 region with
            // the west 2x4 region. 
            //
            // If we move the center of that ahead one tick we get an all-alive quad1. evenLookup[key]'s northeast corner
            // is all alive.
            //
            // Similarly the southwest corner answers the question "what if we flipped the key quad2 top-bottom?" and
            // the southeast corner answers "what if we flipped it both ways?"
            //
            // Summing up:
            //
            // evenLookup[key].NW is center of key stepped one.
            // evenLookup[key].NE is center of key.Mirror stepped one.
            // evenLookup[key].SW is center of key.Flip stepped one.
            // evenLookup[key].SE is center of key.Mirror.Flip stepped one.
            //
            // The odd lookup table is almost the same:
            //
            // oddLookup[key].NW is center of key.Mirror.Flip stepped one.
            // oddLookup[key].NE is center of key.Flip stepped one.
            // oddLookup[key].SW is center of key.Mirror stepped one.
            // oddLookup[key].SE is center of key stepped one.

            for (int i = 0; i <= 0xffff; i += 1)
            {                
                Quad2 normal = new Quad2((ushort)i);
                Quad2 mirror = normal.Mirror;
                Quad2 flip = normal.Flip;
                Quad2 both = mirror.Flip;

                int result = StepQuad2(normal);

                oddLookup[(ushort)normal] |= new Quad2((ushort)result);
                oddLookup[(ushort)flip] |= new Quad2((ushort)(result << 4));
                oddLookup[(ushort)mirror] |= new Quad2((ushort)(result << 8));
                oddLookup[(ushort)both] |= new Quad2((ushort)(result << 12));

                evenLookup[(ushort)both] |= new Quad2((ushort)result);
                evenLookup[(ushort)mirror] |= new Quad2((ushort)(result << 4));
                evenLookup[(ushort)flip] |= new Quad2((ushort)(result << 8));
                evenLookup[(ushort)normal] |= new Quad2((ushort)(result << 12));
            }
        }

        static Quad2 EvenLookup(Quad2 q) => evenLookup[(ushort)q];
        static Quad2 OddLookup(Quad2 q) => oddLookup[(ushort)q];

        /*
Given the nine quad2s shown, the next step of the shaded region is returned:
        nw    n     ne
        ....  ....  ....
        .XXX  XXXX  X...
        .XXX  XXXX  X...
        .XXX  XXXX  X...
        w     c     e
        .XXX  XXXX  X...
        .XXX  XXXX  X...
        .XXX  XXXX  X...
        .XXX  XXXX  X...
        sw    s     se
        .XXX  XXXX  X...
        ....  ....  ....
        ....  ....  ....
        ....  ....  ....
        */

        public static Quad3 Step9Quad2ToQuad3Even(
            Quad2 nw,
            Quad2 n,
            Quad2 ne,
            Quad2 w,
            Quad2 c,
            Quad2 e,
            Quad2 sw,
            Quad2 s,
            Quad2 se)
        {
            Quad2 n_w = nw.HorizontalMiddleMirrored(n);
            Quad2 n_e = n.HorizontalMiddleMirrored(ne);
            Quad2 c_w = w.HorizontalMiddleMirrored(c);
            Quad2 c_e = c.HorizontalMiddleMirrored(e);

            Quad2 w_n = nw.VerticalMiddleFlipped(w);
            Quad2 c_n = n.VerticalMiddleFlipped(c);
            Quad2 w_s = w.VerticalMiddleFlipped(sw);
            Quad2 c_s = c.VerticalMiddleFlipped(s);

            Quad2 c_nw = w_n.HorizontalMiddleMirrored(c_n);
            Quad2 c_ne = n_e.VerticalMiddleFlipped(c_e);
            Quad2 c_sw = w_s.HorizontalMiddleMirrored(c_s);
            Quad2 c_se = c_e.SouthEdge | c_s.NE | se.NW;

            Quad2 newNW = EvenLookup(nw).NW | EvenLookup(n_w).NE | EvenLookup(w_n).SW | EvenLookup(c_nw).SE;
            Quad2 newNE = EvenLookup(n).NW  | EvenLookup(n_e).NE | EvenLookup(c_n).SW | EvenLookup(c_ne).SE;
            Quad2 newSW = EvenLookup(w).NW  | EvenLookup(c_w).NE | EvenLookup(w_s).SW | EvenLookup(c_sw).SE;
            Quad2 newSE = EvenLookup(c).NW  | EvenLookup(c_e).NE | EvenLookup(c_s).SW | EvenLookup(c_se).SE;
            return new Quad3(newNW, newNE, newSW, newSE);
        }

        /*
Given the nine quad2s shown, the next step of the shaded region is returned:
        nw    n     ne
        ....  ....  ....
        ....  ....  ....
        ....  ....  ....
        ...X  XXXX  XXX.
        w     c     e
        ...X  XXXX  XXX.
        ...X  XXXX  XXX.
        ...X  XXXX  XXX.
        ...X  XXXX  XXX.
        sw    s     se
        ...X  XXXX  XXX.
        ...X  XXXX  XXX.
        ...X  XXXX  XXX.
        ....  ....  ....
        */

        public static Quad3 Step9Quad2ToQuad3Odd(
            Quad2 nw,
            Quad2 n,
            Quad2 ne,
            Quad2 w,
            Quad2 c,
            Quad2 e,
            Quad2 sw,
            Quad2 s,
            Quad2 se)
        {
            Quad2 c_e = c.HorizontalMiddleMirrored(e);
            Quad2 s_e = s.HorizontalMiddleMirrored(se);
            Quad2 c_w = w.HorizontalMiddleMirrored(c);
            Quad2 s_w = sw.HorizontalMiddleMirrored(s);

            Quad2 c_s = c.VerticalMiddleFlipped(s);
            Quad2 e_s = e.VerticalMiddleFlipped(se);
            Quad2 c_n = n.VerticalMiddleFlipped(c);
            Quad2 e_n = ne.VerticalMiddleFlipped(e);

            Quad2 c_ne = c_n.HorizontalMiddleMirrored(e_n);
            Quad2 c_sw = c_w.VerticalMiddleFlipped(s_w);
            Quad2 c_se = c_s.HorizontalMiddleMirrored(e_s);
            Quad2 c_nw = c_w.NorthEdge | c_n.SW | nw.SE;

            Quad2 newNW = OddLookup(c).SE  | OddLookup(c_n).NE | OddLookup(c_w).SW | OddLookup(c_nw).NW;
            Quad2 newNE = OddLookup(e).SE  | OddLookup(e_n).NE | OddLookup(c_e).SW | OddLookup(c_ne).NW;
            Quad2 newSW = OddLookup(s).SE  | OddLookup(c_s).NE | OddLookup(s_w).SW | OddLookup(c_sw).NW;
            Quad2 newSE = OddLookup(se).SE | OddLookup(e_s).NE | OddLookup(s_e).SW | OddLookup(c_se).NW;
            return new Quad3(newNW, newNE, newSW, newSE);
        }
    }
}
