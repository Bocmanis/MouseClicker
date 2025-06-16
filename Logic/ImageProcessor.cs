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
using System.IO;

namespace BetterClicker.Logic
{
    public class ImageProcessingLogic
    {
        private bool Retried;
        private int previousSize;

        public Random Random { get; private set; }
        public SettingsModel Settings
        {
            get
            {
                return MainWindow.AppModel.Settings;
            }
        }

        public Dictionary<string, Bitmap> ConditionImages { get; private set; }

        public ImageProcessingLogic()
        {
            this.Random = new Random(DateTime.Now.Millisecond);
        }

        public Models.Point GetColouredBoxPoint(ActionType actionType)
        {
            // locating objects
            var centerPoint = FindBlobs(actionType);
            return centerPoint;
        }

        private Bitmap GetScreenshot(string name = "Base")
        {
            double screenLeft = SystemParameters.VirtualScreenLeft;

            double screenTop = SystemParameters.VirtualScreenTop;
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;

            Bitmap bmp = new Bitmap((int)screenWidth, (int)screenHeight);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen((int)screenLeft, (int)screenTop, 0, 0, bmp.Size);
                SaveImage(bmp, $"{name}UnProcessed.png");
                return bmp;
            }
        }
        private Bitmap GetScreenshot(Rectangle field)
        {
            Bitmap bmp = new Bitmap(field.Width, field.Height, PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(field.Left, field.Top, 0, 0, field.Size, CopyPixelOperation.SourceCopy);
                return bmp;
            }
        }

