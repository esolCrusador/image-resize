using ImageResize.Contract;
using Xunit;

namespace ImageResize.Tests
{
    public class ImageSizeParamTests
    {
        [Theory]
        [InlineData("xq1", null, null, 1)]
        [InlineData("100xq100", 100, null, 100)]
        [InlineData("100x300q100", 100, 300, 100)]
        public void ShouldParseParameters(string input, int? width, int? height, int quality)
        {
            ImageSizeParam sizes = ImageSizeParam.Parse(input);

            Assert.Equal(sizes.Width, width);
            Assert.Equal(sizes.Height, height);
            Assert.Equal(sizes.Quality, quality);
        }

        [Theory]
        [InlineData("cxq1")]
        [InlineData(" xq100")]
        [InlineData("100.1x300q1e00")]
        public void ShouldFailForWrongParameters(string input)
        {
            bool parsed = ImageSizeParam.TryParse(input, out ImageSizeParam size);

            Assert.False(parsed);
        }

        [Theory]
        [InlineData(null, null, 1, "xq1")]
        [InlineData(100, null, 10, "100xq10")]
        [InlineData(null, 200, 10, "x200q10")]
        [InlineData(100, 200, 100, "100x200q100")]
        public void ShouldConvertToCorrectString(int? width, int? height, int quality, string output)
        {
            ImageSizeParam size = new ImageSizeParam(width, height, quality);

            Assert.Equal(size.ToString(), output);
        }
    }
}
