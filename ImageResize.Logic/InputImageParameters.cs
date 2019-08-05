using SkiaSharp;
using System.IO;

namespace ImageResize.Logic
{
    public struct InputImageParameters
    {
        public string OutputContentType { get; set; }
        public SKEncodedImageFormat Format { get; private set; }
        public double MinimumDifference { get; private set; }
        public string UploadUrl { get; set; }
        public Stream InputStream { get; set; }
        public InputImageParameters(string contentType, SKEncodedImageFormat format, double minimumDifference, string uploadUrl)
        {
            OutputContentType = contentType;
            Format = format;
            MinimumDifference = minimumDifference;
            UploadUrl = uploadUrl;

            InputStream = null;
        }
    }
}
