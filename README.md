# PyImageSearch in C#
A collection of C# projects translated from [Adrian Rosebrock excellent python examples](http://www.pyimagesearch.com/) using OpenCV.

# Implemented projects
- [Document Scanner](9-1-2014-DocumentScanner)
- [Skin Detection](8-18-2014-SkinDetection)

# Utility Code
In both the OpenCVSharp and Emgu versions the OpenCV objects implement IDisposable and should be properly disposed. To facilitate this, and to keep the code resembling the original blog post, a [Disposer class](Common/Disposer.cs) is used that simply maintains a list of objects to be disposed of at the end.


This code was done as a learning experience, and provided as a guide for otheres. The code should be seen as an example, and not production ready. I have tried to keep the code similar to the original blog posts, rather than clean refactoring to make it clean C# code.
Pull requests welcome.
