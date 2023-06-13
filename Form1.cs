using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Point = OpenCvSharp.Point;
using Size = OpenCvSharp.Size;

namespace Lab5
{
    public partial class Form1 : Form
    {
        bool runVideo, drawTriangle;
        VideoCapture capture;
        Mat matInput, matWorkZone;
        Thread cameraThread;
        readonly Size sizeObject = new Size(640, 480);
        string pathToFile;
        readonly Point2f[] pointWorkZoneElite = new Point2f[] { new Point(100, 400), new Point(200, 180), new Point(440, 180), new Point(540, 400) };
        Point2f[] sizeMatrixPoints = new Point2f[4] { new Point(0, 240), new Point(0, 0), new Point(320, 0), new Point(320, 240) };
        Point2f[] pointWorkZone = new Point2f[4];
        Scalar scalarZone;
        Point[] pointsTriangle = new Point[3];
        int degreeRotate = 0;
        public Form1()
        {
            InitializeComponent();
            matWorkZone = new Mat();
            for (byte i = 0; i < 4; i++)
            {
                pointWorkZone[i] = pointWorkZoneElite[i];
            }
            scalarZone = new Scalar(0, 0, 255);
        }
        private void DisposeVideo()
        {
            pictureBox1.Image = null;
            pictureBox2.Image = null;
            if (cameraThread != null && cameraThread.IsAlive) cameraThread.Abort();
            matInput?.Dispose();
            capture?.Dispose();
        }
        private void DrawLinesAtMat(ref Mat mat, Point2f[] points, Scalar scalar)
        {
            mat.Line(points[0].ToPoint(), points[1].ToPoint(), scalar);
            mat.Line(points[1].ToPoint(), points[2].ToPoint(), scalar);
            mat.Line(points[2].ToPoint(), points[3].ToPoint(), scalar);
            mat.Line(points[0].ToPoint(), points[3].ToPoint(), scalar);
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (runVideo)
            {
                runVideo = false;
                DisposeVideo();
                button1.Text = "Старт";
            }
            else
            {
                runVideo = true;
                matInput = new Mat();

                if (radioButton1.Checked)
                {
                    capture = new VideoCapture(0)
                    {
                        FrameHeight = sizeObject.Height,
                        FrameWidth = sizeObject.Width,
                        AutoFocus = true
                    };
                }
                cameraThread = new Thread(new ThreadStart(CaptureCameraCallback));
                cameraThread.Start();
                button1.Text = "Стоп";
            }
        }
        private void CaptureCameraCallback()
        {
            while (runVideo)
            {
                matInput = radioButton1.Checked ? capture.RetrieveMat() : new Mat(pathToFile).Resize(sizeObject);
                Mat matWithZone = matInput.Clone();

                DrawLinesAtMat(ref matWithZone, pointWorkZone, scalarZone);

                Cv2.WarpPerspective(matInput, matWorkZone, Cv2.GetPerspectiveTransform(pointWorkZone, sizeMatrixPoints), new Size(320, 240));

                if (drawTriangle)
                {
                    DrawTriangle(ref matWorkZone, pointsTriangle, degreeRotate);
                }

                Invoke(new Action(() =>
                {
                    listBox1_SelectedIndexChanged(null, null);
                    pictureBox1.Image = BitmapConverter.ToBitmap(matWithZone);
                    pictureBox2.Image = BitmapConverter.ToBitmap(matWorkZone);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }));
            }
        }
        private void CheckDegrees(ref Point[] points, Point center, int degree)
        {
            double s = Math.Sin(degree * 3.14 / 180);
            double c = Math.Cos(degree * 3.14 / 180);
            for (byte j = 0; j < 3; j++)
            {
                points[j].X -= center.X;
                points[j].Y -= center.Y;

                int xnew = (int)Math.Floor(points[j].X * c - points[j].Y * s);
                int ynew = (int)Math.Floor(points[j].X * s + points[j].Y * c);

                points[j].X = xnew + center.X;
                points[j].Y = ynew + center.Y;

            }
        }
        private void DrawTriangle(ref Mat mat, Point[] points, int degree = 0)
        {
            var scalar = new Scalar(255, 255, 255);
            var center = SearchCenterOfThreePoint(points);

            CheckDegrees(ref points, center, degree);

            mat.Line(points[0], points[1], scalar);
            mat.Line(points[1], points[2], scalar);
            mat.Line(points[2], points[0], scalar);
            mat.Circle(center, 0, scalar);
        }
        private Point SearchCenterOfThreePoint(Point[] points)
        {
            int x = 0, y = 0;
            foreach (var point in points)
            {
                x += point.X;
                y += point.Y;
            }
            x /= 3; y /= 3;
            return new Point(x, y);
        }
        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog file = new OpenFileDialog()
            {
                Multiselect = false,
                Filter = "Мультимедия (*.bmp, *.jpg, *.png, *.mp4)| *.bmp; *.jpg; *.png; *.mp4"
            };
            if (file.ShowDialog() == DialogResult.OK)
            {
                var tempPath = file.FileName;
                if (File.Exists(tempPath))
                {
                    pathToFile = tempPath;
                    textBox1.Text = pathToFile;
                }
            }
            file.Dispose();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            DisposeVideo();
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            panel2.Enabled = true;
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            panel2.Enabled = false;
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            for (byte i = 0; i < 4; i++)
            {
                pointWorkZone[i].Y = pointWorkZoneElite[i].Y - trackBar1.Value;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            OpenFileDialog file = new OpenFileDialog()
            {
                Multiselect = false,
                Filter = "Файл с данными (*.txt, *.csv)| *.txt; *.csv"
            };
            if (file.ShowDialog() == DialogResult.OK)
            {
                var tempPath = file.FileName;
                if (File.Exists(tempPath))
                {
                    listBox1.Items.Clear();
                    foreach (var line in File.ReadAllLines(tempPath))
                    {
                        listBox1.Items.Add(line);
                    }
                    listBox1.SelectedIndex = 0;
                    listBox1_SelectedIndexChanged(null,null);
                }
            }
            file.Dispose();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex == -1) return;

            var items = listBox1.Items[listBox1.SelectedIndex].ToString().Split(';');
            pointsTriangle[0].X = (int)(double.Parse(items[0]) * 10.66666);
            pointsTriangle[0].Y = (int)(double.Parse(items[1]) * 8);
            pointsTriangle[1].X = (int)(double.Parse(items[2]) * 10.66666);
            pointsTriangle[1].Y = (int)(double.Parse(items[3]) * 8);
            pointsTriangle[2].X = (int)(double.Parse(items[4]) * 10.66666);
            pointsTriangle[2].Y = (int)(double.Parse(items[5]) * 8);
        }

        private void trackBar3_Scroll(object sender, EventArgs e)
        {
            degreeRotate = trackBar3.Value;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            drawTriangle = checkBox1.Checked;
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {

            pointWorkZone[1].X = pointWorkZoneElite[1].X + trackBar2.Value;
            pointWorkZone[2].X = pointWorkZoneElite[2].X - trackBar2.Value;

        }
    }
}
