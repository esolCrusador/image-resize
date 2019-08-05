using System.Collections.Generic;

namespace ImageResize.Contract
{
    public delegate Dictionary<string, string> GetUrlParameters(string widthParameter, string heightParameter, string qualityParameter);
}
