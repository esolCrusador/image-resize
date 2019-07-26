using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using SkiaSharp;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ImageResize
{
    public static class ResizeFunction
    {
        [FunctionName("resize")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            StringValues sizeParam = req.Query["size"];
            StringValues qualityParam = req.Query["quality"];
            if (sizeParam.Count == 0)
                return new BadRequestObjectResult(new
                {
                    Query = new
                    {
                        Size = "Required"
                    }
                });
            if (qualityParam.Count == 0)
                return new BadRequestObjectResult(new
                {
                    Query = new
                    {
                        Quality = "Required: 0 - 100"
                    }
                });

            int size = int.Parse(sizeParam.First());
            int quality = int.Parse(qualityParam.First());

            SKEncodedImageFormat format;
            try
            {
                format = GetImageFormat(req.ContentType);
            }
            catch (ArgumentOutOfRangeException)
            {
                return new BadRequestObjectResult(new
                {
                    Headers = new
                    {
                        ContentType = $"Content type \"{req.ContentType}\" is not supported."
                    }
                });
            }

            Stream result = await ResizeImage(req.Body, format, size, quality);
            if (result == null)
                return new BadRequestObjectResult(new
                {
                    Body = new
                    {
                        Image = "Something wrong with incoming image"
                    }
                });

            return new FileStreamResult(result, req.ContentType);
        }

        private static async Task<Stream> ResizeImage(Stream imageStream, SKEncodedImageFormat format, int size, int quality)
        {
            using (MemoryStream imageStreamCopy = new MemoryStream())
            {
                await imageStream.CopyToAsync(imageStreamCopy);
                imageStreamCopy.Position = 0;

                using (SKBitmap original = SKBitmap.Decode(imageStreamCopy))
                {
                    if (original == null)
                        return null;

                    int width;
                    int height;

                    if (original.Width > original.Height)
                    {
                        width = size;
                        height = original.Height * size / original.Width;
                    }
                    else
                    {
                        width = original.Width * size / original.Height;
                        height = size;
                    }

                    using (SKBitmap resized = original.Resize(new SKImageInfo(width, height), SKFilterQuality.High))
                    {
                        if (resized == null)
                            return null;

                        using (SKImage image = SKImage.FromBitmap(resized))
                        {
                            MemoryStream output = new MemoryStream();
                            image.Encode(format, quality).SaveTo(output);
                            output.Position = 0;

                            return output;
                        }
                    }
                }
            }
        }

        private static SKEncodedImageFormat GetImageFormat(string contentType)
        {
            if (contentType == "image/jpeg")
                return SKEncodedImageFormat.Jpeg;

            throw new ArgumentOutOfRangeException(nameof(contentType));
        }
    }
}
