using System.Text;
using System.Reflection;
using System.ComponentModel;

using LinqToDB.DataProvider;
using LinqToDB.DataProvider.PostgreSQL;

namespace IB.WatchServer.Service.Infrastructure.Linq2DB
{
    /// <summary>
    /// Implementation of <see cref="IConnectionSettings"/> for Postgres 
    /// </summary>
    public class PostgresProviderSettings : IConnectionSettings
    {
        /// <summary>
        /// Return connection string
        /// </summary>
        public string GetConnectionString()
        {
            var connectionString = new StringBuilder();
            foreach (PropertyInfo propertyInfo in GetType().GetProperties())
            {
                var name = propertyInfo.GetCustomAttribute<DisplayNameAttribute>() != null
                    ? propertyInfo.GetCustomAttribute<DisplayNameAttribute>().DisplayName
                    : propertyInfo.Name;
                connectionString.Append($"{name}={propertyInfo.GetValue(this).ToString()};");
            }

            return connectionString.ToString();
        }

        /// <summary>
        /// Return new instance of <see cref="PostgreSQLDataProvider"/>
        /// </summary>
        public IDataProvider GetDataProvider()
        {
            return new PostgreSQLDataProvider();
        }

        /// <summary>
        /// Server name
        /// </summary>
        public string Server { get; set; }

        /// <summary>
        /// Server port, default 5432 
        /// </summary>
        public string Port { get; set; }

        /// <summary>
        /// Database name
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        /// Authorized user id
        /// </summary>
        [DisplayName("User Id")]
        public string UserId { get; set; }

        /// <summary>
        /// User password
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Is pooling enabled
        /// </summary>
        public bool Pooling { get; set; }

        /// <summary>
        /// pool size minimum
        /// </summary>
        public int MinPoolSize { get; set; }

        /// <summary>
        /// Pool size maximum
        /// </summary>
        public int MaxPoolSize { get; set; }
    }
}
