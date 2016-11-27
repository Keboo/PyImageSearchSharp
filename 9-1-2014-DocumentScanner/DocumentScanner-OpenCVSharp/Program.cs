using CommandLine;
using OpenCvSharp;
using System;
using System.Linq;

namespace DocumentScanner_OpenCVSharp
{

    /// <summary>
    /// Based on the work of Adrian Rosebrock
    /// http://www.pyimagesearch.com/2014/09/01/build-kick-ass-mobile-document-scanner-just-5-minutes/
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            var options = new Options();
            if (!Parser.Default.ParseArguments(args, options))
            {
                return;
            }
            //TODO: Disposing

            //load the image and compute the ratio of the old height
            //to the new height, clone it, and resize it
            Mat image = new Mat(options.Image);
            Mat orig = image.Clone();
            double ratio = image.Height / 500.0;
            image = Resize(image, 500);

            Mat gray = image.CvtColor(ColorConversionCodes.BGR2GRAY);

            gray = gray.GaussianBlur(new Size(5, 5), 0);
            Mat edged = gray.Canny(75, 200);
            Console.WriteLine("STEP 1: Edge Detection");
            Cv2.ImShow("Image", image);
            Cv2.ImShow("Edged", edged);
            Cv2.WaitKey();
            Cv2.DestroyAllWindows();

            //find the contours in the edged image, keeping only the
            //largest ones, and initialize the screen contour
            Mat[] cnts;
            edged.Clone().FindContours(out cnts, new Mat(), RetrievalModes.List, ContourApproximationModes.ApproxSimple);

            Mat screenCnt = null;
            //loop over the contours
            foreach (Mat c in cnts.OrderByDescending(c => c.ContourArea()).Take(5))
            {
                //approximate the contour
                double peri = c.ArcLength(true);
                Mat approx = c.ApproxPolyDP(0.02 * peri, true);
                //if our approximated contour has four points, then we
                //can assume that we have found our screen
                if (approx.Rows == 4)
                {
                    screenCnt = approx;
                    break;
                }
            }

            //show the contour (outline) of the piece of paper
            Console.WriteLine("STEP 2: Find contours of paper");
            Cv2.DrawContours(image, new[] { screenCnt }, -1, Scalar.FromRgb(0, 255, 0), 2);
            Cv2.ImShow("Outline", image);
            Cv2.WaitKey();
            Cv2.DestroyAllWindows();

            //apply the four point transform to obtain a top-down
            //view of the original image
            Mat warped = FourPointTransform(orig, screenCnt * ratio);

            //convert the warped image to grayscale, then threshold it
            //to give it that 'black and white' paper effect
            warped = warped.CvtColor(ColorConversionCodes.BGR2GRAY);
            //warped = threshold_adaptive(warped, 251, offset = 10)
            Cv2.AdaptiveThreshold(warped, warped, 251, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 251, 10);
            //Cv2.Threshold(warped, warped, 251, 255, ThresholdTypes.Binary);

            Console.WriteLine("STEP 3: Apply perspective transform");
            Cv2.ImShow("Original", Resize(orig, 650));
            Cv2.ImShow("Scanned", Resize(warped, 650));
            Cv2.WaitKey();
            Cv2.DestroyAllWindows();

        }

        private static Mat FourPointTransform(Mat image, Mat pts)
        {
            //obtain a consistent order of the points and unpack them
            //individually
            Tuple<Point2f, Point2f, Point2f, Point2f> orderedPoints = OrderPoints(pts);
            Point2f tl = orderedPoints.Item1, tr = orderedPoints.Item2, br = orderedPoints.Item3, bl = orderedPoints.Item4;

            //compute the width of the new image, which will be the
            //maximum distance between bottom-right and bottom-left
            //x-coordiates or the top-right and top-left x-coordinates
            double widthA = Point2f.Distance(bl, br);
            double widthB = Point2f.Distance(tl, tr);
            int maxWidth = Math.Max((int)widthA, (int)widthB);


            //compute the height of the new image, which will be the
            //maximum distance between the top-right and bottom-right
            //y-coordinates or the top-left and bottom-left y-coordinates
            double heightA = Point2f.Distance(tr, br);
            double heightB = Point2f.Distance(tl, bl);
            int maxHeight = Math.Max((int)heightA, (int)heightB);

            //now that we have the dimensions of the new image, construct
            //the set of destination points to obtain a "birds eye view",
            //(i.e. top-down view) of the image, again specifying points
            //in the top-left, top-right, bottom-right, and bottom-left
            //order
            var dst = new[]
            {
                new Point2f(0,0),
                new Point2f(maxWidth - 1, 0),
                new Point2f(maxWidth - 1, maxHeight - 1),
                new Point2f(0, maxHeight - 1),
            };

            //compute the perspective transform matrix and then apply it
            Mat m = Cv2.GetPerspectiveTransform(new[] { tl,tr,br,bl }, dst);
            Mat warped = image.WarpPerspective(m, new Size(maxWidth, maxHeight));

            return warped;
        }

        /// <summary>
        /// Returns points in the order
        /// top left
        /// top right
        /// bottom right
        /// bottom left
        /// Updated to fixed version of the order points method
        /// http://www.pyimagesearch.com/2016/03/21/ordering-coordinates-clockwise-with-python-and-opencv/
        /// </summary>
        /// <param name="pts"></param>
        /// <returns></returns>
        private static Tuple<Point2f, Point2f, Point2f, Point2f> OrderPoints(Mat pts)
        {
            //TODO: This is begging for C#7 tuples
            //TODO: Error handling

            //Extract points
            var p1 = pts.Get<Point>(0);
            var p2 = pts.Get<Point>(1);
            var p3 = pts.Get<Point>(2);
            var p4 = pts.Get<Point>(3);

            Point2f[] points =
            {
                new Point2f(p1.X, p1.Y), 
                new Point2f(p2.X, p2.Y), 
                new Point2f(p3.X, p3.Y), 
                new Point2f(p4.X, p4.Y)
            };

            //sort the points based on their x-coordinates
            Point2f[] xSorted = points.OrderBy(pt => pt.X).ToArray();

            //grab the left-most and right-most points from the sorted
            //x-roodinate points
            Point2f[] leftMost = xSorted.Take(2).ToArray();
            Point2f[] rightMost = xSorted.Skip(2).ToArray();

            //now, sort the left-most coordinates according to their
            //y-coordinates so we can grab the top-left and bottom-left
            //points, respectively
            Point2f tl = leftMost.OrderBy(pt => pt.Y).First();
            Point2f bl = xSorted.OrderBy(pt => pt.Y).Last();

            //now that we have the top-left coordinate, use it as an
            //anchor to calculate the Euclidean distance between the
            //top-left and right-most points; by the Pythagorean
            //theorem, the point with the largest distance will be
            //our bottom-right point
            Point2f[] d = rightMost.OrderBy(pt => tl.DistanceTo(pt)).ToArray();
            Point2f tr = d.First();
            Point2f br = d.Last();

            //return the coordinates in top-left, top-right,
            //bottom-right, and bottom-left order
            return Tuple.Create(tl, tr, br, bl);
        }

        public static Mat Resize(Mat mat, double height)
        {
            double ratio = mat.Height / height;
            return mat.Resize(new Size(mat.Width / ratio, 500.0));
        }
    }

    public class Options
    {
        [Option('i', "image", Required = true, HelpText = "Path to the image to be scanned")]
        public string Image { get; set; }
    }
}
