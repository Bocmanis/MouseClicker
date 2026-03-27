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

        public Models.Point GetColouredBoxPoint(ActionType actionType, Models.Point overrideCenter = null)
        {
            // locating objects
            var centerPoint = FindBlobs(actionType, overrideCenter);
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

        public Models.Point FindBlobs(ActionType actionType, Models.Point overrideCenter = null)
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
                    return GetClosestToCenterBlob(image, blobCounter, blobs, overrideCenter);
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

        private BlobCounter GetTinyBlobCounter()
        {
            BlobCounter blobCounter = new BlobCounter();
            blobCounter.FilterBlobs = true;
            blobCounter.MinHeight = 1;
            blobCounter.MinWidth = 1;
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

        private void FilterOutRedBlobs(Bitmap image)
        {
            ColorFiltering filter = new ColorFiltering();
            // set color ranges to keep
            filter.Red = new IntRange(Settings.RedFilterRedMin, Settings.RedFilterRedMax);
            filter.Green = new IntRange(Settings.RedFilterGreenMin, Settings.RedFilterGreenMax);
            filter.Blue = new IntRange(Settings.RedFilterBlueMin, Settings.RedFilterBlueMax);

            filter.ApplyInPlace(image);
        }

        private void FilterOutGreenBlobs(Bitmap image)
        {
            ColorFiltering filter = new ColorFiltering();
            // set color ranges to keep
            filter.Red = new IntRange(Settings.GreenFilterRedMin, Settings.GreenFilterRedMax);
            filter.Green = new IntRange(Settings.GreenFilterGreenMin, Settings.GreenFilterGreenMax);
            filter.Blue = new IntRange(Settings.GreenFilterBlueMin, Settings.GreenFilterBlueMax);

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
        private Models.Point GetClosestToCenterBlob(Bitmap image, BlobCounter blobCounter, Blob[] blobs, Models.Point overrideCenter = null)
        {
            Blob closestBlob = GetClosestToCenterBlobBlob(blobs, overrideCenter);
            if (closestBlob != null)
            {
                return GetPointFromEdgeToCenter(blobCounter, closestBlob);
            }
            return new Models.Point(0, 0);
        }

        private Blob GetClosestToCenterBlobBlob(Blob[] blobs, Models.Point overrideCenter = null)
        {
            var center = overrideCenter != null && overrideCenter.X != 0
                ? overrideCenter
                : Settings.ScreenCenter;
            var centerPoint = new AForge.Point(center.X, center.Y);
            var closestBlob = blobs.OrderBy(x => x.CenterOfGravity.DistanceTo(centerPoint)).FirstOrDefault();
            return closestBlob;
        }

        private Models.Point GetPointFromEdgeToCenter(BlobCounter blobCounter, Blob blob, bool prettyAccurate = true)
        {
            var center = blob.CenterOfGravity;
            var edges = blobCounter.GetBlobsEdgePoints(blob);

            // Try up to 10 times to find a point near center that is inside the blob
            for (int attempt = 0; attempt < 10; attempt++)
            {
                // Pick a random edge point and move a small random amount from center toward it
                var randomEdge = edges[Random.Next(edges.Count)];

                // Stay within 30% of center-to-edge distance (close to center of mass)
                float maxPercent = prettyAccurate ? 0.20f : 0.30f;
                float percent = (float)(Random.NextDouble() * maxPercent);

                var candidateX = center.X + (randomEdge.X - center.X) * percent;
                var candidateY = center.Y + (randomEdge.Y - center.Y) * percent;

                // Verify the point is inside the blob by checking it's closer to center
                // than the nearest edge in that direction
                if (IsPointInsideBlob(candidateX, candidateY, center, edges))
                {
                    return new Models.Point((int)candidateX, (int)candidateY);
                }
            }

            // Fallback to center of mass
            return new Models.Point((int)center.X, (int)center.Y);
        }

        private bool IsPointInsideBlob(float px, float py, AForge.Point center, List<AForge.IntPoint> edges)
        {
            // Find the edge point closest to the candidate's direction from center
            float dirX = px - center.X;
            float dirY = py - center.Y;
            float distFromCenter = (float)Math.Sqrt(dirX * dirX + dirY * dirY);

            if (distFromCenter < 1f)
                return true; // essentially at center, always inside

            // Normalize direction
            dirX /= distFromCenter;
            dirY /= distFromCenter;

            // Find the edge point most aligned with this direction
            float minEdgeDist = float.MaxValue;
            foreach (var edge in edges)
            {
                float edgeDirX = edge.X - center.X;
                float edgeDirY = edge.Y - center.Y;
                float edgeDist = (float)Math.Sqrt(edgeDirX * edgeDirX + edgeDirY * edgeDirY);
                if (edgeDist < 1f) continue;

                // Dot product to check alignment
                float dot = (edgeDirX / edgeDist) * dirX + (edgeDirY / edgeDist) * dirY;
                if (dot > 0.9f) // roughly same direction
                {
                    if (edgeDist < minEdgeDist)
                        minEdgeDist = edgeDist;
                }
            }

            return distFromCenter < minEdgeDist * 0.85f;
        }

        public bool HasColorInRegion(Models.Point topLeft, Models.Point bottomRight, bool searchGreen)
        {
            var rectangle = GetConditionRectangle(topLeft, bottomRight);
            Bitmap image = GetScreenshot(rectangle);

            if (searchGreen)
                FilterOutGreenBlobs(image);
            else
                FilterOutRedBlobs(image);

            BlobCounter blobCounter = GetTinyBlobCounter();
            blobCounter.ProcessImage(image);
            Blob[] blobs = blobCounter.GetObjectsInformation();
             
            return blobs.Length > 0;
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


        public Rectangle? GetBiggestBlobRectangle(ActionType actionType)
        {
            Bitmap image = GetScreenshot("preview");

            if (actionType == ActionType.ClickRedBox)
                FilterOutRedBlobs(image);
            else
                FilterOutGreenBlobs(image);

            BlobCounter blobCounter = GetBlobCounter();
            blobCounter.ProcessImage(image);
            Blob[] blobs = blobCounter.GetObjectsInformation();

            var biggestBlob = blobs.OrderByDescending(x => x.Area).FirstOrDefault();
            if (biggestBlob == null)
                return null;

            return biggestBlob.Rectangle;
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