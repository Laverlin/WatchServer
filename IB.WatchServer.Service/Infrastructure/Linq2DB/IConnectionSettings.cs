using LinqToDB.DataProvider;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace IB.WatchServer.Service.Infrastructure.Linq2DB
{
    /// <summary>
    /// The contract for Connection providers
    /// </summary>
    public interface IConnectionSettings
    {
        /// <summary>
        /// The instance of <see cref="IDataProvider"/>
        /// </summary>
        /// <returns>The instance of <see cref="IDataProvider"/></returns>
        public IDataProvider GetDataProvider();

        /// <summary>
        /// Build connection string by combining all public properties in string in format name=value;...
        /// If property name needs to be diferent it is possible to use DisplayNameAttribute
        /// </summary>
        /// <returns>Connection String</returns>
        public string BuildConnectionString()
        {
            var connectionString = new StringBuilder();
            foreach (PropertyInfo propertyInfo in GetType().GetProperties())
            {
                var value = propertyInfo.GetValue(this);
                if (value != null)
                {
                    var name = propertyInfo.GetCustomAttribute<DisplayNameAttribute>() != null
                        ? propertyInfo.GetCustomAttribute<DisplayNameAttribute>().DisplayName
                        : propertyInfo.Name;
                    connectionString.Append($"{name}={value.ToString()};");
                }
            }

            return connectionString.ToString();
        }
    }
}
