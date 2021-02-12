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

namespace BetterClicker.Logic
{
    public class ImageProcessingLogic
    {
        public Random Random { get; private set; }

        public ImageProcessingLogic()
        {
            this.Random = new Random(DateTime.Now.Millisecond);
        }

        public Models.Point Test()
        {
            // Open your image

            Bitmap image = GetScreenshot();
            // locating objects
            // create filter
            YCbCrFiltering filter = new YCbCrFiltering();
            // set color ranges to keep
            filter.Cb = new Range(-0.7f, -0.1f);
            filter.Cr = new Range(-0.7f, -0.1f);
            // apply the filter
            //image.Save(@"C:\RBP\beforeFilter.png");
            filter.ApplyInPlace(image);
            var centerPoint = FindBlobs(image);
            //image.Save(@"C:\RBP\result.png");
            return centerPoint;
        }

        private Bitmap GetScreenshot()
        {
            double screenLeft = SystemParameters.VirtualScreenLeft;
            double screenTop = SystemParameters.VirtualScreenTop;
            double screenWidth = SystemParameters.VirtualScreenWidth;
            double screenHeight = SystemParameters.VirtualScreenHeight;

            Bitmap bmp = new Bitmap((int)screenWidth, (int)screenHeight);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen((int)screenLeft, (int)screenTop, 0, 0, bmp.Size);
                return bmp;
            }
        }

        public Models.Point FindBlobs(Bitmap image)
        {
            BlobCounter blobCounter = new BlobCounter();
            blobCounter.FilterBlobs = true;
            blobCounter.MinHeight = 15;
            blobCounter.MinWidth = 15;
            blobCounter.ProcessImage(image);
            Blob[] blobs = blobCounter.GetObjectsInformation();
            // check for rectangles
            var corners = new List<IntPoint>();
            SimpleShapeChecker shapeChecker = new SimpleShapeChecker();
            var biggestBlob = blobs.OrderByDescending(x => x.Area).FirstOrDefault();
            if (biggestBlob != null)
            {
                var edges = blobCounter.GetBlobsEdgePoints(biggestBlob);
                var randomEdgeNumber = Random.Next(edges.Count);
                var randomEdge = edges[randomEdgeNumber];

                var rectangle = biggestBlob.Rectangle;

                List<System.Drawing.Point> Points = new List<System.Drawing.Point>();

                Graphics g = Graphics.FromImage(image);
                g.DrawRectangle(new Pen(Color.Red, 5.0f), rectangle);
                var point = biggestBlob.CenterOfGravity;
                var randomValue = Random.Next(5, 95);
                float randomPercent = randomValue / 100f;
                var xDelta = (point.X - randomEdge.X) * randomPercent;
                var yDelta = (point.Y - randomEdge.Y) * randomPercent;
                var resultX = point.X - xDelta;
                var resultY = point.Y - yDelta;

                return new Models.Point((int)resultX, (int)resultY);
            }
            return new Models.Point(0, 0);
        }
    }
}