using System;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace IB.WatchServer.Abstract.Settings
{
    /// <summary>
    /// Helper class to get data from configuration object
    /// </summary>
    public static class SettingsExtensions
    {
        /// <summary>
        /// Cache connection string builder result
        /// </summary>
        private static Lazy<string> _connectionString;

        /// <summary>
        /// Build connection string by combining all public properties in string in format "name=value;..."
        /// If property name needs to be different you need to use the DisplayName Attribute
        /// </summary>
        /// <returns>Connection String</returns>
        public static string BuildConnectionString(this IConnectionSettings connectionSettings)
        {
            _connectionString = new Lazy<string>(() =>
            {
                var connectionString = new StringBuilder();
                foreach (var propertyInfo in connectionSettings.GetType().GetProperties())
                {
                    var value = propertyInfo.GetValue(connectionSettings);
                    if (value != null)
                    {
                        var name = propertyInfo.GetCustomAttribute<DisplayNameAttribute>() != null
                            ? propertyInfo.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName
                            : propertyInfo.Name;
                        connectionString.Append($"{name}={value};");
                    }
                }

                return connectionString.ToString();
            });

            return _connectionString.Value;
        }
    }
}
