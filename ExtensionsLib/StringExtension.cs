using System.Globalization;
using System.Text.RegularExpressions;

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

        //public static string ToLower(this string str) => (str ?? "").ToLower();

        public static string coalesce(this string str) => str ?? "";

        public static bool isNullOrempty(this string str) => str == null || str == "";

        public static string left(this string str, int chars) =>
            String.Concat(str.ToCharArray().Take(chars));

        public static string right(this string str, int chars) =>
            String.Concat(str.ToCharArray().TakeLast(chars));

        public static string inside(this string str, string from, string to) =>
            str.Substring(str.IndexOf(from) + 1, str.IndexOf(to) - str.IndexOf(from) - 1);

        public static bool IsValidDateTimeJson(this string dateString)
        {
            string format = "ddd, dd MMM yyyy HH:mm:ss GMT";
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

        public static DateTime? ToDateTimeFromJsonText(this string source)
        {
            string format = "ddd, dd MMM yyyy hh:mm:ss GMT";
            string dateString = source.ToString();
            DateTime dateTime;
            if (DateTime.TryParseExact(dateString, format, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out dateTime))
            {
                return dateTime;
            }
            else
            {
                return null;
            }
        }

        public static bool isDate(this string date)
        {
            try
            {
                DateTime dt = DateTime.Parse(date);
                return true;
            }
            catch
            {
                return false;
            }

        }
    }
}
