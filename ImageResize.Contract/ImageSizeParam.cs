using System;
using System.Text.RegularExpressions;

namespace ImageResize.Contract
{
    public struct ImageSizeParam
    {
        private static readonly Regex ParseRegex = new Regex(@"^(?<width>\d*)x(?<height>\d*)?q(?<quality>\d+)$", RegexOptions.Compiled);
        public int? Width { get; set; }
        public int? Height { get; set; }
        public int Quality { get; set; }

        public ImageSizeParam(int? width, int? height, int quality)
        {
            Width = width;
            Height = height;
            Quality = quality;
        }

        public override string ToString()
        {
            return $"{Width}x{Height}q{Quality}";
        }

        public static bool TryParse(string str, out ImageSizeParam size)
        {
            size = Default;
            Match parsed = ParseRegex.Match(str);

            if (!parsed.Success)
                return false;

            string widthParam = parsed.Groups["width"].Value;
            string heightParam = parsed.Groups["height"].Value;
            string qualityParam = parsed.Groups["quality"].Value;

            size = new ImageSizeParam(
                string.IsNullOrEmpty(widthParam) ? null : (int?)int.Parse(widthParam),
                string.IsNullOrEmpty(heightParam) ? null : (int?)int.Parse(heightParam),
                int.Parse(qualityParam)
            );

            return true;
        }

        public static ImageSizeParam Parse(string str)
        {
            if (!TryParse(str, out ImageSizeParam size))
                throw new ArgumentException($"Could not parse string \"${str}\"");

            return size;
        }

        public static readonly ImageSizeParam Default = new ImageSizeParam(null, null, 0);
    }
}
