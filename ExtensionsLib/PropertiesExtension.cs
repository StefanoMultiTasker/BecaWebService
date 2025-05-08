using System.Reflection;

namespace ExtensionsLib
{
    public static partial class PropertiesExtension
    {
        public static bool HasPropertyValue<T>(this T @this, string propertyNames) where T : class, new()
        {
            if (@this == null || string.IsNullOrWhiteSpace(propertyNames))
                return false;

            // Caso 1: ExpandoObject (o qualsiasi IDictionary<string, object>)
            if (@this is IDictionary<string, object> dict)
            {
                bool foundD = propertyNames.Split(new[] { ',', '+' }, StringSplitOptions.RemoveEmptyEntries)
                    .All(propertyName => dict.Keys.Any(p => p.ToLower().Equals(propertyName.ToLower().Trim(), StringComparison.OrdinalIgnoreCase)));
                return foundD;
            }

            Type type = @this.GetType();
            PropertyInfo[] props = type.GetProperties();

            bool found = propertyNames.Split(new[] { ',', '+' }, StringSplitOptions.RemoveEmptyEntries)
                .All(propertyName => props.Any(p => p.Name.ToLower().Equals(propertyName.ToLower().Trim(), StringComparison.OrdinalIgnoreCase))
            );
            return found;
            //return props.FirstOrDefault(p => p.Name.ToLower() == propertyName.ToLower()) == null ? false : true;
        }

        public static object GetPropertyValue<T>(this T @this, string propertyName) where T : class
        {
            if (@this == null || string.IsNullOrWhiteSpace(propertyName))
                return null;

            // Caso 1: ExpandoObject (o qualsiasi IDictionary<string, object>)
            if (@this is IDictionary<string, object> dict)
            {
                var match = dict.Keys.FirstOrDefault(k => string.Equals(k, propertyName, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                    return dict[match];
                return null;
            }

            // Caso 2: Oggetto normale con riflessione
            Type type = @this.GetType();
            var prop = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                           .FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase));
            return prop?.GetValue(@this);
        }

        public static string GetPropertyString<T>(this T @this, string propertyName) where T : class, new()
        {
            if (@this == null || string.IsNullOrWhiteSpace(propertyName))
                return null;

            // Caso 1: ExpandoObject (o qualsiasi IDictionary<string, object>)
            if (@this is IDictionary<string, object> dict)
            {
                var match = dict.Keys.FirstOrDefault(k => string.Equals(k, propertyName, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                    return dict[match] == null ? "" : dict[match].ToString();
                return null;
            }

            // Caso 2: Oggetto normale con riflessione
            Type type = @this.GetType();
            var prop = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                           .FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase));
            if (prop == null) return null;
            return prop.GetValue(@this) == null ? "" : prop.GetValue(@this).ToString() ?? "";
        }

        public static string GetPropertyStringByPos<T>(this T @this, Int16 propertyPosition) where T : class, new()
        {
            if (@this == null || propertyPosition < 0)
                return null;

            // Caso 1: ExpandoObject (o qualsiasi IDictionary<string, object>)
            if (@this is IDictionary<string, object> dict)
            {
                if (dict.Count < propertyPosition) return null;
                var match = dict.ElementAt(propertyPosition).Value;
                if (match != null)
                    return match.ToString() ?? "";
                return null;
            }

            Type type = @this.GetType();
            PropertyInfo[] p = type.GetProperties();
            PropertyInfo property = type.GetProperty(p[propertyPosition].Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

            return property.GetValue(@this, null) == null ? "" : property.GetValue(@this, null).ToString();
        }

        public static object SetPropertyValue<T>(this T @this, string propertyName, object value) where T : class, new()
        {
            if (@this == null || string.IsNullOrWhiteSpace(propertyName))
                return false;

            // Caso 1: ExpandoObject (o qualsiasi IDictionary<string, object>)
            if (@this is IDictionary<string, object> dict)
            {
                var match = dict.Keys.FirstOrDefault(k => string.Equals(k, propertyName, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                    dict[propertyName] = value;
                return true;
            }

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
            if (@this == null || string.IsNullOrWhiteSpace(propertyName))
                return null;

            // Caso 1: ExpandoObject (o qualsiasi IDictionary<string, object>)
            if (@this is IDictionary<string, object> dict)
            {
                var match = dict.Keys.FirstOrDefault(k => string.Equals(k, propertyName, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                    return (List<object>)dict[match];
                return null;
            }

            Type type = @this.GetType();
            PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

            return (List<object>)property.GetValue(@this, null);
        }

        public static object SetPropertyValuearray<T>(this T @this, string propertyName, List<object> value) where T : class, new()
        {
            if (@this == null || string.IsNullOrWhiteSpace(propertyName))
                return false;

            // Caso 1: ExpandoObject (o qualsiasi IDictionary<string, object>)
            if (@this is IDictionary<string, object> dict)
            {
                var match = dict.Keys.FirstOrDefault(k => string.Equals(k, propertyName, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                    dict[propertyName] = value;
                return true;
            }

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
