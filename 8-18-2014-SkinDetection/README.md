# Mobile Document Scanner
The following solution is a C# implementation of Adrian Rosebrock [Skin Detection: A Step-by-Step Example using Python and OpenCV (8-18-2014)](http://www.pyimagesearch.com/2014/08/18/skin-detection-step-step-example-using-python-opencv/).

The original post was written to run on Python 2.7/Python 3.4+ and OpenCV 2.4.X/OpenCV 3.0+. These examples use OpenCV 3.x via EmguCV and OpenCVSharp.

# Points of interest
In both the OpenCVSharp and Emgu versions the OpenCV objects implement IDisposable and should be properly disposed. To facilitate this, and to keep the code resembling the original blog post, a Disposer class is used that simply maintains a list of objects to be disposed of at the end.