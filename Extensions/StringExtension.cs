using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BecaWebService.Extensions
{
    public static class StringExtension
    {
        public static string ToCamelCase(this string str)
        {
            return Regex.Replace(str, @"([A-Z])([A-Z]+|[a-z0-9_]+)($|[A-Z]\w*)",
                        m =>
                        {
                            return m.Groups[1].Value.ToLower() + m.Groups[2].Value.ToLower() + m.Groups[3].Value;
                        });
        }
    }
}
