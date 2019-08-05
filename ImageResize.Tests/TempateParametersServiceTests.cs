using ImageResize.Logic;
using Newtonsoft.Json;
using System.Collections.Generic;
using Xunit;

namespace ImageResize.Tests
{
    public class TempateParametersServiceTests
    {
        private readonly TemplateParametersService _templateParametersService = new TemplateParametersService();

        [Theory]
        [InlineData("/:size/:sizeName/:sizeValue", @"{ size: 3, sizeName: ""Huge"" }", "/3/Huge/3Value")]
        [InlineData("/:size/:size/:size1/:size2", @"{ size: 3, sizeName: ""Huge"", size2: 4 }", "/3/3/31/4")]
        public void ShouldProperlyReplaceParameters(string template, string paramsDictionary, string result)
        {
            Dictionary<string, string> data = JsonConvert.DeserializeObject<Dictionary<string, string>>(paramsDictionary);

            Assert.Equal(_templateParametersService.ReplaceParameters(template, data, key => $":{key}"), result);
        }
    }
}
