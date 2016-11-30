using System;
using Emgu.CV;
using Emgu.CV.CvEnum;

namespace PyImageSearchSharp
{
    public static class ImageUtil
    {
        public static Image<TColor, TDepth> Resize<TColor, TDepth>(Image<TColor, TDepth> mat, double? width = null, double? height = null)
            where TColor : struct, IColor where TDepth : new()
        {
            if (width == null && height == null)
                throw new ArgumentException($"Must specify a {nameof(width)} or a {nameof(height)}");
            if (width != null && height != null)
            {
                mat.Resize((int)width.Value, (int)height.Value, Inter.Linear);
            }
            if (width != null)
            {
                double ratio = mat.Width / width.Value;
                return mat.Resize((int)width.Value, (int)(mat.Height / ratio), Inter.Linear);
            }
            else
            {
                double ratio = mat.Height / height.Value;
                return mat.Resize((int)(mat.Width / ratio), (int)height.Value, Inter.Linear);
            }
        }

    }
}