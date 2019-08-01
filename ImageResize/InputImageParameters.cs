using SkiaSharp;
using System.IO;

namespace ImageResize
{
    public struct InputImageParameters
    {
        public InputImageParameters(string contentType, SKEncodedImageFormat format, int? targetWith, int? targetHeight, int quality, double minimumDifference)
        {
            OutputContentType = contentType;
            Format = format;
            TargetWidth = targetWith;
            TargetHeight = targetHeight;
            Quality = quality;
            MinimumDifference = minimumDifference;

            InputStream = null;
        }

        public string OutputContentType { get; set; }
        public SKEncodedImageFormat Format { get; private set; }
        public int? TargetWidth { get; private set; }
        public int? TargetHeight { get; private set; }
        public double MinimumDifference { get; private set; }
        public int Quality { get; private set; }
        public Stream InputStream { get; set; }
    }
}
