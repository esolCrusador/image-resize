using ImageResize.Contract;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ImageResize.Logic
{
    public class ImageUploadService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly TemplateParametersService _templateParametersService;

        public ImageUploadService(TemplateParametersService templateParametersService)
        {
            _httpClient = new HttpClient();
            _templateParametersService = templateParametersService;
        }

        public async Task UploadImage(string templateUrl, OutputImageParameters outputImageParameters)
        {
            string url = _templateParametersService.ReplaceParameters(
                templateUrl,
                ImageUrlTemplateFactory.GetUrlParameters(outputImageParameters.Width.ToString(), outputImageParameters.Height.ToString(), outputImageParameters.Quality.ToString()),
                ImageUrlTemplateFactory.FormatParameter
            );

            using (HttpResponseMessage response = await _httpClient.PutAsync(url, new StreamContent(outputImageParameters.OutputStream)))
            {
                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"Image upload PUT {url} failed with response: \r\n{await response.Content.ReadAsStringAsync()}");
            }
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
