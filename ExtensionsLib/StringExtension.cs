using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BecaWebService.ExtensionsLib
{
    public static class StringExtension
    {
        public static string ToLowerToCamelCase(this string str)
        {
            str = str.Substring(0, 1).ToUpper() + str.Substring(1);
            var words = str.Split(new[] { "_", " " }, StringSplitOptions.RemoveEmptyEntries);

            var leadWord = Regex.Replace(words[0], @"([A-Z])([A-Z]+|[a-z0-9]+)($|[A-Z]\w*)",
                m =>
                {
                    return m.Groups[1].Value.ToLower() + m.Groups[2].Value.ToLower() + m.Groups[3].Value;
                });

            var tailWords = words.Skip(1)
                .Select(word => char.ToUpper(word[0]) + word.Substring(1))
                .ToArray();

            return $"{leadWord}{string.Join(string.Empty, tailWords)}";
        }

        public static string ToCamelCase(this string str)
        {
            if (str == null) return null;

            str = str.Substring(0, 1).ToUpper() + str.Substring(1);
            return Regex.Replace(str, @"([A-Z])([A-Z]+|[a-z0-9]+)($|[A-Z]\w*)",
                        m =>
                        {
                            return m.Groups[1].Value.ToLower() + m.Groups[2].Value.ToLower() + m.Groups[3].Value;
                        });
        }

        public static bool IsValidDateTimeJson(this string dateString)
        {
            string format = "ddd, dd MMM yyyy hh:mm:ss GMT";
            DateTime dateTime;
            if (DateTime.TryParseExact(dateString, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
