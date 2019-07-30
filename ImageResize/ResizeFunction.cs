using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ImageResize
{
    public static class ResizeFunction
    {
        [FunctionName("resize")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "{*query}")] HttpRequestMessage request,
            string query,
            ILogger log)
        {
            if (query == "ping")
                return request.CreateResponse(HttpStatusCode.BadRequest, new { Content = "OK" });

            if (query != "resize")
                return request.CreateResponse(HttpStatusCode.BadRequest, new { Query = new { Route = "Not Supported" } });

            InputImageParameters inputParameters;
            try
            {
                inputParameters = ParseImageInputParameters(request);
            }
            catch (ArgumentException ex)
            {
                object validationData = ex.Data["ValidationData"];
                if (validationData != null)
                    return request.CreateResponse(HttpStatusCode.BadRequest, validationData);

                throw;
            }

            OutputImageParameters result;
            using (MemoryStream imageStreamCopy = new MemoryStream())
            {
                Stream imageStream = await request.Content.ReadAsStreamAsync();
                await imageStream.CopyToAsync(imageStreamCopy);
                if (imageStreamCopy.Length == 0)
                    return request.CreateResponse(HttpStatusCode.BadRequest, new { Body = new { Image = "Required" } });

                imageStreamCopy.Position = 0;
                inputParameters.InputStream = imageStreamCopy;

                result = ResizeImage(inputParameters);
            }
            if (result == null)
                return request.CreateResponse(HttpStatusCode.BadRequest, new { Body = new { Image = "Something wrong with incoming image" } });

            HttpResponseMessage response = request.CreateResponse();
            response.Headers.Add("X-Original-Width", result.OriginalWidth.ToString());
            response.Headers.Add("X-Original-Height", result.OriginalHeight.ToString());
            response.Headers.Add("X-Original-Size", result.OriginalSize.ToString());

            if (result.Resized)
            {
                response.Headers.Add("X-Result-Width", result.ResultWidth.ToString());
                response.Headers.Add("X-Result-Height", result.ResultHeight.ToString());
                response.Headers.Add("X-Result-Size", result.ResultSize.ToString());

                response.Content = new StreamContent(result.OutputStream)
                {
                    Headers = { ContentType = new MediaTypeHeaderValue(inputParameters.ContentType) }
                };
                response.StatusCode = HttpStatusCode.OK;
            }
            else
            {
                response.StatusCode = HttpStatusCode.NoContent;
            }


            return response;
        }

        private static InputImageParameters ParseImageInputParameters(HttpRequestMessage request)
        {
            NameValueCollection queryString = request.RequestUri.ParseQueryString();
            string widthParam = queryString.Get("width");
            string heightParam = queryString.Get("height");
            string qualityParam = queryString.Get("quality");
            string minimumDifferenceParam = queryString.Get("diff");

            if (widthParam == null && heightParam == null) // TODO: Remove after migration. backward compatibility
                widthParam = heightParam = queryString.Get("size");

            if (widthParam == null && heightParam == null)
                throw new ArgumentException { Data = { { "ValidationData", new { Query = new { Width = "Required", Height = "Required" } } } } };

            if (qualityParam == null)
                throw new ArgumentException { Data = { { "ValidationData", new { Query = new { Quality = "Required: 0 - 100" } } } } };

            int? width = widthParam == null ? (int?)null : int.Parse(widthParam);
            int? height = heightParam == null ? (int?)null : int.Parse(heightParam);
            int quality = int.Parse(qualityParam);

            double minimumDifference;
            if (minimumDifferenceParam == null || !double.TryParse(minimumDifferenceParam, out minimumDifference))
                minimumDifference = 20; /* Below this percentage of difference no sense to make new image. */

            minimumDifference /= 100;

            string contentType = request.Content.Headers.ContentType.MediaType;

            SKEncodedImageFormat format;
            try
            {
                format = GetImageFormat(contentType);
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new ArgumentException { Data = { { "ValidationData", new { Headers = new { ContentType = $"Content type \"{contentType}\" is not supported." } } } } };
            }

            return new InputImageParameters(contentType, format, width, height, quality, minimumDifference);
        }

        private static OutputImageParameters ResizeImage(InputImageParameters inputImage)
        {
            int originalSize = (int)inputImage.InputStream.Length;
            using (SKBitmap original = SKBitmap.Decode(inputImage.InputStream))
            {
                if (original == null)
                    return null;

                int width;
                int height;

                int widthDifference = original.Width - (inputImage.TargetWidth ?? 0);
                int heightDifference = original.Height - (inputImage.TargetHeight ?? 0);
                double difference = Math.Min((double)widthDifference / original.Width, (double)heightDifference / original.Height);
                if (difference < inputImage.MinimumDifference)
                    return new OutputImageParameters(original.Width, original.Height, originalSize);

                if (widthDifference < heightDifference)
                {
                    width = inputImage.TargetWidth.Value;
                    height = original.Height * width / original.Width;
                }
                else
                {
                    height = inputImage.TargetHeight.Value;
                    width = original.Width * height / original.Height;
                }

                using (SKBitmap resized = original.Resize(new SKImageInfo(width, height), SKFilterQuality.High))
                {
                    if (resized == null)
                        return null;

                    using (SKImage image = SKImage.FromBitmap(resized))
                    {
                        MemoryStream output = new MemoryStream();
                        image.Encode(inputImage.Format, inputImage.Quality).SaveTo(output);
                        output.Position = 0;

                        return new OutputImageParameters(original.Width, original.Height, originalSize, output, image.Width, image.Height, (int)output.Length);
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
