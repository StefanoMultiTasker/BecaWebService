using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Serialization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BecaWebService.ExtensionsLib
{
    public static class ObjectExtension
    {
        public static T CloneObject<T>(this object source)
        {
            T result = Activator.CreateInstance<T>();
            //// **** made things  
            return result;
        }

        public static T deepCopy<T>(this T object2Copy)
        {
            using (var stream = new MemoryStream())
            {
                var serializer = new XmlSerializer(typeof(T));

                serializer.Serialize(stream, object2Copy);
                stream.Position = 0;
                return (T)serializer.Deserialize(stream);
            }
        }

        public static T getObjectFromJSON<T>(this object source, string jsonRecord)
        {
            var obj = JsonConvert.DeserializeObject<T>(jsonRecord);
            return obj;
        }

        public static T ToAnonymousType<T>(this JObject source, T destinationType)
        {
            return source.ToObject<T>();
        }

        public static DateTime? ToDateTimeFromJson(this object source)
        {
            string format = "ddd, dd MMM yyyy hh:mm:ss GMT";
            string dateString = source.ToString();
            DateTime dateTime;
            if (DateTime.TryParseExact(dateString, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime))
            {
                return dateTime;
            }
            else
            {
                return null;
            }
        }
    }
}
