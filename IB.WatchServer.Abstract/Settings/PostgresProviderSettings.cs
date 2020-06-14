using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.PostgreSQL;

namespace IB.WatchServer.Abstract.Settings
{
    /// <summary>
    /// Implementation of <see cref="IConnectionSettings"/> for Postgres 
    /// </summary>
    public class PostgresProviderSettings : IConnectionSettings
    {
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
        [Required]
        public string Server { get; set; }

        /// <summary>
        /// Server port, default 5432 
        /// </summary>
        public string Port { get; set; } = "5432";

        /// <summary>
        /// Database name
        /// </summary>
        [Required]
        public string Database { get; set; }

        /// <summary>
        /// Authorized user id
        /// </summary>
        [Required]
        [DisplayName("User Id")]
        public string UserId { get; set; }

        /// <summary>
        /// User password
        /// </summary>
        [Required]
        public string Password { get; set; }

        /// <summary>
        /// Is pooling enabled
        /// </summary>
        public bool? Pooling { get; set; } = true;

        /// <summary>
        /// pool size minimum
        /// </summary>
        public int MinPoolSize { get; set; } = 10;

        /// <summary>
        /// Pool size maximum
        /// </summary>
        public int MaxPoolSize { get; set; } = 100;
    }
}
