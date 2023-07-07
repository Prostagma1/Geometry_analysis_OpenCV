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
        Point[] tolerancePointsOut = new Point[3];
        Point[] tolerancePointsIn = new Point[3];

        volatile int degreeRotate = 0;
        double toleranceField = 1.5;

        int countWhitePixelsAtTemplate = -1;
        int countWhitePixelsAtVideo = -1;
        double countWhitePixelsOutside = -1;
        double forText = -1;
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

                Mat matWorkZoneWithTemplate = matWorkZone.Clone();

                if (drawTriangle)
                {
                    DrawTriangle(ref matWorkZoneWithTemplate, pointsTriangle, degreeRotate, true);
                    DrawTriangle(ref matWorkZoneWithTemplate, tolerancePointsOut, degreeRotate);
                    DrawTriangle(ref matWorkZoneWithTemplate, tolerancePointsIn, degreeRotate);
                }
                Invoke(new Action(() =>
                {
                    listBox1_SelectedIndexChanged(null, null);
                    label6.Text = $"{forText}";
                    pictureBox1.Image = BitmapConverter.ToBitmap(matWithZone);
                    pictureBox2.Image = BitmapConverter.ToBitmap(matWorkZoneWithTemplate);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }));
            }
        }
        double maxProc = -1;
        volatile int id = 0;
        Scalar sc = new Scalar(0, 0, 0);
        volatile int gg = 0;
        const int round = 361;
        private void FindContour(ref Mat mat)
        {
            Point[][] contours;
            Mat temp1 = new Mat(new Size(320, 240), MatType.CV_8UC1, sc);
            Mat temp2 = new Mat(new Size(320, 240), MatType.CV_8UC1, sc);
            Mat inversMat2 = new Mat(new Size(320, 240), MatType.CV_8UC1, sc);
            Mat mat1 = new Mat(new Size(320, 240), MatType.CV_8UC1, sc);
            Mat mat3 = new Mat(new Size(320, 240), MatType.CV_8UC1, sc);
            Mat mat2 = new Mat(new Size(320, 240), MatType.CV_8UC1, sc);
            Mat outZoneMat = new Mat(new Size(320, 240), MatType.CV_8UC1, sc);
            Mat tryZoneMat = new Mat(new Size(320, 240), MatType.CV_8UC1, sc);
            Mat inZoneMat = new Mat(new Size(320, 240), MatType.CV_8UC1, sc);
            Mat mat4 = new Mat(new Size(320, 240), MatType.CV_8UC1, sc);
            Mat mat5 = new Mat(new Size(320, 240), MatType.CV_8UC1, sc);
            Mat temp3 = new Mat(new Size(320, 240), MatType.CV_8UC1, sc);


            maxProc = -1;
            id = 0;
            for (gg = 0; gg < round; gg++)
            {
                temp1 = new Mat(new Size(320, 240), MatType.CV_8UC1, sc);
                temp2 = new Mat(new Size(320, 240), MatType.CV_8UC1, sc);
                temp3 = new Mat(new Size(320, 240), MatType.CV_8UC1, sc);
                inversMat2 = new Mat(new Size(320, 240), MatType.CV_8UC1, sc);
                mat1 = new Mat(new Size(320, 240), MatType.CV_8UC1, sc);
                mat3 = new Mat(new Size(320, 240), MatType.CV_8UC1, sc);
                mat2 = new Mat(new Size(320, 240), MatType.CV_8UC1, sc);
                outZoneMat = new Mat(new Size(320, 240), MatType.CV_8UC1, sc);
                tryZoneMat = new Mat(new Size(320, 240), MatType.CV_8UC1, sc);
                inZoneMat = new Mat(new Size(320, 240), MatType.CV_8UC1, sc);
                mat4 = new Mat(new Size(320, 240), MatType.CV_8UC1, sc);
                mat5 = new Mat(new Size(320, 240), MatType.CV_8UC1, sc);

                DrawTriangle(ref inZoneMat, tolerancePointsIn, gg);
                mat4 = FindWhitePiexel(inZoneMat, out contours);

                mat5 = FindWhitePiexel(mat, out _, true);

                mat1 = FindWhitePiexel(mat, out _);
                DrawTriangle(ref outZoneMat, tolerancePointsOut, gg);
                mat2 = FindWhitePiexel(outZoneMat, out _);

                Cv2.BitwiseAnd(mat4, mat5, temp3);

                DrawTriangle(ref tryZoneMat, pointsTriangle, gg);
                mat3 = FindWhitePiexel(tryZoneMat, out _);

                Cv2.BitwiseAnd(mat1, mat2, temp1);

                Cv2.BitwiseNot(mat2, inversMat2);
                Cv2.BitwiseAnd(mat1, inversMat2, temp2);
                Cv2.BitwiseOr(temp2, temp3, temp2);
                /*
                 * Бинарные изображения
                 * mat1 - заготовки с камеры 
                 * mat2 - максимальный размер шаблона (с допуском)
                 * temp1 - mat1 & mat2 - сколько заготовки попало в шаблон
                 * temp2 - сколько заготовки вышло за шаблон
                 * mat3 - истинный размер шаблона (без допуска) 
                */
                countWhitePixelsAtVideo = temp1.CountNonZero();
                countWhitePixelsOutside = temp2.CountNonZero();
                countWhitePixelsAtTemplate = mat3.CountNonZero();
                double proc = (countWhitePixelsAtVideo - countWhitePixelsOutside) / countWhitePixelsAtTemplate;

                if (proc > maxProc)
                {

                    forText = countWhitePixelsOutside / countWhitePixelsAtTemplate;
                    maxProc = proc;

                    trackBar3.Value = gg;
                    degreeRotate = gg;
                    pictureBox3.Image = BitmapConverter.ToBitmap(mat1);
                    pictureBox4.Image = BitmapConverter.ToBitmap(mat2);
                    pictureBox5.Image = BitmapConverter.ToBitmap(temp1);
                    pictureBox6.Image = BitmapConverter.ToBitmap(temp2);
                    pictureBox7.Image = BitmapConverter.ToBitmap(mat3);
                }
            }

        }
        private Mat FindWhitePiexel(Mat matForThis, out Point[][] contours, bool b = false)
        {
            Mat binaryMat = new Mat(new Size(320, 240), MatType.CV_8UC1, sc);
            matForThis.Blur(new Size(3, 3)).Canny(42, 185).FindContours(out contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
            if (b)
            {
                binaryMat.DrawContours(contours, -1, Scalar.White, 3);
            }
            else
            {
                for (byte i = 0; i < contours.Length; i++)
                {
                    binaryMat.FillConvexPoly(contours[i], Scalar.White);
                }
            }


            return binaryMat;
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
        private void DrawTriangle(ref Mat mat, Point[] points, int degree = 0, bool main = false)
        {
            Scalar scalar;
            if (main) scalar = new Scalar(0, 0, 255);
            else scalar = new Scalar(255, 255, 255);

            var center = SearchCenterOfThreePoint(points);

            CheckDegrees(ref points, center, degree);

            mat.Line(points[0], points[1], scalar, lineType: LineTypes.AntiAlias);
            mat.Line(points[1], points[2], scalar, lineType: LineTypes.AntiAlias);
            mat.Line(points[2], points[0], scalar, lineType: LineTypes.AntiAlias);
            mat.Circle(center, 0, scalar);
        }
        private Point SearchCenterOfThreePoint(Point[] points)
        {
            int x = 0, y = 0;
            int countPoint = points.Length;
            foreach (var point in points)
            {
                x += point.X;
                y += point.Y;
            }
            x /= countPoint; y /= countPoint;
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
                    listBox1_SelectedIndexChanged(null, null);
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

            for (byte i = 0; i < 3; i++)
            {
                tolerancePointsIn[i].X = pointsTriangle[i].X;
                tolerancePointsIn[i].Y = pointsTriangle[i].Y;

                tolerancePointsOut[i].X = pointsTriangle[i].X;
                tolerancePointsOut[i].Y = pointsTriangle[i].Y;
            }

            int addForX = (int)(toleranceField * 10.66666);
            int addForY = (int)(toleranceField * 8);
            tolerancePointsOut[0].X -= addForX;
            tolerancePointsIn[0].X += addForX;
            tolerancePointsOut[0].Y += addForY;
            tolerancePointsIn[0].Y -= addForY;

            tolerancePointsOut[1].Y -= addForY;
            tolerancePointsIn[1].Y += addForY;

            tolerancePointsOut[2].X += addForX;
            tolerancePointsIn[2].X -= addForX;
            tolerancePointsOut[2].Y += addForY;
            tolerancePointsIn[2].Y -= addForY;

        }

        private void trackBar3_Scroll(object sender, EventArgs e)
        {
            degreeRotate = trackBar3.Value;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            FindContour(ref matWorkZone);
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            toleranceField = double.Parse(textBox2.Text);
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
