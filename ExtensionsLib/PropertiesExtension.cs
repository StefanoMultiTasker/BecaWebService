using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ExtensionsLib
{
    public  static partial class PropertiesExtension
    {
        public static bool HasPropertyValue<T>(this T @this, string propertyName) where T : class, new()
        {
            Type type = @this.GetType();
            PropertyInfo[] props = type.GetProperties();
            return props.FirstOrDefault(p => p.Name == propertyName) == null ? false : true;
        }

        public static object GetPropertyValue<T>(this T @this, string propertyName) where T : class, new()
        {
            Type type = @this.GetType();
            PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

            return property.GetValue(@this, null);
        }
        public static object SetPropertyValue<T>(this T @this, string propertyName, object value) where T : class, new()
        {
            Type type = @this.GetType();
            PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

            if (property != null)
            {
                Type t = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                object safeValue = (value == null) ? null : Convert.ChangeType(value, t);
                property.SetValue(@this, safeValue, null);
            }
            //property.SetValue(@this, Convert.ChangeType(value, property.PropertyType));
            return true;
        }
    }
}
