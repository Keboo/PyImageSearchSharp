using System;
using OpenCvSharp;

namespace PyImageSearchSharp
{
    public static class ImageUtil
    {
        public static Mat Resize(Mat mat, double? width = null, double? height = null)
        {
            if (width == null && height == null)
                throw new ArgumentException($"Must specify a {nameof(width)} or a {nameof(height)}");
            if (width != null && height != null)
            {
                return mat.Resize(new Size(width.Value, height.Value));
            }
            if (width != null)
            {
                double ratio = mat.Width/width.Value;
                return mat.Resize(new Size(width.Value, mat.Height/ratio));
            }
            else
            {
                double ratio = mat.Height / height.Value;
                return mat.Resize(new Size(mat.Width / ratio, height.Value));
            }
        }
    }
}