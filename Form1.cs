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
        bool runVideo;
        VideoCapture capture;
        Mat matInput,matWorkZone;
        Thread cameraThread;
        readonly Size sizeObject = new Size(640, 480);
        string pathToFile;
        readonly Point2f[] pointWorkZoneElite = new Point2f[] { new Point(100, 400), new Point(200, 180), new Point(440, 180), new Point(540, 400) };
        Point2f[] sizeMatrixPoints = new Point2f[4] { new Point(0,240), new Point(0, 0), new Point(320, 0), new Point(320, 240)};
        Point2f[] pointWorkZone = new Point2f[4];
        Scalar scalarZone;
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
                var matWithZone = matInput.Clone();
                DrawLinesAtMat(ref matWithZone, pointWorkZone, scalarZone);

                var matrix = Cv2.GetPerspectiveTransform(pointWorkZone, sizeMatrixPoints);
                
                Cv2.WarpPerspective(matInput, matWorkZone, matrix, new Size(320,240));

                Invoke(new Action(() =>
                {
                    pictureBox1.Image = BitmapConverter.ToBitmap(matWithZone);
                    pictureBox2.Image = BitmapConverter.ToBitmap(matWorkZone);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }));
            }
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

        private void trackBar2_Scroll(object sender, EventArgs e)
        {

            pointWorkZone[1].X = pointWorkZoneElite[1].X + trackBar2.Value;
            pointWorkZone[2].X = pointWorkZoneElite[2].X - trackBar2.Value;

        }
    }
}
