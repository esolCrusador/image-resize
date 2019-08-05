using System;
using System.Collections.Generic;

namespace ImageResize.Contract
{
    public static class ImageUrlTemplateFactory
    {
        public static string GetUrlTemplate(GetUrlTemplate getUrlTemplate) => getUrlTemplate(FormatParameter("width"), FormatParameter("height"), FormatParameter("quality"));
        public static Func<string, string> FormatParameter = parameterName => $":{parameterName}";
        public static GetUrlParameters GetUrlParameters = (width, height, quality) => new Dictionary<string, string>
        {
            { "width", width },
            { "height", height },
            {"quality", quality }
        };
    }
}
