using AForge.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AForge.Math.Geometry;
using System.Drawing;
using System.Drawing.Imaging;
using AForge;
using AForge.Imaging.Filters;
using System.Windows;
using System.Windows.Input;
using BetterClicker.Win32Actions;
using System.Threading;
using BetterClicker.Models;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace BetterClicker.Logic
{
    public class ImageProcessingLogic
    {
        public Random Random { get; private set; }
        public SettingsModel Settings
        {
            get
            {
                return MainWindow.AppModel.Settings;
            }
        }

        public Bitmap ConditionImage { get; private set; }

        public ImageProcessingLogic()
        {
            this.Random = new Random(DateTime.Now.Millisecond);
        }

        public Models.Point GetColouredBoxPoint()
        {
            // Open your image

            Bitmap image = GetScreenshot();
            // locating objects
            var centerPoint = FindBlobs(image);
            //image.Save(@"C:\RBP\result.png");
            return centerPoint;
        }

        private Bitmap GetScreenshot()
        {
            double screenLeft = SystemParameters.VirtualScreenLeft;
            
            double screenTop = SystemParameters.VirtualScreenTop;
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;

            Bitmap bmp = new Bitmap((int)screenWidth, (int)screenHeight);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen((int)screenLeft, (int)screenTop, 0, 0, bmp.Size);
                return bmp;
            }
        }

        public Models.Point FindBlobs(Bitmap image)
        {
            FilterOutGreenBlobs(image);
            
            BlobCounter blobCounter = new BlobCounter();
            blobCounter.FilterBlobs = true;
            blobCounter.MinHeight = Settings.MinBlobSize ?? 40;
            blobCounter.MinWidth = Settings.MinBlobSize ?? 40;
            blobCounter.ProcessImage(image);
            Blob[] blobs = blobCounter.GetObjectsInformation();

            Graphics g2 = Graphics.FromImage(image);
            foreach (var redBlob in blobs)
            {
                g2.DrawRectangle(new Pen(Color.Green, 3.0f), redBlob.Rectangle);
            }
            image.Save(@"C:\RBP\GreenResultMarked.png");

            if (blobs.Length == 0)
            {
                var newImage = GetScreenshot();
                FilterOutRedBlobs(newImage);
                blobCounter.ProcessImage(newImage);
                Blob[] redBlobs = blobCounter.GetObjectsInformation();
                Graphics g = Graphics.FromImage(newImage);
                foreach (var redBlob in redBlobs)
                {
                    g.DrawRectangle(new Pen(Color.Green, 3.0f), redBlob.Rectangle);
                }
                newImage.Save(@"C:\RBP\RedResultMarked.png");
                if (Settings.AgilityMode)
                {
                    if (redBlobs.Length == 2)
                    {
                        var smallBlob = redBlobs.Where(x => x.Area < 650).OrderByDescending(x => x.Rectangle.X).FirstOrDefault();
                        if (smallBlob == null)
                        {
                            smallBlob = redBlobs.FirstOrDefault();
                        }
                        var point = GetPointFromEdgeToCenter(blobCounter, smallBlob, true);
                        MouseActions.DoLeftClick(point);
                        Thread.Sleep(4000);
                        return GetColouredBoxPoint();
                    }
                }
                if (redBlobs.Length > 1)
                {
                    var centerPoint = new AForge.Point(Settings.ScreenCenter.X, Settings.ScreenCenter.Y);
                    var closestBlob = redBlobs.OrderBy(x => x.CenterOfGravity.DistanceTo(centerPoint)).FirstOrDefault();
                    var distance = closestBlob.CenterOfGravity.DistanceTo(centerPoint);
                    var point = GetPointFromEdgeToCenter(blobCounter, closestBlob, true);
                    if (distance > 350)
                    {
                        Thread.Sleep(600);
                        if (point.X > 650)
                        {
                            Thread.Sleep(600);
                        }
                    }
                    return point;
                }
                if (redBlobs.Length != 0)
                {
                    return GetBiggestBlobRandomMedianFromCenterToEdge(newImage, blobCounter, redBlobs);
                }
                else
                {
                    Thread.Sleep(2000);
                    return GetColouredBoxPoint();
                }
            }
            // check for rectangles
            return GetBiggestBlobRandomMedianFromCenterToEdge(image, blobCounter, blobs);
        }

        public void SetCondition()
        {
            Bitmap conditionImage = GetConditionImage();
            this.ConditionImage = conditionImage;
        }

        private Bitmap GetConditionImage()
        {
            var width = Settings.ConditionRightBottom.X - Settings.ConditionLeftTop.X;
            var height = Settings.ConditionRightBottom.Y - Settings.ConditionLeftTop.Y;
            var rectangle = new Rectangle(Settings.ConditionLeftTop.X, Settings.ConditionLeftTop.Y, width, height);


            var image = GetScreenshot();
            Bitmap cloneBitmap = image.Clone(rectangle, image.PixelFormat);
            return cloneBitmap;
        }

        public bool IsConditionMet()
        {
            var currentImage = GetConditionImage();
            var isSame = Utils.AreEqual(currentImage, ConditionImage);
            return isSame;
        }

        private static void FilterOutRedBlobs(Bitmap image)
        {
            image.Save(@"C:\RBP\original.png", ImageFormat.Png);
            YCbCrFiltering filter = new YCbCrFiltering();
            // set color ranges to keep
            filter.Cb = new Range(-0.2f, -0.05f);
            filter.Cr = new Range(0.2f, 0.5f);

            filter.ApplyInPlace(image);
            image.Save(@"C:\RBP\redresult.png", ImageFormat.Png);
        }

        private static void FilterOutGreenBlobs(Bitmap image)
        {
            YCbCrFiltering filter = new YCbCrFiltering();
            // set color ranges to keep
            filter.Cb = new Range(-0.7f, -0.1f);
            filter.Cr = new Range(-0.7f, -0.1f);

            filter.ApplyInPlace(image);
        }

        private Models.Point GetBiggestBlobRandomMedianFromCenterToEdge(Bitmap image, BlobCounter blobCounter, Blob[] blobs)
        {
            var corners = new List<IntPoint>();
            SimpleShapeChecker shapeChecker = new SimpleShapeChecker();
            var biggestBlob = blobs.OrderByDescending(x => x.Area).FirstOrDefault();
            if (biggestBlob != null)
            {
                return GetPointFromEdgeToCenter(blobCounter, biggestBlob);
            }
            return new Models.Point(0, 0);
        }

        private Models.Point GetPointFromEdgeToCenter(BlobCounter blobCounter, Blob blob, bool prettyAccurate = false)
        {
            var edges = blobCounter.GetBlobsEdgePoints(blob);
            var randomEdgeNumber = Random.Next(edges.Count);
            var randomEdge = edges[randomEdgeNumber];

            var point = blob.CenterOfGravity;
            var randomValue = Random.Next(15, 85);

            float randomPercent = randomValue / 100f;
            var xDelta = (point.X - randomEdge.X) * randomPercent;
            var yDelta = (point.Y - randomEdge.Y) * randomPercent;
            if (prettyAccurate)
            {
                xDelta = xDelta / 2;
                yDelta = yDelta / 2;
            }
            var resultX = point.X - xDelta;
            var resultY = point.Y - yDelta;

            return new Models.Point((int)resultX, (int)resultY);
        }
        private static double GetDistance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt(Math.Pow((x2 - x1), 2) + Math.Pow((y2 - y1), 2));
        }
    }
}