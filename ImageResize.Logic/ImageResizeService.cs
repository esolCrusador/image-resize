using ImageResize.Contract;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ImageResize.Logic
{
    public class ImageResizeService
    {
        private static SemaphoreSlim _resizeSemaphore = new SemaphoreSlim(3);

        public async Task<DisponseDelegate> CaptureAsync()
        {
            await _resizeSemaphore.WaitAsync();

            return new DisponseDelegate(() => _resizeSemaphore.Release());
        }

        public OutputImageParameters Resize(InputImageParameters inputImage, ImageSizeParam targetSize)
        {
            using (SKBitmap original = SKBitmap.Decode(inputImage.InputStream))
            {
                if (original == null)
                    throw new ArgumentException { Data = { { "ValidationData", new { Body = new { Image = "Something wrong with incoming image" } } } } };

                return Resize(original, inputImage, targetSize);
            }
        }

        public IEnumerable<OutputImageParameters> ResizeMultiple(InputImageParameters inputImage, IEnumerable<ImageSizeParam> targetSizes)
        {
            using (SKBitmap original = SKBitmap.Decode(inputImage.InputStream))
            {
                if (original == null)
                    throw new ArgumentException { Data = { { "ValidationData", new { Body = new { Image = "Something wrong with incoming image" } } } } };

                IOrderedEnumerable<OutputImageParameters> outputImages = targetSizes
                    .Select(targetSize => CalculateSize(original.Width, original.Height, targetSize, inputImage.MinimumDifference))
                    .OrderByDescending(s => s.Width);

                foreach (OutputImageParameters outputImage in outputImages)
                {
                    if (!outputImage.Resized)
                        continue;

                    ApplyResize(original, inputImage, outputImage);

                    yield return outputImage;
                }
            }
        }

        private OutputImageParameters Resize(SKBitmap original, InputImageParameters inputImage, ImageSizeParam targetSize)
        {
            OutputImageParameters outputImage = CalculateSize(original.Width, original.Height, targetSize, inputImage.MinimumDifference);

            ApplyResize(original, inputImage, outputImage);

            return outputImage;
        }

        private void ApplyResize(SKBitmap original, InputImageParameters inputImage, OutputImageParameters outputImage)
        {
            if (!outputImage.Resized)
                return;

            using (SKBitmap resized = original.Resize(new SKImageInfo(outputImage.Width, outputImage.Height), SKFilterQuality.High))
            {
                if (resized == null)
                    throw new ArgumentException { Data = { { "ValidationData", new { Body = new { Image = "Something wrong with incoming image" } } } } };

                using (SKImage image = SKImage.FromBitmap(resized))
                {
                    MemoryStream output = new MemoryStream();
                    image.Encode(inputImage.Format, outputImage.Quality).SaveTo(output);

                    outputImage.OutputStream = output;
                }
            }
        }

        private OutputImageParameters CalculateSize(int originalWidth, int originalHeight, ImageSizeParam targetSize, double minimumDifference)
        {
            int width;
            int height;
            if (targetSize.Width.HasValue || targetSize.Height.HasValue)
            {
                int widthDifference = originalWidth - (targetSize.Width ?? (int.MinValue / 2));
                int heightDifference = originalHeight - (targetSize.Height ?? (int.MinValue / 2));
                double difference = Math.Min((double)widthDifference / originalWidth, (double)heightDifference / originalHeight);
                if (difference < minimumDifference)
                    return OutputImageParameters.NotResized;

                if (widthDifference < heightDifference)
                {
                    width = targetSize.Width.Value;
                    height = originalHeight * width / originalWidth;
                }
                else
                {
                    height = targetSize.Height.Value;
                    width = originalWidth * height / originalHeight;
                }
            }
            else
            {
                width = originalWidth;
                height = originalHeight;
            }

            return new OutputImageParameters(width, height, targetSize.Quality);
        }
    }
}
