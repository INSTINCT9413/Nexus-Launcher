using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace HorizonUI
{
    public class StepProgress : Control
    {
        // -------------------------
        // Fields
        // -------------------------
        private Timer rotateTimer;
        private int currentAngle = 0;

        private Image imgMin;
        private Image imgProgress;
        private Image imgMax;

        private Image imgMinTinted;
        private Image imgProgressTinted;
        private Image imgMaxTinted;

        private int _value = 0;
        private int _minimum = 0;
        private int _maximum = 100;

        private int rotationSpeed = 6; // degrees per tick
        private Color progressColor = Color.FromArgb(50, 205, 50);
        private Color imageTintColor = Color.FromArgb(124, 252, 0); //Tint color applied to icons (default is slightly off-color because tint)
        private bool hideLoading = false;

        // -------------------------
        // Enums / Events
        // -------------------------
        public enum ProgressState
        {
            Min,
            Progressing,
            Max
        }

        // -------------------------
        // Properties
        // -------------------------
        [Category("Behavior")]
        [Description("How fast the loading icon rotates (degrees per timer tick).")]
        [DefaultValue(6)]
        public int RotationSpeed
        {
            get { return rotationSpeed; }
            set
            {
                rotationSpeed = Math.Max(0, value);
                Invalidate();
            }
        }

        [Category("Appearance")]
        [Description("Color used to draw the progress bar fill.")]
        public Color ProgressColor
        {
            get { return progressColor; }
            set
            {
                progressColor = value;
                Invalidate();
            }
        }

        [Category("Appearance")]
        [Description("Tint color applied to the icons.")]
        public Color ImageTintColor
        {
            get { return imageTintColor; }
            set
            {
                imageTintColor = value;
                RefreshTintedIcons();
                Invalidate();
            }
        }

        [Category("Behavior")]
        [Description("Hide the loading/progress bar visuals (useful for special states).")]
        [DefaultValue(false)]
        public bool HideLoading
        {
            get { return hideLoading; }
            set
            {
                hideLoading = value;
                Invalidate();
            }
        }

        [Category("Behavior")]
        [Description("Current numeric value for the progress control.")]
        public int Value
        {
            get { return _value; }
            set
            {
                int clamped = Math.Max(Minimum, Math.Min(value, Maximum));
                if (clamped != _value)
                {
                    _value = clamped;
                    UpdateTimerState();
                    Invalidate();
                }
            }
        }

        [Category("Behavior")]
        [Description("Minimum allowed value for the progress control.")]
        [DefaultValue(0)]
        public int Minimum
        {
            get { return _minimum; }
            set
            {
                _minimum = value;
                if (_maximum < _minimum) _maximum = _minimum;
                if (_value < _minimum) _value = _minimum;
                UpdateTimerState();
                Invalidate();
            }
        }

        [Category("Behavior")]
        [Description("Maximum allowed value for the progress control.")]
        [DefaultValue(100)]
        public int Maximum
        {
            get { return _maximum; }
            set
            {
                _maximum = Math.Max(1, value);
                if (_value > _maximum) _value = _maximum;
                UpdateTimerState();
                Invalidate();
            }
        }

        [Browsable(false)]
        public ProgressState State
        {
            get
            {
                if (_value <= Minimum) return ProgressState.Min;
                if (_value >= Maximum) return ProgressState.Max;
                return ProgressState.Progressing;
            }
        }

        // -------------------------
        // Constructor
        // -------------------------
        public StepProgress()
        {
            DoubleBuffered = true;

            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);

            BackColor = Color.Transparent;
            LoadIconsSafe();

            rotateTimer = new Timer();
            rotateTimer.Interval = 50; // about 20 FPS
            rotateTimer.Tick += RotateTimer_Tick;
            UpdateTimerState();
        }

        // -------------------------
        // Timer handling
        // -------------------------
        private void RotateTimer_Tick(object sender, EventArgs e)
        {
            // rotationSpeed degrees per tick
            currentAngle += rotationSpeed;
            if (currentAngle >= 360) currentAngle -= 360;
            Invalidate();
        }

        private void UpdateTimerState()
        {
            if (!hideLoading && _value > Minimum && _value < Maximum)
            {
                if (!rotateTimer.Enabled) rotateTimer.Start();
            }
            else
            {
                if (rotateTimer.Enabled) rotateTimer.Stop();
                currentAngle = 0;
            }
        }

        // -------------------------
        // Public step methods
        // -------------------------
        public void Increment(int step = 1)
        {
            Value = Math.Min(Maximum, Value + Math.Max(1, step));
        }

        public void Decrement(int step = 1)
        {
            Value = Math.Max(Minimum, Value - Math.Max(1, step));
        }

        // -------------------------
        // Click behavior (cycles states)
        // -------------------------

        // This is just for demonstration/testing. In a real app, you might want to control state changes externally.
        //protected override void OnClick(EventArgs e)
        //{
        //    base.OnClick(e);
        //    // Cycle: Min -> Progressing(mid) -> Max -> Min
        //    if (State == ProgressState.Min)
        //    {
        //        Value = (Minimum + Maximum) / 2;
        //    }
        //    else if (State == ProgressState.Progressing)
        //    {
        //        Value = Maximum;
        //    }
        //    else // Max
        //    {
        //        Value = Minimum;
        //    }
        //}

        // -------------------------
        // Painting
        // -------------------------
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

            // Clear to parent back color for designer-run safety
            //Color bg = (Parent != null) ? Parent.BackColor : BackColor;
            //g.Clear(bg);

            // Draw the base "track" if not hidden
            int barX = 31;
            int barY = 13;
            int barHeight = 3;
            int barWidth = Math.Max(0, Width - 32);

            if (!hideLoading)
            {
                using (Brush track = new SolidBrush(Color.FromArgb(180, 180, 180)))
                {
                    g.FillRectangle(track, new Rectangle(barX, barY, barWidth, barHeight));
                }
            }

            // Calculate filled width (safely)
            int fillWidth = 0;
            if (Maximum > Minimum)
            {
                double ratio = (double)(Value - Minimum) / (double)(Maximum - Minimum);
                fillWidth = (int)(ratio * barWidth);
            }

            // Draw icon according to state
            Image iconToDraw = null;
            if (State == ProgressState.Min) iconToDraw = imgMinTinted ?? imgMin;
            else if (State == ProgressState.Max) iconToDraw = imgMaxTinted ?? imgMax;
            else iconToDraw = imgProgressTinted ?? imgProgress;

            if (iconToDraw != null)
            {
                // Draw rotating if Progressing
                if (State == ProgressState.Progressing && !hideLoading)
                {
                    DrawRotatedImage(g, iconToDraw, 0, 0, currentAngle);
                }
                else
                {
                    g.DrawImage(iconToDraw, new Point(0, 0));
                }
            }

            // Draw fill if needed
            if (!hideLoading && fillWidth > 0)
            {
                using (SolidBrush fillBrush = new SolidBrush(progressColor))
                {
                    g.FillRectangle(fillBrush, new Rectangle(barX, barY, fillWidth, barHeight));
                }
            }
        }
        protected override void OnPaintBackground(
    PaintEventArgs e)
        {
            if (Parent != null)
            {
                Rectangle rect =
                    new Rectangle(
                        Left,
                        Top,
                        Width,
                        Height);

                GraphicsState state =
                    e.Graphics.Save();

                try
                {
                    e.Graphics.TranslateTransform(
                        -Left,
                        -Top);

                    PaintEventArgs pea =
                        new PaintEventArgs(
                            e.Graphics,
                            rect);

                    InvokePaintBackground(
                        Parent,
                        pea);

                    InvokePaint(
                        Parent,
                        pea);
                }
                finally
                {
                    e.Graphics.Restore(state);
                }
            }
            else
            {
                base.OnPaintBackground(e);
            }
        }
        private void DrawRotatedImage(Graphics g, Image img, float x, float y, float angle)
        {
            // Avoid drift by rendering into a stable buffer first
            int w = img.Width;
            int h = img.Height;

            using (Bitmap buffer = new Bitmap(w, h))
            {
                buffer.SetResolution(g.DpiX, g.DpiY);

                using (Graphics g2 = Graphics.FromImage(buffer))
                {
                    g2.Clear(Color.Transparent);
                    g2.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    g2.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g2.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                    // Center rotation
                    g2.TranslateTransform(w / 2f, h / 2f);
                    g2.RotateTransform(angle);
                    g2.TranslateTransform(-w / 2f, -h / 2f);

                    g2.DrawImage(img, 0, 0, w, h);
                }

                // Draw result buffer without transforms → prevents drifting
                g.DrawImage(buffer, x, y, w, h);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            // Keep height fixed to original visual style (32 px)
            this.Height = 32;
        }

        // -------------------------
        // Icon loading & tinting
        // -------------------------
        private void LoadIconsSafe()
        {
            imgMin = SafeLoadBase64(IconBase64_Min);
            imgProgress = SafeLoadBase64(IconBase64_Progressing);
            imgMax = SafeLoadBase64(IconBase64_Max);

            RefreshTintedIcons();
        }

        private Image SafeLoadBase64(string base64)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(base64)) return new Bitmap(32, 32);
                byte[] bytes = Convert.FromBase64String(base64);
                using (MemoryStream ms = new MemoryStream(bytes))
                {
                    Image img = Image.FromStream(ms);
                    return new Bitmap(img); // return a copy
                }
            }
            catch
            {
                // designer-safe fallback
                Bitmap fallback = new Bitmap(32, 32);
                using (Graphics g = Graphics.FromImage(fallback))
                {
                    using (Brush b = new SolidBrush(Color.FromArgb(200, 200, 200)))
                    {
                        g.FillRectangle(b, 0, 0, fallback.Width, fallback.Height);
                    }
                }
                return fallback;
            }
        }

        private void RefreshTintedIcons()
        {
            // Dispose old tinted if present
            if (imgMinTinted != null) { imgMinTinted.Dispose(); imgMinTinted = null; }
            if (imgProgressTinted != null) { imgProgressTinted.Dispose(); imgProgressTinted = null; }
            if (imgMaxTinted != null) { imgMaxTinted.Dispose(); imgMaxTinted = null; }

            // Create tinted copies
            imgMinTinted = CreateTintedImage(imgMin, imageTintColor);
            imgProgressTinted = CreateTintedImage(imgProgress, imageTintColor);
            imgMaxTinted = CreateTintedImage(imgMax, imageTintColor);
        }

        private Image CreateTintedImage(Image src, Color tint)
        {
            if (src == null) return null;

            Bitmap bmp = new Bitmap(src.Width, src.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                // preserve alpha and apply tint using color matrix
                float r = tint.R / 255f;
                float gC = tint.G / 255f;
                float b = tint.B / 255f;

                ColorMatrix cm = new ColorMatrix(new float[][]
                {
                    new float[] { r, 0, 0, 0, 0 },
                    new float[] { 0, gC, 0, 0, 0 },
                    new float[] { 0, 0, b, 0, 0 },
                    new float[] { 0, 0, 0, 1, 0 },
                    new float[] { 0, 0, 0, 0, 1 }
                });

                ImageAttributes ia = new ImageAttributes();
                ia.SetColorMatrix(cm, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                g.DrawImage(src,
                    new Rectangle(0, 0, src.Width, src.Height),
                    0, 0, src.Width, src.Height,
                    GraphicsUnit.Pixel, ia);
            }
            return bmp;
        }

        // -------------------------
        // Embedded Base64 icons (replace if you want other icons)
        // These are the three base64 strings you provided earlier.
        // -------------------------
        private const string IconBase64_Min =
@"iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAABzenr0AAAC6UlEQVRYhc3XMYhcVRQG4G8fi4Qt
QrCw2iJVsNxiiAbLrFtYBES49XaSTrPFZguLaLFJsatdSGd9QQSRhWw2lYjJMoQtxUK2WCwsJKQI
IjJY3PNmxsmbmTtCWH8YuPe8c89/5tz7zv3fkkrknC9gHdexhsu4GI9f4BQneIyjlNKfNXGXKohX
sYVNXKrM9zm+xl5K6ew/JZBzXsZt7GBl7NFL9PEL/gjbm7iCXofvLu6mlP6uTiD+9Te4GqYBDvAA
hymlv6asewMb+BgfoIlHx/ioqxqvJJBzfhuPsBqmPm6mlPpdpNOQc+7hvlIVOMP7KaWfx/2aiUWr
E+T7uLYoOcSaaxFDxHwUHEMMKxB7/qNR2bdSSvumIOf8Lr6I6WcppSczfG9hL6bHeK89E+MVuD1G
vj+H/C08VF7LdTwMWyciVhvvanAZJhBl2QlbH9vTggVuGPUAMb4xZ812xIaddivaCmwpr89AOXCd
r8wEYY1tiIh5MzhWglMTHW4z/A4qD1xXl5vb+SL2QUw3c84XGmUP2w73oIKcUQOaZ+tCy3EJ643S
2yld67AyyO+Vti4cBhdcb5SLBfrTOlwHfqu0vYLgaLd5rVFuNUpvr8WpcphaDMJWi5brcmN0emv3
UFy144SntdfvBNfFZqbbbJxMGS+ERhETlCt1EfwwZVyDlutFY1TKKwsG+U7Z+0GMF0HLddoYla8X
93kVUkq/4g7uxLgKwdFe0SfLiob7RGmPG/h+gSQ+r/Udw4aRanrc4EjRcBQl87rRcjzH0RLknL9U
qjDAOzX3Qc55Bd/G9MOU0stZ/rGmh6fK4f8qpfRp+xruKe2xwf0QJ/OwppRzw6ibziJfViRaE1x7
YiLE4m749nCvIoHJTjgP94wO324rUMcb0V1FLsGtkFGz8Aw5fs9mOUasNt5xcGFCFYdK+cm/Rel2
hUCZRrys/POW/EwRuUN5fu6y/P/3YTIW7Pw+zSYSOZ+P045EXsvn+T/nYyK/KE7IRwAAAABJRU5E
rkJggg==";

        private const string IconBase64_Progressing =
@"iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAABzenr0AAADDElEQVRYhbWXT0hVQRTGf0qIiIQ0
Ei5cRFQLkwyLFgMlBUEFgWiWDBUUCekmiooiQlpIBdE/KioSWtgsKoogahNEUIOFi1rkIlqkSEQ4
IhEiEtJirjrMm2vvXV/f6t0zc873vbnnnnOmhIxQRqwB9gHdWtqJrHFKszoC5cBx4LMyYkfWICVZ
nJQRFUAz8MAzPwSOamm//xcByogaYD/QCjQCiyLbxoHTwF0t7XRRBCgjKoFzQBfu2PPBW+CAlvbr
vzbG/oVP3gg8ApbnSTyDH0BeiZmahMqILcCbAsmHgZ1a2jY/F5QRVQUJUEasBZ4BlZHlUeAW0O3Z
/gCXgdVa2udBrF3AkDJCxrhyciDJ8E/AimBpEpcL17W0E8qIDcB7oB/o1NJ+jMQ6DNzE/dERoEFL
O+bvieXA2Qj5KLBdSzvg2caBTlIyXhnRlZDPoBboSXxmURI4VQNDQIVnngKatLT9EbGpUEbU4U7I
f41TwEot7fCMIcyBvQE5QE+h5ABa2kHgTGAuAzp8QyigNXgeAy4VSu7hNu7d+2iOClBGlAEbgs1P
FtJotLRTgA7MdcqIJTkCgGW4I/LxLiv5PDFKgVUxAdUR5/D4siAWY5ZrIe24KPAFjEbWa4vAEYsx
y+UL+Iardj42FkFAU8T2JUdAkrHh996StONMSL6s3YF50C/HYQ48DZ6rgBNZBeBmiPAVPPYfQgF9
wO/Adiqtk82HpKP2BOYpoDdVQHI0VwOnMmBTCkmpMuKgMmJ9YG8EXpJb1u/5fSBHQILzzCXJNG7Q
vBAhr8cNLL0k37UyolwZcRJXfGoClxFye0N8Jkxm/tfAES1tX7BWgWvZx5irnDeS3y3EC9oksDnW
1FKHUmXEYi3tr8C2DdfjCxnTJoA2Le2L2GJeY7kyYilwDWgvgBhcbdmjpf2QtiHfUlxKfD5MwyRu
RmyYjxwKvBkpI9qBK+QmGLjBdABXS+5raX/mE7Pgq1kyYl8EDjF3gh2AzjI7ZLobJkIkcAeoB7Zq
aV9liZO5HWtpDbAOdxfMPDX9Ba4m4yf9OQ8TAAAAAElFTkSuQmCC";

        private const string IconBase64_Max =
@"iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAABzenr0AAAC5ElEQVRYhbXXTYgcRRjG8d80yxJC
kJCWsIJgCB70IkEDYkdCcJWIJ0E82DEEQTSKiDGnCB4kfoEYEBUxF9FDiV4UDx78FrS8LDKoiEgM
S/QQAg2yhGVZluChunEy6Z3pncw+l6G6uuf/9Ftdb71vT0eVMZ/F3bgHt2I3tmEVyziPBXyNL0JR
LXf5314H8E4cx6PY0dHvEj7Aa6Gozk1koIz5DJ7B87imI3hYKziFk6GoVjobKGM+h4+wf0LwsPp4
IBTV2bEGypjvxpfSGk9TF3AwFFV/XQP1ev+0CfBBE3cMRiIbgGf4cBPhsBOflDHfcoUBPIW7NhHe
6Ba80Ax6UMZ8B/7C9ilBLuJPKV+0aRU3h6I620Tg2BThcAi34bCUE4Y1ixPQq/f735ibEvx0KKrH
m0EZ8+fwUst9y7guw51ThJ+RsmYD3yJFoU1bcV+G+SnBL+GRUFQXB66dxE0jnpnPsHdKBk6Fovqh
GZQxL6RUPkp7Mlw/Bfjv0pnRwLfifcyMeW5XJq3F1WgNR4YOm1dwY4dnZzJp7cbpM9yAT1vmXg5F
tdAMypgfkJJaJ2XSkTlK30gn2Tk8iI8H5n6WPrQGvg3vuTzDjtJahn/G3PRHKKo1qH8PIdTGjzRz
tV7Hro5wWMykMmqUjpYxf7IZ1MDD0tH6W3O9jPm9UtW0EfV7Zcz34/sxN17CE6GoTrdNljHfjl9t
fEc9lCFKBeUoZXinjPlj68y/OQF8GZ9ndUhb32wdE0cHL5Yxvx8PbxAOIRTVUvO1voF/O5p4u4z5
szV8Du9OAF+VcsX/JVkZ86drI121IIV9koPs1VBUJ7h8v74l7fmu2jshvG+4ImpUF6U/6pZGJ9F5
7GstSiEU1QWp9TqzSfCDw73BFSkzFNUi9uG7KcL70pv/MjzRmrPrSMxL1U1bTddVK3gRt7d1RXRv
To9JafbajuCrb05bjMzigPSN7JEamKaSXsKiFOpv8VXX9vw/9LbblYIHwywAAAAASUVORK5CYII=";

        // -------------------------
        // Dispose (cleanup)
        // -------------------------
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (rotateTimer != null)
                {
                    rotateTimer.Stop();
                    rotateTimer.Tick -= RotateTimer_Tick;
                    rotateTimer.Dispose();
                    rotateTimer = null;
                }

                if (imgMin != null) { imgMin.Dispose(); imgMin = null; }
                if (imgProgress != null) { imgProgress.Dispose(); imgProgress = null; }
                if (imgMax != null) { imgMax.Dispose(); imgMax = null; }

                if (imgMinTinted != null) { imgMinTinted.Dispose(); imgMinTinted = null; }
                if (imgProgressTinted != null) { imgProgressTinted.Dispose(); imgProgressTinted = null; }
                if (imgMaxTinted != null) { imgMaxTinted.Dispose(); imgMaxTinted = null; }
            }
            base.Dispose(disposing);
        }
    }
}
