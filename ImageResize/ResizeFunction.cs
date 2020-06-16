using ImageResize.Contract;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using ImageResize.Logic;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace ImageResize
{
    public static class ResizeFunction
    {
        private static readonly ImageResizeService _imageResizeService = new ImageResizeService();
        private static readonly ImageUploadService _imageUploadService = new ImageUploadService(new TemplateParametersService());

        [FunctionName("resize")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "{*query}")] HttpRequestMessage request,
            string query,
            ILogger log)
        {
            if (query == "ping")
                return request.CreateResponse(HttpStatusCode.OK, "OK");

            if (query != "resize")
                return request.CreateResponse(HttpStatusCode.BadRequest, new { Query = new { Route = "Not Supported" } });

            InputImageParameters inputParameters;
            IReadOnlyCollection<ImageSizeParam> imageSizes;
            try
            {
                inputParameters = ParseImageInputParameters(request);
                imageSizes = ParseImageSizeParameters(request);
            }
            catch (ArgumentException ex)
            {
                object validationData = ex.Data["ValidationData"];
                if (validationData != null)
                    return request.CreateResponse(HttpStatusCode.BadRequest, validationData);

                throw;
            }

            try
            {
                if (inputParameters.UploadUrl == null)
                {
                    return await ResizeSingle(request, inputParameters, imageSizes, log);
                }
                else
                {
                    return await ResizeMultiple(request, inputParameters, imageSizes, log);
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message, ex);

                return request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        private static async Task<HttpResponseMessage> ResizeSingle(HttpRequestMessage request, InputImageParameters inputParameters, IReadOnlyCollection<ImageSizeParam> imageSizes, ILogger log)
        {
            OutputImageParameters result;

            using (await _imageResizeService.CaptureAsync())
            {
                try
                {
                    using (MemoryStream imageStreamCopy = new MemoryStream())
                    {
                        Stream imageStream = await request.Content.ReadAsStreamAsync();
                        await imageStream.CopyToAsync(imageStreamCopy);
                        if (imageStreamCopy.Length == 0)
                            return request.CreateResponse(HttpStatusCode.BadRequest, new { Body = new { Image = "Required" } });

                        imageStreamCopy.Position = 0;
                        inputParameters.InputStream = imageStreamCopy;

                        result = _imageResizeService.Resize(inputParameters, imageSizes.Single());
                    }
                    if (result == null)
                        return request.CreateResponse(HttpStatusCode.BadRequest, new { Body = new { Image = "Something wrong with incoming image" } });
                }
                catch (ArgumentException ex)
                {
                    object validationData = ex.Data["ValidationData"];
                    if (validationData != null)
                        return request.CreateResponse(HttpStatusCode.BadRequest, validationData);

                    throw;
                }
            }

            HttpResponseMessage response = request.CreateResponse();

            if (result.Resized)
            {
                response.Headers.Add("X-Width", result.Width.ToString());
                response.Headers.Add("X-Height", result.Height.ToString());
                response.Headers.Add("X-Size", result.Size.ToString());

                response.Content = new StreamContent(result.OutputStream)
                {
                    Headers = { ContentType = new MediaTypeHeaderValue(inputParameters.OutputContentType) }
                };
                response.StatusCode = HttpStatusCode.OK;
            }
            else
            {
                response.StatusCode = HttpStatusCode.NoContent;
            }

            return response;
        }

        private static async Task<HttpResponseMessage> ResizeMultiple(HttpRequestMessage request, InputImageParameters inputParameters, IReadOnlyCollection<ImageSizeParam> imageSizes, ILogger log)
        {
            List<ImageResizeResultModel> resizeResults = new List<ImageResizeResultModel>();
            List<Task> uploadTasks = new List<Task>();

            using (await _imageResizeService.CaptureAsync())
            {
                try
                {
                    using (MemoryStream imageStreamCopy = new MemoryStream())
                    {
                        Stream imageStream = await request.Content.ReadAsStreamAsync();
                        await imageStream.CopyToAsync(imageStreamCopy);
                        if (imageStreamCopy.Length == 0)
                            return request.CreateResponse(HttpStatusCode.BadRequest, new { Body = new { Image = "Required" } });

                        imageStreamCopy.Position = 0;
                        inputParameters.InputStream = imageStreamCopy;

                        foreach (OutputImageParameters image in _imageResizeService.ResizeMultiple(inputParameters, imageSizes))
                        {
                            resizeResults.Add(new ImageResizeResultModel(image.Width, image.Height, image.Size, image.Quality));
                            uploadTasks.Add(_imageUploadService.UploadImage(inputParameters.UploadUrl, inputParameters.OutputContentType, image));
                        }
                    }
                }
                catch (ArgumentException ex)
                {
                    object validationData = ex.Data["ValidationData"];
                    if (validationData != null)
                        return request.CreateResponse(HttpStatusCode.BadRequest, validationData);

                    throw;
                }
            }

            try
            {
                await Task.WhenAll(uploadTasks);
            }
            catch (HttpRequestException ex)
            {
                log.LogError(ex, "Upload requests failed");
                request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }

            HttpResponseMessage response = request.CreateResponse(
                HttpStatusCode.OK,
                resizeResults,
                JsonMediaTypeFormatter.DefaultMediaType
            );

            return response;
        }

        private static IReadOnlyCollection<ImageSizeParam> ParseImageSizeParameters(HttpRequestMessage request)
        {
            NameValueCollection queryString = request.RequestUri.ParseQueryString();

            string[] sizeParams = queryString.GetValues("size");
            if (sizeParams == null || sizeParams.Length == 0)
                throw new ArgumentException { Data = { { "ValidationData", new { Query = new { Size = "Required" } } } } };

            return sizeParams.Select(sizeParam =>
            {
                if (!ImageSizeParam.TryParse(sizeParam, out ImageSizeParam size))
                    throw new ArgumentException { Data = { { "ValidationData", new { Query = new { Size = "Incorrect size format. It should be \"${width}x${height}q${quality}\"" } } } } };

                return size;
            }).ToList();
        }

        private static InputImageParameters ParseImageInputParameters(HttpRequestMessage request)
        {
            NameValueCollection queryString = request.RequestUri.ParseQueryString();
            string minimumDifferenceParam = queryString.Get("diff");

            double minimumDifference;
            if (minimumDifferenceParam == null || !double.TryParse(minimumDifferenceParam, out minimumDifference))
                minimumDifference = 0.20; /* Below this percentage of difference no sense to make new image. */

            string outputContentType;
            if (request.Headers.TryGetValues("Accept", out IEnumerable<string> acceptHeaders))
                outputContentType = acceptHeaders.First();
            else
                throw new ArgumentException { Data = { { "ValidationData", new { Headers = new { Accept = $"Required" } } } } };

            string uploadUrl = queryString.Get("upload-url");

            SKEncodedImageFormat format;
            try
            {
                format = GetImageFormat(outputContentType);
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new ArgumentException { Data = { { "ValidationData", new { Headers = new { ContentType = $"Content type \"{outputContentType}\" is not supported" } } } } };
            }

            return new InputImageParameters(outputContentType, format, minimumDifference, uploadUrl);
        }

        private static SKEncodedImageFormat GetImageFormat(string contentType)
        {
            if (contentType == "image/jpeg")
                return SKEncodedImageFormat.Jpeg;
            if (contentType == "image/png")
                return SKEncodedImageFormat.Png;

            throw new ArgumentOutOfRangeException(nameof(contentType));
        }
    }
}
