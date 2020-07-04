using System.Diagnostics;

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
            // TODO: Add a correct explanation here.


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
            Quad2 center,
            Quad2 e,
            Quad2 sw,
            Quad2 s,
            Quad2 se)
        {
            Quad2 n_w_mirror = nw.HorizontalMiddleMirrored(n);
            Quad2 w_n_flip = nw.VerticalMiddleFlipped(w);
            Quad2 n_e_mirror = n.HorizontalMiddleMirrored(ne);
            Quad2 center_n_flip = n.VerticalMiddleFlipped(center);
            Quad2 center_w_mirror = w.HorizontalMiddleMirrored(center);
            Quad2 w_s_flip = w.VerticalMiddleFlipped(sw);
            Quad2 center_e_mirror = center.HorizontalMiddleMirrored(e);
            Quad2 center_s_flip = center.VerticalMiddleFlipped(s);

            Quad2 center_nw_flip_mirror = w_n_flip.HorizontalMiddleMirrored(center_n_flip);
            Quad2 center_ne_flip_mirror = n_e_mirror.VerticalMiddleFlipped(center_e_mirror);
            Quad2 center_sw_flip_mirror = w_s_flip.HorizontalMiddleMirrored(center_s_flip);
            Quad2 center_se_flip_mirror = center_e_mirror.SouthEdge | center_s_flip.NE | se.NW;

            Quad2 new_nw =
                evenLookup[(ushort)nw].NW | 
                evenLookup[(ushort)n_w_mirror].NE |
                evenLookup[(ushort)w_n_flip].SW | 
                evenLookup[(ushort)center_nw_flip_mirror].SE;
            Quad2 new_ne = 
                evenLookup[(ushort)n].NW | 
                evenLookup[(ushort)n_e_mirror].NE |
                evenLookup[(ushort)center_n_flip].SW | 
                evenLookup[(ushort)center_ne_flip_mirror].SE;
            Quad2 new_sw = 
                evenLookup[(ushort)w].NW | 
                evenLookup[(ushort)center_w_mirror].NE |
                evenLookup[(ushort)w_s_flip].SW | 
                evenLookup[(ushort)center_sw_flip_mirror].SE;
            Quad2 new_se =
                evenLookup[(ushort)center].NW |
                evenLookup[(ushort)center_e_mirror].NE |
                evenLookup[(ushort)center_s_flip].SW |
                evenLookup[(ushort)center_se_flip_mirror].SE;
            var r = new Quad3(new_nw, new_ne, new_sw, new_se);
            return r;
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
            Quad2 center,
            Quad2 e,
            Quad2 sw,
            Quad2 s,
            Quad2 se)
        {
            Quad2 center_e_mirror = center.HorizontalMiddleMirrored(e);
            Quad2 s_e_mirror = s.HorizontalMiddleMirrored(se);
            Quad2 center_s_flip = center.VerticalMiddleFlipped(s);
            Quad2 e_s_flip = e.VerticalMiddleFlipped(se);
            Quad2 center_n_flip = n.VerticalMiddleFlipped(center);
            Quad2 e_n_flip = ne.VerticalMiddleFlipped(e);
            Quad2 center_w_mirror = w.HorizontalMiddleMirrored(center);
            Quad2 s_w_mirror = sw.HorizontalMiddleMirrored(s);
            Quad2 center_ne_flip_mirror = center_n_flip.HorizontalMiddleMirrored(e_n_flip);
            Quad2 center_sw_flip_mirror = center_w_mirror.VerticalMiddleFlipped(s_w_mirror);
            Quad2 center_se_flip_mirror = center_s_flip.HorizontalMiddleMirrored(e_s_flip);
            Quad2 center_nw_flip_mirror = center_w_mirror.NorthEdge | center_n_flip.SW | nw.SE;

            Quad2 new_nw =
                oddLookup[(ushort)center].SE |
                oddLookup[(ushort)center_n_flip].NE |
                oddLookup[(ushort)center_w_mirror].SW |
                oddLookup[(ushort)center_nw_flip_mirror].NW;
            Quad2 new_ne =
                oddLookup[(ushort)e].SE |
                oddLookup[(ushort)e_n_flip].NE |
                oddLookup[(ushort)center_e_mirror].SW |
                oddLookup[(ushort)center_ne_flip_mirror].NW;
            Quad2 new_sw =
                oddLookup[(ushort)s].SE |
                oddLookup[(ushort)center_s_flip].NE |
                oddLookup[(ushort)s_w_mirror].SW |
                oddLookup[(ushort)center_sw_flip_mirror].NW;
            Quad2 new_se =
                oddLookup[(ushort)se].SE |
                oddLookup[(ushort)e_s_flip].NE |
                oddLookup[(ushort)s_e_mirror].SW |
                oddLookup[(ushort)center_se_flip_mirror].NW;
            var r = new Quad3(new_nw, new_ne, new_sw, new_se);
            return r;
        }

    }
}
