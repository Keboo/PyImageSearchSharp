using CommandLine;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using PyImageSearchSharp;
using System;
using System.Drawing;
using System.IO;

namespace SkinDetection_EmguCV
{
    class Program
    {
        public static void Run(Options options)
        {
            //In order to playback video opencv_ffmpeg*.dll must be found.
            string includePath = Environment.Is64BitProcess ? @".\x64" : @".\x86";
            foreach (string file in Directory.EnumerateFiles(includePath, "*.dll"))
            {
                File.Copy(file, Path.GetFileName(file), true);
            }

            //define the upper and lower boundaries of the HSV pixel
            //intensities to be considered 'skin'
            var lower = new Hsv(0, 48, 80);
            var upper = new Hsv(20, 255, 255);

            //if a video path was not supplied, grab the reference
            //to the gray
            //otherwise, load the video
            Capture capture = string.IsNullOrEmpty(options.Video)
                ? new Capture(CaptureType.Any)
                : new Capture(Path.GetFullPath(options.Video));
            using (capture)
            {
                //keep looping over the frames in the video
                while (true)
                {
                    //grab the current frame
                    //bool grabbed = capture.Grab();
                    //capture.Retrieve()
                    using (Image<Bgr, byte> frame = capture.QueryFrame()?.ToImage<Bgr, byte>())
                    {
                        bool grabbed = frame != null;
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
                        using (Image<Bgr, byte> resizedFrame = ImageUtil.Resize(frame, width:400))
                        using (Image<Hsv, byte> converted = resizedFrame.Convert<Hsv, byte>())
                        using (Image<Gray, byte> skinMask = converted.InRange(lower, upper))
                        {
                            //apply a series of erosions and dilations to the mask
                            //using an elliptical kernel
                            using (Mat kernel = CvInvoke.GetStructuringElement(ElementShape.Ellipse, new Size(11, 11), Point.Empty))
                            {
                                CvInvoke.Erode(skinMask, skinMask, kernel, new Point(-1, -1), 2, BorderType.Constant,
                                    CvInvoke.MorphologyDefaultBorderValue);
                                CvInvoke.Dilate(skinMask, skinMask, kernel, new Point(-1, -1), 2, BorderType.Constant,
                                    CvInvoke.MorphologyDefaultBorderValue);
                            }
                            //blur the mask to help remove noise, then apply the
                            //mask to the frame
                            CvInvoke.GaussianBlur(skinMask, skinMask, new Size(3, 3), 0);

                            Image<Bgr, byte> skin = resizedFrame.And(resizedFrame, skinMask);

                            //show the skin in the image along with the mask
                            CvInvoke.Imshow("images", resizedFrame);
                            CvInvoke.Imshow("mask", skin);

                            //if the 'q' key is pressed, stop the loop
                            if ((CvInvoke.WaitKey(1) & 0xff) == 'q')
                            {
                                break;
                            }
                        }
                    }
                }
            }

            CvInvoke.DestroyAllWindows();
        }
    }

    public class Options
    {
        [Option('v', "video", HelpText = "path to the (optional) video file", DefaultValue = "<Embeded|skin_example.mov>")]
        public string Video { get; set; }
    }
}
