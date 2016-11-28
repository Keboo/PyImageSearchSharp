# Mobile Document Scanner
The following solution is a C# implementation of Adrian Rosebrock [How to Build a Kick-Ass Mobile Document Scanner in Just 5 Minutes (9-1-2014)](http://www.pyimagesearch.com/2014/09/01/build-kick-ass-mobile-document-scanner-just-5-minutes/).

The original post was written to use Python 2.7 and OpenCV 2.4.X. These examples use OpenCV 3.x via EmguCV and OpenCVSharp.

# Points of interest
In both the OpenCVSharp and Emgu versions the OpenCV objects implement IDisposable and should be properly disposed. To facilitate this, and to keep the code resembling the original blog post, a Disposer class is used that simply maintains a list of objects to be disposed of at the end.