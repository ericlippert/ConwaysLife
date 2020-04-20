using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace ConwaysLife
{
    public partial class LifeForm : Form
    {
        private readonly Color deadColor = Color.LightGray;
        private readonly Color liveColor = Color.DimGray;
        private readonly Color gridColor = Color.DarkGray;
        private Brush liveBrush;
        private Pen gridPen;
        ILife life;

        // A significant amount of the code in this form deals with
        // translating coordinates in the "infinite" Life grid into
        // coordinates in the display bitmap. In particular, we will
        // want to be able to scale our view of some portion of the
        // grid. We could support arbitrary scaling, but it will
        // be convenient for our purposes to support scaling only
        // in powers of two.

        // scale is the negative log of the scaling factor:

        // scale == -3 means a cell is 8 pixels wide
        // scale == -2 means a cell is 4 pixels wide
        // scale == -1 means a cell is 2 pixels wide
        // scale ==  0 means a cell is 1 pixel wide
        
        // TODO: Positive scales are not yet implemented.

        private int scale = -5;

        // If the cells are rendered 8 pixels wide or wider, draw a grid.
        private const int gridScale = -3;

        // This operation multiplies v by the scale factor.
        private long ScaleUp(long v)
        {
            if (scale >= 0)
                return v << scale;
            return v >> -scale;
        }

        // This operation divides v by the scale factor.
        private long ScaleDown(long l)
        {
            if (scale >= 0)
                return l >> scale;
            return l << -scale;
        }

        // This is the Life coordinate of the upper left corner of the display.
        private LifePoint corner;

        // These are the width and height of the display in Life cells.
        private long LifeWidth => ScaleUp(display.Width);
        private long LifeHeight => ScaleUp(display.Height);
        private LifeRect LifeRect => new LifeRect(corner, LifeWidth, LifeHeight);

        // These functions convert between Life grid coordinates and display coordinates.
        private Point LifeToBitmap(LifePoint v) => 
            new Point(
                (int)ScaleDown(v.X - corner.X),
                (int)ScaleDown(corner.Y - v.Y));
        
        private LifePoint BitmapToLife(Point p) => 
            new LifePoint(corner.X + ScaleUp(p.X), corner.Y - ScaleUp(p.Y));

        private bool IsValidBitmapPoint(Point p) =>
            0 <= p.X && p.X < display.Width && 0 <= p.Y && p.Y < display.Height;

        public LifeForm()
        {
            InitializeComponent();
        }

        private void LifeForm_Load(object sender, EventArgs e)
        {
            Initialize();
            Draw();
        }

        private void Initialize()
        {
            liveBrush = new SolidBrush(liveColor);
            gridPen = new Pen(gridColor);
            life = new BoolArrayLife();
            life.AddBlinker(new LifePoint(5, 5));
            corner = new LifePoint(-2, LifeHeight - 2);
            display.Image = new Bitmap(display.Width, display.Height);
        }

        private bool GridEnabled() => 
            scale <= gridScale;

        private void DrawGrid()
        {
            if (!GridEnabled())
                return;
            int width = display.Width;
            int height = display.Height;
            using (Graphics g = Graphics.FromImage(display.Image))
            {
                for (int i = 0; i < width; i += 1 << -scale)
                    g.DrawLine(gridPen, i, 0, i, height - 1);
                for (int i = 0; i < height; i += 1 << -scale)
                    g.DrawLine(gridPen, 0, i, width - 1, i);
            }
        }

        private void DrawBlocks()
        {
            using (Graphics g = Graphics.FromImage(display.Image))
            {
                life.Draw(LifeRect, DrawBlock);
                void DrawBlock(LifePoint v)
                {
                    Point p = LifeToBitmap(v);
                    if (IsValidBitmapPoint(p))
                        g.FillRectangle(liveBrush, p.X, p.Y, 1 << -scale, 1 << -scale);
                }
            }
            DrawGrid();
        }

        private unsafe void DrawPixels()
        {
            // We might be changing a large number of pixels; setting individual pixels in
            // a bitmap is expensive.  Party on the bitmap memory directly!

            Bitmap bitmap = (Bitmap)display.Image;

            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, display.Width, display.Height),
                ImageLockMode.ReadWrite,
                bitmap.PixelFormat);
            int* pixels = (int*)data.Scan0;
            int color = liveColor.ToArgb();
            life.Draw(LifeRect, DrawPixel);
            bitmap.UnlockBits(data);

            void DrawPixel(LifePoint v)
            {
                Point p = LifeToBitmap(v);
                if (IsValidBitmapPoint(p))
                    pixels[p.Y * display.Width + p.X] = color;
            }
        }

        private void Draw()
        {
            DrawDisplay();
        }

        private void ClearDisplay()
        {
            using (Graphics g = Graphics.FromImage(display.Image))
                g.Clear(deadColor);
        }

        private void DrawDisplay()
        {
            ClearDisplay();
            if (scale < 0)
                DrawBlocks();
            else
                DrawPixels();
            display.Invalidate();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            life.Step();
            Draw();
        }
    }
}
