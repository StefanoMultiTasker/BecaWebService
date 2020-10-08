using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BecaWebService.ExtensionsLib
{
    public static class StringExtension
    {
        public static string ToCamelCase(this string str)
        {
            str = str.Substring(0, 1).ToUpper() + str.Substring(1);
            return Regex.Replace(str, @"([A-Z])([A-Z]+|[a-z0-9]+)($|[A-Z]\w*)",
                        m =>
                        {
                            return m.Groups[1].Value.ToLower() + m.Groups[2].Value.ToLower() + m.Groups[3].Value;
                        });
        }
    }
}