        public Models.Point FindBlobs(ActionType actionType)
        {
            switch (actionType)
            {
                case ActionType.ClickRedBox:
                    return GetRedBiggestBlob();
                case ActionType.ClickGreenBox:
                    return GetGreenBiggestBlob();
                case ActionType.QuickGreenBox:
                    return GetGreenQuickBiggest();
                default:
                    break;
            }
            Bitmap image = GetScreenshot("NormalGreen");
            FilterOutGreenBlobs(image);

            BlobCounter blobCounter = GetBlobCounter();
            blobCounter.ProcessImage(image);
            Blob[] blobs = blobCounter.GetObjectsInformation();

            Graphics g2 = Graphics.FromImage(image);
            foreach (var blob in blobs)
            {
                g2.DrawRectangle(new Pen(Color.White, 3.0f), blob.Rectangle);
            }
            SaveImage(image, "greenResultMarked.png");

            if (blobs.Length == 0)
            {
                var newImage = GetScreenshot("FailedGreenRed");
                FilterOutRedBlobs(newImage);
                blobCounter.ProcessImage(newImage);
                Blob[] redBlobs = blobCounter.GetObjectsInformation();
                Graphics g = Graphics.FromImage(newImage);
                foreach (var redBlob in redBlobs)
                {
                    g.DrawRectangle(new Pen(Color.Red, 3.0f), redBlob.Rectangle);
                }
                SaveImage(newImage, "RedResultMarked.png");
                if (Settings.AgilityMode)
                {
                    if (redBlobs.Length == 2)
                    {
                        var rectanlge = GetConditionRectangle(Settings.ConditionLeftTop, Settings.ConditionRightBottom);
                        var conditionArea = rectanlge.Width * rectanlge.Height;
                        var smallBlob = redBlobs.Where(x => x.Area < conditionArea).OrderByDescending(x => x.Area).FirstOrDefault();
                        if (smallBlob == null)
                        {
                            smallBlob = redBlobs.FirstOrDefault();
                        }
                        var point = GetPointFromEdgeToCenter(blobCounter, smallBlob, true);
                        MouseActions.DoLeftClick(point);
                        Thread.Sleep(4000);
                        return GetColouredBoxPoint(actionType);
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
                    return GetColouredBoxPointRetry(actionType);
                }
            }
            // check for rectangles
            switch (actionType)
            {
                case ActionType.ClickBiggestColBox:
                    return GetBiggestBlobRandomMedianFromCenterToEdge(image, blobCounter, blobs);
                case ActionType.ClickNearestToCenterColBox:
                    return GetClosestToCenterBlob(image, blobCounter, blobs);
                default:
                    return GetBiggestBlobRandomMedianFromCenterToEdge(image, blobCounter, blobs);
            }
        }

        public Models.Point GetRedBiggestBlob()
        {
            Bitmap image = GetScreenshot("red");
            FilterOutRedBlobs(image);

            BlobCounter blobCounter = GetBlobCounter();
            blobCounter.ProcessImage(image);
            Blob[] blobs = blobCounter.GetObjectsInformation();

            Graphics g2 = Graphics.FromImage(image);
            foreach (var blob in blobs)
            {
                g2.DrawRectangle(new Pen(Color.White, 3.0f), blob.Rectangle);
            }
            SaveImage(image, "redResultMarked.png");

            return GetBiggestBlobRandomMedianFromCenterToEdge(image, blobCounter, blobs);
        }

        public Models.Point GetGreenBiggestBlob()
        {
            Bitmap image = GetScreenshot("green");
            FilterOutGreenBlobs(image);

            BlobCounter blobCounter = GetBlobCounter();
            blobCounter.ProcessImage(image);
            Blob[] blobs = blobCounter.GetObjectsInformation();

            Graphics g2 = Graphics.FromImage(image);
            foreach (var blob in blobs)
            {
                g2.DrawRectangle(new Pen(Color.White, 3.0f), blob.Rectangle);
            }
            SaveImage(image, "greenResultMarked.png");

            return GetBiggestBlobRandomMedianFromCenterToEdge(image, blobCounter, blobs);
        }

        public Models.Point GetGreenQuickBiggest()
        {
            Bitmap image = GetScreenshot("greenQuick");
            FilterOutGreenBlobs(image);

            BlobCounter blobCounter = GetBlobCounter();
            blobCounter.ProcessImage(image);
            Blob[] blobs = blobCounter.GetObjectsInformation();

            var biggestBlob = blobs.OrderByDescending(x => x.Area).FirstOrDefault();
            if (biggestBlob == null)
            {
                return new Models.Point(0, 0);
            }
            var center = biggestBlob.CenterOfGravity;
            return new Models.Point((int)center.X, (int)center.Y);
        }

        private BlobCounter GetBlobCounter()
        {
            BlobCounter blobCounter = new BlobCounter();
            blobCounter.FilterBlobs = true;
            blobCounter.MinHeight = Settings.MinBlobSize ?? 40;
            blobCounter.MinWidth = Settings.MinBlobSize ?? 40;
            return blobCounter;
        }

        private Models.Point GetColouredBoxPointRetry(ActionType actionType)
        {
            if (this.Retried)
            {
                this.Retried = false;
                return new Models.Point(0, 0);
            }
            this.Retried = true;
            return GetColouredBoxPoint(actionType);
        }

        public void SetCondition()
        {
            this.ConditionImages = new Dictionary<string, Bitmap>();
            Bitmap conditionImage = GetConditionImage(Settings.ConditionLeftTop, Settings.ConditionRightBottom);
            var key = MakeConditionsString(Settings.ConditionLeftTop, Settings.ConditionRightBottom);
            SaveImage(conditionImage, $"{key}ConditionStart.png");
            this.ConditionImages.Add(key, conditionImage);
        }

        public void AddCondition(BetterClicker.Models.Point leftTop, BetterClicker.Models.Point rightBottom)
        {
            this.ConditionImages = new Dictionary<string, Bitmap>();
            Bitmap conditionImage = GetConditionImage(leftTop, rightBottom);
            var key = MakeConditionsString(leftTop, rightBottom);
            SaveImage(conditionImage, $"{key}ActionCondition.png");
            this.ConditionImages.Add(key, conditionImage);
        }

        private string MakeConditionsString(Models.Point topLeft, Models.Point bottomRight)
        {
            return $"{topLeft.X}-{topLeft.Y}_{bottomRight.X}-{bottomRight.Y}";
        }

        public Bitmap GetConditionImage(Models.Point topLeft, Models.Point bottomRight)
        {
            Rectangle rectangle = GetConditionRectangle(topLeft, bottomRight);

            var image = GetScreenshot(rectangle);
            var conditionKey = MakeConditionsString(topLeft, bottomRight);
            SaveImage(image, $"{conditionKey}_current.png");

            return image;
        }

        private Rectangle GetConditionRectangle(Models.Point topLeft, Models.Point bottomRight)
        {
            var width = bottomRight.X - topLeft.X;
            var height = bottomRight.Y - topLeft.Y;
            var rectangle = new Rectangle(topLeft.X, topLeft.Y, width, height);
            return rectangle;
        }

        public bool IsConditionMet(Models.Point topLeft, Models.Point bottomRight)
        {
            var conditionKey = MakeConditionsString(topLeft, bottomRight);
            if (!ConditionImages.TryGetValue(conditionKey, out Bitmap baseImage))
            {
                var image = GetConditionImage(topLeft, bottomRight);
                ConditionImages.Add(conditionKey, image);
                return false;
            }

            var currentImage = GetConditionImage(topLeft, bottomRight);
            var isSame = Utils.AreEqual(currentImage, baseImage);
            return isSame;
        }

        private static void FilterOutRedBlobs(Bitmap image)
        {
            ColorFiltering filter = new ColorFiltering();
            // set color ranges to keep
            filter.Red = new IntRange(140, 255);
            filter.Green = new IntRange(0, 60);
            filter.Blue = new IntRange(140, 255);

            filter.ApplyInPlace(image);
        }

        private static void FilterOutGreenBlobs(Bitmap image)
        {
            YCbCrFiltering filter = new YCbCrFiltering();
            // set color ranges to keep
            filter.Cb = new Range(-0.7f, -0.2f);
            filter.Cr = new Range(-0.7f, -0.2f);


            filter.ApplyInPlace(image);
        }


        private static void SaveImage(Bitmap image, string fileName)
        {
            var directory = System.AppDomain.CurrentDomain.BaseDirectory;
            var path = Path.Combine(directory, "screenshots", fileName);

            image.Save(path);
        }

        private Models.Point GetBiggestBlobRandomMedianFromCenterToEdge(Bitmap image, BlobCounter blobCounter, Blob[] blobs)
        {
            var biggestBlob = blobs.OrderByDescending(x => x.Area).FirstOrDefault();
            if (biggestBlob != null)
            {
                return GetPointFromEdgeToCenter(blobCounter, biggestBlob);
            }
            return new Models.Point(0, 0);
        }
        private Models.Point GetClosestToCenterBlob(Bitmap image, BlobCounter blobCounter, Blob[] blobs)
        {
            Blob closestBlob = GetClosestToCenterBlobBlob(blobs);
            if (closestBlob != null)
            {
                return GetPointFromEdgeToCenter(blobCounter, closestBlob);
            }
            return new Models.Point(0, 0);
        }

        private Blob GetClosestToCenterBlobBlob(Blob[] blobs)
        {
            var centerPoint = new AForge.Point(Settings.ScreenCenter.X, Settings.ScreenCenter.Y);
            var closestBlob = blobs.OrderBy(x => x.CenterOfGravity.DistanceTo(centerPoint)).FirstOrDefault();
            return closestBlob;
        }

        private Models.Point GetPointFromEdgeToCenter(BlobCounter blobCounter, Blob blob, bool prettyAccurate = true)
        {
            var edges = blobCounter.GetBlobsEdgePoints(blob);
            var randomEdgeNumber = Random.Next(edges.Count);
            var randomEdge = edges[randomEdgeNumber];

            var point = blob.CenterOfGravity;
            var randomValue = Random.Next(1, 85);

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

        internal Models.Point SearchForGreen(int searchDelay, CancellationToken token)
        {
            var ignoreCounter = 0;
            for (int i = 0; i < 60; i++)
            {
                if (token.IsCancellationRequested)
                    return new Models.Point(0, 0);

                Thread.Sleep(searchDelay);
                var result = GetGreenClosestToCenterGreenBlob();
                if (result.Size == 0 || previousSize * 0.7 < result.Size && result.Size < previousSize*1.3)
                {
                    if (ignoreCounter < 5)
                    {
                        ignoreCounter++;
                        continue;
                    }
                }
                previousSize = result.Size;

                if (result.Point.X != 0)
                {
                    return result.Point;
                }
            }
            return new Models.Point(0, 0);
        }


        internal (int Size, Models.Point Point) GetGreenClosestToCenterGreenBlob()
        {
            Bitmap image = GetScreenshot("green");
            FilterOutGreenBlobs(image);

            BlobCounter blobCounter = GetBlobCounter();
            blobCounter.ProcessImage(image);
            Blob[] blobs = blobCounter.GetObjectsInformation();

            Graphics g2 = Graphics.FromImage(image);
            foreach (var blob in blobs)
            {
                g2.DrawRectangle(new Pen(Color.White, 3.0f), blob.Rectangle);
            }
            SaveImage(image, "greenResultMarked.png");

            var resultBlob = GetClosestToCenterBlobBlob(blobs);
            var resultPoint = new Models.Point(0, 0);
            if (resultBlob != null)
            {
                resultPoint = GetPointFromEdgeToCenter(blobCounter, resultBlob);
            }
            return ( Size: resultBlob?.Area ?? 0, Point: resultPoint );
        }
    }
}