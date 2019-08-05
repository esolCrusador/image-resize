using System;
using System.Collections.Generic;
using System.Linq;

namespace ImageResize.Logic
{
    public class TemplateParametersService
    {
        public string ReplaceParameters(string template, Dictionary<string, string> metadataParameters, Func<string, string> formatParameterKey, List<string> replaceKeys = null, Func<string, string> sanitizeParameter = null)
        {
            if (template == null)
                return template;

            string resultString = template;

            IOrderedEnumerable<string> parameterKeys = metadataParameters.Keys.OrderByDescending(key => key.Length);
            foreach (string key in parameterKeys)
            {
                string keyPlaceholder = formatParameterKey(key);
                if (replaceKeys == null || replaceKeys.LastIndexOf(key) != -1)
                {
                    int keyPlaceholderIndex = resultString.IndexOf(keyPlaceholder);
                    while (keyPlaceholderIndex != -1)
                    {
                        string paramValue = metadataParameters[key];

                        string paramString = paramValue ?? "";
                        if (paramString != null && sanitizeParameter != null)
                        {
                            paramString = sanitizeParameter(paramString);
                        }

                        resultString = resultString.Substring(0, keyPlaceholderIndex) + paramString + resultString.Substring(keyPlaceholderIndex + keyPlaceholder.Length);

                        keyPlaceholderIndex = resultString.IndexOf(keyPlaceholder);
                    }
                }
            }

            return resultString;
        }
    }
}
