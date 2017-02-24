using CommandLine;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using PyImageSearchSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace DocumentScanner_EmguCV
{
    /// <summary>
    /// Based on the work of Adrian Rosebrock
    /// http://www.pyimagesearch.com/2014/09/01/build-kick-ass-mobile-document-scanner-just-5-minutes/
    /// </summary>
    class Program
    {
        public static void Run(Options options)
        {
            //load the image and compute the ratio of the old height
            //to the new height, clone it, and resize it
            using (var disposer = new Disposer())
            {
                var image = new Image<Bgr, byte>(options.Image);
                disposer.Add(image);
                Image<Bgr, byte> orig = image.Clone();
                disposer.Add(orig);
                double ratio = image.Height / 500.0;
                image = ImageUtil.Resize(image, height: 500);
                disposer.Add(image);

                Image<Gray, byte> gray = image.Convert<Gray, byte>();
                disposer.Add(gray);

                gray = gray.SmoothGaussian(5);
                disposer.Add(gray);

                Image<Gray, byte> edged = gray.Canny(75, 200);
                disposer.Add(edged);

                Console.WriteLine("STEP 1: Edge Detection");

                CvInvoke.Imshow("Image", image);
                CvInvoke.Imshow("Edged", edged);
                CvInvoke.WaitKey();
                CvInvoke.DestroyAllWindows();

                //find the contours in the edged image, keeping only the
                //largest ones, and initialize the screen contour
                VectorOfVectorOfPoint cnts = new VectorOfVectorOfPoint();
                disposer.Add(cnts);

                using (Image<Gray, byte> edgedClone = edged.Clone())
                {
                    CvInvoke.FindContours(edgedClone, cnts, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);
                }


                Point[] screenCnt = null;
                foreach (VectorOfPoint c in
                    Enumerable.Range(0, cnts.Size).Select(i => cnts[i]).OrderByDescending(c => CvInvoke.ContourArea(c)).Take(5))
                {
                    //approximate the contour
                    double peri = CvInvoke.ArcLength(c, true);
                    using (VectorOfPoint approx = new VectorOfPoint())
                    {
                        CvInvoke.ApproxPolyDP(c, approx, 0.02 * peri, true);
                        if (approx.Size == 4)
                        {
                            screenCnt = approx.ToArray();
                            break;
                        }
                    }
                }
                if (screenCnt == null)
                {
                    Console.WriteLine("Failed to find polygon with four points");
                    return;
                }

                //show the contour (outline) of the piece of paper
                Console.WriteLine("STEP 2: Find contours of paper");
                image.Draw(screenCnt, new Bgr(0, 255, 0), 2);
                CvInvoke.Imshow("Outline", image);
                CvInvoke.WaitKey();
                CvInvoke.DestroyAllWindows();

                //apply the four point transform to obtain a top-down
                //view of the original image
                Image<Bgr, byte> warped = FourPointTransform(orig, screenCnt.Select(pt => new PointF((int)(pt.X * ratio), (int)(pt.Y * ratio))));
                disposer.Add(warped);

                //convert the warped image to grayscale, then threshold it
                //to give it that 'black and white' paper effect
                Image<Gray, byte> warpedGray = warped.Convert<Gray, byte>();
                disposer.Add(warpedGray);

                warpedGray = warpedGray.ThresholdAdaptive(new Gray(251), AdaptiveThresholdType.GaussianC, ThresholdType.Binary, 251, new Gray(10));
                disposer.Add(warpedGray);

                Console.WriteLine("STEP 3: Apply perspective transform");
                Image<Bgr, byte> origResized = ImageUtil.Resize(orig, height: 650);
                disposer.Add(origResized);
                CvInvoke.Imshow("Original", origResized);
                Image<Gray, byte> warpedResized = ImageUtil.Resize(warpedGray, height: 650);
                disposer.Add(warpedResized);
                CvInvoke.Imshow("Scanned", warpedResized);
                CvInvoke.WaitKey();
                CvInvoke.DestroyAllWindows();
            }
        }

        private static Image<TColor, TDepth> FourPointTransform<TColor, TDepth>(Image<TColor, TDepth> image, IEnumerable<PointF> pts)
            where TDepth : new() where TColor : struct, IColor
        {
            //obtain a consistent order of the points and unpack them
            //individually
            Tuple<PointF, PointF, PointF, PointF> orderedPoints = OrderPoints(pts.ToArray());
            PointF tl = orderedPoints.Item1, tr = orderedPoints.Item2, br = orderedPoints.Item3, bl = orderedPoints.Item4;

            //compute the width of the new image, which will be the
            //maximum distance between bottom-right and bottom-left
            //x-coordiates or the top-right and top-left x-coordinates
            double widthA = Distance(bl, br);
            double widthB = Distance(tl, tr);
            int maxWidth = Math.Max((int)widthA, (int)widthB);


            //compute the height of the new image, which will be the
            //maximum distance between the top-right and bottom-right
            //y-coordinates or the top-left and bottom-left y-coordinates
            double heightA = Distance(tr, br);
            double heightB = Distance(tl, bl);
            int maxHeight = Math.Max((int)heightA, (int)heightB);

            //now that we have the dimensions of the new image, construct
            //the set of destination points to obtain a "birds eye view",
            //(i.e. top-down view) of the image, again specifying points
            //in the top-left, top-right, bottom-right, and bottom-left
            //order
            var dst = new[]
            {
                new PointF(0,0),
                new PointF(maxWidth - 1, 0),
                new PointF(maxWidth - 1, maxHeight - 1),
                new PointF(0, maxHeight - 1),
            };

            //compute the perspective transform matrix and then apply it
            using (Mat m = CvInvoke.GetPerspectiveTransform(new[] { tl, tr, br, bl }, dst))
            {
                var warped = new Image<TColor, TDepth>(new Size(maxWidth, maxHeight));
                CvInvoke.WarpPerspective(image, warped, m, new Size(maxWidth, maxHeight));
                return warped;
            }
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
        private static Tuple<PointF, PointF, PointF, PointF> OrderPoints(IList<PointF> pts)
        {
            //TODO: This is begging for C#7 tuples
            //TODO: Error handling

            //Extract points

            PointF p1 = pts[0];
            PointF p2 = pts[1];
            PointF p3 = pts[2];
            PointF p4 = pts[3];

            PointF[] points =
            {
                new PointF(p1.X, p1.Y),
                new PointF(p2.X, p2.Y),
                new PointF(p3.X, p3.Y),
                new PointF(p4.X, p4.Y)
            };

            //sort the points based on their x-coordinates
            PointF[] xSorted = points.OrderBy(pt => pt.X).ToArray();

            //grab the left-most and right-most points from the sorted
            //x-roodinate points
            PointF[] leftMost = xSorted.Take(2).ToArray();
            PointF[] rightMost = xSorted.Skip(2).ToArray();

            //now, sort the left-most coordinates according to their
            //y-coordinates so we can grab the top-left and bottom-left
            //points, respectively
            PointF tl = leftMost.OrderBy(pt => pt.Y).First();
            PointF bl = xSorted.OrderBy(pt => pt.Y).Last();

            //now that we have the top-left coordinate, use it as an
            //anchor to calculate the Euclidean distance between the
            //top-left and right-most points; by the Pythagorean
            //theorem, the point with the largest distance will be
            //our bottom-right point

            PointF[] d = rightMost.OrderBy(pt => Distance(tl, pt)).ToArray();
            PointF tr = d.First();
            PointF br = d.Last();

            //return the coordinates in top-left, top-right,
            //bottom-right, and bottom-left order
            return Tuple.Create(tl, tr, br, bl);
        }

        private static double Distance(PointF p1, PointF p2)
        {
            return Math.Sqrt(Math.Pow(p2.X - p1.X, 2.0) + Math.Pow(p2.Y - p1.Y, 2.0));
        }
    }

    public class Options
    {
        [Option('i', "image", HelpText = "Path to the image to be scanned", DefaultValue = "<Embeded|receipt.jpg>")]
        public string Image { get; set; }
    }
}
