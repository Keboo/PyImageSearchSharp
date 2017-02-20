using OpenCvSharp;
using System;
using CommandLine;
using CommandLine.Text;
using PyImageSearchSharp;

namespace ColorTransfer_OpenCVSharp
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = new Options();
            if (!Parser.Default.ParseArguments(args, options))
            {
                Console.WriteLine(HelpText.AutoBuild(options));
                Console.ReadLine();
                return;
            }

            //load the images
            using (Mat source = new Mat(options.Source))
            using (Mat target = new Mat(options.Target))
            {
                //transfer the color distribution from the source image
                //to the target image
                using (Mat transfer = ColorTransfer(source, target))
                {
                    //check to see if the output image should be saved
                    if (!string.IsNullOrWhiteSpace(options.Output))
                    {
                        transfer.SaveImage(options.Output);
                    }

                    //show the images and wait for a key press
                    ShowImage("Source", source);
                    ShowImage("Target", target);
                    ShowImage("Transfer", transfer);
                    Cv2.WaitKey();
                }
            }
        }

        private static void ShowImage(string title, Mat image, int width = 300)
        {
            //resize the image to have a constant width, just to
            //make displaying the images take up less screen real
            //estate
            using (var resized = ImageUtil.Resize(image, width))
            {
                //show the resized image
                Cv2.ImShow(title, resized);
            }
        }

        public static Mat ColorTransfer(Mat source, Mat target)
        {
            using (var disposer = new Disposer())
            {
                //convert the images from the RGB to L*ab* color space, being
                //sure to utilizing the floating point data type (note: OpenCV
                //expects floats to be 32-bit, so use that instead of 64-bit)
                source = source.CvtColor(ColorConversionCodes.BGR2Lab);
                disposer.Add(source);
                target = target.CvtColor(ColorConversionCodes.BGR2Lab);
                disposer.Add(target);

                //compute color statistics for the source and target images
                var sourceStats = ImageStats(source);
                double lMeanSrc = sourceStats.Item1,
                    lStdSrc = sourceStats.Item2,
                    aMeanSrc = sourceStats.Item3,
                    aStdSrc = sourceStats.Item4,
                    bMeanSrc = sourceStats.Item5,
                    bStdSrc = sourceStats.Item6;

                var targetStates = ImageStats(target);
                double lMeanTar = targetStates.Item1,
                    lStdTar = targetStates.Item2,
                    aMeanTar = targetStates.Item3,
                    aStdTar = targetStates.Item4,
                    bMeanTar = targetStates.Item5,
                    bStdTar = targetStates.Item6;

                //subtract the means from the target image
                Mat[] targetChannels = target.Split();
                disposer.Add(targetChannels);
                Mat l = targetChannels[0] - lMeanTar;
                Mat a = targetChannels[1] - aMeanTar;
                Mat b = targetChannels[2] - bMeanTar;

                //scale by the standard deviations
                l = lStdTar/lStdSrc*l;
                a = aStdTar/aStdSrc*a;
                b = bStdTar/bStdSrc*b;

                //add in the source mean
                l += lMeanSrc;
                a += aMeanSrc;
                b += bMeanSrc;

                //clip the pixel intensities to [0, 255] if they fall outside
                //this range
                //NB: in the original code the values of l, a, and b are cliped to ensure they fall within the expected range.
                //This is not needed here as it is handled by the color space conversion.

                //merge the channels together and convert back to the RGB color
                //space, being sure to utilize the 8-bit unsigned integer data
                //type
                Mat transfer = new Mat();
                disposer.Add(transfer);
                Cv2.Merge(new[] {l, a, b}, transfer);
                transfer = transfer.CvtColor(ColorConversionCodes.Lab2BGR);

                return transfer;
            }
        }

        //TODO: C#7 tuples
        private static Tuple<double, double, double, double, double, double> ImageStats(Mat image)
        {
            //compute the mean and standard deviation of each channel
            Mat[] channels = image.Split();
            Mat l = channels[0];
            Mat a = channels[1];
            Mat b = channels[2];
            Mat lMean = new Mat(), lStd = new Mat();
            Cv2.MeanStdDev(l, lMean, lStd);
            Mat aMean = new Mat(), aStd = new Mat();
            Cv2.MeanStdDev(a, aMean, aStd);
            Mat bMean = new Mat(), bStd = new Mat();
            Cv2.MeanStdDev(b, bMean, bStd);

            //return the color statistics
            return Tuple.Create(lMean.At<double>(0), lStd.At<double>(0), aMean.At<double>(0), aStd.At<double>(0), bMean.At<double>(0), bStd.At<double>(0));
        }
    }

    public class Options
    {
        [Option('s', "source", Required = true, HelpText = "Path to the source image")]
        public string Source { get; set; }

        [Option('t', "target", Required = true, HelpText = "Path to the target image")]
        public string Target { get; set; }

        [Option('o', "output", Required = false, HelpText = "Path to the output image (optional)")]
        public string Output { get; set; }
    }
}
