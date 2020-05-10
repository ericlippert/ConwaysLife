using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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
        private ILife life;
        private bool dragging = false;
        private LifePoint dragStart;

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

        private const int maxScale = 0;
        private const int minScale = -6;
        private int scale = -1;

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

        // These helpers (1) change the scale level, and (2) compute the new 
        // upper left corner point of the viewing rectangle in Life grid
        // coordinates. The passed-in point is the fixed point that we are
        // zooming at; that is, it is the point which has roughly the same
        // screen coordinates before and after the zoom.

        // That is to say, if you want to zoom in on a region, point the mouse
        // at it and zoom in with the mouse wheel; the zoomed-in region will
        // remain under the mouse. And similarly for zooming out.

        private void ZoomOut(LifePoint v)
        {
            if (scale < maxScale)
            {
                corner = new LifePoint(2 * corner.X - v.X, 2 * corner.Y - v.Y);
                scale += 1;
                DrawDisplay();
            }
        }

        private void ZoomIn(LifePoint v)
        {
            if (scale > minScale)
            {
                corner = new LifePoint((corner.X + v.X) / 2, (corner.Y + v.Y) / 2);
                scale -= 1;
                DrawDisplay();
            }
        }

        public LifeForm()
        {
            InitializeComponent();
        }

        private void LifeForm_Load(object sender, EventArgs e)
        {
            Initialize();
            Draw();
            // The mouse wheel event handler is not automatically generated
            // by the forms designer, so we will hook it up manually.
            display.MouseWheel += display_MouseWheel;
        }

        private void Initialize()
        {
            liveBrush = new SolidBrush(liveColor);
            gridPen = new Pen(gridColor);
            life = new Stafford();
            life.AddAcorn(new LifePoint(128, 128));
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
                    pixels[p.Y * (data.Stride / sizeof(int)) + p.X] = color;
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

        // In order to get mouse wheel events, the picture box needs to have
        // focus, but it does not automatically get focus when moused over.

        private void display_MouseEnter(object sender, EventArgs e)
        {
            display.Focus();
        }

        private void display_MouseWheel(object sender, MouseEventArgs e)
        {
            LifePoint v = this.BitmapToLife(new Point(e.X, e.Y));
            if (e.Delta > 0)
                ZoomIn(v);
            else if (e.Delta < 0)
                ZoomOut(v);
        }

        private void PerfTest(ILife perf)
        {
            bool save = timer.Enabled;
            timer.Enabled = false;
            perf.AddAcorn(new LifePoint(128, 128));
            const int ticks = 5000;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            for (int i = 0; i < ticks; i += 1)
                perf.Step();
            stopwatch.Stop();
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string path = Path.Combine(desktop, "lifeperf.txt");
            using (var file = File.AppendText(path))
            {
                file.WriteLine($"{DateTime.Now}:{perf.GetType()}:{stopwatch.ElapsedMilliseconds}");
            }
            timer.Enabled = save;
        }

        private void LifeForm_KeyDown(object sender, KeyEventArgs e)
        {
            // Don't forget to set KeyPreview to True in the designer.
            switch (e.KeyCode)
            {
                case Keys.P:
                    PerfTest(new Stafford());
                    break;
                case Keys.S:
                    Snapshot.SaveImage(display.Image);
                    break;
                case Keys.Space:
                    timer.Enabled = !timer.Enabled;
                    break;
            }
        }

        private void display_MouseDown(object sender, MouseEventArgs e)
        {
            dragging = true;
            dragStart = BitmapToLife(e.Location);
        }

        private void display_MouseUp(object sender, MouseEventArgs e)
        {
            dragging = false;
        }

        private void display_MouseMove(object sender, MouseEventArgs e)
        {
            if (!dragging)
                return;
            LifePoint current = BitmapToLife(e.Location);
            corner = new LifePoint(corner.X + dragStart.X - current.X, corner.Y + dragStart.Y - current.Y);
            DrawDisplay();
        }
    }
}