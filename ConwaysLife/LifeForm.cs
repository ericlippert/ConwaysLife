﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static ConwaysLife.Patterns;
using ConwaysLife.Hensel;

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
        private IPattern pattern;
        private bool running = true;
        private bool dragging = false;
        private LifePoint dragStart;
        private OpenFileDialog fileDialog;
        private bool logging = false;
        private static string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        // Record when we start up how much space was left
        // around the display box on the form, so that we can
        // preserve that as the form is resized.  We never move
        // the location of the display box upper left corner,
        // so the only information we need is the difference
        // between original display width/height and original
        // form height.
        private int displayHeightOffset;
        private int displayWidthOffset;

        // How far from the display is the panel? When we resize,
        // keep the panel relative to the display.
        private int panelOffset;


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
        private const int defaultScale = -1;
        private int scale = defaultScale;

        // Just as the space scale is the log2 of the side of the square, the
        // the time scale is the log2 of the number of ticks: 0 is one tick,
        // 1 is two ticks, 2 is four ticks, and so on.
        //
        // However, from the perspective of the client what we care about is
        // ticks per second. We want the timer to tick every 30ms or slower,
        // so what we will do is: if the speed is 0 through 5, we'll just
        // set the timer to 1000ms to 30ms, and calculate one tick per timer
        // event. If the speed is higher than 5 then we will calculate two to the 
        // speed - 5 ticks per timer event, and keep the timer at 30ms.

        private const int defaultSpeed = 5;  // 32 ticks per second
        private const int maximumTimerSpeed = 5;
        private const int maximumSpeed = 15; // 32K ticks per second
        private int speed = defaultSpeed;

        private void SetTimer()
        {
            if (speed <= maximumTimerSpeed)
                timer.Interval = 1000 >> speed;
            else
                timer.Interval = 1000 >> maximumTimerSpeed;
        }

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
            fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Life Files(*.cells; *.rle)|*.cells;*.rle|All files(*.*)|*.*";
            var cd = Directory.GetCurrentDirectory();
            fileDialog.InitialDirectory = new DirectoryInfo(cd)
                .Parent
                .Parent
                .EnumerateDirectories("Patterns")
                .FirstOrDefault()
                ?.FullName 
                ?? cd;

            UpdateSpeed();
            timer.Enabled = running;
            Initialize();
            // The mouse wheel event handler is not automatically generated
            // by the forms designer, so we will hook it up manually.
            display.MouseWheel += display_MouseWheel;
        }

        private void Initialize()
        {
            panelOffset = panel.Location.Y - display.Height;
            displayHeightOffset = Height - display.Height;
            displayWidthOffset = Width - display.Width;
            liveBrush = new SolidBrush(liveColor);
            gridPen = new Pen(gridColor);
            pattern = Acorn;
            Reset();
            StartRunning();
        }

        private void Reset()
        {
            StopRunning();
            life = new Gosper();

            life.AddPattern(new LifePoint(128, 128), pattern);

            scale = defaultScale;
            corner = new LifePoint(40, 200);

            Draw();
        }

        private void EnsureBitmap()
        {
            if (display.Image == null ||
                display.Image.Width != display.Width ||
                display.Image.Height != display.Height)
            {
                display.Image?.Dispose();
                display.Image = new Bitmap(display.Width, display.Height);
            }
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
            if (life is IReport r)
                reportBox.Lines = r.Report().Split('\n');
            else
                reportBox.Text = life.GetType().Name;
            if (logging && life is ILog log)
            {
                string path = Path.Combine(desktop, "lifelog.txt");
                using (var file = File.AppendText(path))
                    file.WriteLine(log.Log());
            }
        }

        private void ClearDisplay()
        {
            using (Graphics g = Graphics.FromImage(display.Image))
                g.Clear(deadColor);
        }

        private void DrawDisplay()
        {
            EnsureBitmap();
            ClearDisplay();
            if (scale < 0)
                DrawBlocks();
            else
                DrawPixels();
            display.Invalidate();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            int tickSpeed = speed <= maximumTimerSpeed ? 0 : speed - maximumTimerSpeed;
            life.Step(tickSpeed);
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

        private void LifeForm_Resize(object sender, EventArgs e)
        {
            // Expand or shrink the picture box to stay centered in the form.
            const int minWidth = 100;
            const int minHeight = 100;
            display.Width = Math.Max(minWidth, Width - displayWidthOffset);
            display.Height = Math.Max(minHeight, Height - displayHeightOffset);
            panel.Location = new Point(panel.Location.X, display.Height + panelOffset);
            Draw();
        }

        private void ToggleRunning()
        {
            running = !running;
            timer.Enabled = running;
        }

        private void StopRunning()
        {
            running = false;
            timer.Enabled = false;
        }

        private void StartRunning()
        {
            running = true;
            timer.Enabled = true;
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
            string path = Path.Combine(desktop, "lifeperf.txt");
            using (var file = File.AppendText(path))
            {
                file.WriteLine($"{DateTime.Now}:{perf.GetType()}:{stopwatch.ElapsedMilliseconds}");
            }
            timer.Enabled = save;
        }

        private void PerfTest2()
        {
            bool save = timer.Enabled;
            timer.Enabled = false;
            const int size = 11;
            var perf = new Stafford(size);
            for (int x = 40; x < (1 << size); x += 40)
                perf.AddPattern(new LifePoint(x, 20), GliderGun);
            const int blocks = 20;
            const int ticks = 500;
            long[] times = new long[blocks];
            long[] changes = new long[blocks];
            var stopwatch = new Stopwatch();
            for (int block = 0; block < blocks; block += 1)
            {
                int c = 0;
                stopwatch.Restart();
                for (int tick = 0; tick < ticks; tick += 1)
                {
                    perf.Step();
                    c += perf.ChangedTriplets;
                }
                stopwatch.Stop();
                times[block] = stopwatch.ElapsedMilliseconds;
                changes[block] = c;
            }

            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string path = Path.Combine(desktop, "lifeperf.txt");
            using (var file = File.AppendText(path))
            {
                file.WriteLine($"{DateTime.Now}:{perf.GetType()}");
                file.WriteLine(string.Join("\n", changes));
                file.WriteLine();
                file.WriteLine(string.Join("\n", times));
            }
            timer.Enabled = save;
        }


        private void LifeForm_KeyDown(object sender, KeyEventArgs e)
        {
            // Don't forget to set KeyPreview to True in the designer.
            switch (e.KeyCode)
            {
                case Keys.L:
                    LoadFile();
                    break;
                case Keys.G:
                    logging = !logging;
                    break;
                case Keys.P:
                    Debug.Fail("FYI you are performance testing in the debug build.");
                    // PerfTest2();
                    //PerfTest(new Abrash());
                    //PerfTest(new AbrashChangeList());
                    //PerfTest(new AbrashOneArray());
                    //PerfTest(new StaffordChangeList());
                    //PerfTest(new StaffordLookup());
                    // PerfTest(new Stafford());
                    PerfTest(new ProtoQuickLife());
                    PerfTest(new QuickLife());
                    // PerfTest(new SparseArray());

                    //// Run this one twice!
                    //PerfTest(new GosperSlow());
                    //PerfTest(new GosperSlow());
                    break;
                case Keys.R:
                    Reset();
                    break;
                case Keys.S:
                    Screenshot.SaveImage(display.Image);
                    break;
                case Keys.Space:
                    ToggleRunning();
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

        private void playButton_Click(object sender, EventArgs e)
        {
            ToggleRunning();
        }

        private void resetButton_Click(object sender, EventArgs e)
        {
            Reset();
        }

        private void LoadFile()
        {
            StopRunning();
            var result = fileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                var name = fileDialog.FileName.ToLowerInvariant();
                if (name.EndsWith(".cells"))
                {
                    pattern = new PlaintextPattern(File.ReadAllText(name));
                    Reset();
                }
                else if (name.EndsWith(".rle"))
                {
                    pattern = new RLEPattern(File.ReadAllText(name));
                    Reset();
                }
            }
        }

        private void loadButton_Click(object sender, EventArgs e)
        {
            LoadFile();
        }

        private void UpdateSpeed()
        {
            speedLabel.Text = speed.ToString();
            SetTimer();
        }

        private void Slower()
        {
            if (speed > 0)
            {
                speed -= 1;
                UpdateSpeed();
            }
        }

        private void slowerButton_Click(object sender, EventArgs e)
        {
            Slower();
        }

        private void Faster()
        {
            if (speed < maximumSpeed)
            {
                speed += 1;
                UpdateSpeed();
            }
        }

        private void fasterButton_Click(object sender, EventArgs e)
        {
            Faster();
        }
    }
}
