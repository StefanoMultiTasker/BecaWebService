using System.Reflection;

namespace ExtensionsLib
{
    public static partial class PropertiesExtension
    {
        public static bool HasPropertyValue<T>(this T @this, string propertyNames) where T : class, new()
        {
            Type type = @this.GetType();
            PropertyInfo[] props = type.GetProperties();

            return propertyNames.Split(new[] { ',', '+' }, StringSplitOptions.RemoveEmptyEntries)
                .All(propertyName => props.Any(p => p.Name.ToLower().Equals(propertyName.ToLower().Trim(), StringComparison.OrdinalIgnoreCase))
            );
            //return props.FirstOrDefault(p => p.Name.ToLower() == propertyName.ToLower()) == null ? false : true;
        }

        public static object GetPropertyValue<T>(this T @this, string propertyName) where T : class, new()
        {
            Type type = @this.GetType();
            PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

            return property.GetValue(@this, null);
        }

        public static string GetPropertyString<T>(this T @this, string propertyName) where T : class, new()
        {
            Type type = @this.GetType();
            PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

            return property.GetValue(@this, null) == null ? "" : property.GetValue(@this, null).ToString();
        }

        public static string GetPropertyStringByPos<T>(this T @this, Int16 propertyPosition) where T : class, new()
        {
            Type type = @this.GetType();
            PropertyInfo[] p = type.GetProperties();
            PropertyInfo property = type.GetProperty(p[propertyPosition].Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

            return property.GetValue(@this, null) == null ? "" : property.GetValue(@this, null).ToString();
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

        public static List<object> GetPropertyValueArray<T>(this T @this, string propertyName) where T : class, new()
        {
            Type type = @this.GetType();
            PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

            return (List<object>)property.GetValue(@this, null);
        }

        public static object SetPropertyValuearray<T>(this T @this, string propertyName, List<object> value) where T : class, new()
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
