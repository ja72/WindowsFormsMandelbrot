using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        Canvas canvas;
        public Form1()
        {
            InitializeComponent();
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            canvas = new Canvas(pictureBox1.ClientSize, -0.5f, 0.0f, 2.0f);

            SetManderbrotPicture();
        }
        private void PictureBox1_SizeChanged(object sender, System.EventArgs e)
        {
            canvas.Target = pictureBox1.ClientSize;
            SetManderbrotPicture();
        }

        private void SetManderbrotPicture()
        {
            var bmp = GenerateMandelbrotFast(canvas, 100);
            pictureBox1.Image = bmp;
        }

        public Bitmap GenerateMandelbrotFast(Canvas canvas, int iterLimit)
        {
            var bmp = new Bitmap(canvas.Target.Width, canvas.Target.Height, PixelFormat.Format32bppArgb);
            var bmpdata = bmp.LockBits(
                new Rectangle(Point.Empty, canvas.Target),
                ImageLockMode.ReadWrite,
                PixelFormat.Format32bppArgb);
            var byteSize = Bitmap.GetPixelFormatSize(bmpdata.PixelFormat)/8;
            byte[] data = new byte[bmpdata.Stride * bmpdata.Height];
            Marshal.Copy(bmpdata.Scan0, data, 0, data.Length);
            for (int py = 0; py < canvas.Target.Height; py++)
            {
                for (int px = 0; px < canvas.Target.Width; px++)
                {
                    // get coordinates of pixel (px, py)
                    (double x, double y) = canvas.GetCoord(px, py);
                    double a = x, b = y;
                    int iter = 0;
                    do
                    {
                        // use tuples for iteration
                        (a, b) = (a * a - b * b + x, 2 * a * b + y);
                        iter++;
                    } while (iter <= iterLimit && a * a + b * b < 4);
                    Color color = Color.Black;
                    if (iter > iterLimit)
                    {
                        double la = Math.Min(1f, (a * a) / 2);
                        double lb = Math.Min(1f, (b * b) / 2);
                        color = Color.FromArgb(
                            (int)(255 * la),
                            (int)(127 * la+ 127*lb),
                            (int)(255 * lb)
                            );
                    }
                    else
                    {
                        color = iter % 2 == 0 ? Color.DarkOrange : Color.Yellow;
                    }
                    data[bmpdata.Stride * py + px * byteSize + 0] = color.B;
                    data[bmpdata.Stride * py + px * byteSize + 1] = color.G;
                    data[bmpdata.Stride * py + px * byteSize + 2] = color.R;
                    data[bmpdata.Stride * py + px * byteSize + 3] = color.A;
                }
            }
            Marshal.Copy(data, 0, bmpdata.Scan0, data.Length);
            bmp.UnlockBits(bmpdata);
            return bmp;
        }
        Point mdown, mmove;
        MouseButtons buttons;

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                mdown = e.Location;
                mmove = mdown;
                buttons = e.Button;

                pictureBox1.Invalidate();
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                mmove = e.Location;
                buttons = e.Button;

                pictureBox1.Invalidate();
            }

        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (buttons == MouseButtons.Left)
            {
                e.Graphics.DrawRectangle(
                    SystemPens.HotTrack,
                    mdown.X, mdown.Y,
                    mmove.X - mdown.X, mmove.Y - mdown.Y);
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                (double xMin, double yMin) = (canvas.XMin, canvas.XMax);
                (double xMax, double yMax) = (canvas.YMin, canvas.YMin);
                double zoom = 2;
                canvas.XMin = (xMin + xMax) / 2 - zoom * (xMax - xMin) / 2;
                canvas.XMax = (xMin + xMax) / 2 + zoom * (xMax - xMin) / 2;
                canvas.YMin = (yMin + yMax) / 2 - zoom * (yMax - yMin) / 2;
                canvas.YMax = (yMin + yMax) / 2 + zoom * (yMax - yMin) / 2;

                buttons = MouseButtons.None;

                SetManderbrotPicture();

            }
            if (buttons == MouseButtons.Left)
            {
                (double xMin, double yMin) = canvas.GetCoord(mdown.X, mdown.Y);
                (double xMax, double yMax) = canvas.GetCoord(mmove.X, mmove.Y);

                canvas.XMin = xMin;
                canvas.XMax = xMax;
                canvas.YMin = yMin;
                canvas.YMax = yMax;

                buttons = MouseButtons.None;

                SetManderbrotPicture();
            }
        }
    }

    public class Canvas
    {
        public Canvas(Size target, double xCenter, double yCenter, double span)
        {
            Target = target;
            var pixels = Math.Min(target.Width, target.Height);
            double dx = span * target.Width / pixels;
            double dy = span * target.Height / pixels;
            XMin = xCenter - dx / 2;
            YMin = yCenter - dy / 2;
            XMax = xCenter + dx / 2;
            YMax = yCenter + dy / 2;
        }
        public Canvas(Size target, double xMin, double xMax, double yMin, double yMax)
        {
            Target = target;
            XMin = xMin;
            XMax = xMax;
            YMin = yMin;
            YMax = yMax;
        }

        public Size Target { get; set; }
        public int Pixels { get => Math.Min(Target.Width, Target.Height); }

        public double XMin { get; set; }
        public double XMax { get; set; }
        public double YMin { get; set; }
        public double YMax { get; set; }

        public double XCenter { get => (XMin + XMax) / 2; }
        public double YCenter { get => (YMin + YMax) / 2; }
        public double XSpan { get => (XMax - XMin); }
        public double YSpan { get => (YMax - YMin); }

        public (double x, double y) GetCoord(int px, int py)
        {
            return (XMin + ((XMax - XMin) * px) / (Target.Width - 1),
                     YMin + ((YMax - YMin) * py) / (Target.Height - 1));
        }

        public (int px, int py) GetPixels(double x, double y)
        {
            return ((int)((Target.Width - 1) * (x - XMin) / (XMax - XMin)),
                (int)((Target.Height- 1) * (y - YMin) / (YMax - YMin)));
        }
    }
}
