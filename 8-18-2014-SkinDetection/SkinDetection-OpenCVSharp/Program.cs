using CommandLine;
using OpenCvSharp;
using System;
using System.IO;
using CommandLine.Text;

namespace SkinDetection_OpenCVSharp
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = new Options();
            if (!Parser.Default.ParseArguments(args, options))
            {
                Console.WriteLine(HelpText.AutoBuild(options));
                return;
            }
            //In order to playback video opencv_ffmpeg*.dll must be found.
            string includePath = Environment.Is64BitProcess ? @".\dll\x64" : @".\dll\x86";
            foreach (string file in Directory.EnumerateFiles(includePath, "*.dll"))
            {
                File.Copy(file, Path.GetFileName(file), true);
            }

            //define the upper and lower boundaries of the HSV pixel
            //intensities to be considered 'skin'
            var lower = new Scalar(0, 48, 80);
            var upper = new Scalar(20, 255, 255);

            //if a video path was not supplied, grab the reference
            //to the gray
            //otherwise, load the video
            VideoCapture camera = string.IsNullOrEmpty(options.Video)
                ? VideoCapture.FromCamera(CaptureDevice.Any)
                : new VideoCapture(Path.GetFullPath(options.Video));
            if (!camera.IsOpened())
            {
                Console.WriteLine("Failed to initialize video");
                return;
            }
            using (camera)
            {
                //keep looping over the frames in the video
                while (true)
                {
                    //grab the current frame
                    using (var frame = new Mat())
                    {
                        bool grabbed = camera.Read(frame);
                        //if we are viewing a video and we did not grab a
                        //frame, then we have reached the end of the video
                        if (!grabbed || frame.Width == 0 || frame.Height == 0)
                        {
                            if (!string.IsNullOrEmpty(options.Video))
                            {
                                break;
                            }
                            continue;
                        }

                        //resize the frame, convert it to the HSV color space,
                        //and determine the HSV pixel intensities that fall into
                        //the speicifed upper and lower boundaries
                        Mat resizedFrame = Resize(frame, 400);
                        Mat converted = resizedFrame.CvtColor(ColorConversionCodes.BGR2HSV);
                        Mat skinMask = new Mat();
                        Cv2.InRange(converted, lower, upper, skinMask);

                        //apply a series of erosions and dilations to the mask
                        //using an elliptical kernel
                        Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(11, 11));
                        skinMask = skinMask.Erode(kernel, iterations: 2);
                        skinMask = skinMask.Dilate(kernel, iterations: 2);

                        //blur the mask to help remove noise, then apply the
                        //mask to the frame
                        skinMask = skinMask.GaussianBlur(new Size(3, 3), 0);
                        Mat skin = new Mat();
                        Cv2.BitwiseAnd(resizedFrame, resizedFrame, skin, skinMask);

                        //show the skin in the image along with the mask
                        Cv2.ImShow("images", resizedFrame);
                        Cv2.ImShow("mask", skin);

                        //if the 'q' key is pressed, stop the loop
                        if ((Cv2.WaitKey(1) & 0xff) == 'q')
                        {
                            break;
                        }
                    }
                }
            }

            Cv2.DestroyAllWindows();
        }

        private static Mat Resize(Mat mat, double width)
        {
            double ratio = mat.Width / width;
            return mat.Resize(new Size(width, mat.Height / ratio));
        }
    }

    public class Options
    {
        [Option('v', "video", Required = false, HelpText = "path to the (optional) video file")]
        public string Video { get; set; }
    }
}
